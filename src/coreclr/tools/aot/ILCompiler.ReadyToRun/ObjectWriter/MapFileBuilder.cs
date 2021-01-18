// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.Diagnostics;

namespace ILCompiler.PEWriter
{
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

            public void AddNode(OutputNode node)
            {
                Debug.Assert(Name == node.Name);
                Count++;
                Length += node.Length;
            }
        }

        private OutputInfoBuilder _outputInfoBuilder;

        private long _fileSize;

        public MapFileBuilder(OutputInfoBuilder outputInfoBuilder)
        {
            _outputInfoBuilder = outputInfoBuilder;
        }

        public void SetFileSize(long fileSize)
        {
            _fileSize = fileSize;
        }

        public void SaveMap(string mapFileName)
        {
            Console.WriteLine("Emitting map file: {0}", mapFileName);

            _outputInfoBuilder.Sort();

            using (StreamWriter mapWriter = new StreamWriter(mapFileName))
            {
                WriteHeader(mapWriter);
                WriteNodeTypeStatistics(mapWriter);
                WriteRelocTypeStatistics(mapWriter);
                WriteSections(mapWriter);
                WriteMap(mapWriter);
            }
        }

        public void SaveCsv(string nodeStatsCsvFileName, string mapCsvFileName)
        {
            Console.WriteLine("Emitting csv files: {0}, {1}", nodeStatsCsvFileName, mapCsvFileName);

            _outputInfoBuilder.Sort();

            using (StreamWriter nodeStatsWriter = new StreamWriter(nodeStatsCsvFileName))
            {
                WriteNodeTypeStatisticsCsv(nodeStatsWriter);
            }

            using (StreamWriter mapCsvWriter = new StreamWriter(mapCsvFileName))
            {
                WriteMapCsv(mapCsvWriter);
            }
        }

        private void WriteHeader(StreamWriter writer)
        {
            WriteTitle(writer, "Summary Info");

            writer.WriteLine($"Output file size: {_fileSize,10}");
            writer.WriteLine($"Section count:    {_outputInfoBuilder.Sections.Count,10}");
            writer.WriteLine($"Node count:       {_outputInfoBuilder.Nodes.Count,10}");
            writer.WriteLine($"Symbol count:     {_outputInfoBuilder.Symbols.Count,10}");
            writer.WriteLine($"Relocation count: {_outputInfoBuilder.RelocCounts.Values.Sum(),10}");
        }

        private IEnumerable<NodeTypeStatistics> GetNodeTypeStatistics()
        {
            List<NodeTypeStatistics> nodeTypeStats = new List<NodeTypeStatistics>();
            Dictionary<string, int> statsNameIndex = new Dictionary<string, int>();
            foreach (OutputNode node in _outputInfoBuilder.Nodes)
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

            return nodeTypeStats;
        }

        private void WriteNodeTypeStatistics(StreamWriter writer)
        {
            IEnumerable<NodeTypeStatistics> nodeTypeStats = GetNodeTypeStatistics();

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

        private void WriteNodeTypeStatisticsCsv(StreamWriter writer)
        {
            IEnumerable<NodeTypeStatistics> nodeTypeStats = GetNodeTypeStatistics();

            writer.WriteLine("Length,% Of File,Average Size,Count,Node Type");
            foreach (NodeTypeStatistics nodeStats in nodeTypeStats)
            {
                writer.Write($"{nodeStats.Length},");
                writer.Write($"{(nodeStats.Length * 100.0 / _fileSize)},");
                writer.Write($"{(nodeStats.Length / (double)nodeStats.Count)},");
                writer.Write($"{nodeStats.Count},");
                writer.WriteLine(nodeStats.Name);
            }
        }

        private void WriteRelocTypeStatistics(StreamWriter writer)
        {
            KeyValuePair<RelocType, int>[] relocTypeCounts = _outputInfoBuilder.RelocCounts.ToArray();
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

            foreach (OutputNode node in _outputInfoBuilder.Nodes.Where(node => node.Relocations != 0).OrderByDescending(node => node.Relocations).Take(NumberOfTopNodesByRelocType))
            {
                writer.Write($"{node.Relocations,8} | ");
                if (_outputInfoBuilder.FindSymbol(node, out int symbolIndex))
                {
                    writer.Write($"{_outputInfoBuilder.Symbols[symbolIndex].Name}");
                }
                writer.WriteLine($"  ({node.Name})");
            }
        }

        private void WriteSections(StreamWriter writer)
        {
            WriteTitle(writer, "Section Map");
            WriteTitle(writer, "INDEX | FILEOFFSET | RVA        | END_RVA    | LENGTH     | NAME");
            for (int sectionIndex = 0; sectionIndex < _outputInfoBuilder.Sections.Count; sectionIndex++)
            {
                Section section = _outputInfoBuilder.Sections[sectionIndex];
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

            while (nodeIndex < _outputInfoBuilder.Nodes.Count || symbolIndex < _outputInfoBuilder.Symbols.Count)
            {
                if (nodeIndex >= _outputInfoBuilder.Nodes.Count
                    || symbolIndex < _outputInfoBuilder.Symbols.Count
                        && OutputItem.Comparer.Instance.Compare(_outputInfoBuilder.Symbols[symbolIndex], _outputInfoBuilder.Nodes[nodeIndex]) < 0)
                {
                    // No more nodes or next symbol is below next node - emit symbol
                    OutputSymbol symbol = _outputInfoBuilder.Symbols[symbolIndex++];
                    Section section = _outputInfoBuilder.Sections[symbol.SectionIndex];
                    writer.Write($"0x{symbol.Offset + section.RVAWhenPlaced:X8} | ");
                    writer.Write("         | ");
                    writer.Write("       | ");
                    writer.Write($"{GetNameHead(section),-SectionNameHeadLength} | ");
                    writer.WriteLine(symbol.Name);
                }
                else
                {
                    // Emit node and optionally symbol
                    OutputNode node = _outputInfoBuilder.Nodes[nodeIndex++];
                    Section section = _outputInfoBuilder.Sections[node.SectionIndex];

                    writer.Write($"0x{node.Offset + section.RVAWhenPlaced:X8} | ");
                    writer.Write($"0x{node.Length:X6} | ");
                    writer.Write($"{node.Relocations,6} | ");
                    writer.Write($"{GetNameHead(section),-SectionNameHeadLength} | ");
                    if (symbolIndex < _outputInfoBuilder.Symbols.Count && OutputItem.Comparer.Instance.Compare(node, _outputInfoBuilder.Symbols[symbolIndex]) == 0)
                    {
                        OutputSymbol symbol = _outputInfoBuilder.Symbols[symbolIndex++];
                        writer.Write($"{symbol.Name}");
                    }
                    writer.WriteLine($"  ({node.Name})");
                }
            }
        }

        private void WriteMapCsv(StreamWriter writer)
        {
            writer.WriteLine("Rva,Length,Relocs,Section,Symbol,Node Type");

            int nodeIndex = 0;
            int symbolIndex = 0;

            while (nodeIndex < _outputInfoBuilder.Nodes.Count || symbolIndex < _outputInfoBuilder.Symbols.Count)
            {
                if (nodeIndex >= _outputInfoBuilder.Nodes.Count
                    || symbolIndex < _outputInfoBuilder.Symbols.Count
                        && OutputItem.Comparer.Instance.Compare(_outputInfoBuilder.Symbols[symbolIndex], _outputInfoBuilder.Nodes[nodeIndex]) < 0)
                {
                    // No more nodes or next symbol is below next node - emit symbol
                    OutputSymbol symbol = _outputInfoBuilder.Symbols[symbolIndex++];
                    Section section = _outputInfoBuilder.Sections[symbol.SectionIndex];
                    writer.Write($"0x{symbol.Offset + section.RVAWhenPlaced:X8},");
                    writer.Write(",");
                    writer.Write(",");
                    writer.Write($"{section.Name},");
                    writer.Write(",");
                    writer.WriteLine(symbol.Name);
                }
                else
                {
                    // Emit node and optionally symbol
                    OutputNode node = _outputInfoBuilder.Nodes[nodeIndex++];
                    Section section = _outputInfoBuilder.Sections[node.SectionIndex];

                    writer.Write($"0x{node.Offset + section.RVAWhenPlaced:X8},");
                    writer.Write($"{node.Length},");
                    writer.Write($"{node.Relocations},");
                    writer.Write($"{section.Name},");
                    if (symbolIndex < _outputInfoBuilder.Symbols.Count && OutputItem.Comparer.Instance.Compare(node, _outputInfoBuilder.Symbols[symbolIndex]) == 0)
                    {
                        OutputSymbol symbol = _outputInfoBuilder.Symbols[symbolIndex++];
                        writer.Write($"{symbol.Name}");
                    }
                    writer.Write(",");
                    writer.WriteLine($"{node.Name}");
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
