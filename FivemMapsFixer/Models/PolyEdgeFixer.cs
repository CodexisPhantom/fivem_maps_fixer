using System.IO;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models;

public class PolyEdgeFixer
{
    private void RunPolyEdge(string filename, string filetype)
    {
        byte[] oldData = File.ReadAllBytes(filename);
        byte[] newData = [];
        
        switch (filetype)
        {
            case "ydr":
            {
                YdrFile ydr = new();
                RpfFile.LoadResourceFile(ydr, oldData, 165);
                newData = ydr.Save();
                break;
            }
            case "ydd":
            {
                YddFile ydd = new();
                RpfFile.LoadResourceFile(ydd, oldData, 165);
                newData = ydd.Save();
                break;
            }
            case "yft":
            {
                YftFile yft = new();
                RpfFile.LoadResourceFile(yft, oldData, 162);
                newData = yft.Save();
                break;
            }
        }
        File.WriteAllBytes(filename, newData);
    }
}