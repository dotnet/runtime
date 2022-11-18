// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.LoongArch64;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// LoongArch64 specific portions of ReadyToRunHelperNode
    /// </summary>
    public partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref LoongArch64Emitter encoder, bool relocsOnly)
        {
            switch (Id)
            {
                case ReadyToRunHelperId.VirtualCall:
                    {
                        MethodDesc targetMethod = (MethodDesc)Target;

                        Debug.Assert(!targetMethod.OwningType.IsInterface);
                        Debug.Assert(!targetMethod.CanMethodBeInSealedVTable());

                        int pointerSize = factory.Target.PointerSize;

                        int slot = 0;
                        if (!relocsOnly)
                        {
                            slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                        }

                        encoder.EmitLD(encoder.TargetRegister.IntraProcedureCallScratch1, encoder.TargetRegister.Arg0, 0);
                        encoder.EmitLD(encoder.TargetRegister.IntraProcedureCallScratch1, encoder.TargetRegister.IntraProcedureCallScratch1,
                                        EETypeNode.GetVTableOffset(pointerSize) + (slot * pointerSize));
                        encoder.EmitJMP(encoder.TargetRegister.IntraProcedureCallScratch1);
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
                            encoder.EmitADD(encoder.TargetRegister.Arg3, encoder.TargetRegister.Result, -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));
                            encoder.EmitLD(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg3, factory.Target.PointerSize);
                            encoder.EmitXOR(encoder.TargetRegister.IntraProcedureCallScratch1, encoder.TargetRegister.Arg2, 1);
                            encoder.EmitRETIfEqual(encoder.TargetRegister.IntraProcedureCallScratch1);

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg3);

                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeThreadStaticIndex(target));

                        // First arg: address of the TypeManager slot that provides the helper with
                        // information about module index and the type manager instance (which is used
                        // for initialization on first access).
                        encoder.EmitLD(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg2, 0);

                        // Second arg: index of the type in the ThreadStatic section of the modules
                        encoder.EmitLD(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg2, factory.Target.PointerSize);

                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType));
                        }
                        else
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target));
                            encoder.EmitADD(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg2, -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            encoder.EmitLD(encoder.TargetRegister.Arg3, encoder.TargetRegister.Arg2, factory.Target.PointerSize);
                            encoder.EmitXOR(encoder.TargetRegister.IntraProcedureCallScratch1, encoder.TargetRegister.Arg3, 1);
                            encoder.EmitJE(encoder.TargetRegister.IntraProcedureCallScratch1, factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType));

                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;

                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeGCStaticsSymbol(target));
                        encoder.EmitLD(encoder.TargetRegister.Result, encoder.TargetRegister.Result, 0);

                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target));
                            encoder.EmitADD(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg2, -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));
                            encoder.EmitLD(encoder.TargetRegister.Arg3, encoder.TargetRegister.Arg2, factory.Target.PointerSize);
                            encoder.EmitXOR(encoder.TargetRegister.IntraProcedureCallScratch1, encoder.TargetRegister.Arg3, 1);
                            encoder.EmitRETIfEqual(Register.R21);

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg2);

                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        DelegateCreationInfo target = (DelegateCreationInfo)Target;

                        if (target.TargetNeedsVTableLookup)
                        {
                            Debug.Assert(!target.TargetMethod.CanMethodBeInSealedVTable());

                            encoder.EmitLD(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg1, 0);

                            int slot = 0;
                            if (!relocsOnly)
                                slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, target.TargetMethod, target.TargetMethod.OwningType);

                            Debug.Assert(slot != -1);
                            encoder.EmitLD(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg2,
                                            EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize));
                        }
                        else
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, target.GetTargetNode(factory));
                        }

                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            encoder.EmitMOV(encoder.TargetRegister.Arg3, target.Thunk);
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
                        // Not tested
                        encoder.EmitBreak();

                        MethodDesc targetMethod = (MethodDesc)Target;
                        if (targetMethod.OwningType.IsInterface)
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Arg1, factory.InterfaceDispatchCell(targetMethod));
                            encoder.EmitJMP(factory.ExternSymbol("RhpResolveInterfaceMethod"));
                        }
                        else
                        {
                            if (relocsOnly)
                                break;

                            encoder.EmitLD(encoder.TargetRegister.Result, encoder.TargetRegister.Arg0, 0);

                            Debug.Assert(!targetMethod.CanMethodBeInSealedVTable());

                            int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                            encoder.EmitLD(encoder.TargetRegister.Result, encoder.TargetRegister.Result,
                                            EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize));
                            encoder.EmitRET();
                        }
                    }
                    break;


                default:
                    throw new NotImplementedException();
            }
        }
    }
}
