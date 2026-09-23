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
    public partial class ReadyToRunGenericHelperNode : INodeWithTypeSignature
    {
        private MethodSignature _signature;

        public MethodSignature Signature => _signature ??= InitializeWasmSignature();
        public bool IsUnmanagedCallersOnly => false;
        public bool IsAsyncCall => false;
        public bool HasGenericContextArg => false;

        private MethodSignature InitializeWasmSignature()
        {
            TypeSystemContext context = DictionaryOwner.Context;
            TypeDesc nativeIntType = context.GetWellKnownType(WellKnownType.IntPtr);
            TypeDesc[] parameters = Id == ReadyToRunHelperId.DelegateCtor ?
                [nativeIntType, nativeIntType, nativeIntType] : [nativeIntType];
            TypeDesc returnType = Id == ReadyToRunHelperId.DelegateCtor ?
                context.GetWellKnownType(WellKnownType.Void) : nativeIntType;

            return new MethodSignature(MethodSignatureFlags.Static, genericParameterCount: 0, returnType, parameters);
        }

        private void EmitDictionaryLookup(
            NodeFactory factory,
            List<WasmExpr> expressions,
            int contextLocalIndex,
            int resultLocalIndex,
            GenericLookupResult lookup,
            bool relocsOnly)
        {
            int dictionarySlot = 0;
            if (!relocsOnly &&
                !factory.GenericDictionaryLayout(DictionaryOwner).TryGetSlotForEntry(lookup, out dictionarySlot))
            {
                expressions.Add(I32.Const(0));
                return;
            }

            expressions.Add(Local.Get(contextLocalIndex));
            expressions.Add(I32.Load((ulong)(dictionarySlot * factory.Target.PointerSize)));

            if (!relocsOnly && _hasInvalidEntries)
            {
                expressions.Add(Local.Tee(resultLocalIndex));
                expressions.Add(I32.Eqz);
                expressions.Add(Block.If(WasmBlockType.Empty));
                expressions.Add(Local.Get(0));
                expressions.Add(ControlFlow.Call(GetBadSlotHelper(factory)));
                expressions.Add(ControlFlow.Unreachable);
                expressions.Add(Block.End);
                expressions.Add(Local.Get(resultLocalIndex));
            }
        }

        protected sealed override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            Debug.Assert(!encoder.Is64Bit);

            WasmFuncType signature = WasmLowering.GetSignature(this).FuncType;
            int contextLocalIndex = signature.Params.Types.Length;
            int resultLocalIndex = contextLocalIndex + 1;
            List<WasmExpr> expressions = new List<WasmExpr>();

            EmitLoadGenericContext(factory, expressions, relocsOnly);
            expressions.Add(Local.Set(contextLocalIndex));

            switch (Id)
            {
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    if (!TriggersLazyStaticConstructor(factory))
                    {
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                    }
                    else
                    {
                        expressions.Add(Local.Get(0));
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                        expressions.Add(Local.Set(resultLocalIndex));
                        expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                        expressions.Add(I32.Sub);
                        expressions.Add(Local.Get(resultLocalIndex));
                        expressions.Add(ControlFlow.ReturnCall(factory.HelperEntrypoint(
                            HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase)));
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    if (!TriggersLazyStaticConstructor(factory))
                    {
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                        expressions.Add(I32.Load(0));
                    }
                    else
                    {
                        expressions.Add(Local.Get(0));
                        GenericLookupResult nonGcRegionLookup =
                            factory.GenericLookup.TypeNonGCStaticBase((MetadataType)Target);
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, nonGcRegionLookup, relocsOnly);
                        expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                        expressions.Add(I32.Sub);
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                        expressions.Add(I32.Load(0));
                        expressions.Add(ControlFlow.ReturnCall(factory.HelperEntrypoint(
                            HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase)));
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    expressions.Add(Local.Get(0));
                    EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                    expressions.Add(Local.Tee(resultLocalIndex));
                    expressions.Add(I32.Load(0));
                    expressions.Add(Local.Get(resultLocalIndex));
                    expressions.Add(I32.Load((ulong)factory.Target.PointerSize));

                    ISymbolNode helperEntrypoint;
                    if (!TriggersLazyStaticConstructor(factory))
                    {
                        helperEntrypoint = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                    }
                    else
                    {
                        GenericLookupResult nonGcRegionLookup =
                            factory.GenericLookup.TypeNonGCStaticBase((MetadataType)Target);
                        EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, nonGcRegionLookup, relocsOnly);
                        expressions.Add(I32.Const(NonGCStaticsNode.GetClassConstructorContextSize(factory.Target)));
                        expressions.Add(I32.Sub);
                        helperEntrypoint = factory.HelperEntrypoint(
                            HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase);
                    }

                    expressions.Add(ControlFlow.ReturnCall(helperEntrypoint));
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    DelegateCreationInfo target = (DelegateCreationInfo)Target;
                    expressions.Add(Local.Get(0));
                    expressions.Add(Local.Get(1));
                    expressions.Add(Local.Get(2));
                    EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);

                    if (target.Thunk is not null)
                    {
                        Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                        expressions.Add(I32.ConstRVA(target.Thunk));
                    }
                    else
                    {
                        Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                    }

                    expressions.Add(ControlFlow.ReturnCall(target.Constructor));
                    break;

                case ReadyToRunHelperId.TypeHandle:
                case ReadyToRunHelperId.NecessaryTypeHandle:
                case ReadyToRunHelperId.MetadataTypeHandle:
                case ReadyToRunHelperId.MethodHandle:
                case ReadyToRunHelperId.FieldHandle:
                case ReadyToRunHelperId.MethodDictionary:
                case ReadyToRunHelperId.MethodEntry:
                case ReadyToRunHelperId.DispatchCell:
                case ReadyToRunHelperId.DefaultConstructor:
                case ReadyToRunHelperId.ObjectAllocator:
                case ReadyToRunHelperId.TypeHandleForCasting:
                case ReadyToRunHelperId.ConstrainedDirectCall:
                    EmitDictionaryLookup(factory, expressions, contextLocalIndex, resultLocalIndex, LookupSignature, relocsOnly);
                    break;

                default:
                    throw new NotImplementedException();
            }

            encoder.FunctionBody = new WasmFunctionBody(
                signature,
                new[] { WasmValueType.I32, WasmValueType.I32 },
                expressions.ToArray());
        }

        protected virtual void EmitLoadGenericContext(
            NodeFactory factory,
            List<WasmExpr> expressions,
            bool relocsOnly)
        {
            expressions.Add(Local.Get(Id == ReadyToRunHelperId.DelegateCtor ? 3 : 1));
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(
            NodeFactory factory,
            List<WasmExpr> expressions,
            bool relocsOnly)
        {
            base.EmitLoadGenericContext(factory, expressions, relocsOnly);

            int vtableSlot = 0;
            if (!relocsOnly)
            {
                vtableSlot = VirtualMethodSlotHelper.GetGenericDictionarySlot(factory, (TypeDesc)DictionaryOwner);
            }

            int pointerSize = factory.Target.PointerSize;
            int slotOffset = EETypeNode.GetVTableOffset(pointerSize) + (vtableSlot * pointerSize);
            expressions.Add(I32.Load((ulong)slotOffset));
        }
    }
}
