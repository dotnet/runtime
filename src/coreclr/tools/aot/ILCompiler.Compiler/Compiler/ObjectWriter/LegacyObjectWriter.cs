// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Object writer using src/Native/ObjWriter
    /// </summary>
    internal sealed class LegacyObjectWriter : IDisposable, ITypesDebugInfoWriter
    {
        private readonly ObjectWritingOptions _options;

        // This is used to build mangled names
        private Utf8StringBuilder _sb = new Utf8StringBuilder();

        // This is used to look up file id for the given file name.
        // This is a global table across nodes.
        private Dictionary<string, int> _debugFileToId = new Dictionary<string, int>();

        // Track offsets in node data that prevent writing all bytes in one single blob. This includes
        // relocs, symbol definitions, debug data that must be streamed out using the existing LLVM API
        private bool[] _byteInterruptionOffsets;
        // This is used to look up DebugLocInfo for the given native offset.
        // This is for individual node and should be flushed once node is emitted.
        private Dictionary<int, NativeSequencePoint> _offsetToDebugLoc = new Dictionary<int, NativeSequencePoint>();

        // Code offset to defined names
        private Dictionary<int, List<ISymbolDefinitionNode>> _offsetToDefName = new Dictionary<int, List<ISymbolDefinitionNode>>();

        // Code offset to Cfi blobs
        private Dictionary<int, List<byte[]>> _offsetToCfis = new Dictionary<int, List<byte[]>>();
        // Code offset to Lsda label index
        private Dictionary<int, byte[]> _offsetToCfiLsdaBlobName = new Dictionary<int, byte[]>();
        // Code offsets that starts a frame
        private HashSet<int> _offsetToCfiStart = new HashSet<int>();
        // Code offsets that ends a frame
        private HashSet<int> _offsetToCfiEnd = new HashSet<int>();
        // Code offsets to compact unwind encoding
        private Dictionary<int, uint> _offsetToCfiCompactEncoding = new Dictionary<int, uint>();
        // Used to assert whether frames are not overlapped.
        private bool _frameOpened;

        //  The size of CFI_CODE that RyuJit passes.
        private const int CfiCodeSize = 8;

        // The section for the current node being processed.
        private ObjectNodeSection _currentSection;

        // The first defined symbol name of the current node being processed.
        private Utf8String _currentNodeZeroTerminatedName;

        private const string NativeObjectWriterFileName = "objwriter";

        // Target platform ObjectWriter is instantiated for.
        private TargetDetails _targetPlatform;

        // Nodefactory for which ObjectWriter is instantiated for.
        private NodeFactory _nodeFactory;
        private readonly bool _isSingleFileCompilation;

        // Unix section containing LSDA data, like EH Info and GC Info
        public static readonly ObjectNodeSection LsdaSection = new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly);

        private UserDefinedTypeDescriptor _userDefinedTypeDescriptor;

#if DEBUG
        private static Dictionary<string, ISymbolNode> _previouslyWrittenNodeNames = new Dictionary<string, ISymbolNode>();
#endif

        [DllImport(NativeObjectWriterFileName)]
        private static extern IntPtr InitObjWriter([MarshalAs(UnmanagedType.LPUTF8Str)] string objectFilePath, string triple = null);

        [DllImport(NativeObjectWriterFileName)]
        private static extern void FinishObjWriter(IntPtr objWriter);

        [DllImport(NativeObjectWriterFileName)]
        private static extern void SetDwarfVersion(IntPtr objWriter, ushort v);

        [DllImport(NativeObjectWriterFileName)]
        private static extern void SwitchSection(IntPtr objWriter, string sectionName, CustomSectionAttributes attributes = 0, string comdatName = null);

        public void SetSection(ObjectNodeSection section)
        {
            if (!section.IsStandardSection)
            {
                SwitchSection(_nativeObjectWriter, section.Name, GetCustomSectionAttributes(section), section.ComdatName);
            }
            else
            {
                SwitchSection(_nativeObjectWriter, section.Name);
            }

            _currentSection = section;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void SetCodeSectionAttribute(IntPtr objWriter, string sectionName, CustomSectionAttributes attributes = 0, string comdatName = null);

        public void SetCodeSectionAttribute(ObjectNodeSection section)
        {
            if (!section.IsStandardSection)
            {
                SetCodeSectionAttribute(_nativeObjectWriter, section.Name, GetCustomSectionAttributes(section), section.ComdatName);
            }
            else
            {
                SetCodeSectionAttribute(_nativeObjectWriter, section.Name);
            }
        }

        public void EnsureCurrentSection()
        {
            SetSection(_currentSection);
        }

        [Flags]
        public enum CustomSectionAttributes
        {
            ReadOnly = 0x0000,
            Writeable = 0x0001,
            Executable = 0x0002,
            Uninitialized = 0x0004,
        };

        /// <summary>
        /// Builds a set of CustomSectionAttributes flags from an ObjectNodeSection.
        /// </summary>
        private static CustomSectionAttributes GetCustomSectionAttributes(ObjectNodeSection section)
        {
            CustomSectionAttributes attributes = 0;

            switch (section.Type)
            {
                case SectionType.Executable:
                    attributes |= CustomSectionAttributes.Executable;
                    break;
                case SectionType.ReadOnly:
                    attributes |= CustomSectionAttributes.ReadOnly;
                    break;
                case SectionType.Writeable:
                    attributes |= CustomSectionAttributes.Writeable;
                    break;
                case SectionType.Uninitialized:
                    attributes |= CustomSectionAttributes.Uninitialized | CustomSectionAttributes.Writeable;
                    break;
            }

            return attributes;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitAlignment(IntPtr objWriter, int byteAlignment);
        public void EmitAlignment(int byteAlignment)
        {
            EmitAlignment(_nativeObjectWriter, byteAlignment);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitBlob(IntPtr objWriter, int blobSize, byte[] blob);
        public void EmitBlob(byte[] blob)
        {
            EmitBlob(_nativeObjectWriter, blob.Length, blob);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitIntValue(IntPtr objWriter, ulong value, int size);
        public void EmitIntValue(ulong value, int size)
        {
            EmitIntValue(_nativeObjectWriter, value, size);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitBlob(IntPtr objWriter, int blobSize, IntPtr blob);
        public void EmitBytes(IntPtr pArray, int length)
        {
            EmitBlob(_nativeObjectWriter, length, pArray);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitSymbolDef(IntPtr objWriter, byte[] symbolName, int global);
        public void EmitSymbolDef(byte[] symbolName, bool global = false)
        {
            EmitSymbolDef(_nativeObjectWriter, symbolName, global ? 1 : 0);
        }
        public void EmitSymbolDef(Utf8StringBuilder symbolName, bool global = false)
        {
            EmitSymbolDef(_nativeObjectWriter, symbolName.Append('\0').UnderlyingArray, global ? 1 : 0);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern int EmitSymbolRef(IntPtr objWriter, byte[] symbolName, RelocType relocType, int delta, SymbolRefFlags flags);
        private int EmitSymbolRef(Utf8StringBuilder symbolName, RelocType relocType, int delta = 0, SymbolRefFlags flags = 0)
        {
            return EmitSymbolRef(_nativeObjectWriter, symbolName.Append('\0').UnderlyingArray, relocType, delta, flags);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitWinFrameInfo(IntPtr objWriter, byte[] methodName, int startOffset, int endOffset,
                                                    byte[] blobSymbolName);
        public void EmitWinFrameInfo(int startOffset, int endOffset, int blobSize, byte[] blobSymbolName)
        {
            _ = blobSize;
            EmitWinFrameInfo(_nativeObjectWriter, _currentNodeZeroTerminatedName.UnderlyingArray, startOffset, endOffset, blobSymbolName);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitCFIStart(IntPtr objWriter, int nativeOffset);
        public void EmitCFIStart(int nativeOffset)
        {
            Debug.Assert(!_frameOpened);
            EmitCFIStart(_nativeObjectWriter, nativeOffset);
            _frameOpened = true;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitCFIEnd(IntPtr objWriter, int nativeOffset);
        public void EmitCFIEnd(int nativeOffset)
        {
            Debug.Assert(_frameOpened);
            EmitCFIEnd(_nativeObjectWriter, nativeOffset);
            _frameOpened = false;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitCFILsda(IntPtr objWriter, byte[] blobSymbolName);
        public void EmitCFILsda(byte[] blobSymbolName)
        {
            Debug.Assert(_frameOpened);
            EmitCFILsda(_nativeObjectWriter, blobSymbolName);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitCFICode(IntPtr objWriter, int nativeOffset, byte[] blob);
        public void EmitCFICode(int nativeOffset, byte[] blob)
        {
            Debug.Assert(_frameOpened);
            EmitCFICode(_nativeObjectWriter, nativeOffset, blob);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitCFICompactUnwindEncoding(IntPtr objWriter, uint encoding);
        public void EmitCFICompactUnwindEncoding(uint encoding)
        {
            Debug.Assert(_frameOpened);
            EmitCFICompactUnwindEncoding(_nativeObjectWriter, encoding);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugFileInfo(IntPtr objWriter, int fileId, string fileName);
        public void EmitDebugFileInfo(int fileId, string fileName)
        {
            EmitDebugFileInfo(_nativeObjectWriter, fileId, fileName);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugLoc(IntPtr objWriter, int nativeOffset, int fileId, int linueNumber, int colNumber);
        public void EmitDebugLoc(int nativeOffset, int fileId, int linueNumber, int colNumber)
        {
            EmitDebugLoc(_nativeObjectWriter, nativeOffset, fileId, linueNumber, colNumber);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetEnumTypeIndex(IntPtr objWriter, EnumTypeDescriptor enumTypeDescriptor, EnumRecordTypeDescriptor[] typeRecords);

        public uint GetEnumTypeIndex(EnumTypeDescriptor enumTypeDescriptor, EnumRecordTypeDescriptor[] typeRecords)
        {
            return GetEnumTypeIndex(_nativeObjectWriter, enumTypeDescriptor, typeRecords);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetClassTypeIndex(IntPtr objWriter, ClassTypeDescriptor classTypeDescriptor);

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetCompleteClassTypeIndex(IntPtr objWriter, ClassTypeDescriptor classTypeDescriptor,
                                                             ClassFieldsTypeDescriptor classFieldsTypeDescriptior, DataFieldDescriptor[] fields,
                                                             StaticDataFieldDescriptor[] statics);

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetPrimitiveTypeIndex(IntPtr objWriter, int type);

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitARMFnStart(IntPtr objWriter);
        public void EmitARMFnStart()
        {
            Debug.Assert(!_frameOpened);
            EmitARMFnStart(_nativeObjectWriter);
            _frameOpened = true;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitARMFnEnd(IntPtr objWriter);
        public void EmitARMFnEnd()
        {
            Debug.Assert(_frameOpened);
            EmitARMFnEnd(_nativeObjectWriter);
            _frameOpened = false;
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitARMExIdxCode(IntPtr objWriter, int nativeOffset, byte[] blob);
        public void EmitARMExIdxCode(int nativeOffset, byte[] blob)
        {
            Debug.Assert(_frameOpened);
            EmitARMExIdxCode(_nativeObjectWriter, nativeOffset, blob);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitARMExIdxLsda(IntPtr objWriter, byte[] blob);
        public void EmitARMExIdxLsda(byte[] blob)
        {
            Debug.Assert(_frameOpened);
            EmitARMExIdxLsda(_nativeObjectWriter, blob);
        }

        public uint GetClassTypeIndex(ClassTypeDescriptor classTypeDescriptor)
        {
            return GetClassTypeIndex(_nativeObjectWriter, classTypeDescriptor);
        }

        public uint GetCompleteClassTypeIndex(ClassTypeDescriptor classTypeDescriptor, ClassFieldsTypeDescriptor classFieldsTypeDescriptior,
                                              DataFieldDescriptor[] fields, StaticDataFieldDescriptor[] statics)
        {
            return GetCompleteClassTypeIndex(_nativeObjectWriter, classTypeDescriptor, classFieldsTypeDescriptior, fields, statics);
        }

        public uint GetPrimitiveTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive, "it is not a primitive type");
            return GetPrimitiveTypeIndex(_nativeObjectWriter, (int)type.Category);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetArrayTypeIndex(IntPtr objWriter, ClassTypeDescriptor classDescriptor, ArrayTypeDescriptor arrayTypeDescriptor);

        public uint GetArrayTypeIndex(ClassTypeDescriptor classDescriptor, ArrayTypeDescriptor arrayTypeDescriptor)
        {
            return GetArrayTypeIndex(_nativeObjectWriter, classDescriptor, arrayTypeDescriptor);
        }

        public string GetMangledName(TypeDesc type)
        {
            return _nodeFactory.NameMangler.GetMangledTypeName(type);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugVar(IntPtr objWriter, string name, uint typeIndex, bool isParam, int rangeCount, ref NativeVarInfo range);

        public void EmitDebugVar(INodeWithDebugInfo owningNode, in DebugVarInfoMetadata debugVar)
        {
            uint typeIndex;
            string varName = debugVar.Name;
            try
            {
                if (owningNode.IsStateMachineMoveNextMethod && debugVar.DebugVarInfo.VarNumber == 0)
                {
                    typeIndex = _userDefinedTypeDescriptor.GetStateMachineThisVariableTypeIndex(debugVar.Type);
                    varName = "locals";
                }
                else
                {
                    typeIndex = _userDefinedTypeDescriptor.GetVariableTypeIndex(debugVar.Type);
                }
            }
            catch (TypeSystemException)
            {
                typeIndex = 0; // T_NOTYPE
            }

            DebugVarRangeInfo[] rangeInfos = debugVar.DebugVarInfo.Ranges;
            Span<NativeVarInfo> varInfos = rangeInfos.Length < 128 ?
                stackalloc NativeVarInfo[rangeInfos.Length] :
                new NativeVarInfo[rangeInfos.Length];

            for (int i = 0; i < rangeInfos.Length; i++)
            {
                varInfos[i] = new NativeVarInfo
                {
                    endOffset = rangeInfos[i].EndOffset,
                    startOffset = rangeInfos[i].StartOffset,
                    varLoc = rangeInfos[i].VarLoc,
                    varNumber = debugVar.DebugVarInfo.VarNumber,
                };
            }

            EmitDebugVar(_nativeObjectWriter, varName, typeIndex, debugVar.IsParameter, varInfos.Length, ref varInfos[0]);
        }

        public void EmitDebugVarInfo(ObjectNode node)
        {
            // No interest if it's not a debug node.
            var nodeWithDebugInfo = node as INodeWithDebugInfo;
            if (nodeWithDebugInfo != null)
            {
                foreach (var debugVar in nodeWithDebugInfo.GetDebugVars())
                {
                    EmitDebugVar(nodeWithDebugInfo, debugVar);
                }
            }
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugEHClause(IntPtr objWriter, uint TryOffset, uint TryLength, uint HandlerOffset, uint HandlerLength);

        public void EmitDebugEHClause(DebugEHClauseInfo ehClause)
        {
            EmitDebugEHClause(_nativeObjectWriter, ehClause.TryOffset, ehClause.TryLength, ehClause.HandlerOffset, ehClause.HandlerLength);
        }

        public void EmitDebugEHClauseInfo(ObjectNode node)
        {
            var nodeWithCodeInfo = node as INodeWithCodeInfo;
            if (nodeWithCodeInfo != null)
            {
                DebugEHClauseInfo[] clauses = nodeWithCodeInfo.DebugEHClauseInfos;
                if (clauses != null)
                {
                    foreach (var clause in clauses)
                    {
                        EmitDebugEHClause(clause);
                    }
                }
            }
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugFunctionInfo(IntPtr objWriter, byte[] methodName, int methodSize, uint methodTypeIndex);
        public void EmitDebugFunctionInfo(ObjectNode node, int methodSize)
        {
            if (node is IMethodNode methodNode)
            {
                uint methodTypeIndex = _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method);
                EmitDebugFunctionInfo(_nativeObjectWriter, _currentNodeZeroTerminatedName.UnderlyingArray, methodSize, methodTypeIndex);
            }
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern void EmitDebugModuleInfo(IntPtr objWriter);
        public void EmitDebugModuleInfo()
        {
            if (HasModuleDebugInfo())
            {
                EmitDebugModuleInfo(_nativeObjectWriter);
            }
        }

        public bool HasModuleDebugInfo()
        {
            return (_options & ObjectWritingOptions.GenerateDebugInfo) != 0;
        }

        public bool HasFunctionDebugInfo()
        {
            if (_offsetToDebugLoc.Count > 0)
            {
                Debug.Assert(HasModuleDebugInfo());
                return true;
            }

            return false;
        }

        private int GetDocumentId(string document)
        {
            if (_debugFileToId.TryGetValue(document, out int result))
            {
                return result;
            }

            result = _debugFileToId.Count + 1;
            _debugFileToId.Add(document, result);
            this.EmitDebugFileInfo(result, document);
            return result;
        }

        public void BuildDebugLocInfoMap(ObjectNode node)
        {
            if (!HasModuleDebugInfo())
            {
                return;
            }

            _offsetToDebugLoc.Clear();
            INodeWithDebugInfo debugNode = node as INodeWithDebugInfo;
            if (debugNode != null)
            {
                IEnumerable<NativeSequencePoint> locs = debugNode.GetNativeSequencePoints();
                foreach (var loc in locs)
                {
                    Debug.Assert(!_offsetToDebugLoc.ContainsKey(loc.NativeOffset));
                    _offsetToDebugLoc[loc.NativeOffset] = loc;
                    _byteInterruptionOffsets[loc.NativeOffset] = true;
                }
            }
        }


        public void PublishUnwindInfo(ObjectNode node)
        {
            INodeWithCodeInfo nodeWithCodeInfo = node as INodeWithCodeInfo;
            if (nodeWithCodeInfo == null)
            {
                return;
            }

            FrameInfo[] frameInfos = nodeWithCodeInfo.FrameInfos;
            if (frameInfos == null)
            {
                // Data should only be present if the method has unwind info
                Debug.Assert(nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) == null);

                return;
            }

            byte[] gcInfo = nodeWithCodeInfo.GCInfo;
            MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
            ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory);

            for (int i = 0; i < frameInfos.Length; i++)
            {
                FrameInfo frameInfo = frameInfos[i];

                int start = frameInfo.StartOffset;
                int end = frameInfo.EndOffset;
                int len = frameInfo.BlobData.Length;
                byte[] blob = frameInfo.BlobData;

                _sb.Clear().Append(_nodeFactory.NameMangler.CompilationUnitPrefix).Append("_unwind").Append(i.ToStringInvariant());

                byte[] blobSymbolName = _sb.Append(_currentNodeZeroTerminatedName).ToUtf8String().UnderlyingArray;

                ObjectNodeSection section = GetSharedSection(ObjectNodeSection.XDataSection, _sb.ToString());
                SwitchSection(_nativeObjectWriter, section.Name, GetCustomSectionAttributes(section), section.ComdatName);

                EmitAlignment(4);
                EmitSymbolDef(blobSymbolName);

                FrameInfoFlags flags = frameInfo.Flags;
                flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                EmitBlob(blob);

                EmitIntValue((byte)flags, 1);

                if (associatedDataNode != null)
                {
                    EmitSymbolReference(associatedDataNode, 0, RelocType.IMAGE_REL_BASED_ABSOLUTE);
                    associatedDataNode = null;
                }

                if (ehInfo != null)
                {
                    EmitSymbolReference(ehInfo, 0, RelocType.IMAGE_REL_BASED_ABSOLUTE);
                    ehInfo = null;
                }

                if (gcInfo != null)
                {
                    EmitBlob(gcInfo);
                    gcInfo = null;
                }

                // For window, just emit the frame blob (UNWIND_INFO) as a whole.
                EmitWinFrameInfo(start, end, len, blobSymbolName);

                EnsureCurrentSection();
            }
        }

        // This represents the following DWARF code:
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_offset: +16
        //   DW_CFA_offset: W29 -16
        //   DW_CFA_offset: W30 -8
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_register: W29
        // which is generated for the following frame prolog/epilog:
        //   stp fp, lr, [sp, #-10]!
        //   mov fp, sp
        //   ...
        //   ldp fp, lr, [sp], #0x10
        //   ret
        private static ReadOnlySpan<byte> DwarfArm64EmptyFrame => new byte[]
        {
            0x04, 0x00, 0xFF, 0xFF, 0x10, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1E, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x08, 0x01, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private bool TryGetCompactUnwindEncoding(byte[] blob, out uint encoding)
        {
            if (_targetPlatform.Architecture == TargetArchitecture.ARM64)
            {
                if (blob.AsSpan().SequenceEqual(DwarfArm64EmptyFrame))
                {
                    // Frame-based encoding, no saved registers
                    encoding = 0x04000000;
                    return true;
                }
            }
            encoding = 0;
            return false;
        }

        public void BuildCFIMap(ObjectNode node)
        {
            _offsetToCfis.Clear();
            _offsetToCfiStart.Clear();
            _offsetToCfiEnd.Clear();
            _offsetToCfiLsdaBlobName.Clear();
            _offsetToCfiCompactEncoding.Clear();
            _frameOpened = false;

            INodeWithCodeInfo nodeWithCodeInfo = node as INodeWithCodeInfo;
            if (nodeWithCodeInfo == null)
            {
                return;
            }

            FrameInfo[] frameInfos = nodeWithCodeInfo.FrameInfos;
            if (frameInfos == null)
            {
                // Data should only be present if the method has unwind info
                Debug.Assert(nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) == null);

                return;
            }

            byte[] gcInfo = nodeWithCodeInfo.GCInfo;
            MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
            ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory);

            for (int i = 0; i < frameInfos.Length; i++)
            {
                FrameInfo frameInfo = frameInfos[i];

                int start = frameInfo.StartOffset;
                int end = frameInfo.EndOffset;
                int len = frameInfo.BlobData.Length;
                byte[] blob = frameInfo.BlobData;
                bool emitDwarf = true;

                ObjectNodeSection lsdaSection = LsdaSection;
                if (ShouldShareSymbol(node))
                {
                    lsdaSection = GetSharedSection(lsdaSection, ((ISymbolNode)node).GetMangledName(_nodeFactory.NameMangler));
                }
                SwitchSection(_nativeObjectWriter, lsdaSection.Name, GetCustomSectionAttributes(lsdaSection), lsdaSection.ComdatName);

                _sb.Clear().Append("_lsda").Append(i.ToStringInvariant()).Append(_currentNodeZeroTerminatedName);
                byte[] blobSymbolName = _sb.ToUtf8String().UnderlyingArray;
                EmitSymbolDef(blobSymbolName);

                if (_targetPlatform.IsApplePlatform &&
                    TryGetCompactUnwindEncoding(blob, out uint compactEncoding))
                {
                    _offsetToCfiCompactEncoding[start] = compactEncoding;
                    emitDwarf = false;
                }

                FrameInfoFlags flags = frameInfo.Flags;
                flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                EmitIntValue((byte)flags, 1);

                if (i != 0)
                {
                    EmitSymbolRef(_sb.Clear().Append("_lsda0").Append(_currentNodeZeroTerminatedName), RelocType.IMAGE_REL_BASED_RELPTR32);

                    // emit relative offset from the main function
                    EmitIntValue((ulong)(start - frameInfos[0].StartOffset), 4);
                }

                if (associatedDataNode != null)
                {
                    EmitSymbolReference(associatedDataNode, 0, RelocType.IMAGE_REL_BASED_RELPTR32);
                    associatedDataNode = null;
                }

                if (ehInfo != null)
                {
                    EmitSymbolReference(ehInfo, 0, RelocType.IMAGE_REL_BASED_RELPTR32);
                    ehInfo = null;
                }

                if (gcInfo != null)
                {
                    EmitBlob(gcInfo);
                    gcInfo = null;
                }

                // For Unix, we build CFI blob map for each offset.
                Debug.Assert(len % CfiCodeSize == 0);

                // Record start/end of frames which shouldn't be overlapped.
                _offsetToCfiStart.Add(start);
                _offsetToCfiEnd.Add(end);
                _byteInterruptionOffsets[start] = true;
                _byteInterruptionOffsets[end] = true;
                _offsetToCfiLsdaBlobName.Add(start, blobSymbolName);

                if (emitDwarf)
                {
                    for (int j = 0; j < len; j += CfiCodeSize)
                    {
                        // The first byte of CFI_CODE is offset from the range the frame covers.
                        // Compute code offset from the root method.
                        int codeOffset = blob[j] + start;
                        List<byte[]> cfis;
                        if (!_offsetToCfis.TryGetValue(codeOffset, out cfis))
                        {
                            cfis = new List<byte[]>();
                            _offsetToCfis.Add(codeOffset, cfis);
                            _byteInterruptionOffsets[codeOffset] = true;
                        }
                        byte[] cfi = new byte[CfiCodeSize];
                        Array.Copy(blob, j, cfi, 0, CfiCodeSize);
                        cfis.Add(cfi);
                    }
                }
            }

            EnsureCurrentSection();
        }

        public void EmitCFICodes(int offset)
        {
            // Emit end the old frame before start a frame.
            if (_offsetToCfiEnd.Contains(offset))
            {
                if (_targetPlatform.Architecture == TargetArchitecture.ARM)
                    EmitARMFnEnd();
                else
                    EmitCFIEnd(offset);
            }

            if (_offsetToCfiStart.Contains(offset))
            {
                if (_targetPlatform.Architecture == TargetArchitecture.ARM)
                    EmitARMFnStart();
                else
                    EmitCFIStart(offset);

                byte[] blobSymbolName;
                if (_offsetToCfiLsdaBlobName.TryGetValue(offset, out blobSymbolName))
                {
                    if (_targetPlatform.Architecture == TargetArchitecture.ARM)
                        EmitARMExIdxLsda(blobSymbolName);
                    else
                        EmitCFILsda(blobSymbolName);

                }
                else
                {
                    // Internal compiler error
                    Debug.Assert(false);
                }

                if (_targetPlatform.IsApplePlatform)
                {
                    // Emit a symbol for beginning of the frame. This is workaround for ld64
                    // linker bug which would produce DWARF with incorrect pcStart offsets for
                    // exception handling blocks if there is no symbol present for them.
                    //
                    // To make things simple we just reuse blobSymbolName and change `_lsda`
                    // prefix to `_fram`.
                    "_fram"u8.CopyTo(blobSymbolName);
                    EmitSymbolDef(blobSymbolName);

                    if (_offsetToCfiCompactEncoding.TryGetValue(offset, out uint compactEncoding))
                    {
                        EmitCFICompactUnwindEncoding(compactEncoding);
                    }
                }
            }

            // Emit individual cfi blob for the given offset
            List<byte[]> cfis;
            if (_offsetToCfis.TryGetValue(offset, out cfis))
            {
                foreach (byte[] cfi in cfis)
                {
                    if (_targetPlatform.Architecture == TargetArchitecture.ARM)
                    {
                        EmitARMExIdxCode(offset, cfi);
                    }
                    else
                    {
                        EmitCFICode(offset, cfi);
                    }
                }
            }
        }

        public void EmitDebugLocInfo(int offset)
        {
            NativeSequencePoint loc;
            if (_offsetToDebugLoc.TryGetValue(offset, out loc))
            {
                EmitDebugLoc(offset,
                    GetDocumentId(loc.FileName),
                    loc.LineNumber,
                    loc.ColNumber);
            }
        }

        public void BuildSymbolDefinitionMap(ObjectNode node, ISymbolDefinitionNode[] definedSymbols)
        {
            _offsetToDefName.Clear();
            foreach (ISymbolDefinitionNode n in definedSymbols)
            {
                if (!_offsetToDefName.TryGetValue(n.Offset, out var nodes))
                {
                    nodes = new List<ISymbolDefinitionNode>();
                    _offsetToDefName[n.Offset] = nodes;
                }

                nodes.Add(n);
                _byteInterruptionOffsets[n.Offset] = true;
            }

            var symbolNode = node as ISymbolDefinitionNode;
            if (symbolNode != null)
            {
                _sb.Clear();
                AppendExternCPrefix(_sb);
                symbolNode.AppendMangledName(_nodeFactory.NameMangler, _sb);
                _currentNodeZeroTerminatedName = _sb.Append('\0').ToUtf8String();
            }
            else
            {
                _currentNodeZeroTerminatedName = default(Utf8String);
            }
        }

        private void AppendExternCPrefix(Utf8StringBuilder sb)
        {
            if (_targetPlatform.IsApplePlatform)
            {
                // On OSX-like systems, we need to prefix an extra underscore to account for correct linkage of
                // extern "C" functions.
                sb.Append('_');
            }
        }

        // Returns size of the emitted symbol reference
        public int EmitSymbolReference(ISymbolNode target, int delta, RelocType relocType)
        {
            _sb.Clear();
            AppendExternCPrefix(_sb);
            target.AppendMangledName(_nodeFactory.NameMangler, _sb);

            SymbolRefFlags flags = 0;

            // For now consider all method symbols address taken.
            // We could restrict this in the future to those that are referenced from
            // reflection tables, EH tables, were actually address taken in code, or are referenced from vtables.
            if ((_options & ObjectWritingOptions.ControlFlowGuard) != 0 && (target is IMethodNode || target is AssemblyStubNode))
            {
                flags |= SymbolRefFlags.AddressTakenFunction;
            }

            return EmitSymbolRef(_sb, relocType, checked(delta + target.Offset), flags);
        }

        public void EmitSymbolDefinition(int currentOffset)
        {
            List<ISymbolDefinitionNode> nodes;
            if (_offsetToDefName.TryGetValue(currentOffset, out nodes))
            {
                foreach (var name in nodes)
                {
                    _sb.Clear();
                    AppendExternCPrefix(_sb);
                    name.AppendMangledName(_nodeFactory.NameMangler, _sb);

                    EmitSymbolDef(_sb);

                    string alternateName = _nodeFactory.GetSymbolAlternateName(name);
                    if (alternateName != null)
                    {
                        _sb.Clear();
                        AppendExternCPrefix(_sb);
                        _sb.Append(alternateName);

                        EmitSymbolDef(_sb, global: true);
                    }
                }
            }
        }

        private IntPtr _nativeObjectWriter = IntPtr.Zero;

        public LegacyObjectWriter(string objectFilePath, NodeFactory factory, ObjectWritingOptions options)
        {
            var triple = GetLLVMTripleFromTarget(factory.Target);

            _nativeObjectWriter = InitObjWriter(objectFilePath, triple);
            if (_nativeObjectWriter == IntPtr.Zero)
            {
                throw new IOException("Fail to initialize Native Object Writer");
            }
            _nodeFactory = factory;
            _targetPlatform = _nodeFactory.Target;
            _isSingleFileCompilation = _nodeFactory.CompilationModuleGroup.IsSingleFileCompilation;
            _userDefinedTypeDescriptor = new UserDefinedTypeDescriptor(this, factory);
            _options = options;

            if ((_options & ObjectWritingOptions.UseDwarf5) != 0)
            {
                SetDwarfVersion(_nativeObjectWriter, 5);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool bDisposing)
        {
            if (_nativeObjectWriter != IntPtr.Zero)
            {
                // Finalize object emission.
                FinishObjWriter(_nativeObjectWriter);
                _nativeObjectWriter = IntPtr.Zero;
            }

            _nodeFactory = null;

            if (bDisposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        ~LegacyObjectWriter()
        {
            Dispose(false);
        }

        private bool ShouldShareSymbol(ObjectNode node)
        {
            // Foldable sections are always COMDATs
            ObjectNodeSection section = node.GetSection(_nodeFactory);
            if (section == ObjectNodeSection.FoldableManagedCodeUnixContentSection ||
                section == ObjectNodeSection.FoldableManagedCodeWindowsContentSection ||
                section == ObjectNodeSection.FoldableReadOnlyDataSection)
                return true;

            if (_isSingleFileCompilation)
                return false;

            if (_targetPlatform.IsApplePlatform)
                return false;

            if (!(node is ISymbolNode))
                return false;

            // These intentionally clash with one another, but are merged with linker directives so should not be Comdat folded
            if (node is ModulesSectionNode)
                return false;

            return true;
        }

        private static ObjectNodeSection GetSharedSection(ObjectNodeSection section, string key)
        {
            string standardSectionPrefix = "";
            if (section.IsStandardSection)
                standardSectionPrefix = ".";

            return new ObjectNodeSection(standardSectionPrefix + section.Name, section.Type, key);
        }

        public void ResetByteRunInterruptionOffsets(ObjectData nodeContents)
        {
            int neededInterruptionsBytes = nodeContents.Data.Length + 1;
            if (_byteInterruptionOffsets == null || _byteInterruptionOffsets.Length < neededInterruptionsBytes)
                _byteInterruptionOffsets = new bool[neededInterruptionsBytes];
            else
                Array.Clear(_byteInterruptionOffsets, 0, neededInterruptionsBytes);

            foreach (var reloc in nodeContents.Relocs)
            {
                _byteInterruptionOffsets[reloc.Offset] = true;
            }
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            LegacyObjectWriter objectWriter = new LegacyObjectWriter(objectFilePath, factory, options);
            bool succeeded = false;

            try
            {
                ObjectNodeSection managedCodeSection;
                if (factory.Target.OperatingSystem == TargetOS.Windows)
                {
                    managedCodeSection = ObjectNodeSection.ManagedCodeWindowsContentSection;
                }
                else
                {
                    managedCodeSection = ObjectNodeSection.ManagedCodeUnixContentSection;
                    // TODO 2916: managed code section has to be created here, switch is not necessary.
                    objectWriter.SetSection(ObjectNodeSection.ManagedCodeUnixContentSection);
                    objectWriter.SetSection(LsdaSection);
                }
                objectWriter.SetCodeSectionAttribute(managedCodeSection);

                ProgressReporter progressReporter = default;
                if (logger.IsVerbose)
                {
                    int count = 0;
                    foreach (var node in nodes)
                        if (node is ObjectNode)
                            count++;

                    logger.LogMessage($"Writing {count} object nodes...");

                    progressReporter = new ProgressReporter(logger, count);
                }

                foreach (DependencyNode depNode in nodes)
                {
                    ObjectNode node = depNode as ObjectNode;
                    if (node == null)
                        continue;

                    if (logger.IsVerbose)
                        progressReporter.LogProgress();

                    if (node.ShouldSkipEmittingObjectNode(factory))
                        continue;

                    ObjectData nodeContents = node.GetData(factory);

                    dumper?.DumpObjectNode(factory, node, nodeContents);

#if DEBUG
                    foreach (ISymbolNode definedSymbol in nodeContents.DefinedSymbols)
                    {
                        try
                        {
                            _previouslyWrittenNodeNames.Add(definedSymbol.GetMangledName(factory.NameMangler), definedSymbol);
                        }
                        catch (ArgumentException)
                        {
                            ISymbolNode alreadyWrittenSymbol = _previouslyWrittenNodeNames[definedSymbol.GetMangledName(factory.NameMangler)];
                            Debug.Fail("Duplicate node name emitted to file",
                            $"Symbol {definedSymbol.GetMangledName(factory.NameMangler)} has already been written to the output object file {objectFilePath} with symbol {alreadyWrittenSymbol}");
                        }
                    }
#endif


                    ObjectNodeSection section = node.GetSection(factory);
                    if (objectWriter.ShouldShareSymbol(node))
                    {
                        section = GetSharedSection(section, ((ISymbolNode)node).GetMangledName(factory.NameMangler));
                    }

                    // Ensure section and alignment for the node.
                    objectWriter.SetSection(section);
                    objectWriter.EmitAlignment(nodeContents.Alignment);

                    objectWriter.ResetByteRunInterruptionOffsets(nodeContents);

                    // Build symbol definition map.
                    objectWriter.BuildSymbolDefinitionMap(node, nodeContents.DefinedSymbols);

                    // The DWARF CFI unwind is only implemented for some architectures.
                    TargetArchitecture tarch = factory.Target.Architecture;
                    if (!factory.Target.IsWindows &&
                        (tarch == TargetArchitecture.X64 || tarch == TargetArchitecture.ARM || tarch == TargetArchitecture.ARM64))
                        objectWriter.BuildCFIMap(node);

                    // Build debug location map
                    objectWriter.BuildDebugLocInfoMap(node);

                    Relocation[] relocs = nodeContents.Relocs;
                    int nextRelocOffset = -1;
                    int nextRelocIndex = -1;
                    if (relocs.Length > 0)
                    {
                        nextRelocOffset = relocs[0].Offset;
                        nextRelocIndex = 0;
                    }

                    int i = 0;

                    while (i < nodeContents.Data.Length)
                    {
                        // Emit symbol definitions if necessary
                        objectWriter.EmitSymbolDefinition(i);

                        // Emit CFI codes for the given offset.
                        objectWriter.EmitCFICodes(i);

                        // Emit debug loc info if needed.
                        objectWriter.EmitDebugLocInfo(i);

                        if (i == nextRelocOffset)
                        {
                            Relocation reloc = relocs[nextRelocIndex];

                            long delta;
                            unsafe
                            {
                                fixed (void* location = &nodeContents.Data[i])
                                {
                                    delta = Relocation.ReadValue(reloc.RelocType, location);
                                }
                            }
                            int size = objectWriter.EmitSymbolReference(reloc.Target, (int)delta, reloc.RelocType);

                            // Emit a copy of original Thumb2/ARM64 instruction that came from RyuJIT

                            switch (reloc.RelocType)
                            {
                                case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                                case RelocType.IMAGE_REL_BASED_THUMB_BRANCH24:
                                case RelocType.IMAGE_REL_BASED_ARM64_BRANCH26:
                                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L:

                                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12:
                                case RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC:
                                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21:
                                case RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12:
                                case RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12:
                                case RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL:
                                    unsafe
                                    {
                                        fixed (void* location = &nodeContents.Data[i])
                                        {
                                            objectWriter.EmitBytes((IntPtr)location, size);
                                        }
                                    }
                                    break;
                            }

                            // Update nextRelocIndex/Offset
                            if (++nextRelocIndex < relocs.Length)
                            {
                                nextRelocOffset = relocs[nextRelocIndex].Offset;
                            }
                            else
                            {
                                // This is the last reloc. Set the next reloc offset to -1 in case the last reloc has a zero size,
                                // which means the reloc does not have vacant bytes corresponding to in the data buffer. E.g,
                                // IMAGE_REL_THUMB_BRANCH24 is a kind of 24-bit reloc whose bits scatte over the instruction that
                                // references it. We do not vacate extra bytes in the data buffer for this kind of reloc.
                                nextRelocOffset = -1;
                            }
                            i += size;
                        }
                        else
                        {
                            int offsetIndex = Array.IndexOf(objectWriter._byteInterruptionOffsets, true, i + 1, nodeContents.Data.Length - i - 1);

                            int nextOffset = offsetIndex == -1 ? nodeContents.Data.Length : offsetIndex;

                            unsafe
                            {
                                // Todo: Use Span<T> instead once it's available to us in this repo
                                fixed (byte* pContents = &nodeContents.Data[i])
                                {
                                    objectWriter.EmitBytes((IntPtr)(pContents), nextOffset - i);
                                    i += nextOffset - i;
                                }
                            }

                        }
                    }
                    Debug.Assert(i == nodeContents.Data.Length);

                    // It is possible to have a symbol just after all of the data.
                    objectWriter.EmitSymbolDefinition(nodeContents.Data.Length);

                    // Publish Windows unwind info.
                    if (factory.Target.IsWindows)
                        objectWriter.PublishUnwindInfo(node);

                    // Emit the last CFI to close the frame.
                    objectWriter.EmitCFICodes(nodeContents.Data.Length);

                    // Generate debug info if we have sequence points, or on Windows, we can also
                    // generate even if no sequence points.
                    bool generateDebugInfo = objectWriter.HasFunctionDebugInfo();
                    generateDebugInfo |= factory.Target.IsWindows && objectWriter.HasModuleDebugInfo();

                    if (generateDebugInfo)
                    {
                        objectWriter.EmitDebugVarInfo(node);
                        objectWriter.EmitDebugEHClauseInfo(node);
                        objectWriter.EmitDebugFunctionInfo(node, nodeContents.Data.Length);
                    }

                    // Ensure any allocated MethodTables have debug info
                    if (node is ConstructedEETypeNode MethodTable)
                    {
                        objectWriter._userDefinedTypeDescriptor.GetTypeIndex(MethodTable.Type, needsCompleteType: true);
                    }
                }

                // Ensure all fields associated with generated static bases have debug info
                foreach (MetadataType typeWithStaticBase in objectWriter._nodeFactory.MetadataManager.GetTypesWithStaticBases())
                    objectWriter._userDefinedTypeDescriptor.GetTypeIndex(typeWithStaticBase, needsCompleteType: true);

                // Native side of the object writer is going to do more native memory allocations.
                // Free up as much memory as possible so that we don't get OOM killed.
                // This is potentially a waste of time. We're about to end the process and let the
                // OS "garbage collect" the entire address space.
                var gcInfo = GC.GetGCMemoryInfo();

                if (logger.IsVerbose)
                    logger.LogMessage($"Memory stats: {gcInfo.TotalCommittedBytes} bytes committed, {gcInfo.TotalAvailableMemoryBytes} bytes available");

                if (gcInfo.TotalCommittedBytes > gcInfo.TotalAvailableMemoryBytes / 5)
                {
                    if (logger.IsVerbose)
                        logger.LogMessage($"Freeing up memory");

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                }

                if (logger.IsVerbose)
                    logger.LogMessage($"Emitting debug information");

                objectWriter.EmitDebugModuleInfo();

                succeeded = true;
            }
            finally
            {
                if (logger.IsVerbose)
                    logger.LogMessage($"Finalizing output to '{objectFilePath}'...");

                objectWriter.Dispose();

                if (!succeeded)
                {
                    // If there was an exception while generating the OBJ file, make sure we don't leave the unfinished
                    // object file around.
                    try
                    {
                        File.Delete(objectFilePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetPointerTypeIndex(IntPtr objWriter, PointerTypeDescriptor pointerDescriptor);

        uint ITypesDebugInfoWriter.GetPointerTypeIndex(PointerTypeDescriptor pointerDescriptor)
        {
            return GetPointerTypeIndex(_nativeObjectWriter, pointerDescriptor);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetMemberFunctionTypeIndex(IntPtr objWriter, MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes);

        uint ITypesDebugInfoWriter.GetMemberFunctionTypeIndex(MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes)
        {
            return GetMemberFunctionTypeIndex(_nativeObjectWriter, memberDescriptor, argumentTypes);
        }

        [DllImport(NativeObjectWriterFileName)]
        private static extern uint GetMemberFunctionIdTypeIndex(IntPtr objWriter, MemberFunctionIdTypeDescriptor memberIdDescriptor);

        uint ITypesDebugInfoWriter.GetMemberFunctionId(MemberFunctionIdTypeDescriptor memberIdDescriptor)
        {
            return GetMemberFunctionIdTypeIndex(_nativeObjectWriter, memberIdDescriptor);
        }

        private static string GetLLVMTripleFromTarget(TargetDetails target)
        {
            // We create a triple based on the Target
            // See https://clang.llvm.org/docs/CrossCompilation.html#target-triple
            // Detect the LLVM arch
            string arch;
            // Not used
            string sub = string.Empty;
            switch (target.Architecture)
            {
                case TargetArchitecture.ARM:
                    arch = "thumbv7";
                    break;
                case TargetArchitecture.ARM64:
                    arch = "aarch64";
                    break;
                case TargetArchitecture.X64:
                    arch = "x86_64";
                    break;
                case TargetArchitecture.X86:
                    arch = "i686";
                    break;
                case TargetArchitecture.Wasm32:
                    arch = "wasm32";
                    break;
                default:
                    throw new InvalidOperationException($"The architecture `{target.Architecture}` is not supported by ObjectWriter");
            }

            string vendor;
            string sys;
            string abi;
            switch (target.OperatingSystem)
            {
                case TargetOS.Windows:
                    vendor = "pc";
                    sys = "win32";
                    abi = "windows";
                    break;
                case TargetOS.Linux:
                case TargetOS.FreeBSD:
                case TargetOS.NetBSD:
                    vendor = "pc";
                    sys = "linux";
                    abi = "elf";
                    break;
                case TargetOS.OSX:
                    vendor = "apple";
                    sys = "darwin16";
                    abi = "macho";
                    break;
                case TargetOS.MacCatalyst:
                    vendor = "apple";
                    sys = target.Architecture == TargetArchitecture.X64 ? "ios13.5" :"ios14.2";
                    abi = "macabi";
                    break;
                case TargetOS.iOS:
                    vendor = "apple";
                    sys = "ios11.0";
                    abi = "macho";
                    break;
                case TargetOS.iOSSimulator:
                    vendor = "apple";
                    sys = "ios11.0";
                    abi = "simulator";
                    break;
                case TargetOS.tvOS:
                    vendor = "apple";
                    sys = "tvos11.0";
                    abi = "macho";
                    break;
                case TargetOS.tvOSSimulator:
                    vendor = "apple";
                    sys = "tvos11.0";
                    abi = "simulator";
                    break;
                case TargetOS.WebAssembly:
                    vendor = "unknown";
                    sys = "unknown";
                    abi = "wasm";
                    break;
                default:
                    throw new InvalidOperationException($"The operating system `{target.OperatingSystem}` is not supported by ObjectWriter");
            }

            return $"{arch}{sub}-{vendor}-{sys}-{abi}";
        }

        private enum SymbolRefFlags
        {
            AddressTakenFunction = 0x0001,
        }

        private struct ProgressReporter
        {
            private readonly Logger _logger;
            private readonly int _increment;
            private int _current;

            // Will report progress every (100 / 10) = 10%
            private const int Steps = 10;

            public ProgressReporter(Logger logger, int total)
            {
                _logger = logger;
                _increment = total / Steps;
                _current = 0;
            }

            public void LogProgress()
            {
                _current++;

                int adjusted = _current + Steps - 1;
                if ((adjusted % _increment) == 0)
                {
                    _logger.LogMessage($"{(adjusted / _increment) * (100 / Steps)}%...");
                }
            }
        }
    }
}
