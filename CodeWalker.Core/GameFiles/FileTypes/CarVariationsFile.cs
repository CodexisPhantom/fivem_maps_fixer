﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using TC = System.ComponentModel.TypeConverterAttribute;
using EXP = System.ComponentModel.ExpandableObjectConverter;

namespace CodeWalker.GameFiles
{
    [TC(typeof(EXP))] public class CarVariationsFile : GameFile, PackedFile
    {
        public PsoFile Pso { get; set; }
        public string Xml { get; set; }

        public CVehicleModelInfoVariation VehicleModelInfo { get; set; }

        public CarVariationsFile() : base(null, GameFileType.CarVariations)
        { }
        public CarVariationsFile(RpfFileEntry entry) : base(entry, GameFileType.CarVariations)
        {
        }

        public void Load(byte[] data, RpfFileEntry entry)
        {
            RpfFileEntry = entry;
            Name = entry.Name;
            FilePath = Name;


            //can be PSO .ymt or XML .meta
            MemoryStream ms = new MemoryStream(data);
            if (PsoFile.IsPSO(ms))
            {
                Pso = new PsoFile();
                Pso.Load(data);
                Xml = PsoXml.GetXml(Pso); //yep let's just convert that to XML :P
            }
            else
            {
                Xml = TextUtil.GetUTF8Text(data);
            }

            XmlDocument xdoc = new XmlDocument();
            if (!string.IsNullOrEmpty(Xml))
            {
                try
                {
                    xdoc.LoadXml(Xml);
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                }
            }
            else
            { }


            if (xdoc.DocumentElement != null)
            {
                VehicleModelInfo = new CVehicleModelInfoVariation(xdoc.DocumentElement);
            }


            Loaded = true;
        }
    }

    [TC(typeof(EXP))] public class CVehicleModelInfoVariation
    {
        public CVehicleModelInfoVariation_418053801[] variationData { get; set; }

        public CVehicleModelInfoVariation(XmlNode node)
        {
            var cnode = node.SelectSingleNode("variationData");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    variationData = new CVehicleModelInfoVariation_418053801[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        variationData[i] = new CVehicleModelInfoVariation_418053801(items[i]);
                    }
                }
            }
        }
    }
    [TC(typeof(EXP))] public class CVehicleModelInfoVariation_418053801
    {
        public string modelName { get; set; }
        public CVehicleModelInfoVariation_2575850962[] colors { get; set; }
        public MetaHash[] kits { get; set; }
        public MetaHash[] windowsWithExposedEdges { get; set; }
        public PlateProbabilities plateProbabilities { get; set; }
        public byte lightSettings { get; set; }
        public byte sirenSettings { get; set; }

        public CVehicleModelInfoVariation_418053801(XmlNode node)
        {
            modelName = Xml.GetChildInnerText(node, "modelName");
            var cnode = node.SelectSingleNode("colors");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    colors = new CVehicleModelInfoVariation_2575850962[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        colors[i] = new CVehicleModelInfoVariation_2575850962(items[i]);
                    }
                }
            }
            cnode = node.SelectSingleNode("kits");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    kits = new MetaHash[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        kits[i] = XmlMeta.GetHash(items[i].InnerText);
                    }
                }
            }
            cnode = node.SelectSingleNode("windowsWithExposedEdges");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    windowsWithExposedEdges = new MetaHash[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        windowsWithExposedEdges[i] = XmlMeta.GetHash(items[i].InnerText);
                    }
                }
            }
            cnode = node.SelectSingleNode("plateProbabilities");
            if (cnode != null)
            {
                plateProbabilities = new PlateProbabilities(cnode);
            }
            lightSettings = (byte)Xml.GetChildIntAttribute(node, "lightSettings", "value");
            sirenSettings = (byte)Xml.GetChildIntAttribute(node, "sirenSettings", "value");
        }

        public override string ToString()
        {
            return modelName;
        }
    }
    [TC(typeof(EXP))] public class CVehicleModelInfoVariation_2575850962
    {
        public byte[] indices { get; set; }
        public bool[] liveries { get; set; }

        public CVehicleModelInfoVariation_2575850962(XmlNode node)
        {
            var cnode = node.SelectSingleNode("indices");
            if (cnode != null)
            {
                string astr = cnode.InnerText;
                string[] arrr = astr.Split(new[] { '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                List<byte> alist = new List<byte>();
                foreach (string item in arrr)
                {
                    string titem = item.Trim();
                    byte v;
                    if (byte.TryParse(titem, out v))
                    {
                        alist.Add(v);
                    }
                }
                indices = alist.ToArray();
            }
            cnode = node.SelectSingleNode("liveries");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    liveries = new bool[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        liveries[i] = Xml.GetBoolAttribute(items[i], "value");
                    }
                }
                else
                {
                    string astr = cnode.InnerText;
                    string[] arrr = astr.Split(new[] { '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    List<bool> alist = new List<bool>();
                    foreach (string item in arrr)
                    {
                        string titem = item.Trim();
                        byte v;
                        if (byte.TryParse(titem, out v))
                        {
                            alist.Add(v > 0);
                        }
                    }
                    liveries = alist.ToArray();
                }
            }


        }
    }
    [TC(typeof(EXP))] public class PlateProbabilities
    {
        public PlateProbabilities_938618322[] Probabilities { get; set; }

        public PlateProbabilities(XmlNode node)
        {
            var cnode = node.SelectSingleNode("Probabilities");
            if (cnode != null)
            {
                XmlNodeList items = cnode.SelectNodes("Item");
                if (items.Count > 0)
                {
                    Probabilities = new PlateProbabilities_938618322[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        Probabilities[i] = new PlateProbabilities_938618322(items[i]);
                    }
                }
            }
        }
    }
    [TC(typeof(EXP))] public class PlateProbabilities_938618322
    {
        public MetaHash Name { get; set; }
        public uint Value { get; set; }

        public PlateProbabilities_938618322(XmlNode node)
        {
            Name = XmlMeta.GetHash(Xml.GetChildInnerText(node, "Name"));
            Value = Xml.GetChildUIntAttribute(node, "Value", "value");
        }

        public override string ToString()
        {
            return Name.ToString() + ": " + Value.ToString();
        }
    }

}
