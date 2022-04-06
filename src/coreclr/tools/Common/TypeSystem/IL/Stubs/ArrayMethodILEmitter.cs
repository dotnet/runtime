// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    internal struct ArrayMethodILEmitter
    {
        private ArrayMethod _method;
        private TypeDesc _elementType;
        private int _rank;

        private ILToken _helperFieldToken;
        private ILEmitter _emitter;

        private ArrayMethodILEmitter(ArrayMethod method)
        {
            Debug.Assert(method.Kind != ArrayMethodKind.Address, "Should be " + nameof(ArrayMethodKind.AddressWithHiddenArg));

            _method = method;

            ArrayType arrayType = (ArrayType)method.OwningType;
            _rank = arrayType.Rank;
            _elementType = arrayType.ElementType;
            _emitter = new ILEmitter();

            // This helper field is needed to generate proper GC tracking. There is no direct way
            // to create interior pointer. 
            _helperFieldToken = _emitter.NewToken(_method.Context.GetWellKnownType(WellKnownType.Object).GetKnownField("m_pEEType"));
        }

        private void EmitLoadInteriorAddress(ILCodeStream codeStream, int offset)
        {
            codeStream.EmitLdArg(0); // this
            codeStream.Emit(ILOpcode.ldflda, _helperFieldToken);
            codeStream.EmitLdc(offset);
            codeStream.Emit(ILOpcode.add);
        }

        private MethodIL EmitIL()
        {
            switch (_method.Kind)
            {
                case ArrayMethodKind.Get:
                case ArrayMethodKind.Set:
                case ArrayMethodKind.AddressWithHiddenArg:
                    EmitILForAccessor();
                    break;

                case ArrayMethodKind.Ctor:
                    // .ctor is implemented as a JIT helper and the JIT shouldn't be asking for the IL.
                default:
                    // Asking for anything else is invalid.
                    throw new InvalidOperationException();
            }

            return _emitter.Link(_method);
        }

        public static MethodIL EmitIL(ArrayMethod arrayMethod)
        {
            return new ArrayMethodILEmitter(arrayMethod).EmitIL();
        }

        private void EmitILForAccessor()
        {
            Debug.Assert(_method.OwningType.IsMdArray);

            var codeStream = _emitter.NewCodeStream();
            var context = _method.Context;

            var int32Type = context.GetWellKnownType(WellKnownType.Int32);

            var totalLocalNum = _emitter.NewLocal(int32Type);
            var lengthLocalNum = _emitter.NewLocal(int32Type);

            int pointerSize = context.Target.PointerSize;

            int argStartOffset = _method.Kind == ArrayMethodKind.AddressWithHiddenArg ? 2 : 1;

            var rangeExceptionLabel = _emitter.NewCodeLabel();
            ILCodeLabel typeMismatchExceptionLabel = null;

            if (_elementType.IsGCPointer)
            {
                // Type check
                if (_method.Kind == ArrayMethodKind.Set)
                {
                    MethodDesc checkArrayStore =
                        context.SystemModule.GetKnownType("System.Runtime", "RuntimeImports").GetKnownMethod("RhCheckArrayStore", null);

                    codeStream.EmitLdArg(0);
                    codeStream.EmitLdArg(_rank + argStartOffset);

                    codeStream.Emit(ILOpcode.call, _emitter.NewToken(checkArrayStore));
                }
                else if (_method.Kind == ArrayMethodKind.AddressWithHiddenArg)
                {
                    TypeDesc objectType = context.GetWellKnownType(WellKnownType.Object);
                    TypeDesc eetypeType = context.SystemModule.GetKnownType("Internal.Runtime", "MethodTable");

                    typeMismatchExceptionLabel = _emitter.NewCodeLabel();

                    ILCodeLabel typeCheckPassedLabel = _emitter.NewCodeLabel();

                    // Codegen will pass a null hidden argument if this is a `constrained.` call to the Address method.
                    // As per ECMA-335 III.2.3, the prefix suppresses the type check.
                    // if (hiddenArg == IntPtr.Zero)
                    //     goto TypeCheckPassed;
                    codeStream.EmitLdArg(1);
                    codeStream.Emit(ILOpcode.brfalse, typeCheckPassedLabel);

                    // MethodTable* actualElementType = this.MethodTable.RelatedParameterType; // ArrayElementType
                    codeStream.EmitLdArg(0);
                    codeStream.Emit(ILOpcode.call, _emitter.NewToken(objectType.GetKnownMethod("get_MethodTable", null)));
                    codeStream.Emit(ILOpcode.call,
                        _emitter.NewToken(eetypeType.GetKnownMethod("get_RelatedParameterType", null)));

                    // MethodTable* expectedElementType = hiddenArg->RelatedParameterType; // ArrayElementType
                    codeStream.EmitLdArg(1);
                    codeStream.Emit(ILOpcode.call,
                        _emitter.NewToken(eetypeType.GetKnownMethod("get_RelatedParameterType", null)));

                    // if (TypeCast.AreTypesEquivalent(expectedElementType, actualElementType))
                    //     ThrowHelpers.ThrowArrayTypeMismatchException();
                    codeStream.Emit(ILOpcode.call, _emitter.NewToken(
                        context.SystemModule.GetKnownType("System.Runtime", "TypeCast").GetKnownMethod("AreTypesEquivalent", null)));
                    codeStream.Emit(ILOpcode.brfalse, typeMismatchExceptionLabel);

                    codeStream.EmitLabel(typeCheckPassedLabel);
                }
            }

            // Methods on Rank 1 MdArray need to be able to handle `this` that is an SzArray
            // because SzArray is castable to Rank 1 MdArray (but not the other way around).

            ILCodeLabel rangeCheckDoneLabel = null;
            if (_rank == 1)
            {
                TypeDesc objectType = context.GetWellKnownType(WellKnownType.Object);
                TypeDesc eetypeType = context.SystemModule.GetKnownType("Internal.Runtime", "MethodTable");

                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.call, _emitter.NewToken(objectType.GetKnownMethod("get_MethodTable", null)));
                codeStream.Emit(ILOpcode.call,
                    _emitter.NewToken(eetypeType.GetKnownMethod("get_IsSzArray", null)));

                ILCodeLabel notSzArrayLabel = _emitter.NewCodeLabel();
                codeStream.Emit(ILOpcode.brfalse, notSzArrayLabel);

                // We have an SzArray - do the bounds check differently
                EmitLoadInteriorAddress(codeStream, pointerSize);
                codeStream.Emit(ILOpcode.dup);
                codeStream.Emit(ILOpcode.ldind_i4);
                codeStream.EmitLdArg(argStartOffset);
                codeStream.EmitStLoc(totalLocalNum);
                codeStream.EmitLdLoc(totalLocalNum);
                codeStream.Emit(ILOpcode.ble_un, rangeExceptionLabel);

                codeStream.EmitLdc(pointerSize);
                codeStream.Emit(ILOpcode.add);

                rangeCheckDoneLabel = _emitter.NewCodeLabel();
                codeStream.Emit(ILOpcode.br, rangeCheckDoneLabel);

                codeStream.EmitLabel(notSzArrayLabel);
            }

            for (int i = 0; i < _rank; i++)
            {
                // The first two fields are MethodTable pointer and total length. Lengths for each dimension follows.
                int lengthOffset = (2 * pointerSize + i * sizeof(int));

                EmitLoadInteriorAddress(codeStream, lengthOffset);
                codeStream.Emit(ILOpcode.ldind_i4);
                codeStream.EmitStLoc(lengthLocalNum);

                codeStream.EmitLdArg(i + argStartOffset);

                // Compare with length
                codeStream.Emit(ILOpcode.dup);
                codeStream.EmitLdLoc(lengthLocalNum);
                codeStream.Emit(ILOpcode.bge_un, rangeExceptionLabel);

                // Add to the running total if we have one already
                if (i > 0)
                {
                    codeStream.EmitLdLoc(totalLocalNum);
                    codeStream.EmitLdLoc(lengthLocalNum);
                    codeStream.Emit(ILOpcode.mul);
                    codeStream.Emit(ILOpcode.add);
                }
                codeStream.EmitStLoc(totalLocalNum);
            }

            // Compute element offset
            // TODO: This leaves unused space for lower bounds to match CoreCLR...
            int firstElementOffset = (2 * pointerSize + 2 * _rank * sizeof(int));
            EmitLoadInteriorAddress(codeStream, firstElementOffset);

            if (rangeCheckDoneLabel != null)
                codeStream.EmitLabel(rangeCheckDoneLabel);

            codeStream.EmitLdLoc(totalLocalNum);
            codeStream.Emit(ILOpcode.conv_u);

            int elementSize = _elementType.GetElementSize().AsInt;
            if (elementSize != 1)
            {
                codeStream.EmitLdc(elementSize);
                codeStream.Emit(ILOpcode.mul);
            }
            codeStream.Emit(ILOpcode.add);

            switch (_method.Kind)
            {
                case ArrayMethodKind.Get:
                    codeStream.Emit(ILOpcode.ldobj, _emitter.NewToken(_elementType));
                    break;

                case ArrayMethodKind.Set:
                    codeStream.EmitLdArg(_rank + argStartOffset);
                    codeStream.Emit(ILOpcode.stobj, _emitter.NewToken(_elementType));
                    break;

                case ArrayMethodKind.AddressWithHiddenArg:
                    break;
            }

            codeStream.Emit(ILOpcode.ret);

            codeStream.EmitLdc(0);
            codeStream.EmitLabel(rangeExceptionLabel); // Assumes that there is one "int" pushed on the stack
            codeStream.Emit(ILOpcode.pop);

            MethodDesc throwHelper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowIndexOutOfRangeException");
            codeStream.EmitCallThrowHelper(_emitter, throwHelper);

            if (typeMismatchExceptionLabel != null)
            {
                codeStream.EmitLabel(typeMismatchExceptionLabel);
                codeStream.EmitCallThrowHelper(_emitter, context.GetHelperEntryPoint("ThrowHelpers", "ThrowArrayTypeMismatchException"));
            }
        }
    }
}
