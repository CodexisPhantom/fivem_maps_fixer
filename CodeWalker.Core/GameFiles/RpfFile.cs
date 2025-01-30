using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CodeWalker.GameFiles
{

    public class RpfFile
    {
        public string Name { get; set; }
        public string NameLower { get; set; }
        public string Path { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string LastError { get; set; }
        public Exception LastException { get; set; }
        public RpfDirectoryEntry Root { get; set; }
        public bool IsAESEncrypted { get; set; }
        public long StartPos { get; set; }
        public uint Version { get; set; }
        public uint EntryCount { get; set; }
        public uint NamesLength { get; set; }
        public RpfEncryption Encryption { get; set; }
        public List<RpfEntry> AllEntries { get; set; }
        public List<RpfFile> Children { get; set; }
        public RpfFile Parent { get; set; }
        public RpfBinaryFileEntry ParentFileEntry { get; set; }

        public BinaryReader CurrentFileReader { get; set; }
        
        public uint TotalFileCount { get; set; }
        public uint TotalFolderCount { get; set; }
        public uint TotalResourceCount { get; set; }
        public uint TotalBinaryFileCount { get; set; }
        public uint GrandTotalRpfCount { get; set; }
        public uint GrandTotalFileCount { get; set; }
        public uint GrandTotalFolderCount { get; set; }
        public uint GrandTotalResourceCount { get; set; }
        public uint GrandTotalBinaryFileCount { get; set; }
        public long ExtractedByteCount { get; set; }
        
        public RpfFile(string fpath, string relpath)
        {
            FileInfo fi = new FileInfo(fpath);
            Name = fi.Name;
            NameLower = Name.ToLowerInvariant();
            Path = relpath.ToLowerInvariant();
            FilePath = fpath;
            FileSize = fi.Length;
        }

        private RpfFile(string name, string path, long filesize)
        {
            Name = name;
            NameLower = Name.ToLowerInvariant();
            Path = path.ToLowerInvariant();
            FilePath = path;
            FileSize = filesize;
        }

        private RpfFile GetTopParent()
        {
            RpfFile pfile = this;
            while (pfile.Parent != null)
            {
                pfile = pfile.Parent;
            }
            return pfile;
        }

        private string GetPhysicalFilePath()
        {
            return GetTopParent().FilePath;
        }
        
        private void ReadHeader(BinaryReader br)
        {
            CurrentFileReader = br;

            StartPos = br.BaseStream.Position;

            Version = br.ReadUInt32();
            EntryCount = br.ReadUInt32();
            NamesLength = br.ReadUInt32();
            Encryption = (RpfEncryption)br.ReadUInt32();

            if (Version != 0x52504637)
            {
                throw new Exception("Invalid Resource - not GTAV!");
            }

            byte[] entriesdata = br.ReadBytes((int)EntryCount * 16);
            byte[] namesdata = br.ReadBytes((int)NamesLength);

            switch (Encryption)
            {
                case RpfEncryption.NONE:
                case RpfEncryption.OPEN:
                    break;
                case RpfEncryption.AES:
                    entriesdata = GTACrypto.DecryptAES(entriesdata);
                    namesdata = GTACrypto.DecryptAES(namesdata);
                    IsAESEncrypted = true;
                    break;
                case RpfEncryption.NG:
                default:
                    entriesdata = GTACrypto.DecryptNG(entriesdata, Name, (uint)FileSize);
                    namesdata = GTACrypto.DecryptNG(namesdata, Name, (uint)FileSize);
                    break;
            }


            DataReader entriesrdr = new DataReader(new MemoryStream(entriesdata));
            DataReader namesrdr = new DataReader(new MemoryStream(namesdata));
            AllEntries = new List<RpfEntry>();
            TotalFileCount = 0;
            TotalFolderCount = 0;
            TotalResourceCount = 0;
            TotalBinaryFileCount = 0;

            for (uint i = 0; i < EntryCount; i++)
            {
                uint y = entriesrdr.ReadUInt32();
                uint x = entriesrdr.ReadUInt32();
                entriesrdr.Position -= 8;

                RpfEntry e;

                if (x == 0x7fffff00)
                {
                    e = new RpfDirectoryEntry();
                    TotalFolderCount++;
                }
                else if ((x & 0x80000000) == 0)
                {
                    e = new RpfBinaryFileEntry();
                    TotalBinaryFileCount++;
                    TotalFileCount++;
                }
                else
                {
                    e = new RpfResourceFileEntry();
                    TotalResourceCount++;
                    TotalFileCount++;
                }

                e.File = this;
                e.H1 = y;
                e.H2 = x;

                e.Read(entriesrdr);

                namesrdr.Position = e.NameOffset;
                e.Name = namesrdr.ReadString();
                if (e.Name.Length > 256)
                {
                    e.Name = e.Name.Substring(0, 256);
                }
                e.NameLower = e.Name.ToLowerInvariant();

                if (e is RpfFileEntry && string.IsNullOrEmpty(e.Name))
                {
                }
                if (e is RpfResourceFileEntry rfe)
                {
                    rfe.IsEncrypted = rfe.NameLower.EndsWith(".ysc");
                }

                AllEntries.Add(e);
            }



            Root = (RpfDirectoryEntry)AllEntries[0];
            Root.Path = Path.ToLowerInvariant();// + "\\" + Root.Name;
            Stack<RpfDirectoryEntry> stack = new Stack<RpfDirectoryEntry>();
            stack.Push(Root);
            while (stack.Count > 0)
            {
                RpfDirectoryEntry item = stack.Pop();

                int starti = (int)item.EntriesIndex;
                int endi = (int)(item.EntriesIndex + item.EntriesCount);

                for (int i = starti; i < endi; i++)
                {
                    RpfEntry e = AllEntries[i];
                    e.Parent = item;
                    switch (e)
                    {
                        case RpfDirectoryEntry rde:
                            rde.Path = item.Path + "\\" + rde.NameLower;
                            item.Directories.Add(rde);
                            stack.Push(rde);
                            break;
                        case RpfFileEntry rfe:
                            rfe.Path = item.Path + "\\" + rfe.NameLower;
                            item.Files.Add(rfe);
                            break;
                    }
                }
            }

            br.BaseStream.Position = StartPos;

            CurrentFileReader = null;

        }
        
        public void ScanStructure(Action<string> updateStatus, Action<string> errorLog)
        {
            using (BinaryReader br = new BinaryReader(File.OpenRead(FilePath)))
            {
                try
                {
                    ScanStructure(br, updateStatus, errorLog);
                }
                catch (Exception ex)
                {
                    LastError = ex.ToString();
                    LastException = ex;
                    errorLog(FilePath + ": " + LastError);
                }
            }
        }
        private void ScanStructure(BinaryReader br, Action<string> updateStatus, Action<string> errorLog)
        {
            ReadHeader(br);

            GrandTotalRpfCount = 1;
            GrandTotalFileCount = 1;
            GrandTotalFolderCount = 0;
            GrandTotalResourceCount = 0;
            GrandTotalBinaryFileCount = 0;

            Children = new List<RpfFile>();

            updateStatus?.Invoke("Scanning " + Path + "...");

            foreach (RpfEntry entry in AllEntries)
            {
                try
                {
                    switch (entry)
                    {
                        case RpfBinaryFileEntry binentry:
                        {
                            string lname = binentry.NameLower;
                            if (lname.EndsWith(".rpf") && binentry.Path.Length < 5000)
                            {
                                br.BaseStream.Position = StartPos + (long)binentry.FileOffset * 512;

                                long l = binentry.GetFileSize();

                                RpfFile subfile = new RpfFile(binentry.Name, binentry.Path, l)
                                {
                                    Parent = this,
                                    ParentFileEntry = binentry
                                };

                                subfile.ScanStructure(br, updateStatus, errorLog);

                                GrandTotalRpfCount += subfile.GrandTotalRpfCount;
                                GrandTotalFileCount += subfile.GrandTotalFileCount;
                                GrandTotalFolderCount += subfile.GrandTotalFolderCount;
                                GrandTotalResourceCount += subfile.GrandTotalResourceCount;
                                GrandTotalBinaryFileCount += subfile.GrandTotalBinaryFileCount;

                                Children.Add(subfile);
                            }
                            else
                            {
                                GrandTotalBinaryFileCount++;
                                GrandTotalFileCount++;
                            }

                            break;
                        }
                        case RpfResourceFileEntry _:
                            GrandTotalResourceCount++;
                            GrandTotalFileCount++;
                            break;
                        case RpfDirectoryEntry _:
                            GrandTotalFolderCount++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errorLog?.Invoke(entry.Path + ": " + ex);
                }
            }

        }

        public byte[] ExtractFile(RpfFileEntry entry)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(GetPhysicalFilePath())))
                {
                    switch (entry)
                    {
                        case RpfBinaryFileEntry binaryFileEntry:
                            return ExtractFileBinary(binaryFileEntry, br);
                        case RpfResourceFileEntry fileEntry:
                            return ExtractFileResource(fileEntry, br);
                        default:
                            return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.ToString();
                LastException = ex;
                return null;
            }
        }

        private byte[] ExtractFileBinary(RpfBinaryFileEntry entry, BinaryReader br)
        {
            br.BaseStream.Position = StartPos + (long)entry.FileOffset * 512;

            long l = entry.GetFileSize();

            if (l <= 0) return null;
            const uint offset = 0; // 0x10;
            uint totlen = (uint)l - offset;

            byte[] tbytes = new byte[totlen];

            br.BaseStream.Position += offset;
            br.Read(tbytes, 0, (int)totlen);

            byte[] decr = tbytes;

            if (entry.IsEncrypted)
            {
                decr = IsAESEncrypted ? GTACrypto.DecryptAES(tbytes) : GTACrypto.DecryptNG(tbytes, entry.Name, entry.FileUncompressedSize);
            }

            byte[] defl = decr;

            if (entry.FileSize > 0)
            {
                defl = DecompressBytes(decr);
            }

            return defl;
        }

        private byte[] ExtractFileResource(RpfResourceFileEntry entry, BinaryReader br)
        {
            br.BaseStream.Position = StartPos + (long)entry.FileOffset * 512;


            if (entry.FileSize <= 0) return null;
            const uint offset = 0x10;
            uint totlen = entry.FileSize - offset;

            byte[] tbytes = new byte[totlen];


            br.BaseStream.Position += offset;
            br.Read(tbytes, 0, (int)totlen);

            byte[] decr = tbytes;
            if (entry.IsEncrypted)
            {
                decr = IsAESEncrypted ? GTACrypto.DecryptAES(tbytes) : GTACrypto.DecryptNG(tbytes, entry.Name, entry.FileSize);
            }

            byte[] deflated = DecompressBytes(decr);

            byte[] data;

            if (deflated != null)
            {
                data = deflated;
            }
            else
            {
                entry.FileSize -= offset;
                data = decr;
            }
            
            return data;

        }

        public static T GetFile<T>(RpfEntry e, byte[] data) where T : class, PackedFile, new()
        {
            RpfFileEntry entry = e as RpfFileEntry;
            if (data == null) return null;
            if (entry == null)
            {
                entry = CreateResourceFileEntry(ref data, 0);
            }
            T file = new T();
            file.Load(data, entry);
            return file;
        }

        public static void LoadResourceFile<T>(T file, byte[] data, uint ver) where T : class, PackedFile
        {
            RpfResourceFileEntry resentry = CreateResourceFileEntry(ref data, ver);

            if (file is GameFile gfile)
            {
                if (gfile.RpfFileEntry is RpfResourceFileEntry oldresentry)
                {
                    oldresentry.SystemFlags = resentry.SystemFlags;
                    oldresentry.GraphicsFlags = resentry.GraphicsFlags;
                    resentry.Name = oldresentry.Name;
                    resentry.NameHash = oldresentry.NameHash;
                    resentry.NameLower = oldresentry.NameLower;
                    resentry.ShortNameHash = oldresentry.ShortNameHash;
                }
                else
                {
                    gfile.RpfFileEntry = resentry;
                }
            }

            data = ResourceBuilder.Decompress(data);
            file.Load(data, resentry);
        }
        public static RpfResourceFileEntry CreateResourceFileEntry(ref byte[] data, uint ver)
        {
            RpfResourceFileEntry resentry = new RpfResourceFileEntry();

            uint rsc7 = BitConverter.ToUInt32(data, 0);
            if (rsc7 == 0x37435352)
            {
                BitConverter.ToInt32(data, 4);
                resentry.SystemFlags = BitConverter.ToUInt32(data, 8);
                resentry.GraphicsFlags = BitConverter.ToUInt32(data, 12);
                if (data.Length > 16)
                {
                    int newlen = data.Length - 16;
                    byte[] newdata = new byte[newlen];
                    Buffer.BlockCopy(data, 16, newdata, 0, newlen);
                    data = newdata;
                }
            }
            else
            {
                resentry.SystemFlags = RpfResourceFileEntry.GetFlagsFromSize(data.Length, 0);
                resentry.GraphicsFlags = RpfResourceFileEntry.GetFlagsFromSize(0, ver);
            }

            resentry.Name = "";
            resentry.NameLower = "";

            return resentry;
        }

        public List<RpfFileEntry> GetFiles(string folder, bool recurse)
        {
            List<RpfFileEntry> result = new List<RpfFileEntry>();
            string[] parts = folder.ToLowerInvariant().Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            RpfDirectoryEntry dir = Root;
            foreach (string t in parts)
            {
                if (dir == null) break;
                dir = FindSubDirectory(dir, t);
            }
            if (dir != null)
            {
                GetFiles(dir, result, recurse);
            }
            return result;
        }

        private static void GetFiles(RpfDirectoryEntry dir, List<RpfFileEntry> result, bool recurse)
        {
            if (dir.Files != null)
            {
                result.AddRange(dir.Files);
            }
            if (!recurse) return;
            if (dir.Directories == null) return;
            foreach (RpfDirectoryEntry t in dir.Directories)
            {
                GetFiles(t, result, true);
            }
        }

        private static RpfDirectoryEntry FindSubDirectory(RpfDirectoryEntry dir, string name)
        {
            return dir?.Directories?.FirstOrDefault(cdir => cdir.Name.ToLowerInvariant() == name);
        }

        private byte[] DecompressBytes(byte[] bytes)
        {
            try
            {
                using (DeflateStream ds = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    using (MemoryStream outstr = new MemoryStream())
                    {
                        ds.CopyTo(outstr);
                        byte[] deflated = outstr.GetBuffer();
                        byte[] outbuf = new byte[outstr.Length]; //need to copy to the right size buffer for output.
                        Buffer.BlockCopy(deflated, 0, outbuf, 0, outbuf.Length);

                        if (outbuf.Length <= bytes.Length)
                        {
                            LastError = "Warning: Decompressed data was smaller than compressed data...";
                        }

                        return outbuf;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = "Could not decompress.";
                LastException = ex;
                return null;
            }
        }

        private static byte[] CompressBytes(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true))
                {
                    ds.Write(data, 0, data.Length);
                    ds.Close();
                    byte[] deflated = ms.GetBuffer();
                    byte[] outbuf = new byte[ms.Length];
                    Buffer.BlockCopy(deflated, 0, outbuf, 0, outbuf.Length);
                    return outbuf;
                }
            }
        }

        private void WriteHeader(BinaryWriter bw)
        {
            byte[] namesdata = GetHeaderNamesData();
            NamesLength = (uint)namesdata.Length;

            uint headersize = GetHeaderBlockCount() * 512;
            EnsureSpace(bw, null, headersize);

            byte[] entriesdata = GetHeaderEntriesData();
            switch (Encryption)
            {
                case RpfEncryption.NONE:
                case RpfEncryption.OPEN:
                    break;
                case RpfEncryption.AES:
                    entriesdata = GTACrypto.EncryptAES(entriesdata);
                    namesdata = GTACrypto.EncryptAES(namesdata);
                    IsAESEncrypted = true;
                    break;
                case RpfEncryption.NG:
                default:
                    entriesdata = GTACrypto.EncryptNG(entriesdata, Name, (uint)FileSize);
                    namesdata = GTACrypto.EncryptNG(namesdata, Name, (uint)FileSize);
                    break;
            }

            bw.BaseStream.Position = StartPos;
            bw.Write(Version);
            bw.Write(EntryCount);
            bw.Write(NamesLength);
            bw.Write((uint)Encryption);
            bw.Write(entriesdata);
            bw.Write(namesdata);

            WritePadding(bw.BaseStream, StartPos + headersize);
        }
        
        private static void WritePadding(Stream s, long upto)
        {
            int diff = (int)(upto - s.Position);
            if (diff > 0)
            {
                s.Write(new byte[diff], 0, diff);
            }
        }
        
        private void EnsureAllEntries()
        {
            if (AllEntries == null)
            {
                AllEntries = new List<RpfEntry>();
                Root = new RpfDirectoryEntry();
                Root.File = this;
                Root.Name = string.Empty;
                Root.NameLower = string.Empty;
                Root.Path = Path.ToLowerInvariant();
            }
            
            if (Children == null)
            {
                Children = new List<RpfFile>();
            }

            List<RpfEntry> temp = new List<RpfEntry>();
            AllEntries.Clear();
            AllEntries.Add(Root);
            Stack<RpfDirectoryEntry> stack = new Stack<RpfDirectoryEntry>();
            stack.Push(Root);
            while (stack.Count > 0)
            {
                RpfDirectoryEntry item = stack.Pop();

                item.EntriesCount = (uint)(item.Directories.Count + item.Files.Count);
                item.EntriesIndex = (uint)AllEntries.Count;
                
                temp.Clear();
                temp.AddRange(item.Directories);
                temp.AddRange(item.Files);
                temp.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

                foreach (RpfEntry entry in temp)
                {
                    AllEntries.Add(entry);
                    if (entry is RpfDirectoryEntry dir)
                    {
                        stack.Push(dir);
                    }
                }
            }

            EntryCount = (uint)AllEntries.Count;

        }
        
        private byte[] GetHeaderNamesData()
        {
            MemoryStream namesstream = new MemoryStream();
            DataWriter nameswriter = new DataWriter(namesstream);
            Dictionary<string, uint> namedict = new Dictionary<string, uint>();
            foreach (RpfEntry entry in AllEntries)
            {
                string name = entry.Name ?? "";
                if (namedict.TryGetValue(name, out uint nameoffset))
                {
                    entry.NameOffset = nameoffset;
                }
                else
                {
                    entry.NameOffset = (uint)namesstream.Length;
                    namedict.Add(name, entry.NameOffset);
                    nameswriter.Write(name);
                }
            }
            byte[] buf = new byte[namesstream.Length];
            namesstream.Position = 0;
            namesstream.Read(buf, 0, buf.Length);
            return PadBuffer(buf, 16);
        }
        
        private byte[] GetHeaderEntriesData()
        {
            MemoryStream entriesstream = new MemoryStream();
            DataWriter entrieswriter = new DataWriter(entriesstream);
            foreach (RpfEntry entry in AllEntries)
            {
                entry.Write(entrieswriter);
            }
            byte[] buf = new byte[entriesstream.Length];
            entriesstream.Position = 0;
            entriesstream.Read(buf, 0, buf.Length);
            return buf;
        }
        
        private uint GetHeaderBlockCount()//make sure EntryCount and NamesLength are updated before calling this...
        {
            uint headerusedbytes = 16 + EntryCount * 16 + NamesLength;
            uint headerblockcount = GetBlockCount(headerusedbytes);
            return headerblockcount;
        }
        
        private static byte[] PadBuffer(byte[] buf, uint n)//add extra bytes as necessary to nearest n
        {
            uint buflen = (uint)buf.Length;
            uint newlen = PadLength(buflen, n);
            if (newlen == buflen) return buf;
            byte[] buf2 = new byte[newlen];
            Buffer.BlockCopy(buf, 0, buf2, 0, buf.Length);
            return buf2;
        }
        
        private static uint PadLength(uint l, uint n)//round up to nearest n bytes
        {
            uint rem = l % n;
            return l + (rem > 0 ? n - rem : 0);
        }
        
        private static uint GetBlockCount(long bytecount)
        {
            uint b0 = (uint)(bytecount & 0x1FF); //511;
            uint b1 = (uint)(bytecount >> 9);
            if (b0 == 0) return b1;
            return b1 + 1;
        }
        
        private RpfFileEntry FindFirstFileAfter(uint block)
        {
            RpfFileEntry nextentry = null;
            foreach (RpfEntry entry in AllEntries)
            {
                if (!(entry is RpfFileEntry fe) || fe.FileOffset <= block) continue;
                if (nextentry == null || fe.FileOffset < nextentry.FileOffset)
                {
                    nextentry = fe;
                }
            }
            return nextentry;
        }
        private uint FindHole(uint reqblocks, uint ignorestart, uint ignoreend)
        {
            List<RpfFileEntry> allfiles = new List<RpfFileEntry>();
            foreach (RpfEntry entry in AllEntries)
            {
                if (entry is RpfFileEntry rfe)
                {
                    allfiles.Add(rfe);
                }
            }
            allfiles.Sort((e1, e2) => e1.FileOffset.CompareTo(e2.FileOffset));

            uint found = 0;
            uint foundsize = 0xFFFFFFFF;
            
            for (int i = 1; i < allfiles.Count(); i++)
            {
                RpfFileEntry e1 = allfiles[i - 1];
                RpfFileEntry e2 = allfiles[i];

                uint e1cnt = GetBlockCount(e1.GetFileSize());
                uint e1end = e1.FileOffset + e1cnt;
                uint e2beg = e2.FileOffset;
                if (e2beg > ignorestart && e1end < ignoreend)
                {
                    continue;
                }

                if (e1end >= e2beg) continue;
                uint space = e2beg - e1end;
                if (space < reqblocks || space >= foundsize) continue;
                found = e1end;
                foundsize = space;
            }

            return found;
        }
        private uint FindEndBlock()
        {
            uint endblock = 0;
            foreach (RpfEntry entry in AllEntries)
            {
                if (!(entry is RpfFileEntry e)) continue;
                uint ecnt = GetBlockCount(e.GetFileSize());
                uint eend = e.FileOffset + ecnt;
                if (eend > endblock)
                {
                    endblock = eend;
                }
            }

            if (endblock == 0)
            {
                endblock = GetHeaderBlockCount();
            }

            return endblock;
        }
        private void GrowArchive(BinaryWriter bw, uint newblockcount)
        {
            uint newsize = newblockcount * 512;
            if (newsize < FileSize)
            {
                return;
            }
            if (FileSize == newsize)
            {
                return;
            }

            FileSize = newsize;

            if (Parent == null) return;
            if (ParentFileEntry == null)
            {
                throw new Exception("Can't grow archive " + Path + ": ParentFileEntry was null!");
            }
            
            ParentFileEntry.FileUncompressedSize = newsize;
            ParentFileEntry.FileSize = 0;

            Parent.EnsureSpace(bw, ParentFileEntry, newsize);
        }
        private void RelocateFile(BinaryWriter bw, RpfFileEntry f, uint newblock)
        {
            uint flen = GetBlockCount(f.GetFileSize());
            uint fbeg = f.FileOffset;
            uint fend = fbeg + flen;
            uint nend = newblock + flen;
            
            if (nend > fbeg && newblock < fend)
            {
                throw new Exception("Unable to relocate file " + f.Path + ": new position was inside the original!");
            }

            Stream stream = bw.BaseStream;
            long origpos = stream.Position;
            long source = StartPos + (long)fbeg * 512;
            long dest = StartPos + (long)newblock * 512;
            long newstart = dest;
            long length = (long)flen * 512;
            long destend = dest + length;
            const int BUFFER_SIZE = 16384;
            byte[] buffer = new byte[BUFFER_SIZE];
            while (length > 0)
            {
                stream.Position = source;
                int i = stream.Read(buffer, 0, (int)Math.Min(length, BUFFER_SIZE));
                stream.Position = dest;
                stream.Write(buffer, 0, i);
                source += i;
                dest += i;
                length -= i;
            }

            WritePadding(stream, destend);
            stream.Position = origpos;
            f.FileOffset = newblock;

            RpfFile child = FindChildArchive(f);
            child?.UpdateStartPos(newstart);

        }
        private void EnsureSpace(BinaryWriter bw, RpfFileEntry e, long bytecount)
        {
            uint blockcount = GetBlockCount(bytecount);
            uint startblock = e?.FileOffset ?? 0;
            uint endblock = startblock + blockcount;

            RpfFileEntry nextentry = FindFirstFileAfter(startblock);

            while (nextentry != null)
            {
                if (nextentry.FileOffset >= endblock)
                {
                    break;
                }

                uint entryblocks = GetBlockCount(nextentry.GetFileSize());
                uint newblock = FindHole(entryblocks, startblock, endblock);
                if (newblock == 0)
                {
                    newblock = FindEndBlock();
                    GrowArchive(bw, newblock + entryblocks);
                }

                RelocateFile(bw, nextentry, newblock);
                nextentry = FindFirstFileAfter(startblock);
            }

            if (nextentry == null)
            {
                uint newblock = FindEndBlock();
                GrowArchive(bw, newblock + (e != null ? blockcount : 0));
            }

            if (e != null)
            {
                WriteHeader(bw);
            }

        }
        private void InsertFileSpace(BinaryWriter bw, RpfFileEntry entry)
        {
            uint blockcount = GetBlockCount(entry.GetFileSize());
            entry.FileOffset = FindHole(blockcount, 0, 0);
            if (entry.FileOffset == 0)
            {
                entry.FileOffset = FindEndBlock();
                GrowArchive(bw, entry.FileOffset + blockcount);
            }
            EnsureAllEntries();
            WriteHeader(bw);
        }

        private void WriteNewArchive(BinaryWriter bw, RpfEncryption encryption)
        {
            Stream stream = bw.BaseStream;
            Encryption = encryption;
            Version = 0x52504637;
            IsAESEncrypted = encryption == RpfEncryption.AES;
            StartPos = stream.Position;
            EnsureAllEntries();
            WriteHeader(bw);
            FileSize = stream.Position - StartPos;
        }

        private void UpdatePaths(RpfDirectoryEntry dir = null)
        {
            if (dir == null)
            {
                Root.Path = Path.ToLowerInvariant();
                dir = Root;
            }
            foreach (RpfFileEntry file in dir.Files)
            {
                file.Path = dir.Path + "\\" + file.NameLower;

                if (!(file is RpfBinaryFileEntry binf) || !file.NameLower.EndsWith(".rpf")) continue;
                RpfFile childrpf = FindChildArchive(binf);
                if (childrpf == null) continue;
                childrpf.Path = binf.Path;
                childrpf.FilePath = binf.Path;
                childrpf.UpdatePaths();

            }
            foreach (RpfDirectoryEntry subdir in dir.Directories)
            {
                subdir.Path = dir.Path + "\\" + subdir.NameLower;
                UpdatePaths(subdir);
            }
        }

        private RpfFile FindChildArchive(RpfFileEntry f)
        {
            RpfFile c = null;
            return Children == null ? c : Children.FirstOrDefault(child => child.ParentFileEntry == f);
        }
        
        private void UpdateStartPos(long newpos)
        {
            StartPos = newpos;
            if (Children == null) return;
            foreach (RpfFile child in Children)
            {
                if (child.ParentFileEntry == null) continue;
                long cpos = StartPos + (long)child.ParentFileEntry.FileOffset * 512;
                child.UpdateStartPos(cpos);
            }
        }
        
        public override string ToString()
        {
            return Path;
        }
    }
    
    public enum RpfEncryption : uint
    {
        NONE = 0,
        OPEN = 0x4E45504F,
        AES =  0x0FFFFFF9,
        NG =   0x0FEFFFFF
    }
    
    [TypeConverter(typeof(ExpandableObjectConverter))] public abstract class RpfEntry
    {
        public RpfFile File { get; set; }
        public RpfDirectoryEntry Parent { get; set; }

        public uint NameHash { get; set; }
        public uint ShortNameHash { get; set; }

        public uint NameOffset { get; set; }
        public string Name { get; set; }
        public string NameLower { get; set; }
        public string Path { get; set; }

        public uint H1; //first 2 header values from RPF table...
        public uint H2;

        public abstract void Read(DataReader reader);
        public abstract void Write(DataWriter writer);

        public override string ToString()
        {
            return Path;
        }

        public string GetShortName()
        {
            int ind = Name.LastIndexOf('.');
            return ind > 0 ? Name.Substring(0, ind) : Name;
        }
        public string GetShortNameLower()
        {
            if (NameLower == null)
            {
                NameLower = Name.ToLowerInvariant();
            }
            int ind = NameLower.LastIndexOf('.');
            return ind > 0 ? NameLower.Substring(0, ind) : NameLower;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public class RpfDirectoryEntry : RpfEntry
    {
        public uint EntriesIndex { get; set; }
        public uint EntriesCount { get; set; }

        public readonly List<RpfDirectoryEntry> Directories = new List<RpfDirectoryEntry>();
        public readonly List<RpfFileEntry> Files = new List<RpfFileEntry>();

        public override void Read(DataReader reader)
        {
            NameOffset = reader.ReadUInt32();
            uint ident = reader.ReadUInt32();
            if (ident != 0x7FFFFF00u)
            {
                throw new Exception("Error in RPF7 directory entry.");
            }
            EntriesIndex = reader.ReadUInt32();
            EntriesCount = reader.ReadUInt32();
        }
        public override void Write(DataWriter writer)
        {
            writer.Write(NameOffset);
            writer.Write(0x7FFFFF00u);
            writer.Write(EntriesIndex);
            writer.Write(EntriesCount);
        }
        public override string ToString()
        {
            return "Directory: " + Path;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public abstract class RpfFileEntry : RpfEntry
    {
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
        public bool IsEncrypted { get; set; }

        public abstract long GetFileSize();
        public abstract void SetFileSize(uint s);
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public class RpfBinaryFileEntry : RpfFileEntry
    {
        public uint FileUncompressedSize { get; set; }
        private uint EncryptionType { get; set; }

        public override void Read(DataReader reader)
        {
            ulong buf = reader.ReadUInt64();
            NameOffset = (uint)buf & 0xFFFF;
            FileSize = (uint)(buf >> 16) & 0xFFFFFF;
            FileOffset = (uint)(buf >> 40) & 0xFFFFFF;

            FileUncompressedSize = reader.ReadUInt32();
            EncryptionType = reader.ReadUInt32();

            switch (EncryptionType)
            {
                case 0: IsEncrypted = false; break;
                case 1: IsEncrypted = true; break;
                default:
                    throw new Exception("Error in RPF7 file entry.");
            }

        }
        public override void Write(DataWriter writer)
        {
            writer.Write((ushort)NameOffset);

            byte[] buf1 = {
                (byte)((FileSize >> 0) & 0xFF),
                (byte)((FileSize >> 8) & 0xFF),
                (byte)((FileSize >> 16) & 0xFF)
            };
            writer.Write(buf1);

            byte[] buf2 = {
                (byte)((FileOffset >> 0) & 0xFF),
                (byte)((FileOffset >> 8) & 0xFF),
                (byte)((FileOffset >> 16) & 0xFF)
            };
            writer.Write(buf2);

            writer.Write(FileUncompressedSize);

            if (IsEncrypted)
                writer.Write((uint)1);
            else
                writer.Write((uint)0);
        }
        public override string ToString()
        {
            return "Binary file: " + Path;
        }

        public override long GetFileSize()
        {
            return FileSize == 0 ? FileUncompressedSize : FileSize;
        }
        public override void SetFileSize(uint s)
        {
            //FileUncompressedSize = s;
            FileSize = s;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public class RpfResourceFileEntry : RpfFileEntry
    {
        public RpfResourcePageFlags SystemFlags { get; set; }
        public RpfResourcePageFlags GraphicsFlags { get; set; }


        public static uint GetFlagsFromSize(int size, uint version)
        {
            int remainder = size & 0x1FF;
            const int blocksize = 0x200;
            if (remainder != 0)
            {
                size = size - remainder + blocksize;
            }

            uint blockcount = (uint)size >> 9;
            uint ss = 0;
            while (blockcount > 1024)
            {
                ss++;
                blockcount >>= 1;
            }

            uint s0 = (blockcount >> 0) & 0x1;
            uint s1 = (blockcount >> 1) & 0x1;
            uint s2 = (blockcount >> 2) & 0x1;
            uint s3 = (blockcount >> 3) & 0x1;
            uint s4 = (blockcount >> 4) & 0x7F;
            uint f = 0;
            
            f |= (version & 0xF) << 28;
            f |= (s0 & 0x1) << 27;
            f |= (s1 & 0x1) << 26;
            f |= (s2 & 0x1) << 25;
            f |= (s3 & 0x1) << 24;
            f |= (s4 & 0x7F) << 17;
            f |= ss & 0xF;
            
            return f;
        }
        public static uint GetFlagsFromBlocks(uint blockCount, uint blockSize, uint version)
        {
            const uint s5 = 0;
            const uint s6 = 0;
            const uint s7 = 0;
            const uint s8 = 0;
            uint ss = 0;

            uint bst = blockSize;
            if (blockCount > 0)
            {
                while (bst > 0x200)
                {
                    ss++;
                    bst >>= 1;
                }
            }
            
            uint s0 = (blockCount >> 0) & 0x1;
            uint s1 = (blockCount >> 1) & 0x1;
            uint s2 = (blockCount >> 2) & 0x1;
            uint s3 = (blockCount >> 3) & 0x1;
            uint s4 = (blockCount >> 4) & 0x7F;
            uint f = 0;
            
            f |= (version & 0xF) << 28;
            f |= (s0 & 0x1) << 27;
            f |= (s1 & 0x1) << 26;
            f |= (s2 & 0x1) << 25;
            f |= (s3 & 0x1) << 24;
            f |= (s4 & 0x7F) << 17;
            f |= (s5 & 0x3F) << 11;
            f |= (s6 & 0xF) << 7;
            f |= (s7 & 0x3) << 5;
            f |= (s8 & 0x1) << 4;
            f |= ss & 0xF;
            
            return f;
        }

        private static int GetVersionFromFlags(uint sysFlags, uint gfxFlags)
        {
            uint sv = (sysFlags >> 28) & 0xF;
            uint gv = (gfxFlags >> 28) & 0xF;
            return (int)((sv << 4) + gv);
        }
        
        public int Version => GetVersionFromFlags(SystemFlags, GraphicsFlags);
        
        public int SystemSize => (int)SystemFlags.Size;

        public int GraphicsSize => (int)GraphicsFlags.Size;

        public override void Read(DataReader reader)
        {
            NameOffset = reader.ReadUInt16();

            byte[] buf1 = reader.ReadBytes(3);
            FileSize = buf1[0] + (uint)(buf1[1] << 8) + (uint)(buf1[2] << 16);

            byte[] buf2 = reader.ReadBytes(3);
            FileOffset = (buf2[0] + (uint)(buf2[1] << 8) + (uint)(buf2[2] << 16)) & 0x7FFFFF;

            SystemFlags = reader.ReadUInt32();
            GraphicsFlags = reader.ReadUInt32();

            if (FileSize != 0xFFFFFF) return;
            BinaryReader cfr = File.CurrentFileReader;
            long opos = cfr.BaseStream.Position;
            cfr.BaseStream.Position = File.StartPos + (long)FileOffset * 512; //need to use the base offset!!
            byte[] buf = cfr.ReadBytes(16);
            FileSize = ((uint)buf[7] << 0) | ((uint)buf[14] << 8) | ((uint)buf[5] << 16) | ((uint)buf[2] << 24);
            cfr.BaseStream.Position = opos;

        }
        public override void Write(DataWriter writer)
        {
            writer.Write((ushort)NameOffset);

            uint fs = FileSize;
            if (fs > 0xFFFFFF) fs = 0xFFFFFF;

            byte[] buf1 = new[] {
                (byte)((fs >> 0) & 0xFF),
                (byte)((fs >> 8) & 0xFF),
                (byte)((fs >> 16) & 0xFF)
            };
            writer.Write(buf1);

            byte[] buf2 = new[] {
                (byte)((FileOffset >> 0) & 0xFF),
                (byte)((FileOffset >> 8) & 0xFF),
                (byte)(((FileOffset >> 16) & 0xFF) | 0x80)
            };
            writer.Write(buf2);

            writer.Write(SystemFlags);
            writer.Write(GraphicsFlags);
        }
        public override string ToString()
        {
            return "Resource file: " + Path;
        }

        public override long GetFileSize()
        {
            return FileSize == 0 ? (long)(SystemSize + GraphicsSize) : FileSize;
        }
        public override void SetFileSize(uint s)
        {
            FileSize = s;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public struct RpfResourcePageFlags
    {
        public uint Value { get; set; }
        
        public RpfResourcePage[] Pages
        {
            get
            {
                uint count = Count;
                if (count == 0) return null;
                RpfResourcePage[] pages = new RpfResourcePage[count];
                uint[] counts = PageCounts;
                uint[] sizes = BaseSizes;
                int n = 0;
                uint o = 0;
                for (int i = 0; i < counts.Length; i++)
                {
                    uint c = counts[i];
                    uint s = sizes[i];
                    for (int p = 0; p < c; p++)
                    {
                        pages[n] = new RpfResourcePage() { Size = s, Offset = o };
                        o += s;
                        n++;
                    }
                }
                return pages;
            }
        }

        private uint BaseShift => Value & 0xF;
        private uint BaseSize => 0x200u << (int)BaseShift;

        private uint[] BaseSizes
        {
            get
            {
                uint baseSize = BaseSize;
                return new[]
                {
                    baseSize << 8,
                    baseSize << 7,
                    baseSize << 6,
                    baseSize << 5,
                    baseSize << 4,
                    baseSize << 3,
                    baseSize << 2,
                    baseSize << 1,
                    baseSize << 0,
                };
            }
        }

        private uint[] PageCounts
        {
            get
            {
                return new[]
                {
                    (Value >> 4)  & 0x1,
                    (Value >> 5)  & 0x3,
                    (Value >> 7)  & 0xF,
                    (Value >> 11) & 0x3F,
                    (Value >> 17) & 0x7F,
                    (Value >> 24) & 0x1,
                    (Value >> 25) & 0x1,
                    (Value >> 26) & 0x1,
                    (Value >> 27) & 0x1,
                };
            }
        }

        public uint Count
        {
            get
            {
                uint[] c = PageCounts;
                return c[0] + c[1] + c[2] + c[3] + c[4] + c[5] + c[6] + c[7] + c[8];
            }
        }
        
        public uint Size 
        { 
            get 
            {
                uint flags = Value;
                uint s0 = ((flags >> 27) & 0x1)  << 0;
                uint s1 = ((flags >> 26) & 0x1)  << 1;
                uint s2 = ((flags >> 25) & 0x1)  << 2;
                uint s3 = ((flags >> 24) & 0x1)  << 3;
                uint s4 = ((flags >> 17) & 0x7F) << 4;
                uint s5 = ((flags >> 11) & 0x3F) << 5;
                uint s6 = ((flags >> 7)  & 0xF)  << 6;
                uint s7 = ((flags >> 5)  & 0x3)  << 7;
                uint s8 = ((flags >> 4)  & 0x1)  << 8;
                uint ss = (flags >> 0)  & 0xF;
                uint baseSize = 0x200u << (int)ss;
                return baseSize * (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);
            }
        }
        
        public RpfResourcePageFlags(uint v)
        {
            Value = v;
        }

        public RpfResourcePageFlags(uint[] pageCounts, uint baseShift)
        {
            uint v = baseShift & 0xF;
            v += (pageCounts[0] & 0x1)  << 4;
            v += (pageCounts[1] & 0x3)  << 5;
            v += (pageCounts[2] & 0xF)  << 7;
            v += (pageCounts[3] & 0x3F) << 11;
            v += (pageCounts[4] & 0x7F) << 17;
            v += (pageCounts[5] & 0x1)  << 24;
            v += (pageCounts[6] & 0x1)  << 25;
            v += (pageCounts[7] & 0x1)  << 26;
            v += (pageCounts[8] & 0x1)  << 27;
            Value = v;
        }
        
        public static implicit operator uint(RpfResourcePageFlags f)
        {
            return f.Value;
        }
        
        public static implicit operator RpfResourcePageFlags(uint v)
        {
            return new RpfResourcePageFlags(v);
        }

        public override string ToString()
        {
            return "Size: " + Size + ", Pages: " + Count;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public struct RpfResourcePage
    {
        public uint Size { get; set; }
        public uint Offset { get; set; }

        public override string ToString()
        {
            return Size + ": " + Offset;
        }
    }

    public interface PackedFile
    {
        void Load(byte[] data, RpfFileEntry entry);
    }










}