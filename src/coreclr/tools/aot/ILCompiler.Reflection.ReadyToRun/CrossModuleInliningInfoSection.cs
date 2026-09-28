// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Internal.ReadyToRunConstants;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Parser for the CrossModuleInlineInfo section (ReadyToRunSectionType 119, added in R2R v6.3).
    /// This format differs from InliningInfo2 (section 114) — it uses a stream-size counted
    /// encoding with 2-bit flags on the inlinee index and supports ILBody import indices for
    /// cross-module inlinees and inliners.
    /// </summary>
    public class CrossModuleInliningInfoSection
    {
        public enum InlineeReferenceKind
        {
            Local,
            CrossModule,
        }

        private enum CrossModuleInlineFlags : uint
        {
            CrossModuleInlinee = 0x1,
            HasCrossModuleInliners = 0x2,
            CrossModuleInlinerIndexShift = 2,
            InlinerRidHasModule = 0x1,
            InlinerRidShift = 1,
        }

        /// <summary>
        /// Identifies a method in the inlining info section.
        /// For cross-module methods, Index is an ILBody import section index.
        /// For local methods, Index is a MethodDef RID and ModuleIndex identifies the owning module.
        /// </summary>
        public readonly struct MethodRef
        {
            public bool IsCrossModule { get; }
            public uint Index { get; }
            public uint ModuleIndex { get; }

            public MethodRef(bool isCrossModule, uint index, uint moduleIndex = 0)
            {
                IsCrossModule = isCrossModule;
                Index = index;
                ModuleIndex = moduleIndex;
            }
        }

        /// <summary>
        /// A single inlinee and all the methods that inline it.
        /// </summary>
        public readonly struct InliningEntry
        {
            public MethodRef Inlinee { get; }
            public IReadOnlyList<MethodRef> Inliners { get; }

            public InliningEntry(MethodRef inlinee, IReadOnlyList<MethodRef> inliners)
            {
                Inlinee = inlinee;
                Inliners = inliners;
            }
        }

        private readonly ReadyToRunReader _r2r;
        private readonly int _startOffset;
        private readonly int _endOffset;
        private readonly bool _multiModuleFormat;

        public CrossModuleInliningInfoSection(ReadyToRunReader reader, int offset, int endOffset)
        {
            _r2r = reader;
            _startOffset = offset;
            _endOffset = endOffset;
            _multiModuleFormat = (reader.ReadyToRunHeader.Flags & (uint)ReadyToRunFlags.READYTORUN_FLAG_MultiModuleVersionBubble) != 0;
        }

        /// <summary>
        /// Parses the section into structured inlining entries.
        /// </summary>
        public List<InliningEntry> GetEntries()
        {
            var entries = new List<InliningEntry>();

            NativeParser parser = new NativeParser(_r2r.ImageReader, (uint)_startOffset);
            NativeHashtable hashtable = new NativeHashtable(_r2r.ImageReader, parser, (uint)_endOffset);

            var enumerator = hashtable.EnumerateAllEntries();
            NativeParser curParser = enumerator.GetNext();
            while (!curParser.IsNull())
            {
                uint streamSize = curParser.GetUnsigned();
                uint inlineeIndexAndFlags = curParser.GetUnsigned();
                streamSize--;

                uint inlineeIndex = inlineeIndexAndFlags >> (int)CrossModuleInlineFlags.CrossModuleInlinerIndexShift;
                bool hasCrossModuleInliners = (inlineeIndexAndFlags & (uint)CrossModuleInlineFlags.HasCrossModuleInliners) != 0;
                bool crossModuleInlinee = (inlineeIndexAndFlags & (uint)CrossModuleInlineFlags.CrossModuleInlinee) != 0;

                MethodRef inlinee;
                if (crossModuleInlinee)
                {
                    inlinee = new MethodRef(isCrossModule: true, index: inlineeIndex);
                }
                else
                {
                    uint moduleIndex = 0;
                    if (_multiModuleFormat && streamSize > 0)
                    {
                        moduleIndex = curParser.GetUnsigned();
                        streamSize--;
                    }
                    inlinee = new MethodRef(isCrossModule: false, index: inlineeIndex, moduleIndex: moduleIndex);
                }

                var inliners = new List<MethodRef>();

                if (hasCrossModuleInliners && streamSize > 0)
                {
                    uint crossModuleInlinerCount = curParser.GetUnsigned();
                    streamSize--;

                    // Cross-module inliner indices are absolute ILBody import indices,
                    // not delta-encoded (the writer never updates baseIndex between entries).
                    for (uint i = 0; i < crossModuleInlinerCount && streamSize > 0; i++)
                    {
                        uint inlinerIndex = curParser.GetUnsigned();
                        streamSize--;
                        inliners.Add(new MethodRef(isCrossModule: true, index: inlinerIndex));
                    }
                }

                uint currentRid = 0;
                while (streamSize > 0)
                {
                    uint inlinerDeltaAndFlag = curParser.GetUnsigned();
                    streamSize--;

                    uint moduleIndex = inlinee.ModuleIndex;
                    if (_multiModuleFormat)
                    {
                        currentRid += inlinerDeltaAndFlag >> (int)CrossModuleInlineFlags.InlinerRidShift;
                        if ((inlinerDeltaAndFlag & (uint)CrossModuleInlineFlags.InlinerRidHasModule) != 0 && streamSize > 0)
                        {
                            moduleIndex = curParser.GetUnsigned();
                            streamSize--;
                        }
                    }
                    else
                    {
                        currentRid += inlinerDeltaAndFlag;
                    }
                    inliners.Add(new MethodRef(isCrossModule: false, index: currentRid, moduleIndex: moduleIndex));
                }

                entries.Add(new InliningEntry(inlinee, inliners));
                curParser = enumerator.GetNext();
            }

            return entries;
        }

        /// <summary>
        /// Resolves a <see cref="MethodRef"/> to a human-readable method name.
        /// Cross-module methods are resolved via the ILBody import section entries.
        /// Local methods are resolved via the R2R reader's compiled method table.
        /// </summary>
        public string ResolveMethodName(MethodRef methodRef)
        {
            if (methodRef.IsCrossModule)
            {
                return ResolveCrossModuleMethod(methodRef.Index);
            }

            return ResolveLocalMethod(methodRef.Index, methodRef.ModuleIndex);
        }

        /// <summary>
        /// Returns all inlining pairs with resolved method names and whether the inlinee is cross-module.
        /// </summary>
        public IEnumerable<(string InlinerName, string InlineeName, InlineeReferenceKind InlineeKind)> GetInliningPairs()
        {
            foreach (var entry in GetEntries())
            {
                string inlineeName = ResolveMethodName(entry.Inlinee);
                var kind = entry.Inlinee.IsCrossModule ? InlineeReferenceKind.CrossModule : InlineeReferenceKind.Local;
                foreach (var inliner in entry.Inliners)
                {
                    yield return (ResolveMethodName(inliner), inlineeName, kind);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in GetEntries())
            {
                if (entry.Inlinee.IsCrossModule)
                {
                    sb.AppendLine($"Inliners for cross-module inlinee (ILBody import index {entry.Inlinee.Index}):");
                }
                else
                {
                    int token = RidToMethodDef((int)entry.Inlinee.Index);
                    if (_multiModuleFormat)
                    {
                        string moduleName = TryGetModuleName(entry.Inlinee.ModuleIndex);
                        sb.AppendLine($"Inliners for inlinee {token:X8} (module {moduleName}):");
                    }
                    else
                    {
                        sb.AppendLine($"Inliners for inlinee {token:X8}:");
                    }
                }

                foreach (var inliner in entry.Inliners)
                {
                    if (inliner.IsCrossModule)
                    {
                        sb.AppendLine($"  cross-module inliner (ILBody import index {inliner.Index})");
                    }
                    else
                    {
                        int token = RidToMethodDef((int)inliner.Index);
                        if (inliner.ModuleIndex != 0 || _multiModuleFormat)
                        {
                            string moduleName = TryGetModuleName(inliner.ModuleIndex);
                            sb.AppendLine($"  {token:X8} (module {moduleName})");
                        }
                        else
                        {
                            sb.AppendLine($"  {token:X8}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string ResolveCrossModuleMethod(uint importIndex)
        {
            if (!_ilBodyImportSectionResolved)
            {
                _ilBodyImportSection = FindILBodyImportSection();
                _ilBodyImportSectionResolved = true;
            }

            if (_ilBodyImportSection.Entries is not null && importIndex < (uint)_ilBodyImportSection.Entries.Count)
            {
                var entry = _ilBodyImportSection.Entries[(int)importIndex];
                if (entry.Signature is not null)
                {
                    string sig = entry.Signature.ToString(new SignatureFormattingOptions());
                    int parenIdx = sig.LastIndexOf(" (", StringComparison.Ordinal);

                    return parenIdx >= 0 ? sig[..parenIdx] : sig;
                }
            }

            return $"<ILBody import #{importIndex}>";
        }

        private string ResolveLocalMethod(uint rid, uint moduleIndex)
        {
            _localMethodMap ??= BuildLocalMethodMap();

            return _localMethodMap.TryGetValue((moduleIndex, rid), out string name)
                ? name
                : $"<MethodDef 0x{RidToMethodDef((int)rid):X8}>";
        }

        private ReadyToRunImportSection FindILBodyImportSection()
        {
            foreach (var section in _r2r.ImportSections)
            {
                foreach (var entry in section.Entries)
                {
                    if (entry.Signature?.FixupKind is ReadyToRunFixupKind.Check_IL_Body or ReadyToRunFixupKind.Verify_IL_Body)
                        return section;
                }
            }

            return default;
        }

        private Dictionary<(uint ModuleIndex, uint Rid), string> BuildLocalMethodMap()
        {
            var map = new Dictionary<(uint ModuleIndex, uint Rid), string>();
            for (int assemblyIndex = 0; assemblyIndex < _r2r.ReadyToRunAssemblies.Count; assemblyIndex++)
            {
                uint moduleIndex = _r2r.Composite
                    ? (uint)(assemblyIndex + _r2r.ComponentAssemblyIndexOffset)
                    : 0;

                foreach (var method in _r2r.ReadyToRunAssemblies[assemblyIndex].Methods)
                {
                    if (method.MethodHandle.Kind == HandleKind.MethodDefinition)
                    {
                        uint methodRid = (uint)MetadataTokens.GetRowNumber((MethodDefinitionHandle)method.MethodHandle);
                        map[(moduleIndex, methodRid)] = method.SignatureString;
                    }
                }
            }

            foreach (var instanceEntry in _r2r.InstanceMethods)
            {
                if (instanceEntry.Method.MethodHandle.Kind == HandleKind.MethodDefinition)
                {
                    uint methodRid = (uint)MetadataTokens.GetRowNumber((MethodDefinitionHandle)instanceEntry.Method.MethodHandle);
                    // Instance methods don't carry a module index — use 0 (owner module) as default.
                    map.TryAdd((0, methodRid), instanceEntry.Method.SignatureString);
                }
            }

            return map;
        }

        private ReadyToRunImportSection _ilBodyImportSection;
        private bool _ilBodyImportSectionResolved;
        private Dictionary<(uint ModuleIndex, uint Rid), string> _localMethodMap;

        private string TryGetModuleName(uint moduleIndex)
        {
            if (moduleIndex == 0)
            {
                return Path.GetFileNameWithoutExtension(_r2r.Filename);
            }

            try
            {
                return _r2r.GetReferenceAssemblyName((int)moduleIndex);
            }
            catch
            {
                return $"<module index {moduleIndex}>";
            }
        }

        static int RidToMethodDef(int rid) => MetadataTokens.GetToken(MetadataTokens.MethodDefinitionHandle(rid));
    }
}
