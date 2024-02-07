// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.X64;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// X64 specific portions of ReadyToRunHelperNode
    /// </summary>
    public partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref X64Emitter encoder, bool relocsOnly)
        {
            switch (Id)
            {
                case ReadyToRunHelperId.VirtualCall:
                    {
                        MethodDesc targetMethod = (MethodDesc)Target;

                        Debug.Assert(!targetMethod.OwningType.IsInterface);
                        Debug.Assert(!targetMethod.CanMethodBeInSealedVTable(factory));

                        AddrMode loadFromThisPtr = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int64);
                        encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromThisPtr);

                        int pointerSize = factory.Target.PointerSize;

                        int slot = 0;
                        if (!relocsOnly)
                        {
                            slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                        }
                        Debug.Assert(((NativeSequencePoint[])((INodeWithDebugInfo)this).GetNativeSequencePoints())[1].NativeOffset == encoder.Builder.CountBytes);

                        AddrMode jmpAddrMode = new AddrMode(encoder.TargetRegister.Result, null, EETypeNode.GetVTableOffset(pointerSize) + (slot * pointerSize), 0, AddrModeSize.Int64);
                        encoder.EmitJmpToAddrMode(ref jmpAddrMode);
                    }
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        bool hasLazyStaticConstructor = factory.PreinitializationManager.HasLazyStaticConstructor(target);
                        encoder.EmitLEAQ(encoder.TargetRegister.Result, factory.TypeNonGCStaticsSymbol(target));

                        if (!hasLazyStaticConstructor)
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg0, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int64);
                            encoder.EmitCMP(ref initialized, 0);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        ISortableSymbolNode index = factory.TypeThreadStaticIndex(target);
                        if (index is TypeThreadStaticIndexNode ti && ti.IsInlined)
                        {
                            if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                            {
                                EmitInlineTLSAccess(factory, ref encoder);
                            }
                            else
                            {
                                // First arg: unused address of the TypeManager
                                // encoder.EmitMOV(encoder.TargetRegister.Arg0, 0);

                                // Second arg: -1 (index of inlined storage)
                                encoder.EmitMOV(encoder.TargetRegister.Arg1, -1);

                                encoder.EmitLEAQ(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                                AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg2, null, 0, 0, AddrModeSize.Int64);
                                encoder.EmitCMP(ref initialized, 0);
                                encoder.EmitJNE(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase));

                                EmitInlineTLSAccess(factory, ref encoder);
                            }
                        }
                        else
                        {
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg2, index);

                            // First arg: address of the TypeManager slot that provides the helper with
                            // information about module index and the type manager instance (which is used
                            // for initialization on first access).
                            AddrMode loadFromArg2 = new AddrMode(encoder.TargetRegister.Arg2, null, 0, 0, AddrModeSize.Int64);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, ref loadFromArg2);

                            // Second arg: index of the type in the ThreadStatic section of the modules
                            AddrMode loadFromArg2AndDelta = new AddrMode(encoder.TargetRegister.Arg2, null, factory.Target.PointerSize, 0, AddrModeSize.Int32);
                            encoder.EmitMOV(encoder.TargetRegister.Arg1, ref loadFromArg2AndDelta);

                            ISymbolNode helper = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                            if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                            {
                                encoder.EmitJMP(helper);
                            }
                            else
                            {
                                encoder.EmitLEAQ(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                                AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg2, null, 0, 0, AddrModeSize.Int64);
                                encoder.EmitCMP(ref initialized, 0);
                                encoder.EmitJE(helper);

                                encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase));
                            }
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;

                        encoder.EmitLEAQ(encoder.TargetRegister.Result, factory.TypeGCStaticsSymbol(target));
                        AddrMode loadFromRax = new AddrMode(encoder.TargetRegister.Result, null, 0, 0, AddrModeSize.Int64);
                        encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromRax);

                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg0, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int64);
                            encoder.EmitCMP(ref initialized, 0);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);

                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        DelegateCreationInfo target = (DelegateCreationInfo)Target;

                        if (target.TargetNeedsVTableLookup)
                        {
                            Debug.Assert(!target.TargetMethod.CanMethodBeInSealedVTable(factory));

                            AddrMode loadFromThisPtr = new AddrMode(encoder.TargetRegister.Arg1, null, 0, 0, AddrModeSize.Int64);
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, ref loadFromThisPtr);

                            int slot = 0;
                            if (!relocsOnly)
                                slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, target.TargetMethod, target.TargetMethod.OwningType);

                            Debug.Assert(slot != -1);
                            AddrMode loadFromSlot = new AddrMode(encoder.TargetRegister.Arg2, null, EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize), 0, AddrModeSize.Int64);
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, ref loadFromSlot);
                        }
                        else
                        {
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg2, target.GetTargetNode(factory));
                        }

                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg3, target.Thunk);
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                        }

                        encoder.EmitJMP(target.Constructor);
                    }
                    break;

                case ReadyToRunHelperId.ResolveVirtualFunction:
                    {
                        MethodDesc targetMethod = (MethodDesc)Target;
                        if (targetMethod.OwningType.IsInterface)
                        {
                            encoder.EmitLEAQ(encoder.TargetRegister.Arg1, factory.InterfaceDispatchCell(targetMethod));
                            encoder.EmitJMP(factory.ExternSymbol("RhpResolveInterfaceMethod"));
                        }
                        else
                        {
                            if (relocsOnly)
                                break;

                            AddrMode loadFromThisPtr = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int64);
                            encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromThisPtr);

                            Debug.Assert(!targetMethod.CanMethodBeInSealedVTable(factory));

                            int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                            AddrMode loadFromSlot = new AddrMode(encoder.TargetRegister.Result, null, EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize), 0, AddrModeSize.Int64);
                            encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromSlot);
                            encoder.EmitRET();
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        // emits code that results in ThreadStaticBase referenced in RAX.
        // may trash volatile registers. (there are calls to the slow helper and possibly to platform's TLS support)
        private static void EmitInlineTLSAccess(NodeFactory factory, ref X64Emitter encoder)
        {
            // For factory.Target.IsApplePlatform
            // movq _\Var @TLVP(% rip), % rdi
            // callq * (% rdi)

            throw new NotImplementedException();
        }
    }
}
