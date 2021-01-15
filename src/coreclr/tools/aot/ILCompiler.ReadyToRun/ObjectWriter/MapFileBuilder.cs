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

        private readonly Dictionary<ISymbolDefinitionNode, MapFileNode> _nodeSymbolMap;
        private readonly Dictionary<ISymbolDefinitionNode, MethodWithGCInfo> _methodSymbolMap;

        private readonly Dictionary<RelocType, int> _relocCounts;

        private long _fileSize;

        public MapFileBuilder()
        {
            _nodes = new List<MapFileNode>();
            _symbols = new List<MapFileSymbol>();
            _sections = new List<Section>();

            _nodeSymbolMap = new Dictionary<ISymbolDefinitionNode, MapFileNode>();
            _methodSymbolMap = new Dictionary<ISymbolDefinitionNode, MethodWithGCInfo>();

            _relocCounts = new Dictionary<RelocType, int>();
        }

        public void AddNode(MapFileNode node, ISymbolDefinitionNode symbol)
        {
            _nodes.Add(node);
            _nodeSymbolMap.Add(symbol, node);
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

        public void AddMethod(MethodWithGCInfo method, ISymbolDefinitionNode symbol)
        {
            _methodSymbolMap.Add(symbol, method);
        }

        public void SetFileSize(long fileSize)
        {
            _fileSize = fileSize;
        }

        public void SaveMap(string mapFileName)
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

        public void SaveCsv(string nodeStatsCsvFileName, string mapCsvFileName)
        {
            Console.WriteLine("Emitting csv files: {0}, {1}", nodeStatsCsvFileName, mapCsvFileName);

            _nodes.Sort(MapFileItem.Comparer.Instance);
            _symbols.Sort(MapFileItem.Comparer.Instance);

            using (StreamWriter nodeStatsWriter = new StreamWriter(nodeStatsCsvFileName))
            {
                WriteNodeTypeStatisticsCsv(nodeStatsWriter);
            }

            using (StreamWriter mapCsvWriter = new StreamWriter(mapCsvFileName))
            {
                WriteMapCsv(mapCsvWriter);
            }
        }

        public void SavePdb(string pdbPath, string dllFileName)
        {
            Console.WriteLine("Emitting PDB file: {0}", Path.Combine(pdbPath, Path.GetFileNameWithoutExtension(dllFileName) + ".ni.pdb"));

            new PdbWriter(pdbPath, PDBExtraData.None).WritePDBData(dllFileName, EnumerateMethods());
        }

        public void SavePerfMap(string perfMapFileName)
        {
            Console.WriteLine("Emitting PerfMap file: {0}", perfMapFileName);

            PerfMapWriter.Write(perfMapFileName, EnumerateMethods());
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

        private IEnumerable<NodeTypeStatistics> GetNodeTypeStatistics()
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

        private void WriteMapCsv(StreamWriter writer)
        {
            writer.WriteLine("Rva,Length,Relocs,Section,Symbol,Node Type");

            int nodeIndex = 0;
            int symbolIndex = 0;

            while (nodeIndex < _nodes.Count || symbolIndex < _symbols.Count)
            {
                if (nodeIndex >= _nodes.Count || symbolIndex < _symbols.Count && MapFileItem.Comparer.Instance.Compare(_symbols[symbolIndex], _nodes[nodeIndex]) < 0)
                {
                    // No more nodes or next symbol is below next node - emit symbol
                    MapFileSymbol symbol = _symbols[symbolIndex++];
                    Section section = _sections[symbol.SectionIndex];
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
                    MapFileNode node = _nodes[nodeIndex++];
                    Section section = _sections[node.SectionIndex];

                    writer.Write($"0x{node.Offset + section.RVAWhenPlaced:X8},");
                    writer.Write($"{node.Length},");
                    writer.Write($"{node.Relocations},");
                    writer.Write($"{section.Name},");
                    if (symbolIndex < _symbols.Count && MapFileItem.Comparer.Instance.Compare(node, _symbols[symbolIndex]) == 0)
                    {
                        MapFileSymbol symbol = _symbols[symbolIndex++];
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

        private IEnumerable<MethodInfo> EnumerateMethods()
        {
            DebugNameFormatter nameFormatter = new DebugNameFormatter();
            TypeNameFormatter typeNameFormatter = new TypeString();
            HashSet<MethodDesc> emittedMethods = new HashSet<MethodDesc>();
            foreach (KeyValuePair<ISymbolDefinitionNode, MethodWithGCInfo> symbolMethodPair in _methodSymbolMap)
            {
                EcmaMethod ecmaMethod = symbolMethodPair.Value.Method.GetTypicalMethodDefinition() as EcmaMethod;
                if (ecmaMethod != null && emittedMethods.Add(ecmaMethod))
                {
                    MethodInfo methodInfo = new MethodInfo();
                    methodInfo.MethodToken = (uint)MetadataTokens.GetToken(ecmaMethod.Handle);
                    methodInfo.AssemblyName = ecmaMethod.Module.Assembly.GetName().Name;
                    methodInfo.Name = FormatMethodName(symbolMethodPair.Value.Method, typeNameFormatter);
                    MapFileNode node = _nodeSymbolMap[symbolMethodPair.Key];
                    Section section = _sections[node.SectionIndex];
                    methodInfo.HotRVA = (uint)(section.RVAWhenPlaced + node.Offset);
                    methodInfo.HotLength = (uint)node.Length;
                    methodInfo.ColdRVA = 0;
                    methodInfo.ColdLength = 0;
                    yield return methodInfo;
                }
            }
        }

        private string FormatMethodName(MethodDesc method, TypeNameFormatter typeNameFormatter)
        {
            StringBuilder output = new StringBuilder();
            if (!method.Signature.ReturnType.IsVoid)
            {
                output.Append(typeNameFormatter.FormatName(method.Signature.ReturnType));
                output.Append(" ");
            }
            output.Append(typeNameFormatter.FormatName(method.OwningType));
            output.Append("::");
            output.Append(method.Name);
            output.Append("(");
            for (int paramIndex = 0; paramIndex < method.Signature.Length; paramIndex++)
            {
                if (paramIndex != 0)
                {
                    output.Append(", ");
                }
                output.Append(typeNameFormatter.FormatName(method.Signature[paramIndex]));
            }
            output.Append(")");
            return output.ToString();
        }
    }
}