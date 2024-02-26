// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.X86;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// X86 specific portions of ReadyToRunHelperNode
    /// </summary>
    public partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            switch (Id)
            {
                case ReadyToRunHelperId.VirtualCall:
                    {
                        MethodDesc targetMethod = (MethodDesc)Target;

                        Debug.Assert(!targetMethod.OwningType.IsInterface);
                        Debug.Assert(!targetMethod.CanMethodBeInSealedVTable(factory));

                        AddrMode loadFromThisPtr = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int32);
                        encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromThisPtr);

                        int pointerSize = factory.Target.PointerSize;

                        int slot = 0;
                        if (!relocsOnly)
                        {
                            slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                        }

                        AddrMode jmpAddrMode = new AddrMode(encoder.TargetRegister.Result, null, EETypeNode.GetVTableOffset(pointerSize) + (slot * pointerSize), 0, AddrModeSize.Int32);
                        encoder.EmitJmpToAddrMode(ref jmpAddrMode);
                    }
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        bool hasLazyStaticConstructor = factory.PreinitializationManager.HasLazyStaticConstructor(target);
                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeNonGCStaticsSymbol(target));

                        if (!hasLazyStaticConstructor)
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int32);
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
                            throw new NotImplementedException();
                        }
                        else
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Result, index);

                            // First arg: address of the TypeManager slot that provides the helper with
                            // information about module index and the type manager instance (which is used
                            // for initialization on first access).
                            AddrMode loadFromEax = new AddrMode(encoder.TargetRegister.Result, null, 0, 0, AddrModeSize.Int32);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, ref loadFromEax);

                            // Second arg: index of the type in the ThreadStatic section of the modules
                            AddrMode loadFromEaxAndDelta = new AddrMode(encoder.TargetRegister.Result, null, factory.Target.PointerSize, 0, AddrModeSize.Int32);
                            encoder.EmitMOV(encoder.TargetRegister.Arg1, ref loadFromEaxAndDelta);

                            ISymbolNode helper = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                            if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                            {
                                encoder.EmitJMP(helper);
                            }
                            else
                            {
                                encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                                AddrMode initialized = new AddrMode(encoder.TargetRegister.Result, null, 0, 0, AddrModeSize.Int32);
                                encoder.EmitCMP(ref initialized, 0);
                                encoder.EmitJE(helper);

                                // Add extra parameter and tail call
                                encoder.EmitStackDup();
                                AddrMode storeAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                                encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);

                                encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase));
                            }
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        bool hasLazyStaticConstructor = factory.PreinitializationManager.HasLazyStaticConstructor(target);
                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeGCStaticsSymbol(target));
                        AddrMode loadFromEax = new AddrMode(encoder.TargetRegister.Result, null, 0, 0, AddrModeSize.Int32);
                        encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromEax);

                        if (!hasLazyStaticConstructor)
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, 0, 0, AddrModeSize.Int32);
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

                        encoder.EmitStackDup();

                        if (target.TargetNeedsVTableLookup)
                        {
                            Debug.Assert(!target.TargetMethod.CanMethodBeInSealedVTable(factory));

                            AddrMode loadFromThisPtr = new AddrMode(encoder.TargetRegister.Arg1, null, 0, 0, AddrModeSize.Int32);
                            encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromThisPtr);

                            int slot = 0;
                            if (!relocsOnly)
                                slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, target.TargetMethod, target.TargetMethod.OwningType);

                            Debug.Assert(slot != -1);
                            AddrMode loadFromSlot = new AddrMode(encoder.TargetRegister.Result, null, EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize), 0, AddrModeSize.Int32);
                            encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromSlot);
                        }
                        else
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Result, target.GetTargetNode(factory));
                        }

                        AddrMode storeAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            AddrMode storeAtEspPlus8 = new AddrMode(Register.ESP, null, 8, 0, AddrModeSize.Int32);
                            encoder.EmitStackDup();
                            encoder.EmitMOV(ref storeAtEspPlus8, encoder.TargetRegister.Result);
                            // TODO: Is it possible to fold this into one MOV?
                            encoder.EmitMOV(encoder.TargetRegister.Result, target.Thunk);
                            encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                            encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);
                        }

                        encoder.EmitJMP(target.Constructor);
                    }
                    break;

                case ReadyToRunHelperId.ResolveVirtualFunction:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
