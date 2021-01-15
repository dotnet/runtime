// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.Diagnostics;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Base class for symbols and nodes in the output file implements common logic
    /// for section / offset ordering.
    /// </summary>
    public class ObjectItem
    {
        public class Comparer : IComparer<ObjectItem>
        {
            public readonly static Comparer Instance = new Comparer();

            public int Compare([AllowNull] ObjectItem x, [AllowNull] ObjectItem y)
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

        public ObjectItem(int sectionIndex, int offset, string name)
        {
            SectionIndex = sectionIndex;
            Offset = offset;
            Name = name;
        }
    }

    /// <summary>
    /// This class represents a single node (contiguous block of data) in the output R2R PE file.
    /// </summary>
    public class ObjectNode : ObjectItem
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

        public ObjectNode(int sectionIndex, int offset, int length, string name)
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
    public class ObjectSymbol : ObjectItem
    {
        public ObjectSymbol(int sectionIndex, int offset, string name)
            : base(sectionIndex, offset, name)
        {
        }
    }

    /// <summary>
    /// Common class used to collect information to use when emitting map files and symbol files.
    /// </summary>
    public class ObjectInfoBuilder
    {
        private readonly List<ObjectNode> _nodes;
        private readonly List<ObjectSymbol> _symbols;
        private readonly List<Section> _sections;

        private readonly Dictionary<ISymbolDefinitionNode, ObjectNode> _nodeSymbolMap;
        private readonly Dictionary<ISymbolDefinitionNode, MethodWithGCInfo> _methodSymbolMap;

        private readonly Dictionary<RelocType, int> _relocCounts;

        public ObjectInfoBuilder()
        {
            _nodes = new List<ObjectNode>();
            _symbols = new List<ObjectSymbol>();
            _sections = new List<Section>();

            _nodeSymbolMap = new Dictionary<ISymbolDefinitionNode, ObjectNode>();
            _methodSymbolMap = new Dictionary<ISymbolDefinitionNode, MethodWithGCInfo>();

            _relocCounts = new Dictionary<RelocType, int>();
        }

        public void AddNode(ObjectNode node, ISymbolDefinitionNode symbol)
        {
            _nodes.Add(node);
            _nodeSymbolMap.Add(symbol, node);
        }

        public void AddRelocation(ObjectNode node, RelocType relocType)
        {
            node.AddRelocation();
            _relocCounts.TryGetValue(relocType, out int relocTypeCount);
            _relocCounts[relocType] = relocTypeCount + 1;
        }

        public void AddSymbol(ObjectSymbol symbol)
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

        public void Sort()
        {
            _nodes.Sort(ObjectItem.Comparer.Instance);
            _symbols.Sort(ObjectItem.Comparer.Instance);
        }

        public bool FindSymbol(ObjectItem item, out int index)
        {
            index = _symbols.BinarySearch(new ObjectSymbol(item.SectionIndex, item.Offset, name: null), ObjectItem.Comparer.Instance);
            bool result = (index >= 0 && index < _symbols.Count && ObjectItem.Comparer.Instance.Compare(_symbols[index], item) == 0);
            if (!result)
            {
                index = -1;
            }
            return result;
        }

        public IEnumerable<MethodInfo> EnumerateMethods()
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
                    ObjectNode node = _nodeSymbolMap[symbolMethodPair.Key];
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

        public IReadOnlyList<ObjectNode> Nodes => _nodes;
        public IReadOnlyList<Section> Sections => _sections;
        public IReadOnlyList<ObjectSymbol> Symbols => _symbols;

        public IReadOnlyDictionary<RelocType, int> RelocCounts => _relocCounts;
    }
}
