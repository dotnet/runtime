// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using Internal.NativeFormat;
using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Stores information about what methods got inlined into other methods.
    /// </summary>
    public class InliningInfoNode : HeaderTableNode
    {
        public enum InfoType
        {
            InliningInfo2,
            CrossModuleInliningForCrossModuleDataOnly,
            CrossModuleAllMethods
        }

        private readonly EcmaModule _module;
        private readonly InfoType _inlineInfoType;
        private ReadyToRunSymbolNodeFactory _symbolNodeFactory;

        public InliningInfoNode(TargetDetails target, EcmaModule module, InfoType inlineInfoType)
            : base(target)
        {
            _inlineInfoType = inlineInfoType;
            if (AllowCrossModuleInlines)
            {
                Debug.Assert(module == null); // Cross module inlining always covers all modules
            }
            else
            {
                Debug.Assert(module != null); // InliningInfo2 is restricted to a single module at a time
            }
            _module = module;
        }

        public void Initialize(ReadyToRunSymbolNodeFactory symbolNodeFactory)
        {
            _symbolNodeFactory = symbolNodeFactory;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            if (_module != null)
            {
                sb.Append("__ReadyToRunInliningInfoTable__");
                sb.Append(_module.Assembly.GetName().Name);
            }
            else
            {
                sb.Append("__ReadyToRunCrossModuleInliningInfoTable__");
            }
        }

        private bool AllowCrossModuleInlines => _inlineInfoType == InfoType.CrossModuleAllMethods || _inlineInfoType == InfoType.CrossModuleInliningForCrossModuleDataOnly;
        private bool ReportAllInlinesInSearch => _inlineInfoType == InfoType.CrossModuleAllMethods;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            Dictionary<EcmaMethod, HashSet<EcmaMethod>> inlineeToInliners = new Dictionary<EcmaMethod, HashSet<EcmaMethod>>();

            // Build a map from inlinee to the list of inliners
            // We are only interested in the generic definitions of these.
            foreach (MethodWithGCInfo methodNode in factory.EnumerateCompiledMethods(_module, CompiledMethodCategory.All))
            {
                MethodDesc[] inlinees = methodNode.InlinedMethods;
                MethodDesc inliner = methodNode.Method;
                EcmaMethod inlinerDefinition = (EcmaMethod)inliner.GetTypicalMethodDefinition();

                if (inlinerDefinition.IsNonVersionable())
                {
                    // Non-versionable methods don't need to be reported
                    continue;
                }

                // Only encode inlining info for inliners within the active module, or if cross module inline format is in use
                Debug.Assert(AllowCrossModuleInlines || (inlinerDefinition.Module == _module));

                bool inlinerReportAllVersionsWithInlinee = !AllowCrossModuleInlines || factory.CompilationModuleGroup.CrossModuleCompileable(inlinerDefinition);

                foreach (MethodDesc inlinee in inlinees)
                {
                    MethodDesc inlineeDefinition = inlinee.GetTypicalMethodDefinition();
                    if (!(inlineeDefinition is EcmaMethod ecmaInlineeDefinition))
                    {
                        // We don't record non-ECMA methods because they don't have tokens that
                        // diagnostic tools could reason about anyway.
                        continue;
                    }

                    if (inlinee.IsNonVersionable())
                    {
                        // Non-versionable methods don't need to be reported
                        continue;
                    }

                    if (ReportAllInlinesInSearch)
                    {
                        // We'll definitely track this inline
                    }
                    else if (factory.CompilationModuleGroup.VersionsWithMethodBody(inlineeDefinition))
                    {
                        if (!inlinerReportAllVersionsWithInlinee)
                        {
                            // We'll won't report this method
                            continue;
                        }
                    }
                    else
                    {
                        Debug.Assert(factory.CompilationModuleGroup.CrossModuleInlineable(inlineeDefinition));
                        if (_inlineInfoType != InfoType.CrossModuleInliningForCrossModuleDataOnly)
                        {
                            // We'll won't report this method
                            continue;
                        }
                    }

                    if (!inlineeToInliners.TryGetValue(ecmaInlineeDefinition, out HashSet<EcmaMethod> inliners))
                    {
                        inliners = new HashSet<EcmaMethod>();
                        inlineeToInliners.Add(ecmaInlineeDefinition, inliners);
                    }
                    inliners.Add((EcmaMethod)inlinerDefinition);
                }
            }

            // Serialize the map as a hash table
            NativeWriter writer = new NativeWriter();
            Section section = writer.NewSection();

            VertexHashtable hashtable = new VertexHashtable();
            section.Place(hashtable);

            foreach (var inlineeWithInliners in inlineeToInliners)
            {
                EcmaMethod inlinee = inlineeWithInliners.Key;
                int inlineeRid = MetadataTokens.GetRowNumber(inlinee.Handle);
                int hashCode;
                
                if (AllowCrossModuleInlines)
                {
                    // CrossModuleInlineInfo format
                    hashCode = ReadyToRunHashCode.MethodHashCode(inlinee);
                }
                else
                {
                    // InliningInfo2 format
                    hashCode = ReadyToRunHashCode.ModuleNameHashCode(inlinee.Module);
                    hashCode ^= inlineeRid;
                }

                var sig = new VertexSequence();

                if (!AllowCrossModuleInlines)
                {
                    // Format of the sequence:
                    // FOR InliningInfo2 table format
                    //    Inlinee RID with flag in the lowest bit
                    //    - if flag is set, followed by module ID
                    //    Followed by inliner RIDs deltas with flag in the lowest bit
                    //    - if flag is set, followed by module ID
                    Debug.Assert(_module != null);
                    bool isForeignInlinee = inlinee.Module != _module;
                    sig.Append(new UnsignedConstant((uint)(inlineeRid << 1 | (isForeignInlinee ? 1 : 0))));
                    if (isForeignInlinee)
                    {
                        sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inlinee.Module)));
                    }

                    List<EcmaMethod> sortedInliners = new List<EcmaMethod>(inlineeWithInliners.Value);
                    sortedInliners.MergeSort((a, b) =>
                    {
                        if (a == b)
                            return 0;

                        int aRid = MetadataTokens.GetRowNumber(a.Handle);
                        int bRid = MetadataTokens.GetRowNumber(b.Handle);
                        if (aRid < bRid)
                            return -1;
                        else if (aRid > bRid)
                            return 1;

                        int result = a.Module.CompareTo(b.Module);
                        Debug.Assert(result != 0);
                        return result;
                    });

                    int baseRid = 0;
                    foreach (EcmaMethod inliner in sortedInliners)
                    {
                        int inlinerRid = MetadataTokens.GetRowNumber(inliner.Handle);
                        int ridDelta = inlinerRid - baseRid;
                        baseRid = inlinerRid;
                        Debug.Assert(ridDelta >= 0);
                        bool isForeignInliner = inliner.Module != _module;
                        sig.Append(new UnsignedConstant((uint)(ridDelta << 1 | (isForeignInliner ? 1 : 0))));
                        if (isForeignInliner)
                        {
                            sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inliner.Module)));
                        }
                    }
                }
                else
                {
                    // Format of the sequence:
                    // FOR CrossModuleInlineInfo format
                    //    Index with 2 flags field in lowest 2 bits to define the inlinee
                    //      - If flags & 1 == 0 then index is a MethodDef RID, and if the module is a composite image, a module index of the method follows
                    //      - If flags & 1 == 1, then index is an index into the ILBody import section
                    //      - If flags & 2 == 0 then what follows is:
                    //        - Inliner RID deltas - See definition below
                    //      - if flags & 2 == 2 then what follows is:
                    //        - count of delta encoded indices into the ILBody import section
                    //        - the sequence of delta encoded indices into the ILBody import section
                    //        - Inliner RID deltas - See definition below
                    //
                    //      Inliner RID deltas (for multi-module version bubble images (specified by the module having the READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE flag set)
                    //        - a sequence of inliner RID deltas with flag in the lowest bit
                    //          - if flag is set, the inliner RID is followed by a module ID
                    //          - otherwise the module is the same as the module of the inlinee method
                    //
                    //      Inliner RID deltas (for single module version bubble images)
                    //        - a sequence of inliner RID deltas

                    bool crossModuleMultiModuleFormat = (factory.CompilationModuleGroup.GetReadyToRunFlags() & ReadyToRunFlags.READYTORUN_FLAG_MultiModuleVersionBubble) != 0;

                    Debug.Assert(_module == null);
                    bool isCrossModuleInlinee = !factory.CompilationModuleGroup.VersionsWithMethodBody(inlinee);
                    Debug.Assert(!isCrossModuleInlinee || factory.CompilationModuleGroup.CrossModuleInlineable(inlinee));

                    EcmaMethod[] sortedInliners = new EcmaMethod[inlineeWithInliners.Value.Count];
                    inlineeWithInliners.Value.CopyTo(sortedInliners);

                    sortedInliners.MergeSort((a, b) =>
                    {
                        if (a == b)
                            return 0;

                        bool isCrossModuleInlinerA = !factory.CompilationModuleGroup.VersionsWithMethodBody(a);
                        bool isCrossModuleInlinerB = !factory.CompilationModuleGroup.VersionsWithMethodBody(b);
                        if (isCrossModuleInlinerA != isCrossModuleInlinerB)
                        {
                            if (isCrossModuleInlinerA)
                                return -1;
                            else
                                return 1;
                        }

                        int result;
                        if (isCrossModuleInlinerA)
                        {
                            int indexA = _symbolNodeFactory.CheckILBodyFixupSignature(a).IndexFromBeginningOfArray;
                            int indexB = _symbolNodeFactory.CheckILBodyFixupSignature(b).IndexFromBeginningOfArray;
                            Debug.Assert(indexA != indexB);
                            result = indexA.CompareTo(indexB);
                        }
                        else
                        {
                            int aRid = MetadataTokens.GetRowNumber(a.Handle);
                            int bRid = MetadataTokens.GetRowNumber(b.Handle);
                            if (aRid < bRid)
                                return -1;
                            else if (aRid > bRid)
                                return 1;

                            result = a.Module.CompareTo(b.Module);
                        }
                        Debug.Assert(result != 0);
                        return result;
                    });

                    uint crossModuleInlinerCount = 0;
                    foreach (var method in sortedInliners)
                    {
                        if (factory.CompilationModuleGroup.VersionsWithMethodBody(method))
                            break;

                        Debug.Assert(factory.CompilationModuleGroup.CrossModuleInlineable(method));
                        crossModuleInlinerCount++;
                    }

                    uint encodedInlinee;
                    checked
                    {
                        uint indexOfInlinee;
                        if (isCrossModuleInlinee)
                        {
                            indexOfInlinee = (uint)_symbolNodeFactory.CheckILBodyFixupSignature(inlinee).IndexFromBeginningOfArray;
                        }
                        else
                        {
                            indexOfInlinee = (uint)MetadataTokens.GetRowNumber(inlinee.Handle);
                        }

                        encodedInlinee = indexOfInlinee << (int)ReadyToRunCrossModuleInlineFlags.CrossModuleInlinerIndexShift;

                        if (isCrossModuleInlinee)
                            encodedInlinee |= (uint)ReadyToRunCrossModuleInlineFlags.CrossModuleInlinee;

                        if (crossModuleInlinerCount > 0)
                            encodedInlinee |= (uint)ReadyToRunCrossModuleInlineFlags.HasCrossModuleInliners;

                        sig.Append(new UnsignedConstant(encodedInlinee));
                        if (crossModuleMultiModuleFormat && !isCrossModuleInlinee)
                            sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inlinee.Module)));

                        int inlinerIndex = 0;
                        if (crossModuleInlinerCount > 0)
                        {
                            sig.Append(new UnsignedConstant(crossModuleInlinerCount));
                            uint baseIndex = 0;
                            for (; inlinerIndex < crossModuleInlinerCount; inlinerIndex++)
                            {
                                var inliner = sortedInliners[inlinerIndex];

                                uint ilBodyIndex = (uint)_symbolNodeFactory.CheckILBodyFixupSignature(inliner).IndexFromBeginningOfArray;
                                uint ridDelta = ilBodyIndex - baseIndex;
                                sig.Append(new UnsignedConstant(ridDelta));
                            }
                        }

                        uint baseRid = 0;
                        for (; inlinerIndex < sortedInliners.Length; inlinerIndex++)
                        {
                            var inliner = sortedInliners[inlinerIndex];
                            uint inlinerRid = (uint)MetadataTokens.GetRowNumber(inliner.Handle);
                            uint ridDelta = inlinerRid - baseRid;
                            baseRid = inlinerRid;
                            bool isForeignInliner = inliner.Module != inlinee.Module;
                            Debug.Assert(!isForeignInliner || crossModuleMultiModuleFormat);

                            if (crossModuleMultiModuleFormat)
                            {
                                uint encodedRid = ridDelta << (int)ReadyToRunCrossModuleInlineFlags.InlinerRidShift;
                                if (isForeignInliner)
                                    encodedRid |= (uint)ReadyToRunCrossModuleInlineFlags.InlinerRidHasModule;

                                sig.Append(new UnsignedConstant(encodedRid));
                                if (isForeignInliner)
                                {
                                    sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inliner.Module)));
                                }
                            }
                            else
                            {
                                sig.Append(new UnsignedConstant(ridDelta));
                            }
                        }
                    }
                }

                hashtable.Append((uint)hashCode, section.Place(sig));
            }

            MemoryStream writerContent = new MemoryStream();
            writer.Save(writerContent);

            return new ObjectData(
                data: writerContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            InliningInfoNode otherInliningInfo = (InliningInfoNode)other;

            if (_module == null)
            {
                Debug.Assert(otherInliningInfo._module != null);
                return -1;
            }
            else if (otherInliningInfo._module == null)
            {
                return 1;
            }
            return _module.Assembly.GetName().Name.CompareTo(otherInliningInfo._module.Assembly.GetName().Name);
        }

        public override int ClassCode => -87382891;
    }
}
