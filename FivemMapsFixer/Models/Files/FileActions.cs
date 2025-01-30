using System;
using System.IO;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models;

public abstract class FileActions
{
    protected static RpfFileEntry CreateFileEntry(string name, string path, ref byte[] data)
    {
        //this should only really be used when loading a file from the filesystem.
        RpfFileEntry e;
        uint rsc7 = data.Length > 4 ? BitConverter.ToUInt32(data, 0) : 0;
        if (rsc7 == 0x37435352) //RSC7 header present! create RpfResourceFileEntry and decompress data...
        {
            e = RpfFile.CreateResourceFileEntry(ref data, 0);//"version" should be loadable from the header in the data..
            data = ResourceBuilder.Decompress(data);
        }
        else
        {
            RpfBinaryFileEntry be = new()
            {
                FileSize = (uint)data.Length
            };
            be.FileUncompressedSize = be.FileSize;
            e = be;
        }
        e.Name = name;
        e.NameLower = name.ToLowerInvariant();
        e.NameHash = JenkHash.GenHash(e.NameLower);
        e.ShortNameHash = JenkHash.GenHash(Path.GetFileNameWithoutExtension(e.NameLower));
        e.Path = path;
        return e;
    }
}