// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

using ILCompiler;
using ILCompiler.DependencyAnalysis;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#if SUPPORT_JIT
using MethodCodeNode = Internal.Runtime.JitSupport.JitMethodCodeNode;
using RyuJitCompilation = ILCompiler.Compilation;
#endif

namespace Internal.JitInterface
{
    unsafe partial class CorInfoImpl
    {
        private const CORINFO_RUNTIME_ABI TargetABI = CORINFO_RUNTIME_ABI.CORINFO_CORERT_ABI;

        private uint OffsetOfDelegateFirstTarget => (uint)(4 * PointerSize); // Delegate::m_functionPointer
        private int SizeOfReversePInvokeTransitionFrame => 2 * PointerSize;

        private RyuJitCompilation _compilation;
        private MethodDebugInformation _debugInfo;
        private MethodCodeNode _methodCodeNode;
        private DebugLocInfo[] _debugLocInfos;
        private DebugVarInfo[] _debugVarInfos;
        private readonly UnboxingMethodDescFactory _unboxingThunkFactory = new UnboxingMethodDescFactory();
        private bool _isFallbackBodyCompilation;
        private DependencyList _additionalDependencies;

        public CorInfoImpl(RyuJitCompilation compilation)
            : this()
        {
            _compilation = compilation;
        }

        private MethodDesc getUnboxingThunk(MethodDesc method)
        {
            return _unboxingThunkFactory.GetUnboxingMethod(method);
        }

        public void CompileMethod(MethodCodeNode methodCodeNodeNeedingCode, MethodIL methodIL = null)
        {
            _methodCodeNode = methodCodeNodeNeedingCode;
            _isFallbackBodyCompilation = methodIL != null;

            if (methodIL == null)
                methodIL = _compilation.GetMethodIL(MethodBeingCompiled);

            try
            {
                CompileMethodInternal(methodCodeNodeNeedingCode, methodIL);
            }
            finally
            {
#if DEBUG
                // RyuJIT makes assumptions around the value of type symbols - in particular, it assumes
                // that type handles and type symbols have a 1:1 relationship. We therefore need to
                // make sure RyuJIT never sees a constructed and unconstructed type symbol for the
                // same type. This check makes sure we didn't accidentally hand out a necessary type symbol
                // that the compilation class didn't agree to handing out.
                // https://github.com/dotnet/runtimelab/issues/1128
                for (int i = 0; i < _codeRelocs.Count; i++)
                {
                    Debug.Assert(_codeRelocs[i].Target.GetType() != typeof(EETypeNode)
                        || _compilation.NecessaryTypeSymbolIfPossible(((EETypeNode)_codeRelocs[i].Target).Type) == _codeRelocs[i].Target);
                }
#endif

                CompileMethodCleanup();
            }
        }

        private enum CFI_OPCODE
        {
            CFI_ADJUST_CFA_OFFSET,    // Offset is adjusted relative to the current one.
            CFI_DEF_CFA_REGISTER,     // New register is used to compute CFA
            CFI_REL_OFFSET,           // Register is saved at offset from the current CFA
            CFI_DEF_CFA               // Take address from register and add offset to it.
        }

        // Get the CFI data in the same shape as clang/LLVM generated one. This improves the compatibility with libunwind and other unwind solutions
        // - Combine in one single block for the whole prolog instead of one CFI block per assembler instruction
        // - Store CFA definition first
        // - Store all used registers in ascending order
        private byte[] CompressARM64CFI(byte[] blobData)
        {
            if (blobData == null || blobData.Length == 0)
            {
                return blobData;
            }

            Debug.Assert(blobData.Length % 8 == 0);

            short spReg = -1;

            int codeOffset = 0;
            short cfaRegister = spReg;
            int cfaOffset = 0;
            int spOffset = 0;

            int[] registerOffset = new int[96];

            for (int i = 0; i < registerOffset.Length; i++)
            {
                registerOffset[i] = int.MinValue;
            }

            int offset = 0;
            while (offset < blobData.Length)
            {
                codeOffset = Math.Max(codeOffset, blobData[offset++]);
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset++];
                short dwarfReg = BitConverter.ToInt16(blobData, offset);
                offset += sizeof(short);
                int cfiOffset = BitConverter.ToInt32(blobData, offset);
                offset += sizeof(int);

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfaRegister = dwarfReg;

                        if (spOffset != 0)
                        {
                            for (int i = 0; i < registerOffset.Length; i++)
                            {
                                if (registerOffset[i] != int.MinValue)
                                {
                                    registerOffset[i] -= spOffset;
                                }
                            }

                            cfaOffset += spOffset;
                            spOffset = 0;
                        }

                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        Debug.Assert(cfaRegister == spReg);
                        registerOffset[dwarfReg] = cfiOffset;
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        if (cfaRegister != spReg)
                        {
                            cfaOffset += cfiOffset;
                        }
                        else
                        {
                            spOffset += cfiOffset;

                            for (int i = 0; i < registerOffset.Length; i++)
                            {
                                if (registerOffset[i] != int.MinValue)
                                {
                                    registerOffset[i] += cfiOffset;
                                }
                            }
                        }
                        break;
                }
            }

            using (MemoryStream cfiStream = new MemoryStream())
            {
                int storeOffset = 0;

                using (BinaryWriter cfiWriter = new BinaryWriter(cfiStream))
                {
                    if (cfaRegister != -1)
                    {
                        cfiWriter.Write((byte)codeOffset);
                        cfiWriter.Write(cfaOffset != 0 ? (byte)CFI_OPCODE.CFI_DEF_CFA : (byte)CFI_OPCODE.CFI_DEF_CFA_REGISTER);
                        cfiWriter.Write(cfaRegister);
                        cfiWriter.Write(cfaOffset);
                        storeOffset = cfaOffset;
                    }
                    else
                    {
                        if (cfaOffset != 0)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_ADJUST_CFA_OFFSET);
                            cfiWriter.Write((short)-1);
                            cfiWriter.Write(cfaOffset);
                        }

                        if (spOffset != 0)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_DEF_CFA);
                            cfiWriter.Write((short)31);
                            cfiWriter.Write(spOffset);
                        }
                    }

                    for (int i = registerOffset.Length - 1; i >= 0; i--)
                    {
                        if (registerOffset[i] != int.MinValue)
                        {
                            cfiWriter.Write((byte)codeOffset);
                            cfiWriter.Write((byte)CFI_OPCODE.CFI_REL_OFFSET);
                            cfiWriter.Write((short)i);
                            cfiWriter.Write(registerOffset[i] + storeOffset);
                        }
                    }
                }

                return cfiStream.ToArray();
            }
        }

        private CORINFO_RUNTIME_LOOKUP_KIND GetLookupKindFromContextSource(GenericContextSource contextSource)
        {
            switch (contextSource)
            {
                case GenericContextSource.MethodParameter:
                    return CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_METHODPARAM;
                case GenericContextSource.TypeParameter:
                    return CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_CLASSPARAM;
                default:
                    Debug.Assert(contextSource == GenericContextSource.ThisObject);
                    return CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_THISOBJ;
            }
        }

        private void ComputeLookup(ref CORINFO_RESOLVED_TOKEN pResolvedToken, object entity, ReadyToRunHelperId helperId, ref CORINFO_LOOKUP lookup)
        {
            if (_compilation.NeedsRuntimeLookup(helperId, entity))
            {
                lookup.lookupKind.needsRuntimeLookup = true;
                lookup.runtimeLookup.signature = null;

                // Do not bother computing the runtime lookup if we are inlining. The JIT is going
                // to abort the inlining attempt anyway.
                if (pResolvedToken.tokenContext != contextFromMethodBeingCompiled())
                {
                    lookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_NOT_SUPPORTED;
                    return;
                }

                MethodDesc contextMethod = methodFromContext(pResolvedToken.tokenContext);

                GenericDictionaryLookup genericLookup = _compilation.ComputeGenericLookup(contextMethod, helperId, entity);

                if (genericLookup.UseHelper)
                {
                    lookup.runtimeLookup.indirections = CORINFO.USEHELPER;
                    lookup.lookupKind.runtimeLookupFlags = (ushort)genericLookup.HelperId;
                    lookup.lookupKind.runtimeLookupArgs = (void*)ObjectToHandle(genericLookup.HelperObject);
                }
                else
                {
                    if (genericLookup.ContextSource == GenericContextSource.MethodParameter)
                    {
                        lookup.runtimeLookup.helper = CorInfoHelpFunc.CORINFO_HELP_RUNTIMEHANDLE_METHOD;
                    }
                    else
                    {
                        lookup.runtimeLookup.helper = CorInfoHelpFunc.CORINFO_HELP_RUNTIMEHANDLE_CLASS;
                    }

                    lookup.runtimeLookup.indirections = (ushort)(genericLookup.NumberOfIndirections + (genericLookup.IndirectLastOffset ? 1 : 0));
                    lookup.runtimeLookup.offset0 = (IntPtr)genericLookup[0];
                    if (genericLookup.NumberOfIndirections > 1)
                    {
                        lookup.runtimeLookup.offset1 = (IntPtr)genericLookup[1];
                        if (genericLookup.IndirectLastOffset)
                            lookup.runtimeLookup.offset2 = IntPtr.Zero;
                    }
                    else if (genericLookup.IndirectLastOffset)
                    {
                        lookup.runtimeLookup.offset1 = IntPtr.Zero;
                    }
                    lookup.runtimeLookup.sizeOffset = CORINFO.CORINFO_NO_SIZE_CHECK;
                    lookup.runtimeLookup.testForFixup = false; // TODO: this will be needed in true multifile
                    lookup.runtimeLookup.testForNull = false;
                    lookup.runtimeLookup.indirectFirstOffset = false;
                    lookup.runtimeLookup.indirectSecondOffset = false;
                    lookup.lookupKind.runtimeLookupFlags = 0;
                    lookup.lookupKind.runtimeLookupArgs = null;
                }

                lookup.lookupKind.runtimeLookupKind = GetLookupKindFromContextSource(genericLookup.ContextSource);
            }
            else
            {
                lookup.lookupKind.needsRuntimeLookup = false;
                ISymbolNode constLookup = _compilation.ComputeConstantLookup(helperId, entity);
                lookup.constLookup = CreateConstLookupToSymbol(constLookup);
            }
        }

        private bool getReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_LOOKUP_KIND pGenericLookupKind, CorInfoHelpFunc id, ref CORINFO_CONST_LOOKUP pLookup)
        {
            switch (id)
            {
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NEW:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NEWARR_1:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_ISINSTANCEOF:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_CHKCAST:
                    return false;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_STATIC_BASE:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;

                        pLookup = CreateConstLookupToSymbol(_compilation.NodeFactory.ReadyToRunHelper(ReadyToRunHelperId.GetNonGCStaticBase, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE:
                    {
                        // Token == 0 means "initialize this class". We only expect RyuJIT to call it for this case.
                        Debug.Assert(pResolvedToken.token == 0 && pResolvedToken.tokenScope == null);
                        Debug.Assert(pGenericLookupKind.needsRuntimeLookup);

                        DefType typeToInitialize = (DefType)MethodBeingCompiled.OwningType;
                        Debug.Assert(typeToInitialize.IsCanonicalSubtype(CanonicalFormKind.Any));

                        DefType helperArg = typeToInitialize.ConvertToSharedRuntimeDeterminedForm();
                        ISymbolNode helper = GetGenericLookupHelper(pGenericLookupKind.runtimeLookupKind, ReadyToRunHelperId.GetNonGCStaticBase, helperArg);
                        pLookup = CreateConstLookupToSymbol(helper);
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GENERIC_HANDLE:
                    {
                        Debug.Assert(pGenericLookupKind.needsRuntimeLookup);

                        ReadyToRunHelperId helperId = (ReadyToRunHelperId)pGenericLookupKind.runtimeLookupFlags;
                        object helperArg = HandleToObject((IntPtr)pGenericLookupKind.runtimeLookupArgs);
                        ISymbolNode helper = GetGenericLookupHelper(pGenericLookupKind.runtimeLookupKind, helperId, helperArg);
                        pLookup = CreateConstLookupToSymbol(helper);
                    }
                    break;
                default:
                    throw new NotImplementedException("ReadyToRun: " + id.ToString());
            }
            return true;
        }

        private void getReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType, ref CORINFO_LOOKUP pLookup)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            fixed (CORINFO_LOOKUP* tmp = &pLookup)
                MemoryHelper.FillMemory((byte*)tmp, 0xcc, sizeof(CORINFO_LOOKUP));
#endif

            MethodDesc expectedTargetMethod = HandleToObject(pTargetMethod.hMethod);
            TypeDesc delegateTypeDesc = HandleToObject(delegateType);

            MethodDesc targetMethod = (MethodDesc)GetRuntimeDeterminedObjectForToken(ref pTargetMethod);

            // If this was a constrained+ldftn sequence, we need to resolve the constraint
            TypeDesc constrainedType = null;
            if (targetConstraint != 0)
            {
                // Should really only be here for static virtuals since constrained ldftn is not allowed elsewhere.
                Debug.Assert(targetMethod.IsVirtual && targetMethod.Signature.IsStatic
                    && pTargetMethod.tokenType != CorInfoTokenKind.CORINFO_TOKENKIND_Ldvirtftn);

                var methodIL = HandleToObject(pTargetMethod.tokenScope);
                var typeOrMethodContext = (pTargetMethod.tokenContext == contextFromMethodBeingCompiled()) ?
                    MethodBeingCompiled : HandleToObject((IntPtr)pTargetMethod.tokenContext);
                var canonConstrainedType = (TypeDesc)ResolveTokenInScope(methodIL, typeOrMethodContext, targetConstraint);
                TypeDesc interfaceType = HandleToObject(pTargetMethod.hClass);
                var interfaceMethod = (MethodDesc)ResolveTokenInScope(methodIL, typeOrMethodContext, pTargetMethod.token);
                constrainedType = (TypeDesc)GetRuntimeDeterminedObjectForToken(methodIL, typeOrMethodContext, targetConstraint);

                MethodDesc directMethod = canonConstrainedType.GetClosestDefType().TryResolveConstraintMethodApprox(interfaceType, interfaceMethod, out bool forceRuntimeLookup);
                if (directMethod != null)
                {
                    // We resolved on a canonical form of the valuetype. Now find the method on the runtime determined form.
                    Debug.Assert(directMethod.OwningType.IsValueType);
                    Debug.Assert(!forceRuntimeLookup);

                    MethodDesc targetOfLookup;
                    if (constrainedType.IsRuntimeDeterminedType)
                        targetOfLookup = _compilation.TypeSystemContext.GetMethodForRuntimeDeterminedType(directMethod.GetTypicalMethodDefinition(), (RuntimeDeterminedType)constrainedType);
                    else if (constrainedType.HasInstantiation)
                        targetOfLookup = _compilation.TypeSystemContext.GetMethodForInstantiatedType(directMethod.GetTypicalMethodDefinition(), (InstantiatedType)constrainedType);
                    else
                        targetOfLookup = directMethod.GetMethodDefinition();

                    if (targetOfLookup.HasInstantiation)
                        targetOfLookup = targetOfLookup.MakeInstantiatedMethod(targetMethod.Instantiation);

                    targetMethod = targetOfLookup;

                    // We resolved the constraint
                    constrainedType = null;
                }
            }

            // We better come up with the same method that getCallInfo came up with, with the only difference being
            // that our targetMethod is RuntimeDetermined.
            Debug.Assert(expectedTargetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific) == targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific));

            bool isLdvirtftn = pTargetMethod.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Ldvirtftn;
            DelegateCreationInfo delegateInfo = _compilation.GetDelegateCtor(delegateTypeDesc, targetMethod, constrainedType, isLdvirtftn);

            if (delegateInfo.NeedsRuntimeLookup)
            {
                pLookup.lookupKind.needsRuntimeLookup = true;

                MethodDesc contextMethod = methodFromContext(pTargetMethod.tokenContext);

                // We should not be inlining these. RyuJIT should have aborted inlining already.
                Debug.Assert(contextMethod == MethodBeingCompiled);

                pLookup.lookupKind.runtimeLookupKind = GetGenericRuntimeLookupKind(contextMethod);
                pLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.DelegateCtor;
                pLookup.lookupKind.runtimeLookupArgs = (void*)ObjectToHandle(delegateInfo);
            }
            else
            {
                pLookup.lookupKind.needsRuntimeLookup = false;
                pLookup.constLookup = CreateConstLookupToSymbol(_compilation.NodeFactory.ReadyToRunHelper(ReadyToRunHelperId.DelegateCtor, delegateInfo));
            }
        }

        private ISymbolNode GetHelperFtnUncached(CorInfoHelpFunc ftnNum)
        {
            ReadyToRunHelper id;

            switch (ftnNum)
            {
                case CorInfoHelpFunc.CORINFO_HELP_THROW:
                    id = ReadyToRunHelper.Throw;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_RETHROW:
                    id = ReadyToRunHelper.Rethrow;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_USER_BREAKPOINT:
                    id = ReadyToRunHelper.DebugBreak;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_OVERFLOW:
                    id = ReadyToRunHelper.Overflow;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_RNGCHKFAIL:
                    id = ReadyToRunHelper.RngChkFail;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FAIL_FAST:
                    id = ReadyToRunHelper.FailFast;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROWNULLREF:
                    id = ReadyToRunHelper.ThrowNullRef;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROWDIVZERO:
                    id = ReadyToRunHelper.ThrowDivZero;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION:
                    id = ReadyToRunHelper.ThrowArgumentOutOfRange;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTEXCEPTION:
                    id = ReadyToRunHelper.ThrowArgument;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROW_NOT_IMPLEMENTED:
                    id = ReadyToRunHelper.ThrowNotImplemented;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED:
                    id = ReadyToRunHelper.ThrowPlatformNotSupported;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF:
                    id = ReadyToRunHelper.WriteBarrier;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF:
                    id = ReadyToRunHelper.CheckedWriteBarrier;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_BYREF:
                    id = ReadyToRunHelper.ByRefWriteBarrier;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_EAX:
                    id = ReadyToRunHelper.WriteBarrier_EAX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_ECX:
                    id = ReadyToRunHelper.WriteBarrier_ECX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_EAX:
                    id = ReadyToRunHelper.CheckedWriteBarrier_EAX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_ECX:
                    id = ReadyToRunHelper.CheckedWriteBarrier_ECX;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ARRADDR_ST:
                    id = ReadyToRunHelper.Stelem_Ref;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LDELEMA_REF:
                    id = ReadyToRunHelper.Ldelema_Ref;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_MEMSET:
                    id = ReadyToRunHelper.MemSet;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MEMCPY:
                    id = ReadyToRunHelper.MemCpy;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
                    id = ReadyToRunHelper.GetRuntimeType;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD:
                    id = ReadyToRunHelper.GetRuntimeMethodHandle;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD:
                    id = ReadyToRunHelper.GetRuntimeFieldHandle;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE:
                    id = ReadyToRunHelper.GetRuntimeTypeHandle;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ARE_TYPES_EQUIVALENT:
                    id = ReadyToRunHelper.AreTypesEquivalent;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_BOX:
                    id = ReadyToRunHelper.Box;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_BOX_NULLABLE:
                    id = ReadyToRunHelper.Box_Nullable;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UNBOX:
                    id = ReadyToRunHelper.Unbox;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UNBOX_NULLABLE:
                    id = ReadyToRunHelper.Unbox_Nullable;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEW_MDARR:
                    id = ReadyToRunHelper.NewMultiDimArr;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWFAST:
                    id = ReadyToRunHelper.NewObject;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWSFAST:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewFast");
                case CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_FINALIZE:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewFinalizable");
                case CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewFastAlign8");
                case CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewFinalizableAlign8");
                case CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8_VC:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewFastMisalign");
                case CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_DIRECT:
                    id = ReadyToRunHelper.NewArray;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_ALIGN8:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewArrayAlign8");
                case CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_VC:
                    return _compilation.NodeFactory.ExternSymbol("RhpNewArray");

                case CorInfoHelpFunc.CORINFO_HELP_STACK_PROBE:
                    return _compilation.NodeFactory.ExternSymbol("RhpStackProbe");

                case CorInfoHelpFunc.CORINFO_HELP_POLL_GC:
                    return _compilation.NodeFactory.ExternSymbol("RhpGcPoll");

                case CorInfoHelpFunc.CORINFO_HELP_LMUL:
                    id = ReadyToRunHelper.LMul;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LMUL_OVF:
                    id = ReadyToRunHelper.LMulOfv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULMUL_OVF:
                    id = ReadyToRunHelper.ULMulOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LDIV:
                    id = ReadyToRunHelper.LDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LMOD:
                    id = ReadyToRunHelper.LMod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULDIV:
                    id = ReadyToRunHelper.ULDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULMOD:
                    id = ReadyToRunHelper.ULMod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LLSH:
                    id = ReadyToRunHelper.LLsh;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LRSH:
                    id = ReadyToRunHelper.LRsh;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LRSZ:
                    id = ReadyToRunHelper.LRsz;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LNG2DBL:
                    id = ReadyToRunHelper.Lng2Dbl;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULNG2DBL:
                    id = ReadyToRunHelper.ULng2Dbl;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_DIV:
                    id = ReadyToRunHelper.Div;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MOD:
                    id = ReadyToRunHelper.Mod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UDIV:
                    id = ReadyToRunHelper.UDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UMOD:
                    id = ReadyToRunHelper.UMod;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_DBL2INT:
                    id = ReadyToRunHelper.Dbl2Int;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2INT_OVF:
                    id = ReadyToRunHelper.Dbl2IntOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2LNG:
                    id = ReadyToRunHelper.Dbl2Lng;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2LNG_OVF:
                    id = ReadyToRunHelper.Dbl2LngOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2UINT:
                    id = ReadyToRunHelper.Dbl2UInt;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2UINT_OVF:
                    id = ReadyToRunHelper.Dbl2UIntOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2ULNG:
                    id = ReadyToRunHelper.Dbl2ULng;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2ULNG_OVF:
                    id = ReadyToRunHelper.Dbl2ULngOvf;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_FLTREM:
                    id = ReadyToRunHelper.FltRem;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBLREM:
                    id = ReadyToRunHelper.DblRem;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FLTROUND:
                    id = ReadyToRunHelper.FltRound;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBLROUND:
                    id = ReadyToRunHelper.DblRound;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_PINVOKE_BEGIN:
                    id = ReadyToRunHelper.PInvokeBegin;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_JIT_PINVOKE_END:
                    id = ReadyToRunHelper.PInvokeEnd;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER:
                    id = ReadyToRunHelper.ReversePInvokeEnter;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT:
                    id = ReadyToRunHelper.ReversePInvokeExit;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTANY:
                    id = ReadyToRunHelper.CheckCastAny;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY:
                    id = ReadyToRunHelper.CheckInstanceAny;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTCLASS:
                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTCLASS_SPECIAL:
                    // TODO: separate helper for the _SPECIAL case
                    id = ReadyToRunHelper.CheckCastClass;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFCLASS:
                    id = ReadyToRunHelper.CheckInstanceClass;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTARRAY:
                    id = ReadyToRunHelper.CheckCastArray;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFARRAY:
                    id = ReadyToRunHelper.CheckInstanceArray;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTINTERFACE:
                    id = ReadyToRunHelper.CheckCastInterface;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFINTERFACE:
                    id = ReadyToRunHelper.CheckInstanceInterface;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_MON_ENTER:
                    id = ReadyToRunHelper.MonitorEnter;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MON_EXIT:
                    id = ReadyToRunHelper.MonitorExit;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_MON_ENTER_STATIC:
                    id = ReadyToRunHelper.MonitorEnterStatic;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MON_EXIT_STATIC:
                    id = ReadyToRunHelper.MonitorExitStatic;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GVMLOOKUP_FOR_SLOT:
                    id = ReadyToRunHelper.GVMLookupForSlot;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL:
                    id = ReadyToRunHelper.TypeHandleToRuntimeType;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_GETREFANY:
                    id = ReadyToRunHelper.GetRefAny;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL:
                    id = ReadyToRunHelper.TypeHandleToRuntimeTypeHandle;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GETCURRENTMANAGEDTHREADID:
                    id = ReadyToRunHelper.GetCurrentManagedThreadId;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_VALIDATE_INDIRECT_CALL:
                    return _compilation.NodeFactory.ExternIndirectSymbol("__guard_check_icall_fptr");
                case CorInfoHelpFunc.CORINFO_HELP_DISPATCH_INDIRECT_CALL:
                    return _compilation.NodeFactory.ExternIndirectSymbol("__guard_dispatch_icall_fptr");

                default:
                    throw new NotImplementedException(ftnNum.ToString());
            }

            string mangledName;
            MethodDesc methodDesc;
            JitHelper.GetEntryPoint(_compilation.TypeSystemContext, id, out mangledName, out methodDesc);
            Debug.Assert(mangledName != null || methodDesc != null);

            ISymbolNode entryPoint;
            if (mangledName != null)
                entryPoint = _compilation.NodeFactory.ExternSymbol(mangledName);
            else
                entryPoint = _compilation.NodeFactory.MethodEntrypoint(methodDesc);

            return entryPoint;
        }

        private void getFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
        {
            MethodDesc method = HandleToObject(ftn);

            // TODO: Implement MapMethodDeclToMethodImpl from CoreCLR
            if (method.IsVirtual &&
                method.OwningType is MetadataType mdType &&
                mdType.VirtualMethodImplsForType.Length > 0)
            {
                throw new NotImplementedException("getFunctionEntryPoint");
            }

            pResult = CreateConstLookupToSymbol(_compilation.NodeFactory.MethodEntrypoint(method));
        }

        private bool canTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix)
        {
            // Assume we can tail call unless proved otherwise
            bool result = true;

            if (!fIsTailPrefix)
            {
                MethodDesc caller = HandleToObject(callerHnd);

                if (caller.IsNoInlining)
                {
                    // Do not tailcall from methods that are marked as noinline (people often use no-inline
                    // to mean "I want to always see this method in stacktrace")
                    result = false;
                }
            }

            return result;
        }

        private InfoAccessType constructStringLiteral(CORINFO_MODULE_STRUCT_* module, mdToken metaTok, ref void* ppValue)
        {
            MethodIL methodIL = (MethodIL)HandleToObject((IntPtr)module);

            ISymbolNode stringObject;
            if (metaTok == (mdToken)CorConstants.CorTokenType.mdtString)
            {
                stringObject = _compilation.NodeFactory.SerializedStringObject("");
            }
            else
            {
                object literal = methodIL.GetObject((int)metaTok);
                stringObject = _compilation.NodeFactory.SerializedStringObject((string)literal);
            }
            ppValue = (void*)ObjectToHandle(stringObject);
            return stringObject.RepresentsIndirectionCell ? InfoAccessType.IAT_PVALUE : InfoAccessType.IAT_VALUE;
        }

        enum RhEHClauseKind
        {
            RH_EH_CLAUSE_TYPED = 0,
            RH_EH_CLAUSE_FAULT = 1,
            RH_EH_CLAUSE_FILTER = 2
        }

        private ObjectNode.ObjectData EncodeEHInfo()
        {
            var builder = new ObjectDataBuilder();
            builder.RequireInitialAlignment(1);

            int totalClauses = _ehClauses.Length;

            // Count the number of special markers that will be needed
            for (int i = 1; i < _ehClauses.Length; i++)
            {
                ref CORINFO_EH_CLAUSE clause = ref _ehClauses[i];
                ref CORINFO_EH_CLAUSE previousClause = ref _ehClauses[i - 1];

                if ((previousClause.TryOffset == clause.TryOffset) &&
                    (previousClause.TryLength == clause.TryLength) &&
                    ((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_SAMETRY) == 0))
                {
                    totalClauses++;
                }
            }

            builder.EmitCompressedUInt((uint)totalClauses);

            for (int i = 0; i < _ehClauses.Length; i++)
            {
                ref CORINFO_EH_CLAUSE clause = ref _ehClauses[i];

                if (i > 0)
                {
                    ref CORINFO_EH_CLAUSE previousClause = ref _ehClauses[i - 1];

                    // If the previous clause has same try offset and length as the current clause,
                    // but belongs to a different try block (CORINFO_EH_CLAUSE_SAMETRY is not set),
                    // emit a special marker to allow runtime distinguish this case.
                    if ((previousClause.TryOffset == clause.TryOffset) &&
                        (previousClause.TryLength == clause.TryLength) &&
                        ((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_SAMETRY) == 0))
                    {
                        builder.EmitCompressedUInt(0);
                        builder.EmitCompressedUInt((uint)RhEHClauseKind.RH_EH_CLAUSE_FAULT);
                        builder.EmitCompressedUInt(0);
                    }
                }

                RhEHClauseKind clauseKind;

                if (((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_FAULT) != 0) ||
                    ((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_FINALLY) != 0))
                {
                    clauseKind = RhEHClauseKind.RH_EH_CLAUSE_FAULT;
                }
                else
                if ((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_FILTER) != 0)
                {
                    clauseKind = RhEHClauseKind.RH_EH_CLAUSE_FILTER;
                }
                else
                {
                    clauseKind = RhEHClauseKind.RH_EH_CLAUSE_TYPED;
                }

                builder.EmitCompressedUInt((uint)clause.TryOffset);

                // clause.TryLength returned by the JIT is actually end offset...
                // https://github.com/dotnet/runtime/issues/5282
                int tryLength = (int)clause.TryLength - (int)clause.TryOffset;
                builder.EmitCompressedUInt((uint)((tryLength << 2) | (int)clauseKind));

                switch (clauseKind)
                {
                    case RhEHClauseKind.RH_EH_CLAUSE_TYPED:
                        {
                            builder.EmitCompressedUInt(clause.HandlerOffset);

                            var methodIL = (MethodIL)HandleToObject((IntPtr)_methodScope);
                            var type = (TypeDesc)methodIL.GetObject((int)clause.ClassTokenOrOffset);

                            // Once https://github.com/dotnet/corert/issues/3460 is done, this should be an assert.
                            // Throwing InvalidProgram is not great, but we want to do *something* if this happens
                            // because doing nothing means problems at runtime. This is not worth piping a
                            // a new exception with a fancy message for.
                            if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                                ThrowHelper.ThrowInvalidProgramException();

                            var typeSymbol = _compilation.NecessaryTypeSymbolIfPossible(type);

                            RelocType rel = (_compilation.NodeFactory.Target.IsWindows) ?
                                RelocType.IMAGE_REL_BASED_ABSOLUTE :
                                RelocType.IMAGE_REL_BASED_RELPTR32;

                            if (_compilation.NodeFactory.Target.Abi == TargetAbi.Jit)
                                rel = RelocType.IMAGE_REL_BASED_REL32;

                            builder.EmitReloc(typeSymbol, rel);
                        }
                        break;
                    case RhEHClauseKind.RH_EH_CLAUSE_FAULT:
                        builder.EmitCompressedUInt(clause.HandlerOffset);
                        break;
                    case RhEHClauseKind.RH_EH_CLAUSE_FILTER:
                        builder.EmitCompressedUInt(clause.HandlerOffset);
                        builder.EmitCompressedUInt(clause.ClassTokenOrOffset);
                        break;
                }
            }

            return builder.ToObjectData();
        }

        private void setVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
        {
            var methodIL = (MethodIL)HandleToObject((IntPtr)_methodScope);

            MethodSignature sig = methodIL.OwningMethod.Signature;
            int numLocals = methodIL.GetLocals().Length;

            ArrayBuilder<DebugVarRangeInfo>[] debugVarInfoBuilders = new ArrayBuilder<DebugVarRangeInfo>[(sig.IsStatic ? 0 : 1) + sig.Length + numLocals];

            for (uint i = 0; i < cVars; i++)
            {
                uint varNumber = vars[i].varNumber;
                if (varNumber < debugVarInfoBuilders.Length)
                    debugVarInfoBuilders[varNumber].Add(new DebugVarRangeInfo(vars[i].startOffset, vars[i].endOffset, vars[i].varLoc));
            }

            var debugVarInfos = new ArrayBuilder<DebugVarInfo>();
            for (uint i = 0; i < debugVarInfoBuilders.Length; i++)
            {
                if (debugVarInfoBuilders[i].Count > 0)
                {
                    debugVarInfos.Add(new DebugVarInfo(i, debugVarInfoBuilders[i].ToArray()));
                }
            }

            _debugVarInfos = debugVarInfos.ToArray();

            // JIT gave the ownership of this to us, so need to free this.
            freeArray(vars);
        }

        /// <summary>
        /// Create a DebugLocInfo which is a table from native offset to source line.
        /// using native to il offset (pMap) and il to source line (_sequencePoints).
        /// </summary>
        private void setBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
        {
            Debug.Assert(_debugLocInfos == null);

            int largestILOffset = 0; // All epiloges point to the largest IL offset.
            for (int i = 0; i < cMap; i++)
            {
                OffsetMapping nativeToILInfo = pMap[i];
                int currectILOffset = (int)nativeToILInfo.ilOffset;
                if (currectILOffset > largestILOffset) // Special offsets are negative.
                {
                    largestILOffset = currectILOffset;
                }
            }

            ArrayBuilder<DebugLocInfo> debugLocInfos = new ArrayBuilder<DebugLocInfo>();
            for (int i = 0; i < cMap; i++)
            {
                OffsetMapping* nativeToILInfo = &pMap[i];
                int ilOffset = (int)nativeToILInfo->ilOffset;
                switch (ilOffset)
                {
                    case (int)MappingTypes.PROLOG:
                        ilOffset = 0;
                        break;
                    case (int)MappingTypes.EPILOG:
                        ilOffset = largestILOffset;
                        break;
                    case (int)MappingTypes.NO_MAPPING:
                        continue;
                }

                debugLocInfos.Add(new DebugLocInfo((int)nativeToILInfo->nativeOffset, ilOffset));
            }

            if (debugLocInfos.Count > 0)
            {
                _debugLocInfos = debugLocInfos.ToArray();
            }

            freeArray(pMap);
        }

        private void SetDebugInformation(IMethodNode methodCodeNodeNeedingCode, MethodIL methodIL)
        {
            _debugInfo = _compilation.GetDebugInfo(methodIL);
        }

        private ISymbolNode GetGenericLookupHelper(CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind, ReadyToRunHelperId helperId, object helperArgument)
        {
            if (runtimeLookupKind == CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_THISOBJ
                || runtimeLookupKind == CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_CLASSPARAM)
            {
                return _compilation.NodeFactory.ReadyToRunHelperFromTypeLookup(helperId, helperArgument, MethodBeingCompiled.OwningType);
            }

            Debug.Assert(runtimeLookupKind == CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_METHODPARAM);
            return _compilation.NodeFactory.ReadyToRunHelperFromDictionaryLookup(helperId, helperArgument, MethodBeingCompiled);
        }

        private CorInfoHelpFunc getCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing)
        {
            TypeDesc type = HandleToObject(pResolvedToken.hClass);

            CorInfoHelpFunc helper;

            if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
            {
                // In shared code just use the catch-all helper for type variables, as the same
                // code may be used for interface/array/class instantiations
                //
                // We may be able to take advantage of constraints to select a specialized helper.
                // This optimizations does not seem to be warranted at the moment.
                helper = CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY;
            }
            else if (type.IsInterface)
            {
                // If it is an interface, use the fast interface helper
                helper = CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFINTERFACE;
            }
            else if (type.IsArray)
            {
                // If it is an array, use the fast array helper
                helper = CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFARRAY;
            }
            else if (type.IsDefType)
            {
                helper = CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFCLASS;
#if !SUPPORT_JIT
                // When this assert is hit, we'll have to do something with the class checks in RyuJIT
                // Frozen strings might end up failing inlined checks generated by RyuJIT for sealed classes.
                Debug.Assert(!_compilation.NodeFactory.CompilationModuleGroup.CanHaveReferenceThroughImportTable);
#endif
            }
            else
            {
                // Otherwise, use the slow helper
                helper = CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY;
            }

            if (fThrowing)
            {
                int delta = CorInfoHelpFunc.CORINFO_HELP_CHKCASTANY - CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY;

                Debug.Assert(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFINTERFACE + delta
                    == CorInfoHelpFunc.CORINFO_HELP_CHKCASTINTERFACE);
                Debug.Assert(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFARRAY + delta
                    == CorInfoHelpFunc.CORINFO_HELP_CHKCASTARRAY);
                Debug.Assert(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFCLASS + delta
                    == CorInfoHelpFunc.CORINFO_HELP_CHKCASTCLASS);

                helper += delta;
            }

            return helper;
        }

        private CorInfoHelpFunc getNewHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref bool pHasSideEffects)
        {
            TypeDesc type = HandleToObject(pResolvedToken.hClass);

            Debug.Assert(!type.IsString && !type.IsArray && !type.IsCanonicalDefinitionType(CanonicalFormKind.Any));

            pHasSideEffects = type.HasFinalizer;

            if (type.RequiresAlign8())
            {
                if (type.HasFinalizer)
                    return CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE;

                if (type.IsValueType)
                    return CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8_VC;

                return CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_ALIGN8;
            }

            if (type.HasFinalizer)
                return CorInfoHelpFunc.CORINFO_HELP_NEWSFAST_FINALIZE;

            return CorInfoHelpFunc.CORINFO_HELP_NEWSFAST;
        }

        private CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls)
        {
            TypeDesc type = HandleToObject(arrayCls);

            Debug.Assert(type.IsArray);

            if (type.RequiresAlign8())
                return CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_ALIGN8;

            return CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_VC;
        }

        private IMethodNode GetMethodEntrypoint(CORINFO_MODULE_STRUCT_* pScope, MethodDesc method)
        {
            bool isUnboxingThunk = method.IsUnboxingThunk();
            if (isUnboxingThunk)
            {
                method = method.GetUnboxedMethod();
            }

            if (method.HasInstantiation || method.OwningType.HasInstantiation)
            {
                MethodIL methodIL = (MethodIL)HandleToObject((IntPtr)pScope);
                _compilation.DetectGenericCycles(methodIL.OwningMethod, method);
            }

            return _compilation.NodeFactory.MethodEntrypoint(method, isUnboxingThunk);
        }

        private static bool IsTypeSpecForTypicalInstantiation(TypeDesc t)
        {
            Instantiation inst = t.Instantiation;
            for (int i = 0; i < inst.Length; i++)
            {
                var arg = inst[i] as SignatureTypeVariable;
                if (arg == null || arg.Index != i)
                    return false;
            }
            return true;
        }

        private void getCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            MemoryHelper.FillMemory((byte*)pResult, 0xcc, Marshal.SizeOf<CORINFO_CALL_INFO>());
#endif
            MethodDesc method = HandleToObject(pResolvedToken.hMethod);

            // Spec says that a callvirt lookup ignores static methods. Since static methods
            // can't have the exact same signature as instance methods, a lookup that found
            // a static method would have never found an instance method.
            if (method.Signature.IsStatic && (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_CALLVIRT) != 0)
            {
                throw new BadImageFormatException();
            }

            TypeDesc exactType = HandleToObject(pResolvedToken.hClass);

            TypeDesc constrainedType = null;
            if (pConstrainedResolvedToken != null)
            {
                constrainedType = HandleToObject(pConstrainedResolvedToken->hClass);
            }

            bool resolvedConstraint = false;
            bool forceUseRuntimeLookup = false;
            bool targetIsFatFunctionPointer = false;
            bool useFatCallTransform = false;

            MethodDesc methodAfterConstraintResolution = method;
            if (constrainedType == null)
            {
                pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM;
            }
            else
            {
                // We have a "constrained." call.  Try a partial resolve of the constraint call.  Note that this
                // will not necessarily resolve the call exactly, since we might be compiling
                // shared generic code - it may just resolve it to a candidate suitable for
                // JIT compilation, and require a runtime lookup for the actual code pointer
                // to call.

                MethodDesc directMethod = constrainedType.GetClosestDefType().TryResolveConstraintMethodApprox(exactType, method, out forceUseRuntimeLookup);
                if (directMethod == null && constrainedType.IsEnum)
                {
                    // Constrained calls to methods on enum methods resolve to System.Enum's methods. System.Enum is a reference
                    // type though, so we would fail to resolve and box. We have a special path for those to avoid boxing.
                    directMethod = _compilation.TypeSystemContext.TryResolveConstrainedEnumMethod(constrainedType, method);
                }

                if (directMethod != null)
                {
                    // Either
                    //    1. no constraint resolution at compile time (!directMethod)
                    // OR 2. no code sharing lookup in call
                    // OR 3. we have have resolved to an instantiating stub

                    methodAfterConstraintResolution = directMethod;

                    Debug.Assert(!methodAfterConstraintResolution.OwningType.IsInterface);
                    resolvedConstraint = true;
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM;

                    exactType = constrainedType;
                }
                else if (method.Signature.IsStatic)
                {
                    Debug.Assert(method.OwningType.IsInterface);
                    exactType = constrainedType;
                }
                else if (constrainedType.IsValueType)
                {
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_BOX_THIS;
                }
                else
                {
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_DEREF_THIS;
                }
            }

            MethodDesc targetMethod = methodAfterConstraintResolution;

            //
            // Initialize callee context used for inlining and instantiation arguments
            //


            if (targetMethod.HasInstantiation)
            {
                pResult->contextHandle = contextFromMethod(targetMethod);
                pResult->exactContextNeedsRuntimeLookup = targetMethod.IsSharedByGenericInstantiations;
            }
            else
            {
                pResult->contextHandle = contextFromType(exactType);
                pResult->exactContextNeedsRuntimeLookup = exactType.IsCanonicalSubtype(CanonicalFormKind.Any);

                // Use main method as the context as long as the methods are called on the same type
                if (pResult->exactContextNeedsRuntimeLookup &&
                    pResolvedToken.tokenContext == contextFromMethodBeingCompiled() &&
                    constrainedType == null &&
                    exactType == MethodBeingCompiled.OwningType &&
                    // But don't allow inlining into generic methods since the generic context won't be the same.
                    // The scanner won't be able to predict such inlinig. See https://github.com/dotnet/runtimelab/pull/489
                    !MethodBeingCompiled.HasInstantiation)
                {
                    var methodIL = (MethodIL)HandleToObject((IntPtr)pResolvedToken.tokenScope);
                    var rawMethod = (MethodDesc)methodIL.GetMethodILDefinition().GetObject((int)pResolvedToken.token);
                    if (IsTypeSpecForTypicalInstantiation(rawMethod.OwningType))
                    {
                        pResult->contextHandle = contextFromMethodBeingCompiled();
                    }
                }
            }

            //
            // Determine whether to perform direct call
            //

            bool directCall = false;
            bool resolvedCallVirt = false;

            if (targetMethod.Signature.IsStatic)
            {
                if (constrainedType != null && (!resolvedConstraint || forceUseRuntimeLookup))
                {
                    // Constrained call to static virtual interface method we didn't resolve statically
                    Debug.Assert(targetMethod.IsVirtual && targetMethod.OwningType.IsInterface);
                }
                else
                {
                    // Static methods are always direct calls
                    directCall = true;
                }
            }
            else if ((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_CALLVIRT) == 0 || resolvedConstraint)
            {
                directCall = true;
            }
            else
            {
                if (!targetMethod.IsVirtual ||
                    // Final/sealed has no meaning for interfaces, but lets us devirtualize otherwise
                    !targetMethod.OwningType.IsInterface && (targetMethod.IsFinal || targetMethod.OwningType.IsSealed()))
                {
                    resolvedCallVirt = true;
                    directCall = true;
                }
            }

            pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup = false;

            bool allowInstParam = (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_ALLOWINSTPARAM) != 0;

            if (directCall && targetMethod.IsAbstract)
            {
                ThrowHelper.ThrowBadImageFormatException();
            }

            if (directCall && resolvedConstraint && pResult->exactContextNeedsRuntimeLookup)
            {
                // We want to do a direct call to a shared method on a valuetype. We need to provide
                // a generic context, but the JitInterface doesn't provide a way for us to do it from here.
                // So we do the next best thing and ask RyuJIT to look up a fat pointer.

                pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER;
                pResult->codePointerOrStubLookup.constLookup.accessType = InfoAccessType.IAT_VALUE;
                pResult->nullInstanceCheck = !targetMethod.Signature.IsStatic;

                // We have the canonical version of the method - find the runtime determined version.
                // This is simplified because we know the method is on a valuetype.
                Debug.Assert(targetMethod.OwningType.IsValueType);
                TypeDesc runtimeDeterminedConstrainedType = (TypeDesc)GetRuntimeDeterminedObjectForToken(ref *pConstrainedResolvedToken);

                if (forceUseRuntimeLookup)
                {
                    // The below logic would incorrectly resolve the lookup into the first match we found,
                    // but there was a compile-time ambiguity due to shared code. The correct fix should
                    // use the ConstrainedMethodUseLookupResult dictionary entry so that the exact
                    // dispatch can be computed with the help of the generic dictionary.
                    // We fail the compilation here to avoid bad codegen. This is not actually an invalid program.
                    // https://github.com/dotnet/runtimelab/issues/1431
                    ThrowHelper.ThrowInvalidProgramException();
                }

                MethodDesc targetOfLookup;
                if (runtimeDeterminedConstrainedType.IsRuntimeDeterminedType)
                    targetOfLookup = _compilation.TypeSystemContext.GetMethodForRuntimeDeterminedType(targetMethod.GetTypicalMethodDefinition(), (RuntimeDeterminedType)runtimeDeterminedConstrainedType);
                else if (runtimeDeterminedConstrainedType.HasInstantiation)
                    targetOfLookup = _compilation.TypeSystemContext.GetMethodForInstantiatedType(targetMethod.GetTypicalMethodDefinition(), (InstantiatedType)runtimeDeterminedConstrainedType);
                else
                    targetOfLookup = targetMethod.GetMethodDefinition();
                if (targetOfLookup.HasInstantiation)
                {
                    var methodToGetInstantiation = (MethodDesc)GetRuntimeDeterminedObjectForToken(ref pResolvedToken);
                    targetOfLookup = targetOfLookup.MakeInstantiatedMethod(methodToGetInstantiation.Instantiation);
                }
                Debug.Assert(targetOfLookup.GetCanonMethodTarget(CanonicalFormKind.Specific) == targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific));

                ComputeLookup(ref pResolvedToken,
                    targetOfLookup,
                    ReadyToRunHelperId.MethodEntry,
                    ref pResult->codePointerOrStubLookup);

                targetIsFatFunctionPointer = true;
                useFatCallTransform = true;
            }
            else if (directCall && !allowInstParam && targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).RequiresInstArg())
            {
                // JIT needs a single address to call this method but the method needs a hidden argument.
                // We need a fat function pointer for this that captures both things.
                targetIsFatFunctionPointer = true;

                // JIT won't expect fat function pointers unless this is e.g. delegate creation
                Debug.Assert((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) != 0);

                pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER;

                if (pResult->exactContextNeedsRuntimeLookup)
                {
                    pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup = true;
                    pResult->codePointerOrStubLookup.lookupKind.runtimeLookupFlags = 0;
                    pResult->codePointerOrStubLookup.runtimeLookup.indirections = CORINFO.USEHELPER;

                    // Do not bother computing the runtime lookup if we are inlining. The JIT is going
                    // to abort the inlining attempt anyway.
                    if (pResolvedToken.tokenContext == contextFromMethodBeingCompiled())
                    {
                        MethodDesc contextMethod = methodFromContext(pResolvedToken.tokenContext);
                        pResult->codePointerOrStubLookup.lookupKind.runtimeLookupKind = GetGenericRuntimeLookupKind(contextMethod);
                        pResult->codePointerOrStubLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.MethodEntry;
                        pResult->codePointerOrStubLookup.lookupKind.runtimeLookupArgs = (void*)ObjectToHandle(GetRuntimeDeterminedObjectForToken(ref pResolvedToken));
                    }
                    else
                    {
                        pResult->codePointerOrStubLookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_NOT_SUPPORTED;
                    }
                }
                else
                {
                    pResult->codePointerOrStubLookup.constLookup =
                        CreateConstLookupToSymbol(_compilation.NodeFactory.FatFunctionPointer(targetMethod));
                }
            }
            else if (directCall)
            {
                bool referencingArrayAddressMethod = false;

                if (targetMethod.IsIntrinsic)
                {
                    // If this is an intrinsic method with a callsite-specific expansion, this will replace
                    // the method with a method the intrinsic expands into. If it's not the special intrinsic,
                    // method stays unchanged.
                    var methodIL = (MethodIL)HandleToObject((IntPtr)pResolvedToken.tokenScope);
                    targetMethod = _compilation.ExpandIntrinsicForCallsite(targetMethod, methodIL.OwningMethod);

                    // For multidim array Address method, we pretend the method requires a hidden instantiation argument
                    // (even though it doesn't need one). We'll actually swap the method out for a differnt one with
                    // a matching calling convention later. See ArrayMethod for a description.
                    referencingArrayAddressMethod = targetMethod.IsArrayAddressMethod();
                }

                pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL;

                TypeDesc owningType = targetMethod.OwningType;
                if (owningType.IsString && targetMethod.IsConstructor)
                {
                    // Calling a string constructor doesn't call the actual constructor.
                    pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                        _compilation.NodeFactory.StringAllocator(targetMethod)
                        );
                }
                else if (owningType.IsArray && targetMethod.IsConstructor)
                {
                    // Constructors on arrays are special and don't actually have entrypoints.
                    // That would be fine by itself and wouldn't need special casing. But
                    // constructors on SzArray have a weird property that causes them not to have canonical forms.
                    // int[][] has a .ctor(int32,int32) to construct the jagged array in one go, but its canonical
                    // form of __Canon[] doesn't have the two-parameter constructor. The canonical form would need
                    // to have an unlimited number of constructors to cover stuff like "int[][][][][][]..."
                    pResult->codePointerOrStubLookup.constLookup = default;
                }
                else if (pResult->exactContextNeedsRuntimeLookup)
                {
                    // Nothing to do... The generic handle lookup gets embedded in to the codegen
                    // during the jitting of the call.
                    // (Note: The generic lookup in R2R is performed by a call to a helper at runtime, not by
                    // codegen emitted at crossgen time)

                    targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    Debug.Assert(!forceUseRuntimeLookup);
                    pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                        GetMethodEntrypoint(pResolvedToken.tokenScope, targetMethod)
                        );
                }
                else
                {
                    MethodDesc concreteMethod = targetMethod;
                    targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    ISymbolNode instParam = null;

                    if (targetMethod.RequiresInstMethodDescArg())
                    {
                        instParam = _compilation.NodeFactory.MethodGenericDictionary(concreteMethod);
                    }
                    else if (targetMethod.RequiresInstMethodTableArg() || referencingArrayAddressMethod)
                    {
                        // Ask for a constructed type symbol because we need the vtable to get to the dictionary
                        instParam = _compilation.NodeFactory.ConstructedTypeSymbol(concreteMethod.OwningType);
                    }

                    if (instParam != null)
                    {
                        pResult->instParamLookup = CreateConstLookupToSymbol(instParam);
                    }

                    pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                        GetMethodEntrypoint(pResolvedToken.tokenScope, targetMethod)
                        );
                }

                pResult->nullInstanceCheck = resolvedCallVirt;
            }
            else if (targetMethod.Signature.IsStatic)
            {
                // This should be an unresolved static virtual interface method call. Other static methods should
                // have been handled as a directCall above.
                Debug.Assert(targetMethod.OwningType.IsInterface && targetMethod.IsVirtual && constrainedType != null);

                pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER;

                TypeDesc runtimeDeterminedConstrainedType = (TypeDesc)GetRuntimeDeterminedObjectForToken(ref *pConstrainedResolvedToken);
                MethodDesc runtimeDeterminedInterfaceMethod = (MethodDesc)GetRuntimeDeterminedObjectForToken(ref pResolvedToken);

                ComputeLookup(ref pResolvedToken,
                    new ConstrainedCallInfo(runtimeDeterminedConstrainedType, runtimeDeterminedInterfaceMethod),
                    ReadyToRunHelperId.ConstrainedDirectCall,
                    ref pResult->codePointerOrStubLookup);

                targetIsFatFunctionPointer = true;
                useFatCallTransform = true;
                pResult->nullInstanceCheck = false;
            }
            else if (targetMethod.HasInstantiation)
            {
                // Generic virtual method call support
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN;
                pResult->codePointerOrStubLookup.constLookup.accessType = InfoAccessType.IAT_VALUE;
                pResult->nullInstanceCheck = true;

                MethodDesc targetOfLookup = _compilation.GetTargetOfGenericVirtualMethodCall((MethodDesc)GetRuntimeDeterminedObjectForToken(ref pResolvedToken));

                _compilation.DetectGenericCycles(
                    ((MethodILScope)HandleToObject((IntPtr)pResolvedToken.tokenScope)).OwningMethod,
                    targetOfLookup.GetCanonMethodTarget(CanonicalFormKind.Specific));

                ComputeLookup(ref pResolvedToken,
                    targetOfLookup,
                    ReadyToRunHelperId.MethodHandle,
                    ref pResult->codePointerOrStubLookup);

                // RyuJIT will assert if we report CORINFO_CALLCONV_PARAMTYPE for a result of a ldvirtftn
                // We don't need an instantiation parameter, so let's just not report it. Might be nice to
                // move that assert to some place later though.
                targetIsFatFunctionPointer = true;
            }
            else if ((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) == 0
                && targetMethod.OwningType.IsInterface)
            {
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB;

                if (pResult->exactContextNeedsRuntimeLookup)
                {
                    ComputeLookup(ref pResolvedToken,
                        GetRuntimeDeterminedObjectForToken(ref pResolvedToken),
                        ReadyToRunHelperId.VirtualDispatchCell,
                        ref pResult->codePointerOrStubLookup);
                    Debug.Assert(pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup);
                }
                else
                {
                    pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup = false;
                    pResult->codePointerOrStubLookup.constLookup.accessType = InfoAccessType.IAT_PVALUE;
                    pResult->codePointerOrStubLookup.constLookup.addr = (void*)ObjectToHandle(
                        _compilation.NodeFactory.InterfaceDispatchCell(targetMethod
#if !SUPPORT_JIT
                        , _compilation.NameMangler.GetMangledMethodName(MethodBeingCompiled).ToString()
#endif
                        ));
                }

                pResult->nullInstanceCheck = false;
            }
            else if ((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) == 0
                // Canonically-equivalent types have the same vtable layout. Check the canonical form.
                // We don't want to accidentally ask about Foo<object, __Canon> that may or may not
                // be available to ask vtable questions about.
                // This can happen in inlining that the scanner didn't expect.
                && _compilation.HasFixedSlotVTable(targetMethod.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific)))
            {
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_VTABLE;
                pResult->nullInstanceCheck = true;
            }
            else
            {
                ReadyToRunHelperId helperId;
                if ((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) != 0)
                {
                    pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN;
                    helperId = ReadyToRunHelperId.ResolveVirtualFunction;
                }
                else
                {
                    // CORINFO_CALL_CODE_POINTER tells the JIT that this is indirect
                    // call that should not be inlined.
                    pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER;
                    helperId = ReadyToRunHelperId.VirtualCall;
                }

                // If this is a non-interface call, we actually don't need a runtime lookup to find the target.
                // We don't even need to keep track of the runtime-determined method being called because the system ensures
                // that if e.g. Foo<__Canon>.GetHashCode is needed and we're generating a dictionary for Foo<string>,
                // Foo<string>.GetHashCode is needed too.
                if (pResult->exactContextNeedsRuntimeLookup && targetMethod.OwningType.IsInterface)
                {
                    // We need JitInterface changes to fully support this.
                    // If this is LDVIRTFTN of an interface method that is part of a verifiable delegate creation sequence,
                    // RyuJIT is not going to use this value.
                    Debug.Assert(helperId == ReadyToRunHelperId.ResolveVirtualFunction);
                    pResult->exactContextNeedsRuntimeLookup = false;
                    pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(_compilation.NodeFactory.ExternSymbol("NYI_LDVIRTFTN"));
                }
                else
                {
                    pResult->exactContextNeedsRuntimeLookup = false;
                    targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    // Get the slot defining method to make sure our virtual method use tracking gets this right.
                    // For normal C# code the targetMethod will always be newslot.
                    MethodDesc slotDefiningMethod = targetMethod.IsNewSlot ?
                        targetMethod : MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(targetMethod);

                    pResult->codePointerOrStubLookup.constLookup =
                        CreateConstLookupToSymbol(
                            _compilation.NodeFactory.ReadyToRunHelper(helperId, slotDefiningMethod));
                }

                // The current CoreRT ReadyToRun helpers do not handle null thisptr - ask the JIT to emit explicit null checks
                // TODO: Optimize this
                pResult->nullInstanceCheck = true;
            }

            pResult->hMethod = ObjectToHandle(targetMethod);

            pResult->accessAllowed = CorInfoIsAccessAllowedResult.CORINFO_ACCESS_ALLOWED;

            // We're pretty much done at this point.  Let's grab the rest of the information that the jit is going to
            // need.
            pResult->classFlags = getClassAttribsInternal(targetMethod.OwningType);

            pResult->methodFlags = getMethodAttribsInternal(targetMethod);

            targetIsFatFunctionPointer |= (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_CALLVIRT) != 0 && !(pResult->kind == CORINFO_CALL_KIND.CORINFO_CALL);

            Get_CORINFO_SIG_INFO(targetMethod, &pResult->sig, scope: null, targetIsFatFunctionPointer);
            if (useFatCallTransform)
            {
                pResult->sig.flags |= CorInfoSigInfoFlags.CORINFO_SIGFLAG_FAT_CALL;
            }

            if ((flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_VERIFICATION) != 0)
            {
                if (pResult->hMethod != pResolvedToken.hMethod)
                {
                    pResult->verMethodFlags = getMethodAttribsInternal(targetMethod);
                    Get_CORINFO_SIG_INFO(targetMethod, &pResult->verSig, scope: null);
                }
                else
                {
                    pResult->verMethodFlags = pResult->methodFlags;
                    pResult->verSig = pResult->sig;
                }
            }

            pResult->_wrapperDelegateInvoke = 0;
        }

        private CORINFO_CLASS_STRUCT_* embedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection)
        {
            TypeDesc type = HandleToObject(handle);
            ISymbolNode typeHandleSymbol = _compilation.NecessaryTypeSymbolIfPossible(type);
            CORINFO_CLASS_STRUCT_* result = (CORINFO_CLASS_STRUCT_*)ObjectToHandle(typeHandleSymbol);

            if (typeHandleSymbol.RepresentsIndirectionCell)
            {
                ppIndirection = result;
                return null;
            }
            else
            {
                ppIndirection = null;
                return result;
            }
        }

        private void embedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, ref CORINFO_GENERICHANDLE_RESULT pResult)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            fixed (CORINFO_GENERICHANDLE_RESULT* tmp = &pResult)
                MemoryHelper.FillMemory((byte*)tmp, 0xcc, Marshal.SizeOf<CORINFO_GENERICHANDLE_RESULT>());
#endif
            ReadyToRunHelperId helperId = ReadyToRunHelperId.Invalid;
            object target = null;

            if (!fEmbedParent && pResolvedToken.hMethod != null)
            {
                MethodDesc md = HandleToObject(pResolvedToken.hMethod);
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_METHOD;

                Debug.Assert(md.OwningType == td);

                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)ObjectToHandle(md);

                if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Ldtoken)
                    helperId = ReadyToRunHelperId.MethodHandle;
                else
                {
                    Debug.Assert(pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Method);
                    helperId = ReadyToRunHelperId.MethodDictionary;
                }

                target = GetRuntimeDeterminedObjectForToken(ref pResolvedToken);
            }
            else if (!fEmbedParent && pResolvedToken.hField != null)
            {
                FieldDesc fd = HandleToObject(pResolvedToken.hField);
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_FIELD;
                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)pResolvedToken.hField;

                Debug.Assert(pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Ldtoken);
                helperId = ReadyToRunHelperId.FieldHandle;
                target = GetRuntimeDeterminedObjectForToken(ref pResolvedToken);
            }
            else
            {
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_CLASS;
                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)pResolvedToken.hClass;

                object obj = GetRuntimeDeterminedObjectForToken(ref pResolvedToken);
                target = obj as TypeDesc;
                if (target == null)
                {
                    Debug.Assert(fEmbedParent);

                    if (obj is MethodDesc objAsMethod)
                    {
                        target = objAsMethod.OwningType;
                    }
                    else
                    {
                        Debug.Assert(obj is FieldDesc);
                        target = ((FieldDesc)obj).OwningType;
                    }
                }

                if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_NewObj
                        || pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Newarr
                        || pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Box
                        || pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Constrained)
                {
                    helperId = ReadyToRunHelperId.TypeHandle;
                }
                else if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Casting)
                {
                    helperId = ReadyToRunHelperId.TypeHandleForCasting;
                }
                else if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_Ldtoken)
                {
                    helperId = _compilation.GetLdTokenHelperForType(td);
                }
                else
                {
                    helperId = ReadyToRunHelperId.NecessaryTypeHandle;
                }
            }

            Debug.Assert(pResult.compileTimeHandle != null);

            ComputeLookup(ref pResolvedToken, target, helperId, ref pResult.lookup);
        }

        private CORINFO_METHOD_STRUCT_* embedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection)
        {
            MethodDesc method = HandleToObject(handle);
            ISymbolNode methodHandleSymbol = _compilation.NodeFactory.RuntimeMethodHandle(method);
            CORINFO_METHOD_STRUCT_* result = (CORINFO_METHOD_STRUCT_*)ObjectToHandle(methodHandleSymbol);

            if (methodHandleSymbol.RepresentsIndirectionCell)
            {
                ppIndirection = result;
                return null;
            }
            else
            {
                ppIndirection = null;
                return result;
            }
        }

        private void getMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
        {
            MethodDesc methodDesc = HandleToObject(method);
            int pointerSize = _compilation.TypeSystemContext.Target.PointerSize;
            offsetOfIndirection = (uint)CORINFO_VIRTUALCALL_NO_CHUNK.Value;
            isRelative = false;

            // Normalize to the slot defining method. We don't have slot information for the overrides.
            methodDesc = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(methodDesc);
            Debug.Assert(!methodDesc.CanMethodBeInSealedVTable());

            // Avoid asking about slots on types like Foo<object, __Canon>. We might not have that information.
            // Canonically-equivalent types have the same slots, so ask for Foo<__Canon, __Canon>.
            methodDesc = methodDesc.GetCanonMethodTarget(CanonicalFormKind.Specific);

            int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(_compilation.NodeFactory, methodDesc, methodDesc.OwningType);
            if (slot == -1)
            {
                throw new InvalidOperationException(methodDesc.ToString());
            }

            offsetAfterIndirection = (uint)(EETypeNode.GetVTableOffset(pointerSize) + slot * pointerSize);
        }

        private void expandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_GENERICHANDLE_RESULT pResult)
        {
            // Resolved token as a potentially RuntimeDetermined object.
            MethodDesc method = (MethodDesc)GetRuntimeDeterminedObjectForToken(ref pResolvedToken);

            switch (method.Name)
            {
                case "EETypePtrOf":
                case "MethodTableOf":
                    ComputeLookup(ref pResolvedToken, method.Instantiation[0], ReadyToRunHelperId.TypeHandle, ref pResult.lookup);
                    break;
                case "DefaultConstructorOf":
                    ComputeLookup(ref pResolvedToken, method.Instantiation[0], ReadyToRunHelperId.DefaultConstructor, ref pResult.lookup);
                    break;
                case "AllocatorOf":
                    ComputeLookup(ref pResolvedToken, method.Instantiation[0], ReadyToRunHelperId.ObjectAllocator, ref pResult.lookup);
                    break;
            }
        }

        private uint getMethodAttribs(CORINFO_METHOD_STRUCT_* ftn)
        {
            return getMethodAttribsInternal(HandleToObject(ftn));
        }

        private void* getMethodSync(CORINFO_METHOD_STRUCT_* ftn, ref void* ppIndirection)
        {
            MethodDesc method = HandleToObject(ftn);
            TypeDesc type = method.OwningType;
            ISymbolNode methodSync = _compilation.NecessaryTypeSymbolIfPossible(type);

            void* result = (void*)ObjectToHandle(methodSync);

            if (methodSync.RepresentsIndirectionCell)
            {
                ppIndirection = result;
                return null;
            }
            else
            {
                ppIndirection = null;
                return result;
            }
        }

        private unsafe HRESULT allocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData)
        {
            throw new NotImplementedException("allocPgoInstrumentationBySchema");
        }

        private CORINFO_CLASS_STRUCT_* getLikelyClass(CORINFO_METHOD_STRUCT_* ftnHnd, CORINFO_CLASS_STRUCT_* baseHnd, uint IlOffset, ref uint pLikelihood, ref uint pNumberOfClasses)
        {
            return null;
        }

        private void getAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
        {
            MethodDesc md = HandleToObject(method);

            string externName = _compilation.PInvokeILProvider.GetDirectCallExternName(md);

            pLookup = CreateConstLookupToSymbol(_compilation.NodeFactory.ExternSymbol(externName));
        }

        private void getGSCookie(IntPtr* pCookieVal, IntPtr** ppCookieVal)
        {
            // TODO: fully implement GS cookies

            if (pCookieVal != null)
            {
                if (PointerSize == 4)
                {
                    *pCookieVal = (IntPtr)0x3F796857;
                }
                else
                {
                    *pCookieVal = (IntPtr)0x216D6F6D202C6948;
                }
                *ppCookieVal = null;
            }
            else
            {
                throw new NotImplementedException("getGSCookie");
            }
        }

        private bool pInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* handle, CORINFO_SIG_INFO* callSiteSig)
        {
            // calli is covered by convertPInvokeCalliToCall
            if (handle == null)
            {
#if DEBUG
                MethodSignature methodSignature = (MethodSignature)HandleToObject((IntPtr)callSiteSig->pSig);

                MethodDesc stub = _compilation.PInvokeILProvider.GetCalliStub(
                    methodSignature,
                    ((MetadataType)HandleToObject(callSiteSig->scope).OwningMethod.OwningType).Module);
                Debug.Assert(!IsPInvokeStubRequired(stub));
#endif

                return false;
            }

            MethodDesc method = HandleToObject(handle);

            if (method.IsRawPInvoke())
                return false;

            // Stub is required to trigger precise static constructor
            TypeDesc owningType = method.OwningType;
            if (_compilation.HasLazyStaticConstructor(owningType) && !((MetadataType)owningType).IsBeforeFieldInit)
                return true;

            // We could have given back the PInvoke stub IL to the JIT and let it inline it, without
            // checking whether there is any stub required. Save the JIT from doing the inlining by checking upfront.
            return IsPInvokeStubRequired(method);
        }

        private bool convertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool mustConvert)
        {
            var methodIL = (MethodIL)HandleToObject((IntPtr)pResolvedToken.tokenScope);

            // Suppress recursive expansion of calli in marshaling stubs
            if (methodIL is Internal.IL.Stubs.PInvokeILStubMethodIL)
                return false;

            MethodSignature signature = (MethodSignature)methodIL.GetObject((int)pResolvedToken.token);
            if ((signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == 0)
                return false;

            MethodDesc stub = _compilation.PInvokeILProvider.GetCalliStub(
                signature,
                ((MetadataType)methodIL.OwningMethod.OwningType).Module);
            if (!mustConvert && !IsPInvokeStubRequired(stub))
                return false;

            pResolvedToken.hMethod = ObjectToHandle(stub);
            pResolvedToken.hClass = ObjectToHandle(stub.OwningType);
            return true;
        }

        private bool IsPInvokeStubRequired(MethodDesc method)
        {
            if (_compilation.GetMethodIL(method) is Internal.IL.Stubs.PInvokeILStubMethodIL stub)
                return stub.IsStubRequired;

            // This path is taken for PInvokes replaced by RemovingILProvider
            return true;
        }

        private int SizeOfPInvokeTransitionFrame
        {
            get
            {
                // struct PInvokeTransitionFrame:
                // #ifdef _TARGET_ARM_
                //  m_ChainPointer
                // #endif
                //  m_RIP
                //  m_FramePointer
                //  m_pThread
                //  m_Flags + align (no align for ARM64 that has 64 bit m_Flags)
                //  m_PreserverRegs - RSP
                //      No need to save other preserved regs because of the JIT ensures that there are
                //      no live GC references in callee saved registers around the PInvoke callsite.
                int size = 5 * this.PointerSize;

                if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM)
                    size += this.PointerSize; // m_ChainPointer

                return size;
            }
        }

        private bool canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
        { throw new NotImplementedException("canGetCookieForPInvokeCalliSig"); }

        private void classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_STRUCT_* cls)
        {
        }

        private void setEHcount(uint cEH)
        {
            _ehClauses = new CORINFO_EH_CLAUSE[cEH];
        }

        private void setEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
        {
            _ehClauses[EHnumber] = clause;
        }

        private void reportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
        {
        }

        private void updateEntryPointForTailCall(ref CORINFO_CONST_LOOKUP entryPoint)
        {
        }

        private int* getAddrOfCaptureThreadGlobal(ref void* ppIndirection)
        {
            ppIndirection = null;
            return (int*)ObjectToHandle(_compilation.NodeFactory.ExternSymbol("RhpTrapThreads"));
        }

        private void getFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            MemoryHelper.FillMemory((byte*)pResult, 0xcc, Marshal.SizeOf<CORINFO_FIELD_INFO>());
#endif

            Debug.Assert(((int)flags & ((int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_GET |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_SET |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_ADDRESS |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_INIT_ARRAY)) != 0);

            var field = HandleToObject(pResolvedToken.hField);

            CORINFO_FIELD_ACCESSOR fieldAccessor;
            CORINFO_FIELD_FLAGS fieldFlags = (CORINFO_FIELD_FLAGS)0;
            uint fieldOffset = (field.IsStatic && field.HasRva ? 0xBAADF00D : (uint)field.Offset.AsInt);

            if (field.IsStatic)
            {
                fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_STATIC;

                if (field.HasRva)
                {
                    fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_UNMANAGED;

                    // TODO: Handle the case when the RVA is in the TLS range
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_RVA_ADDRESS;

                    // We are not going through a helper. The constructor has to be triggered explicitly.
                    if (_compilation.HasLazyStaticConstructor(field.OwningType))
                    {
                        fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_INITCLASS;
                    }
                }
                else if (field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    // The JIT wants to know how to access a static field on a generic type. We need a runtime lookup.
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_READYTORUN_HELPER;
                    pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE;

                    // Don't try to compute the runtime lookup if we're inlining. The JIT is going to abort the inlining
                    // attempt anyway.
                    if (pResolvedToken.tokenContext == contextFromMethodBeingCompiled())
                    {
                        MethodDesc contextMethod = methodFromContext(pResolvedToken.tokenContext);

                        FieldDesc runtimeDeterminedField = (FieldDesc)GetRuntimeDeterminedObjectForToken(ref pResolvedToken);

                        ReadyToRunHelperId helperId;

                        // Find out what kind of base do we need to look up.
                        if (field.IsThreadStatic)
                        {
                            helperId = ReadyToRunHelperId.GetThreadStaticBase;
                        }
                        else if (field.HasGCStaticBase)
                        {
                            helperId = ReadyToRunHelperId.GetGCStaticBase;
                        }
                        else
                        {
                            helperId = ReadyToRunHelperId.GetNonGCStaticBase;
                        }

                        // What generic context do we look up the base from.
                        ISymbolNode helper;
                        if (contextMethod.AcquiresInstMethodTableFromThis() || contextMethod.RequiresInstMethodTableArg())
                        {
                            helper = _compilation.NodeFactory.ReadyToRunHelperFromTypeLookup(
                                helperId, runtimeDeterminedField.OwningType, contextMethod.OwningType);
                        }
                        else
                        {
                            Debug.Assert(contextMethod.RequiresInstMethodDescArg());
                            helper = _compilation.NodeFactory.ReadyToRunHelperFromDictionaryLookup(
                                helperId, runtimeDeterminedField.OwningType, contextMethod);
                        }

                        pResult->fieldLookup = CreateConstLookupToSymbol(helper);
                    }
                }
                else
                {
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER;
                    pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_STATIC_BASE;

                    ReadyToRunHelperId helperId = ReadyToRunHelperId.Invalid;
                    CORINFO_FIELD_ACCESSOR intrinsicAccessor;
                    if (field.IsIntrinsic &&
                        (flags & CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_GET) != 0 &&
                        (intrinsicAccessor = getFieldIntrinsic(field)) != (CORINFO_FIELD_ACCESSOR)(-1))
                    {
                        fieldAccessor = intrinsicAccessor;
                    }
                    else if (field.IsThreadStatic)
                    {
                        helperId = ReadyToRunHelperId.GetThreadStaticBase;
                    }
                    else
                    {
                        helperId = field.HasGCStaticBase ?
                            ReadyToRunHelperId.GetGCStaticBase :
                            ReadyToRunHelperId.GetNonGCStaticBase;

                        //
                        // Currently, we only do this optimization for regular statics, but it
                        // looks like it may be permissible to do this optimization for
                        // thread statics as well.
                        //
                        if ((flags & CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_ADDRESS) != 0 &&
                            (fieldAccessor != CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_TLS))
                        {
                            fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_SAFESTATIC_BYREF_RETURN;
                        }
                    }

                    if (helperId != ReadyToRunHelperId.Invalid)
                    {
                        pResult->fieldLookup = CreateConstLookupToSymbol(
                            _compilation.NodeFactory.ReadyToRunHelper(helperId, field.OwningType));
                    }
                }
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_INSTANCE;
            }

            if (field.IsInitOnly)
                fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_FINAL;

            pResult->fieldAccessor = fieldAccessor;
            pResult->fieldFlags = fieldFlags;
            pResult->fieldType = getFieldType(pResolvedToken.hField, &pResult->structType, pResolvedToken.hClass);
            pResult->accessAllowed = CorInfoIsAccessAllowedResult.CORINFO_ACCESS_ALLOWED;
            pResult->offset = fieldOffset;

            // TODO: We need to implement access checks for fields and methods.  See JitInterface.cpp in mrtjit
            //       and STS::AccessCheck::CanAccess.
        }
    }
}
