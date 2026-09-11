// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            Debug.Assert(!encoder.Is64Bit);

            List<WasmExpr> expressions = new List<WasmExpr>();

            switch (Id)
            {
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        ISymbolNode staticBase = factory.TypeNonGCStaticsSymbol(target);

                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            expressions.Add(I32.ConstRVA(staticBase));
                        }
                        else
                        {
                            ISymbolNode helper = factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase);
                            expressions.Add(Local.Get(0));
                            expressions.Add(I32.ConstRVA(staticBase));
                            expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                            expressions.Add(I32.Sub);
                            expressions.Add(I32.ConstRVA(staticBase));
                            EmitManagedTailCall(expressions, helper);
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

                        expressions.Add(Local.Get(0));
                        expressions.Add(I32.ConstRVA(index));
                        expressions.Add(I32.Load(0));
                        expressions.Add(I32.ConstRVA(index));
                        expressions.Add(I32.Load((ulong)factory.Target.PointerSize));

                        ISymbolNode helper;
                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            helper = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                        }
                        else
                        {
                            ISymbolNode staticBase = factory.TypeNonGCStaticsSymbol(target);
                            expressions.Add(I32.ConstRVA(staticBase));
                            expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                            expressions.Add(I32.Sub);
                            helper = factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase);
                        }

                        EmitManagedTailCall(expressions, helper);
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        ISymbolNode gcStaticBase = factory.TypeGCStaticsSymbol(target);

                        if (!factory.PreinitializationManager.HasLazyStaticConstructor(target))
                        {
                            expressions.Add(I32.ConstRVA(gcStaticBase));
                            expressions.Add(I32.Load(0));
                        }
                        else
                        {
                            ISymbolNode nonGCStaticBase = factory.TypeNonGCStaticsSymbol(target);
                            ISymbolNode helper = factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase);
                            expressions.Add(Local.Get(0));
                            expressions.Add(I32.ConstRVA(nonGCStaticBase));
                            expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                            expressions.Add(I32.Sub);
                            expressions.Add(I32.ConstRVA(gcStaticBase));
                            expressions.Add(I32.Load(0));
                            EmitManagedTailCall(expressions, helper);
                        }
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        DelegateCreationInfo target = (DelegateCreationInfo)Target;

                        expressions.Add(Local.Get(0));
                        expressions.Add(Local.Get(1));
                        expressions.Add(Local.Get(2));

                        if (target.TargetNeedsVTableLookup)
                        {
                            Debug.Assert(!target.TargetMethod.CanMethodBeInSealedVTable(factory));
                            expressions.Add(Local.Get(2));
                            expressions.Add(I32.Load(0));

                            int slot = 0;
                            if (!relocsOnly)
                            {
                                slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, target.TargetMethod, target.TargetMethod.OwningType);
                            }

                            Debug.Assert(slot != -1);
                            expressions.Add(I32.Load((ulong)(EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize))));
                        }
                        else
                        {
                            expressions.Add(I32.ConstRVA(target.GetTargetNode(factory)));
                        }

                        if (target.Thunk is not null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            expressions.Add(I32.ConstRVA(target.Thunk));
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                        }

                        EmitManagedTailCall(expressions, target.Constructor);
                    }
                    break;

                case ReadyToRunHelperId.ResolveVirtualFunction:
                    {
                        MethodDesc targetMethod = (MethodDesc)Target;
                        if (targetMethod.OwningType.IsInterface)
                        {
                            ISymbolNode helper = factory.ExternFunctionSymbol(s_RhpResolveInterfaceMethod);
                            expressions.Add(Local.Get(0));
                            expressions.Add(Local.Get(1));
                            expressions.Add(I32.ConstRVA(factory.DispatchCell(targetMethod)));
                            EmitManagedTailCall(expressions, helper);
                        }
                        else if (!relocsOnly)
                        {
                            expressions.Add(Local.Get(1));
                            expressions.Add(I32.Load(0));

                            Debug.Assert(!targetMethod.CanMethodBeInSealedVTable(factory));
                            int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                            Debug.Assert(slot != -1);
                            expressions.Add(I32.Load((ulong)(EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize))));
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            encoder.FunctionBody = new WasmFunctionBody(WasmLowering.GetSignature(this).FuncType, expressions.ToArray());
        }

        private static void EmitManagedTailCall(List<WasmExpr> expressions, ISymbolNode target)
        {
            expressions.Add(I32.ConstRVA(target));
            expressions.Add(ControlFlow.ReturnCall(target));
        }
    }
}
