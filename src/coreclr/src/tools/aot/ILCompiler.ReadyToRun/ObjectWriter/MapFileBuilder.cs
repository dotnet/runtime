// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Base class for symbols and nodes in the map file implements common logic
    /// for section / offset ordering.
    /// </summary>
    public class MapFileItem
    {
        public class Comparer : IComparer<MapFileItem>
        {
            public readonly static Comparer Instance = new Comparer();

            public int Compare([AllowNull] MapFileItem x, [AllowNull] MapFileItem y)
            {
                return (x.SectionIndex != y.SectionIndex ? x.SectionIndex.CompareTo(y.SectionIndex) : x.Offset.CompareTo(y.Offset));
            }
        }

        /// <summary>
        /// Item section index
        /// </summary>
        public readonly int SectionIndex;

        /// <summary>
        /// Offset relative to section beginning
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// Item name
        /// </summary>
        public readonly string Name;

        public MapFileItem(int sectionIndex, int offset, string name)
        {
            SectionIndex = sectionIndex;
            Offset = offset;
            Name = name;
        }
    }

    /// <summary>
    /// This class represents a single node (contiguous block of data) in the output R2R PE file.
    /// </summary>
    public class MapFileNode : MapFileItem
    {
        /// <summary>
        /// Node length (number of bytes). This doesn't include any external alignment
        /// applied when concatenating the nodes to form sections.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Number of file-level relocations (.reloc section entries) used by the node.
        /// </summary>
        public int Relocations { get; private set; }

        public MapFileNode(int sectionIndex, int offset, int length, string name)
            : base(sectionIndex, offset, name)
        {
            Length = length;
            Relocations = 0;
        }

        public void AddRelocation()
        {
            Relocations++;
        }
    }

    /// <summary>
    /// Symbol is a "pointer" into the PE file. Most (but not all) symbols correspond to
    /// node beginnings (most nodes have a "start symbol" representing the beginning
    /// of the node).
    /// </summary>
    public class MapFileSymbol : MapFileItem
    {
        public MapFileSymbol(int sectionIndex, int offset, string name)
            : base(sectionIndex, offset, name)
        {
        }
    }

    /// <summary>
    /// Helper class used to collect information to be output into the map file.
    /// </summary>
    public class MapFileBuilder
    {
        /// <summary>
        /// Number of first characters of the section name to retain in the symbol &amp; node map.
        /// </summary>
        private const int SectionNameHeadLength = 7;

        /// <summary>
        /// Statistic information for a single node type.
        /// </summary>
        private class NodeTypeStatistics
        {
            public readonly string Name;

            public int Count;
            public int Length;

            public NodeTypeStatistics(string name)
            {
                Name = name;
            }

            public void AddNode(MapFileNode node)
            {
                Debug.Assert(Name == node.Name);
                Count++;
                Length += node.Length;
            }
        }

        private readonly List<MapFileNode> _nodes;
        private readonly List<MapFileSymbol> _symbols;
        private readonly List<Section> _sections;

        private readonly Dictionary<RelocType, int> _relocCounts;

        private long _fileSize;

        public MapFileBuilder()
        {
            _nodes = new List<MapFileNode>();
            _symbols = new List<MapFileSymbol>();
            _sections = new List<Section>();

            _relocCounts = new Dictionary<RelocType, int>();
        }

        public void AddNode(MapFileNode node)
        {
            _nodes.Add(node);
        }

        public void AddRelocation(MapFileNode node, RelocType relocType)
        {
            node.AddRelocation();
            _relocCounts.TryGetValue(relocType, out int relocTypeCount);
            _relocCounts[relocType] = relocTypeCount + 1;
        }

        public void AddSymbol(MapFileSymbol symbol)
        {
            _symbols.Add(symbol);
        }

        public void AddSection(Section section)
        {
            _sections.Add(section);
        }

        public void SetFileSize(long fileSize)
        {
            _fileSize = fileSize;
        }

        public void Save(string mapFileName)
        {
            Console.WriteLine("Emitting map file: {0}", mapFileName);

            _nodes.Sort(MapFileItem.Comparer.Instance);
            _symbols.Sort(MapFileItem.Comparer.Instance);

            using (StreamWriter mapWriter = new StreamWriter(mapFileName))
            {
                WriteHeader(mapWriter);
                WriteNodeTypeStatistics(mapWriter);
                WriteRelocTypeStatistics(mapWriter);
                WriteSections(mapWriter);
                WriteMap(mapWriter);
            }
        }

        private void WriteHeader(StreamWriter writer)
        {
            WriteTitle(writer, "Summary Info");

            writer.WriteLine($"Output file size: {_fileSize,10}");
            writer.WriteLine($"Section count:    {_sections.Count,10}");
            writer.WriteLine($"Node count:       {_nodes.Count,10}");
            writer.WriteLine($"Symbol count:     {_symbols.Count,10}");
            writer.WriteLine($"Relocation count: {_relocCounts.Values.Sum(),10}");
        }

        private void WriteNodeTypeStatistics(StreamWriter writer)
        {
            List<NodeTypeStatistics> nodeTypeStats = new List<NodeTypeStatistics>();
            Dictionary<string, int> statsNameIndex = new Dictionary<string, int>();
            foreach (MapFileNode node in _nodes)
            {
                if (!statsNameIndex.TryGetValue(node.Name, out int statsIndex))
                {
                    statsIndex = nodeTypeStats.Count;
                    nodeTypeStats.Add(new NodeTypeStatistics(node.Name));
                    statsNameIndex.Add(node.Name, statsIndex);
                }
                nodeTypeStats[statsIndex].AddNode(node);
            }
            nodeTypeStats.Sort((a, b) => b.Length.CompareTo(a.Length));

            WriteTitle(writer, "Node Type Statistics");
            WriteTitle(writer, "    LENGTH |   %FILE |    AVERAGE |  COUNT | NODETYPE");
            foreach (NodeTypeStatistics nodeStats in nodeTypeStats)
            {
                writer.Write($"{nodeStats.Length,10} | ");
                writer.Write($"{(nodeStats.Length * 100.0 / _fileSize),7:F3} | ");
                writer.Write($"{(nodeStats.Length / (double)nodeStats.Count),10:F1} | ");
                writer.Write($"{nodeStats.Count,6} | ");
                writer.WriteLine(nodeStats.Name);
            }
        }

        private void WriteRelocTypeStatistics(StreamWriter writer)
        {
            KeyValuePair<RelocType, int>[] relocTypeCounts = _relocCounts.ToArray();
            Array.Sort(relocTypeCounts, (a, b) => b.Value.CompareTo(a.Value));

            WriteTitle(writer, "Reloc Type Statistics");
            WriteTitle(writer, "   COUNT | RELOC_TYPE");
            foreach (KeyValuePair<RelocType, int> relocTypeCount in relocTypeCounts)
            {
                writer.Write($"{relocTypeCount.Value,8} | ");
                writer.WriteLine(relocTypeCount.Key.ToString());
            }

            const int NumberOfTopNodesByRelocType = 10;

            WriteTitle(writer, "Top Nodes By Relocation Count");
            WriteTitle(writer, "   COUNT | SYMBOL  (NODE)");

            foreach (MapFileNode node in _nodes.Where(node => node.Relocations != 0).OrderByDescending(node => node.Relocations).Take(NumberOfTopNodesByRelocType))
            {
                writer.Write($"{node.Relocations,8} | ");
                int symbolIndex = _symbols.BinarySearch(new MapFileSymbol(node.SectionIndex, node.Offset, name: null), MapFileItem.Comparer.Instance);
                if (symbolIndex >= 0 && symbolIndex < _symbols.Count && MapFileItem.Comparer.Instance.Compare(_symbols[symbolIndex], node) == 0)
                {
                    writer.Write($"{_symbols[symbolIndex].Name}");
                }
                writer.WriteLine($"  ({node.Name})");
            }
        }

        private void WriteSections(StreamWriter writer)
        {
            WriteTitle(writer, "Section Map");
            WriteTitle(writer, "INDEX | FILEOFFSET | RVA        | END_RVA    | LENGTH     | NAME");
            for (int sectionIndex = 0; sectionIndex < _sections.Count; sectionIndex++)
            {
                Section section = _sections[sectionIndex];
                writer.Write($"{sectionIndex,5} | ");
                writer.Write($"0x{section.FilePosWhenPlaced:X8} | ");
                writer.Write($"0x{section.RVAWhenPlaced:X8} | ");
                writer.Write($"0x{(section.RVAWhenPlaced + section.Content.Count):X8} | ");
                writer.Write($"0x{section.Content.Count:X8} | ");
                writer.WriteLine(section.Name);
            }
        }

        private void WriteMap(StreamWriter writer)
        {
            WriteTitle(writer, "Node & Symbol Map");
            WriteTitle(writer, "RVA        | LENGTH   | RELOCS | SECTION | SYMBOL (NODE)");

            int nodeIndex = 0;
            int symbolIndex = 0;

            while (nodeIndex < _nodes.Count || symbolIndex < _symbols.Count)
            {
                if (nodeIndex >= _nodes.Count || symbolIndex < _symbols.Count && MapFileItem.Comparer.Instance.Compare(_symbols[symbolIndex], _nodes[nodeIndex]) < 0)
                {
                    // No more nodes or next symbol is below next node - emit symbol
                    MapFileSymbol symbol = _symbols[symbolIndex++];
                    Section section = _sections[symbol.SectionIndex];
                    writer.Write($"0x{symbol.Offset + section.RVAWhenPlaced:X8} | ");
                    writer.Write("         | ");
                    writer.Write("       | ");
                    writer.Write($"{GetNameHead(section),-SectionNameHeadLength} | ");
                    writer.WriteLine(symbol.Name);
                }
                else
                {
                    // Emit node and optionally symbol
                    MapFileNode node = _nodes[nodeIndex++];
                    Section section = _sections[node.SectionIndex];

                    writer.Write($"0x{node.Offset + section.RVAWhenPlaced:X8} | ");
                    writer.Write($"0x{node.Length:X6} | ");
                    writer.Write($"{node.Relocations,6} | ");
                    writer.Write($"{GetNameHead(section),-SectionNameHeadLength} | ");
                    if (symbolIndex < _symbols.Count && MapFileItem.Comparer.Instance.Compare(node, _symbols[symbolIndex]) == 0)
                    {
                        MapFileSymbol symbol = _symbols[symbolIndex++];
                        writer.Write($"{symbol.Name}");
                    }
                    writer.WriteLine($"  ({node.Name})");
                }
            }
        }

        private static string GetNameHead(Section section)
        {
            string sectionNameHead = section.Name;
            if (sectionNameHead.Length > SectionNameHeadLength)
            {
                sectionNameHead = sectionNameHead.Substring(0, SectionNameHeadLength);
            }
            return sectionNameHead;
        }

        private void WriteTitle(StreamWriter writer, string title)
        {
            writer.WriteLine();
            writer.WriteLine(title);
            writer.WriteLine(new string('-', title.Length));
        }
    }
}
