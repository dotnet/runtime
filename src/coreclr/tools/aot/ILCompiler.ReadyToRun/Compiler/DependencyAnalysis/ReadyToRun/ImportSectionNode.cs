// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ImportSectionNode : EmbeddedObjectNode
    {
        private class ImportTable : ArrayOfEmbeddedDataNode<Import>
        {
            public ImportTable(string startSymbol, string endSymbol) : base(startSymbol, endSymbol, nodeSorter: new EmbeddedObjectNodeComparer(CompilerComparer.Instance)) {}

            public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => false;

            public override int ClassCode => (int)ObjectNodeOrder.ImportSectionNode;
        }

        private readonly ImportTable _imports;
        // TODO: annoying - today there's no way to put signature RVA's into R/O data section
        private readonly ArrayOfEmbeddedPointersNode<Signature> _signatures;
        // TODO: annoying - cannot enumerate the ArrayOfEmbeddedPointersNode so we must keep a copy.
        private readonly List<Signature> _signatureList;
        private readonly GCRefMapNode _gcRefMap;

        private readonly ReadyToRunImportSectionType _type;
        private readonly ReadyToRunImportSectionFlags _flags;
        private readonly byte _entrySize;
        private readonly string _name;
        private readonly bool _emitPrecode;
        private readonly bool _emitGCRefMap;

        private bool _materializedSignature;

        public ImportSectionNode(string name, ReadyToRunImportSectionType importType, ReadyToRunImportSectionFlags flags, byte entrySize, bool emitPrecode, bool emitGCRefMap)
        {
            _name = name;
            _type = importType;
            _flags = flags;
            _entrySize = entrySize;
            _emitPrecode = emitPrecode;
            _emitGCRefMap = emitGCRefMap;

            _imports = new ImportTable(_name + "_ImportBegin", _name + "_ImportEnd");
            _signatures = new ArrayOfEmbeddedPointersNode<Signature>(_name + "_SigBegin", _name + "_SigEnd", new EmbeddedObjectNodeComparer(CompilerComparer.Instance));
            _signatureList = new List<Signature>();
            _gcRefMap = _emitGCRefMap ? new GCRefMapNode(this) : null;
        }

        public void MaterializeSignature(NodeFactory r2rFactory)
        {
            if (!_materializedSignature)
            {
                _signatureList.MergeSortAllowDuplicates(new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance));

                foreach (Signature signature in _signatureList)
                {
                    signature.GetData(r2rFactory, relocsOnly: false);
                }
                _materializedSignature = true;
            }
        }

        public void AddImport(NodeFactory factory, Import import)
        {
            if (_materializedSignature)
            {
                throw new Exception("Cannot call AddImport after MaterializeSignature");
            }

            _imports.AddEmbeddedObject(import);
            _signatures.AddEmbeddedObject(import.ImportSignature);

            lock (_signatureList)
            {
                _signatureList.Add(import.ImportSignature.Target);
            }

            if (_emitGCRefMap)
            {
                _gcRefMap.AddImport(import);
            }
        }

        public string Name => _name;

        public bool EmitPrecode => _emitPrecode;

        public bool IsEager => (_flags & ReadyToRunImportSectionFlags.Eager) != 0;

        public override bool StaticDependenciesAreComputed => true;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.ImportSectionNode;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            if (!_imports.ShouldSkipEmittingObjectNode(factory))
            {
                dataBuilder.EmitReloc(_imports.StartSymbol, RelocType.IMAGE_REL_BASED_ADDR32NB, 0);
            }
            else
            {
                dataBuilder.EmitUInt(0);
            }

            if (!relocsOnly)
            {
                dataBuilder.EmitReloc(_imports.StartSymbol, RelocType.IMAGE_REL_SYMBOL_SIZE);
                dataBuilder.EmitShort((short)_flags);
                dataBuilder.EmitByte((byte)_type);
                dataBuilder.EmitByte(_entrySize);
            }

            if (!_signatures.ShouldSkipEmittingObjectNode(factory))
            {
                dataBuilder.EmitReloc(_signatures.StartSymbol, RelocType.IMAGE_REL_BASED_ADDR32NB, 0);
            }
            else
            {
                dataBuilder.EmitUInt(0);
            }

            if (_emitGCRefMap)
            {
                dataBuilder.EmitReloc(_gcRefMap, RelocType.IMAGE_REL_BASED_ADDR32NB, 0);
            }
            else
            {
                dataBuilder.EmitUInt(0);
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            yield return new DependencyListEntry(_imports, "Import section fixup data");
            yield return new DependencyListEntry(_signatures, "Import section signatures");
            if (_emitGCRefMap)
            {
                yield return new DependencyListEntry(_gcRefMap, "GC ref map");
            }
        }

        protected override string GetName(NodeFactory context)
        {
            return _name;
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _name.CompareTo(((ImportSectionNode)other)._name);
        }
    }
}
