using System;
using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using ImageMagick;

namespace FivemMapsFixer.Models;

public static class ImageHelper
{
    static ImageHelper()
    {
        MagickNET.Initialize();
    }

    private static MagickImage? GetImage(string path)
    {
        string ext = Path.GetExtension(path);

        if (ext != ".ytd") return new MagickImage(path);
        YtdFile ytd = new();
        ytd.Load(File.ReadAllBytes(path));

        if (ytd.TextureDict.Textures.Count == 0)
        {
            return null;
        }
        Texture? txt = ytd.TextureDict.Textures[0];
        byte[]? dds = DDSIO.GetDDSFile(txt);

        try
        {
            return new MagickImage(dds);   
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static int GetCorrectMipMapAmount(int width, int height)
    {
        int size = Math.Min(width, height);
        return (int)Math.Log(size, 2) - 1;
    }

    /*public static byte[]? Optimize(string path,bool resize)
    {
        YtdFile ytd = new()
        {
            TextureDict = new TextureDictionary()
        };
        MagickImage? img;
        
        try { img = GetImage(path); }
        catch { return null; }
        if (img == null) return null;
        if(img.Width == 0 || img.Height == 0) {return null;}
        
        img.Format = MagickFormat.Dds;
        
        //Resizing heavy images without consent
        ResizeBiggerThan(ref img, 2048);
        if (resize) { ResizeBiggerThan(ref img, 1024); }
        
        img.Settings.SetDefine(MagickFormat.Dds, "compression", "dxt3");
        img.Settings.SetDefine(MagickFormat.Dds, "cluster-fit", true);
        img.Settings.SetDefine(MagickFormat.Dds, "mipmaps", GetCorrectMipMapAmount(img.Width,img.Height));
        
        Texture? newTxt = DDSIO.GetTexture(img.ToByteArray());
        newTxt.Name = Path.GetFileName(path);
        ytd.TextureDict.BuildFromTextureList([newTxt]);

        byte[]? bytes = ytd.Save();
        img.Dispose();
        return bytes;
    }

    private static void ResizeBiggerThan(ref MagickImage img, int size)
    {
        if (img.Width <= size && img.Height <= size) return;
        if(img.Width> img.Height) { img.Resize(img.Width*(img.Height/size), size); }
        else { img.Resize(size, img.Height*(img.Width/size)); }
    }*/
}