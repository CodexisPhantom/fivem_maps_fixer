﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CodeWalker.GameFiles
{
    public class RpfManager
    {
        public string[] ExcludePaths { get; set; }
        public bool EnableMods { get; set; }
        public bool BuildExtendedJenkIndex { get; set; } = true;
        private Action<string> ErrorLog { get; set; }

        public List<RpfFile> BaseRpfs { get; private set; }
        private List<RpfFile> ModRpfs { get; set; }
        public List<RpfFile> DlcRpfs { get; private set; }
        public List<RpfFile> AllRpfs { get; private set; }
        private List<RpfFile> DlcNoModRpfs { get; set; }
        private List<RpfFile> AllNoModRpfs { get; set; }
        public Dictionary<string, RpfFile> RpfDict { get; private set; }
        private Dictionary<string, RpfEntry> EntryDict { get; set; }
        public Dictionary<string, RpfFile> ModRpfDict { get; private set; }
        private Dictionary<string, RpfEntry> ModEntryDict { get; set; }

        public void Init(string folder, Action<string> updateStatus, Action<string> errorLog, bool rootOnly = false, bool buildIndex = true)
        {
            ErrorLog = errorLog;

            string replpath = folder + "\\";
            SearchOption sopt = rootOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            string[] allfiles = Directory.GetFiles(folder, "*.rpf", sopt);

            BaseRpfs = new List<RpfFile>();
            ModRpfs = new List<RpfFile>();
            DlcRpfs = new List<RpfFile>();
            AllRpfs = new List<RpfFile>();
            DlcNoModRpfs = new List<RpfFile>();
            AllNoModRpfs = new List<RpfFile>();
            RpfDict = new Dictionary<string, RpfFile>();
            EntryDict = new Dictionary<string, RpfEntry>();
            ModRpfDict = new Dictionary<string, RpfFile>();
            ModEntryDict = new Dictionary<string, RpfEntry>();

            foreach (string rpfpath in allfiles)
            {
                try
                {
                    RpfFile rf = new RpfFile(rpfpath, rpfpath.Replace(replpath, ""));

                    if (ExcludePaths != null)
                    {
                        bool excl = ExcludePaths.Any(t => rf.Path.StartsWith(t));
                        if (excl) continue;
                    }

                    rf.ScanStructure(updateStatus, errorLog);

                    if (rf.LastException != null)
                    {
                        continue;
                    }

                    AddRpfFile(rf, false, false);
                }
                catch (Exception ex)
                {
                    errorLog(rpfpath + ": " + ex);
                }
            }

            if (buildIndex)
            {
                updateStatus("Building jenkindex...");
                BuildBaseJenkIndex();
            }

            updateStatus("Scan complete");
        }

        public void Init(List<RpfFile> allRpfs)
        {
            AllRpfs = allRpfs;

            BaseRpfs = new List<RpfFile>();
            ModRpfs = new List<RpfFile>();
            DlcRpfs = new List<RpfFile>();
            DlcNoModRpfs = new List<RpfFile>();
            AllNoModRpfs = new List<RpfFile>();
            RpfDict = new Dictionary<string, RpfFile>();
            EntryDict = new Dictionary<string, RpfEntry>();
            ModRpfDict = new Dictionary<string, RpfFile>();
            ModEntryDict = new Dictionary<string, RpfEntry>();
            foreach (RpfFile rpf in allRpfs)
            {
                RpfDict[rpf.Path] = rpf;
                if (rpf.AllEntries == null) continue;
                foreach (RpfEntry entry in rpf.AllEntries)
                {
                    EntryDict[entry.Path] = entry;
                }
            }

            BuildBaseJenkIndex();
        }


        private void AddRpfFile(RpfFile file, bool isdlc, bool ismod)
        {
            isdlc = isdlc || (file.NameLower == "update.rpf") || (file.NameLower.StartsWith("dlc") && file.NameLower.EndsWith(".rpf"));
            ismod = ismod || (file.Path.StartsWith("mods\\"));

            if (file.AllEntries != null)
            {
                AllRpfs.Add(file);
                if (!ismod)
                {
                    AllNoModRpfs.Add(file);
                }
                if (isdlc)
                {
                    DlcRpfs.Add(file);
                    if (!ismod)
                    {
                        DlcNoModRpfs.Add(file);
                    }
                }
                else
                {
                    if (ismod)
                    {
                        ModRpfs.Add(file);
                    }
                    else
                    {
                        BaseRpfs.Add(file);
                    }
                }
                if (ismod)
                {
                    ModRpfDict[file.Path.Substring(5)] = file;
                }

                RpfDict[file.Path] = file;

                foreach (RpfEntry entry in file.AllEntries)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        if (ismod)
                        {
                            ModEntryDict[entry.Path] = entry;
                            ModEntryDict[entry.Path.Substring(5)] = entry;
                        }
                        else
                        {
                            EntryDict[entry.Path] = entry;
                        }
                        if (!(entry is RpfFileEntry)) continue;
                        entry.NameHash = JenkHash.GenHash(entry.NameLower);
                        int ind = entry.NameLower.LastIndexOf('.');
                        entry.ShortNameHash = (ind > 0) ? JenkHash.GenHash(entry.NameLower.Substring(0, ind)) : entry.NameHash;
                    }
                    catch (Exception ex)
                    {
                        file.LastError = ex.ToString();
                        file.LastException = ex;
                        ErrorLog(entry.Path + ": " + ex.ToString());
                    }
                }
            }

            if (file.Children == null) return;
            foreach (RpfFile cfile in file.Children)
            {
                AddRpfFile(cfile, isdlc, ismod);
            }
        }


        public RpfFile FindRpfFile(string path) => FindRpfFile(path, false);
        
        private RpfFile FindRpfFile(string path, bool exactPathOnly)
        {
            if (EnableMods && ModRpfDict.TryGetValue(path, out var file))
            {
                return file;
            }

            if (RpfDict.TryGetValue(path, out file))
            {
                return file;
            }

            string lpath = path.ToLowerInvariant();
            foreach (RpfFile tfile in AllRpfs)
            {
                if (!exactPathOnly && tfile.NameLower == lpath)
                {
                    return tfile;
                }
                if (tfile.Path == lpath)
                {
                    return tfile;
                }
            }

            return null;
        }
        
        public RpfEntry GetEntry(string path)
        {
            string pathl = path.ToLowerInvariant();
            if (EnableMods && ModEntryDict.TryGetValue(pathl, out var entry))
            {
                return entry;
            }
            
            EntryDict.TryGetValue(pathl, out entry);
            if (entry != null) return entry;
            pathl = pathl.Replace("/", "\\");
            pathl = pathl.Replace("common:", "common.rpf");
            
            if (EnableMods && ModEntryDict.TryGetValue(pathl, out entry))
            {
                return entry;
            }
            
            EntryDict.TryGetValue(pathl, out entry);
            return entry;
        }

        private byte[] GetFileData(string path)
        {
            byte[] data = null;
            if (GetEntry(path) is RpfFileEntry entry)
            {
                data = entry.File.ExtractFile(entry);
            }
            return data;
        }
        
        public string GetFileUtf8Text(string path)
        {
            byte[] bytes = GetFileData(path);
            return TextUtil.GetUTF8Text(bytes);
        }
        
        public XmlDocument GetFileXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            string text = GetFileUtf8Text(path);
            if (!string.IsNullOrEmpty(text))
            {
                doc.LoadXml(text);
            }
            return doc;
        }

        public T GetFile<T>(string path) where T : class, PackedFile, new()
        {
            byte[] data = null;
            RpfFileEntry entry = GetEntry(path) as RpfFileEntry;
            if (entry != null)
            {
                data = entry.File.ExtractFile(entry);
            }

            if (data == null) return null;
            var file = new T();
            file.Load(data, entry);
            return file;
        }
        
        public static T GetFile<T>(RpfEntry e) where T : class, PackedFile, new()
        {
            byte[] data = null;
            RpfFileEntry entry = e as RpfFileEntry;
            
            if (entry != null)
            {
                data = entry.File.ExtractFile(entry);
            }

            if (data == null) return null;
            
            T file = new T();
            file.Load(data, entry);
            
            return file;
        }
        
        public static bool LoadFile<T>(T file, RpfEntry e) where T : class, PackedFile
        {
            byte[] data = null;
            RpfFileEntry entry = e as RpfFileEntry;
            if (entry != null)
            {
                data = entry.File.ExtractFile(entry);
            }

            if (data == null) return false;
            file.Load(data, entry);
            return true;
        }
        
        private void BuildBaseJenkIndex()
        {
            JenkIndex.Clear();
            StringBuilder sb = new StringBuilder();
            foreach (RpfFile file in AllRpfs)
            {
                try
                {
                    JenkIndex.Ensure(file.Name);
                    foreach (RpfEntry entry in file.AllEntries)
                    {
                        string nlow = entry.NameLower;
                        if (string.IsNullOrEmpty(nlow)) continue;
                        
                        int ind = nlow.LastIndexOf('.');
                        if (ind > 0)
                        {
                            JenkIndex.Ensure(entry.Name.Substring(0, ind));
                            JenkIndex.Ensure(nlow.Substring(0, ind));
                        }
                        else
                        {
                            JenkIndex.Ensure(entry.Name);
                            JenkIndex.Ensure(nlow);
                        }

                        if (!BuildExtendedJenkIndex) continue;
                        if (nlow.EndsWith(".ydr"))
                        {
                            string sname = nlow.Substring(0, nlow.Length - 4);
                            JenkIndex.Ensure(sname + "_lod");
                            JenkIndex.Ensure(sname + "_loda");
                            JenkIndex.Ensure(sname + "_lodb");
                        }
                        
                        if (nlow.EndsWith(".ydd"))
                        {
                            if (nlow.EndsWith("_children.ydd"))
                            {
                                string strn = nlow.Substring(0, nlow.Length - 13);
                                JenkIndex.Ensure(strn);
                                JenkIndex.Ensure(strn + "_lod");
                                JenkIndex.Ensure(strn + "_loda");
                                JenkIndex.Ensure(strn + "_lodb");
                            }
                            
                            int idx = nlow.LastIndexOf('_');
                            if (idx > 0)
                            {
                                string str1 = nlow.Substring(0, idx);
                                int idx2 = str1.LastIndexOf('_');
                                if (idx2 > 0)
                                {
                                    string str2 = str1.Substring(0, idx2);
                                    JenkIndex.Ensure(str2 + "_lod");
                                    const int maxi = 100;
                                    for (int i = 1; i <= maxi; i++)
                                    {
                                        string str3 = str2 + "_" + i.ToString().PadLeft(2, '0');
                                        JenkIndex.Ensure(str3 + "_lod");
                                    }
                                }
                            }
                        }
                        
                        if (nlow.EndsWith(".sps"))
                        {
                            JenkIndex.Ensure(nlow);
                        }
                        
                        if (nlow.EndsWith(".awc"))
                        {
                            string[] parts = entry.Path.Split('\\');
                            int pl = parts.Length;
                            if (pl > 2)
                            {
                                string fn = parts[pl - 1];
                                string fd = parts[pl - 2];
                                string hpath = fn.Substring(0, fn.Length - 4);
                                if (fd.EndsWith(".rpf"))
                                {
                                    fd = fd.Substring(0, fd.Length - 4);
                                }
                                hpath = fd + "/" + hpath;
                                if (parts[pl - 3] != "sfx")
                                { }//no hit

                                JenkIndex.Ensure(hpath);
                            }
                        }

                        if (!nlow.EndsWith(".nametable")) continue;
                        if (!(entry is RpfBinaryFileEntry binfe)) continue;
                        
                        byte[] data = file.ExtractFile(binfe);
                        if (data == null) continue;
                        
                        sb.Clear();
                        foreach (byte c in data)
                        {
                            if (c == 0)
                            {
                                string str = sb.ToString();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    string strl = str.ToLowerInvariant();
                                    JenkIndex.Ensure(strl);
                                }
                                sb.Clear();
                            }
                            else
                            {
                                sb.Append((char)c);
                            }
                        }
                    }

                }
                catch
                {
                    //ignore errors
                }
            }

            for (int i = 0; i < 100; i++)
            {
                JenkIndex.Ensure(i.ToString("00"));
            }
        }
    }
}
