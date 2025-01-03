﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;

/*
    Copyright(c) 2017 Neodymium
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/


//ruthlessly stolen


namespace CodeWalker.GameFiles
{

    [TypeConverter(typeof(ExpandableObjectConverter))] public class FrameFilterDictionary : ResourceFileBase
    {
        // pgDictionaryBase
        // pgDictionary<crFrameFilter>
        public override long BlockLength => 0x40;

        // structure data
        public uint Unknown_10h { get; set; } // 0x00000000
        public uint Unknown_14h { get; set; } // 0x00000000
        public uint Unknown_18h { get; set; } = 1; // 0x00000001
        public uint Unknown_1Ch { get; set; } // 0x00000000
        public ResourceSimpleList64_s<MetaHash> FilterNameHashes { get; set; }
        public ResourcePointerList64<FrameFilterBase> Filters { get; set; }

        /// <summary>
        /// Reads the data-block from a stream.
        /// </summary>
        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            this.Unknown_10h = reader.ReadUInt32();
            this.Unknown_14h = reader.ReadUInt32();
            this.Unknown_18h = reader.ReadUInt32();
            this.Unknown_1Ch = reader.ReadUInt32();
            this.FilterNameHashes = reader.ReadBlock<ResourceSimpleList64_s<MetaHash>>();
            this.Filters = reader.ReadBlock<ResourcePointerList64<FrameFilterBase>>();

            if (Filters?.data_items != null)
            {
                for (int i = 0; i < Filters.data_items.Length; i++)
                {
                    MetaHash h = ((FilterNameHashes?.data_items != null) && (i < FilterNameHashes.data_items.Length)) ? FilterNameHashes.data_items[i] : 0;
                    if (Filters.data_items[i] != null)
                    {
                        Filters.data_items[i].NameHash = h;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the data-block to a stream.
        /// </summary>
        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // write structure data
            writer.Write(this.Unknown_10h);
            writer.Write(this.Unknown_14h);
            writer.Write(this.Unknown_18h);
            writer.Write(this.Unknown_1Ch);
            writer.WriteBlock(this.FilterNameHashes);
            writer.WriteBlock(this.Filters);
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            return new Tuple<long, IResourceBlock>[] {
                new Tuple<long, IResourceBlock>(0x20, FilterNameHashes),
                new Tuple<long, IResourceBlock>(0x30, Filters)
            };
        }

        public void WriteXml(StringBuilder sb, int indent)
        {
            if (Filters?.data_items != null)
            {
                foreach (FrameFilterBase filter in Filters.data_items)
                {
                    YfdXml.OpenTag(sb, indent, "Item");
                    filter.WriteXml(sb, indent + 1);
                    YfdXml.CloseTag(sb, indent, "Item");
                }
            }

        }
        public void ReadXml(XmlNode node)
        {
            List<FrameFilterBase> filters = new List<FrameFilterBase>();

            XmlNodeList inodes = node.SelectNodes("Item");
            if (inodes != null)
            {
                foreach (XmlNode inode in inodes)
                {
                    // frame filters are polymorphic but this is the only type used in the files
                    FrameFilterMultiWeight filter = new FrameFilterMultiWeight();
                    filter.ReadXml(inode);
                    filters.Add(filter);
                }
            }

            BuildFromFilterList(filters);
        }
        public static void WriteXmlNode(FrameFilterDictionary d, StringBuilder sb, int indent, string name = "FrameFilterDictionary")
        {
            if (d == null) return;
            if ((d.Filters?.data_items == null) || (d.Filters.data_items.Length == 0))
            {
                YfdXml.SelfClosingTag(sb, indent, name);
            }
            else
            {
                YfdXml.OpenTag(sb, indent, name);
                d.WriteXml(sb, indent + 1);
                YfdXml.CloseTag(sb, indent, name);
            }
        }

        public void BuildFromFilterList(List<FrameFilterBase> filters)
        {
            filters.Sort((a, b) => a.NameHash.Hash.CompareTo(b.NameHash.Hash));

            List<MetaHash> namehashes = new List<MetaHash>();
            foreach (FrameFilterBase f in filters)
            {
                namehashes.Add(f.NameHash);
            }

            FilterNameHashes = new ResourceSimpleList64_s<MetaHash>();
            FilterNameHashes.data_items = namehashes.ToArray();
            Filters = new ResourcePointerList64<FrameFilterBase>();
            Filters.data_items = filters.ToArray();
        }
    }

    public enum FrameFilterType : uint
    {
        Bone = 1,
        BoneBasic = 2,
        BoneMultiWeight = 3,
        MultiWeight = 4, // only type used in .yfd files
        TrackMultiWeight = 5,
        Mover = 6,
    }

    [TypeConverter(typeof(ExpandableObjectConverter))] public class FrameFilterBase : ResourceSystemBlock, IResourceXXSystemBlock//, IMetaXmlItem
    {
        // rage::crFrameFilter
        public override long BlockLength => 0x18;

        // structure data
        public uint VFT { get; set; }
        public uint Unknown_04h { get; set; } // 0x00000001
        public uint RefCount { get; set; } = 1; // 0x00000001
        public uint Signature { get; set; }
        public FrameFilterType Type { get; set; }
        public uint Unknown_14h { get; set; } // 0x00000000


        public MetaHash NameHash { get; set; }

        /// <summary>
        /// Reads the data-block from a stream.
        /// </summary>
        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            // read structure data
            this.VFT = reader.ReadUInt32();
            this.Unknown_04h = reader.ReadUInt32();
            this.RefCount = reader.ReadUInt32();
            this.Signature = reader.ReadUInt32();
            this.Type = (FrameFilterType)reader.ReadUInt32();
            this.Unknown_14h = reader.ReadUInt32();
        }

        /// <summary>
        /// Writes the data-block to a stream.
        /// </summary>
        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // write structure data
            writer.Write(this.VFT);
            writer.Write(this.Unknown_04h);
            writer.Write(this.RefCount);
            writer.Write(this.Signature);
            writer.Write((uint)this.Type);
            writer.Write(this.Unknown_14h);
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            return Array.Empty<Tuple<long, IResourceBlock>>();
        }

        public IResourceSystemBlock GetType(ResourceDataReader reader, params object[] parameters)
        {
            reader.Position += 0x10;
            uint type = reader.ReadUInt32();
            reader.Position -= 0x14;

            return ConstructFilter((FrameFilterType)type);
        }

        public static FrameFilterBase ConstructFilter(FrameFilterType type)
        {
            switch (type)
            {
                case FrameFilterType.MultiWeight: return new FrameFilterMultiWeight();
                default: return null; // throw new Exception("Unknown type");
            }
        }

        public virtual void WriteXml(StringBuilder sb, int indent)
        {
            YfdXml.StringTag(sb, indent, "Name", YfdXml.HashString(NameHash));
        }

        public virtual void ReadXml(XmlNode node)
        {
            NameHash = XmlMeta.GetHash(Xml.GetChildInnerText(node, "Name"));
        }

        public virtual uint CalculateSignature()
        {
            return 0;
        }

        public override string ToString()
        {
            return NameHash + " (" + Type + ")";
        }

        // used to calculate filter signatures
        public static uint Crc32Hash(byte[] data, uint seed = 0)
        {
            if (data == null) return 0;
            uint h = ~seed;
            for (int i = 0; i < data.Length; i++)
            {
                h = Crc32Table[data[i] ^ (h & 0xFF)] ^ (h >> 8);
            }
            return ~h;
        }

        private static readonly uint[] Crc32Table = new uint[256]
        {
            0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419,
            0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4,
            0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07,
            0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
            0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856,
            0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
            0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4,
            0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
            0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
            0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A,
            0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599,
            0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
            0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190,
            0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F,
            0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E,
            0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
            0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED,
            0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
            0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3,
            0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
            0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A,
            0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5,
            0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010,
            0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
            0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17,
            0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6,
            0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
            0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
            0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344,
            0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
            0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A,
            0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
            0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1,
            0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C,
            0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF,
            0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
            0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE,
            0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31,
            0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C,
            0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
            0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B,
            0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
            0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1,
            0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
            0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
            0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7,
            0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66,
            0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
            0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605,
            0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8,
            0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B,
            0x2D02EF8D
        };
    }
    [TypeConverter(typeof(ExpandableObjectConverter))] public class FrameFilterMultiWeight : FrameFilterBase
    {
        // rage::crFrameFilterMultiWeight
        public override long BlockLength => 0x40;
        
        public ResourceSimpleList64_s<TrackIdIndex> Entries { get; set; } // sorted by (BoneId | (Track << 16))
        public ResourceSimpleList64_float Weights { get; set; }
        public ulong Unknown_38h { get; set; } // 0

        public FrameFilterMultiWeight()
        {
            Type = FrameFilterType.MultiWeight;
        }

        /// <summary>
        /// Reads the data-block from a stream.
        /// </summary>
        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);
            // read structure data
            this.Entries = reader.ReadBlock<ResourceSimpleList64_s<TrackIdIndex>>();
            this.Weights = reader.ReadBlock<ResourceSimpleList64_float>();
            this.Unknown_38h = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the data-block to a stream.
        /// </summary>
        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);
            // write structure data
            writer.WriteBlock(this.Entries);
            writer.WriteBlock(this.Weights);
            writer.Write(this.Unknown_38h);
        }

        public override Tuple<long, IResourceBlock>[] GetParts()
        {
            return new Tuple<long, IResourceBlock>[] {
                new Tuple<long, IResourceBlock>(0x18, Entries),
                new Tuple<long, IResourceBlock>(0x28, Weights)
            };
        }

        public override void WriteXml(StringBuilder sb, int indent)
        {
            base.WriteXml(sb, indent);
            YfdXml.WriteItemArray(sb, Entries?.data_items, indent, "Entries");
            YfdXml.WriteRawArray(sb, Weights?.data_items, indent, "Weights", "", FloatUtil.ToString);
        }

        public override void ReadXml(XmlNode node)
        {
            base.ReadXml(node);
            Unknown_38h = 0;

            Entries = new ResourceSimpleList64_s<TrackIdIndex>();
            Entries.data_items = XmlMeta.ReadItemArray<TrackIdIndex>(node, "Entries");

            Weights = new ResourceSimpleList64_float();
            Weights.data_items = Xml.GetChildRawFloatArray(node, "Weights");

            SortEntries();
            Signature = CalculateSignature();
        }

        public void SortEntries()
        {
            if (Entries?.data_items == null)
            {
                return;
            }

            Array.Sort(Entries.data_items, (x, y) => x.GetSortKey().CompareTo(y.GetSortKey()));
        }

        public override uint CalculateSignature()
        {
            // CRC-32 hash of the Entries and Weights arrays
            uint s = 0;
            if (Entries?.data_items != null && Entries?.data_items.Length > 0)
            {
                byte[] data = MetaTypes.ConvertArrayToBytes(Entries.data_items);
                s = Crc32Hash(data, s);
            }
            if (Weights?.data_items != null && Weights?.data_items.Length > 0)
            {
                byte[] data = MetaTypes.ConvertArrayToBytes(Weights.data_items);
                s = Crc32Hash(data, s);
            }
            return s;
        }

        public struct TrackIdIndex : IMetaXmlItem
        {
            public byte Unknown_00h { get; set; } // 0
            public byte Track { get; set; } // rage::crTrack
            public ushort BoneId { get; set; } // rage::crId
            public uint WeightIndex { get; set; }

            public override string ToString()
            {
                return BoneId + ", " + Track + ": " + WeightIndex;
            }

            public uint GetSortKey() => (uint)(BoneId | (Track << 16));

            public void WriteXml(StringBuilder sb, int indent)
            {
                YfdXml.ValueTag(sb, indent, "Track", Track.ToString());
                YfdXml.ValueTag(sb, indent, "BoneId", BoneId.ToString());
                YfdXml.ValueTag(sb, indent, "WeightIndex", WeightIndex.ToString());
            }

            public void ReadXml(XmlNode node)
            {
                Unknown_00h = 0;
                Track = (byte)Xml.GetChildUIntAttribute(node, "Track");
                BoneId = (ushort)Xml.GetChildUIntAttribute(node, "BoneId");
                WeightIndex = Xml.GetChildUIntAttribute(node, "WeightIndex");
            }
        }
    }
}
