// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Internal.IL.Stubs;
using Internal.IL;

using Debug = System.Diagnostics.Debug;
using ILLocalVariable = Internal.IL.Stubs.ILLocalVariable;
using Internal.TypeSystem.Ecma;
using System.Reflection.Metadata;

namespace Internal.TypeSystem.Interop
{
    partial class Marshaller
    {
        protected static Marshaller CreateMarshaller(MarshallerKind kind)
        {
            switch (kind)
            {
                case MarshallerKind.Enum:
                case MarshallerKind.BlittableValue:
                case MarshallerKind.BlittableStruct:
                case MarshallerKind.UnicodeChar:
                    return new BlittableValueMarshaller();
                case MarshallerKind.BlittableStructPtr:
                    return new BlittableStructPtrMarshaller();
                case MarshallerKind.AnsiChar:
                    return new AnsiCharMarshaller();
                case MarshallerKind.Array:
                    return new ArrayMarshaller();
                case MarshallerKind.BlittableArray:
                    return new BlittableArrayMarshaller();
                case MarshallerKind.Bool:
                case MarshallerKind.CBool:
                    return new BooleanMarshaller();
                case MarshallerKind.VariantBool:
                    return new BooleanMarshaller((short)-1);
                case MarshallerKind.AnsiString:
                    return new AnsiStringMarshaller();
                case MarshallerKind.UTF8String:
                    return new UTF8StringMarshaller();
                case MarshallerKind.UnicodeString:
                    return new UnicodeStringMarshaller();
                case MarshallerKind.AnsiBSTRString:
                    return new AnsiBSTRStringMarshaller();
                case MarshallerKind.BSTRString:
                    return new BSTRStringMarshaller();
                case MarshallerKind.SafeHandle:
                    return new SafeHandleMarshaller();
                case MarshallerKind.UnicodeStringBuilder:
                    return new StringBuilderMarshaller(isAnsi: false);
                case MarshallerKind.AnsiStringBuilder:
                    return new StringBuilderMarshaller(isAnsi: true);
                case MarshallerKind.VoidReturn:
                    return new VoidReturnMarshaller();
                case MarshallerKind.FunctionPointer:
                    return new DelegateMarshaller();
                case MarshallerKind.Struct:
                case MarshallerKind.Decimal:
                    return new StructMarshaller();
                case MarshallerKind.ByValAnsiString:
                    return new ByValAnsiStringMarshaller();
                case MarshallerKind.ByValUnicodeString:
                    return new ByValUnicodeStringMarshaller();
                case MarshallerKind.ByValAnsiCharArray:
                case MarshallerKind.ByValArray:
                    return new ByValArrayMarshaller();
                case MarshallerKind.AnsiCharArray:
                    return new AnsiCharArrayMarshaller();
                case MarshallerKind.HandleRef:
                    return new HandleRefMarshaller();
                case MarshallerKind.LayoutClass:
                    return new LayoutClassMarshaler();
                case MarshallerKind.LayoutClassPtr:
                    return new LayoutClassPtrMarshaller();
                case MarshallerKind.AsAnyA:
                    return new AsAnyMarshaller(isAnsi: true);
                case MarshallerKind.AsAnyW:
                    return new AsAnyMarshaller(isAnsi: false);
                case MarshallerKind.ComInterface:
                    return new ComInterfaceMarshaller();
                case MarshallerKind.OleDateTime:
                    return new OleDateTimeMarshaller();
                case MarshallerKind.OleCurrency:
                    return new OleCurrencyMarshaller();
                case MarshallerKind.FailedTypeLoad:
                    return new FailedTypeLoadMarshaller();
                case MarshallerKind.Variant:
                    return new VariantMarshaller();
                case MarshallerKind.CustomMarshaler:
                    return new CustomTypeMarshaller();
                case MarshallerKind.BlittableValueClassByRefReturn:
                    return new BlittableValueClassByRefReturn();

                default:
                    // ensures we don't throw during create marshaller. We will throw NSE
                    // during EmitIL which will be handled and an Exception method body
                    // will be emitted.
                    return new NotSupportedMarshaller();
            }
        }
     }

    class AnsiCharArrayMarshaller : ArrayMarshaller
    {
        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "AllocMemoryForAnsiCharArray");
            LoadManagedValue(codeStream);

            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }


        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "WideCharArrayToAnsiCharArray");

            LoadManagedValue(codeStream);
            LoadNativeValue(codeStream);
            codeStream.Emit(PInvokeFlags.BestFitMapping ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            codeStream.Emit(PInvokeFlags.ThrowOnUnmappableChar ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "AnsiCharArrayToWideCharArray");

            LoadNativeValue(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }
    }

    class AnsiCharMarshaller : Marshaller
    {
        protected override void AllocAndTransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "WideCharToAnsiChar");

            LoadManagedValue(codeStream);
            codeStream.Emit(PInvokeFlags.BestFitMapping ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            codeStream.Emit(PInvokeFlags.ThrowOnUnmappableChar ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "AnsiCharToWideChar");

            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
            StoreManagedValue(codeStream);
        }
    }

    class StringBuilderMarshaller : Marshaller
    {
        private bool _isAnsi;
        public StringBuilderMarshaller(bool isAnsi)
        {
            _isAnsi = isAnsi;
        }

        internal override bool CleanupRequired
        {
            get
            {
                return true;
            }
        }

        internal override void EmitElementCleanup(ILCodeStream codeStream, ILEmitter emitter)
        {
            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                                InteropTypes.GetMarshal(Context).GetKnownMethod("FreeCoTaskMem", null)));
        }

        protected override void AllocNativeToManaged(ILCodeStream codeStream)
        {
            var emitter = _ilCodeStreams.Emitter;
            var lNull = emitter.NewCodeLabel();

            // Check for null
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(
                ManagedType.GetParameterlessConstructor()));
            StoreManagedValue(codeStream);
            codeStream.EmitLabel(lNull);
        }

        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            string helperMethodName = _isAnsi ? "AllocMemoryForAnsiStringBuilder" : "AllocMemoryForUnicodeStringBuilder";
            var helper = Context.GetHelperEntryPoint("InteropHelpers", helperMethodName);
            LoadManagedValue(codeStream);

            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            string helperMethodName = _isAnsi ? "StringBuilderToAnsiString" : "StringBuilderToUnicodeString";
            var helper = Context.GetHelperEntryPoint("InteropHelpers", helperMethodName);

            LoadManagedValue(codeStream);
            LoadNativeValue(codeStream);
            if (_isAnsi)
            {
                codeStream.Emit(PInvokeFlags.BestFitMapping ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
                codeStream.Emit(PInvokeFlags.ThrowOnUnmappableChar ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            }
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            string helperMethodName = _isAnsi ? "AnsiStringToStringBuilder" : "UnicodeStringToStringBuilder";
            var helper = Context.GetHelperEntryPoint("InteropHelpers", helperMethodName);
            LoadNativeValue(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(helper));
        }


        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                                InteropTypes.GetMarshal(Context).GetKnownMethod("FreeCoTaskMem", null)));
        }
    }

    class HandleRefMarshaller : Marshaller
    {
        protected override void AllocAndTransformManagedToNative(ILCodeStream codeStream)
        {
            LoadManagedAddr(codeStream);
            codeStream.Emit(ILOpcode.ldfld, _ilCodeStreams.Emitter.NewToken(InteropTypes.GetHandleRef(Context).GetKnownField("_handle")));
            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ThrowHelper.ThrowMarshalDirectiveException();
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ThrowHelper.ThrowMarshalDirectiveException();
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            LoadManagedAddr(codeStream);
            codeStream.Emit(ILOpcode.ldfld, _ilCodeStreams.Emitter.NewToken(InteropTypes.GetHandleRef(Context).GetKnownField("_wrapper")));
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(InteropTypes.GetGC(Context).GetKnownMethod("KeepAlive", null)));
        }
    }

    class StructMarshaller : Marshaller
    {
        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.initobj, _ilCodeStreams.Emitter.NewToken(NativeType));
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            LoadManagedAddr(codeStream);
            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingManagedToNativeThunk(ManagedType)));
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            LoadNativeAddr(codeStream);
            LoadManagedAddr(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingNativeToManagedThunk(ManagedType)));
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            // Only do cleanup if it is IN
            if (!In)
            {
                return;
            }

            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingCleanupThunk(ManagedType)));
        }
    }

    class ByValArrayMarshaller : ArrayMarshaller
    {
        protected FieldDesc _managedField;
        protected FieldDesc _nativeField;

        public void EmitMarshallingIL(PInvokeILCodeStreams codeStreams, FieldDesc managedField, FieldDesc nativeField)
        {
            _managedField = managedField;
            _nativeField = nativeField;
            EmitMarshallingIL(codeStreams);
        }

        protected override void EmitElementCount(ILCodeStream codeStream, MarshalDirection direction)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            if (MarshalAsDescriptor == null || !MarshalAsDescriptor.SizeConst.HasValue)
            {
                throw new InvalidProgramException("SizeConst is required for ByValArray.");
            }

            if (direction == MarshalDirection.Forward)
            {
                // In forward direction ElementCount = Min(managed.length, SizeConst);

                var vLength = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
                var lSmaller = emitter.NewCodeLabel();
                var lDone = emitter.NewCodeLabel();

                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));

                var lNullCheck = emitter.NewCodeLabel();
                codeStream.Emit(ILOpcode.brfalse, lNullCheck);

                codeStream.EmitLdArg(0);
                codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));

                codeStream.Emit(ILOpcode.ldlen);
                codeStream.Emit(ILOpcode.conv_i4);
                codeStream.EmitStLoc(vLength);

                codeStream.EmitLabel(lNullCheck);

                Debug.Assert(MarshalAsDescriptor.SizeConst.HasValue);
                int sizeConst = (int)MarshalAsDescriptor.SizeConst.Value;

                codeStream.EmitLdc(sizeConst);
                codeStream.EmitLdLoc(vLength);
                codeStream.Emit(ILOpcode.blt, lSmaller);

                codeStream.EmitLdLoc(vLength);
                codeStream.Emit(ILOpcode.br, lDone);

                codeStream.EmitLabel(lSmaller);
                codeStream.EmitLdc(sizeConst);

                codeStream.EmitLabel(lDone);
            }
            else
            {
                // In reverse direction ElementCount = SizeConst;
                Debug.Assert(MarshalAsDescriptor.SizeConst.HasValue);
                int sizeConst = (int)MarshalAsDescriptor.SizeConst.Value;

                codeStream.EmitLdc(sizeConst);
            }
        }

        protected override void EmitMarshalFieldManagedToNative()
        {

            // It generates the following code
            //if (ManagedArg.Field != null)
            //{
            //
            //  fixed (InlineArray* pUnsafe = &NativeArg.Field)
            //  {
            //        uint index = 0u;
            //        while ((ulong)index < (ulong)((long)ManagedArg.Field.Length))
            //        {
            //            NativeArg.s[index] = ManagedArg.Field[(int)index];
            //            index += 1u;
            //        }
            //  }
            //}

            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeStream codeStream = _ilCodeStreams.MarshallingCodeStream;
            var nativeArrayType = NativeType as InlineArrayType;
            Debug.Assert(nativeArrayType != null);
            Debug.Assert(ManagedType is ArrayType);

            var managedElementType = ((ArrayType)ManagedType).ElementType;

            ILCodeLabel lDone = emitter.NewCodeLabel();
            ILCodeLabel lRangeCheck = emitter.NewCodeLabel();
            ILCodeLabel lLoopHeader = emitter.NewCodeLabel();

            ILLocalVariable vIndex = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
            ILLocalVariable vLength = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
            ILLocalVariable vNative = emitter.NewLocal(NativeType.MakeByRefType(), isPinned: true);

            // check if ManagedType == null, then return
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));
            codeStream.Emit(ILOpcode.brfalse, lDone);

            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(_nativeField));
            codeStream.EmitStLoc(vNative);

            EmitElementCount(codeStream, MarshalDirection.Forward);
            codeStream.EmitStLoc(vLength);

            codeStream.EmitLdc(0);
            codeStream.EmitStLoc(vIndex);
            codeStream.Emit(ILOpcode.br, lRangeCheck);

            codeStream.EmitLabel(lLoopHeader);
            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(_nativeField));
            codeStream.EmitLdLoc(vIndex);

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));
            codeStream.EmitLdLoc(vIndex);

            codeStream.EmitLdElem(managedElementType);

            // generate marshalling IL for the element
            GetElementMarshaller(MarshalDirection.Forward)
                .EmitMarshallingIL(new PInvokeILCodeStreams(_ilCodeStreams.Emitter, codeStream));

            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                           nativeArrayType.GetInlineArrayMethod(InlineArrayMethodKind.Setter)));

            codeStream.EmitLdLoc(vIndex);
            codeStream.EmitLdc(1);
            codeStream.Emit(ILOpcode.add);
            codeStream.EmitStLoc(vIndex);

            codeStream.EmitLabel(lRangeCheck);
            codeStream.EmitLdLoc(vIndex);

            codeStream.EmitLdLoc(vLength);
            codeStream.Emit(ILOpcode.blt, lLoopHeader);

            codeStream.EmitLabel(lDone);
        }

        protected override void EmitMarshalFieldNativeToManaged()
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeStream codeStream = _ilCodeStreams.UnmarshallingCodestream;

            // It generates the following IL:
            //  ManagedArg.s = new ElementType[Length];
            //
            //    for (uint index = 0u; index < Length; index += 1u)
            //    {
            //        ManagedArg.s[index] = NativeArg.s[index];
            //    }
            //

            ILCodeLabel lRangeCheck = emitter.NewCodeLabel();
            ILCodeLabel lLoopHeader = emitter.NewCodeLabel();

            Debug.Assert(ManagedType is ArrayType);

            var nativeArrayType = NativeType as InlineArrayType;
            Debug.Assert(nativeArrayType != null);

            var managedElementType = ((ArrayType)ManagedType).ElementType;

            ILLocalVariable vLength = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
            codeStream.EmitLdArg(1);
            // load the length
            EmitElementCount(codeStream, MarshalDirection.Reverse);
            codeStream.EmitStLoc(vLength);

            codeStream.EmitLdLoc(vLength);
            codeStream.Emit(ILOpcode.newarr, emitter.NewToken(managedElementType));
            codeStream.Emit(ILOpcode.stfld, emitter.NewToken(_managedField));


            var vIndex = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));

            // index = 0
            codeStream.EmitLdc(0);
            codeStream.EmitStLoc(vIndex);
            codeStream.Emit(ILOpcode.br, lRangeCheck);

            codeStream.EmitLabel(lLoopHeader);

            // load managed type
            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));

            codeStream.EmitLdLoc(vIndex);

            // load native type
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(_nativeField));
            codeStream.EmitLdLoc(vIndex);

            codeStream.Emit(ILOpcode.call, emitter.NewToken(
               nativeArrayType.GetInlineArrayMethod(InlineArrayMethodKind.Getter)));

            // generate marshalling IL for the element
            GetElementMarshaller(MarshalDirection.Reverse)
                .EmitMarshallingIL(new PInvokeILCodeStreams(_ilCodeStreams.Emitter, codeStream));

            codeStream.EmitStElem(managedElementType);

            codeStream.EmitLdLoc(vIndex);
            codeStream.EmitLdc(1);
            codeStream.Emit(ILOpcode.add);
            codeStream.EmitStLoc(vIndex);

            codeStream.EmitLabel(lRangeCheck);

            codeStream.EmitLdLoc(vIndex);
            codeStream.EmitLdLoc(vLength);
            codeStream.Emit(ILOpcode.blt, lLoopHeader);
        }
    }

    abstract class ByValStringMarshaller : ByValArrayMarshaller
    {
        protected override void EmitElementCount(ILCodeStream codeStream, MarshalDirection direction)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            if (MarshalAsDescriptor == null || !MarshalAsDescriptor.SizeConst.HasValue)
            {
                throw new InvalidProgramException("SizeConst is required for ByValString.");
            }
            codeStream.EmitLdc((int)MarshalAsDescriptor.SizeConst.Value);
        }

        protected abstract bool IsAnsi
        {
            get;
        }

        protected abstract MethodDesc GetManagedToNativeHelper();

        protected abstract MethodDesc GetNativeToManagedHelper();

        protected override void EmitMarshalFieldManagedToNative()
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeStream codeStream = _ilCodeStreams.MarshallingCodeStream;

            var nativeArrayType = NativeType as InlineArrayType;
            Debug.Assert(nativeArrayType != null);

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(_managedField));

            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(_nativeField));
            codeStream.Emit(ILOpcode.conv_u);

            EmitElementCount(codeStream, MarshalDirection.Forward);

            if (IsAnsi)
            {
                codeStream.Emit(PInvokeFlags.BestFitMapping ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
                codeStream.Emit(PInvokeFlags.ThrowOnUnmappableChar ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0);
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(GetManagedToNativeHelper()));
        }

        protected override void EmitMarshalFieldNativeToManaged()
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeStream codeStream = _ilCodeStreams.UnmarshallingCodestream;

            codeStream.EmitLdArg(1);

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(_nativeField));
            codeStream.Emit(ILOpcode.conv_u);

            EmitElementCount(codeStream, MarshalDirection.Reverse);

            codeStream.Emit(ILOpcode.call, emitter.NewToken(GetNativeToManagedHelper()));

            codeStream.Emit(ILOpcode.stfld, emitter.NewToken(_managedField));
        }
    }

    class ByValAnsiStringMarshaller : ByValStringMarshaller
    {
        protected override bool IsAnsi
        {
            get
            {
                return true;
            }
        }

        protected override MethodDesc GetManagedToNativeHelper()
        {
            return Context.GetHelperEntryPoint("InteropHelpers", "StringToByValAnsiString");
        }

        protected override MethodDesc GetNativeToManagedHelper()
        {
            return Context.GetHelperEntryPoint("InteropHelpers", "ByValAnsiStringToString");
        }
    }

    class ByValUnicodeStringMarshaller : ByValStringMarshaller
    {
        protected override bool IsAnsi
        {
            get
            {
                return false;
            }
        }

        protected override MethodDesc GetManagedToNativeHelper()
        {
            return Context.GetHelperEntryPoint("InteropHelpers", "StringToUnicodeFixedArray");
        }

        protected override MethodDesc GetNativeToManagedHelper()
        {
            return Context.GetHelperEntryPoint("InteropHelpers", "UnicodeToStringFixedArray");
        }
    }

    class LayoutClassMarshaler : Marshaller
    {
        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.initobj, _ilCodeStreams.Emitter.NewToken(NativeType));
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadManagedValue(codeStream);
            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingManagedToNativeThunk(ManagedType)));

            codeStream.EmitLabel(lNull);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNonNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brtrue, lNonNull);

            MethodDesc ctor = ManagedType.GetParameterlessConstructor();
            if (ctor == null)
                throw new InvalidProgramException();

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(ctor));
            StoreManagedValue(codeStream);

            codeStream.EmitLabel(lNonNull);
            LoadNativeAddr(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingNativeToManagedThunk(ManagedType)));
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            // Only do cleanup if it is IN
            if (!In)
            {
                return;
            }

            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingCleanupThunk(ManagedType)));
        }
    }

    class LayoutClassPtrMarshaller : Marshaller
    {
        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            codeStream.EmitLdc(0);
            codeStream.Emit(ILOpcode.conv_i);
            StoreNativeValue(codeStream);

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            TypeDesc nativeStructType = InteropStateManager.GetStructMarshallingNativeType(ManagedType);

            ILLocalVariable lNativeType = emitter.NewLocal(nativeStructType);
            codeStream.EmitLdLoca(lNativeType);
            codeStream.Emit(ILOpcode.initobj, emitter.NewToken(nativeStructType));
            codeStream.EmitLdLoca(lNativeType);
            StoreNativeValue(codeStream);

            codeStream.EmitLabel(lNull);
        }

        protected override void AllocNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;

            MethodDesc ctor = ManagedType.GetParameterlessConstructor();
            if (ctor == null)
                throw new InvalidProgramException();

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(ctor));
            StoreManagedValue(codeStream);
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadManagedValue(codeStream);
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingManagedToNativeThunk(ManagedType)));

            codeStream.EmitLabel(lNull);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadNativeValue(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                InteropStateManager.GetStructMarshallingNativeToManagedThunk(ManagedType)));

            codeStream.EmitLabel(lNull);
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            // Only do cleanup if it is IN
            if (!In)
            {
                return;
            }

            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                InteropStateManager.GetStructMarshallingCleanupThunk(ManagedType)));

            codeStream.EmitLabel(lNull);
        }
    }

    class AsAnyMarshaller : Marshaller
    {
        // This flag affects encoding of string, StringBuilder and Char array marshalling.
        // It does not affect LayoutClass marshalling.
        // Note that the CoreLib portion of the marshaller currently only supports LayoutClass.
        private readonly bool _isAnsi;

        public AsAnyMarshaller(bool isAnsi)
        {
            _isAnsi = isAnsi;
        }

        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();
            ILLocalVariable lSize = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            MethodDesc getNativeSizeHelper = Context.GetHelperEntryPoint("InteropHelpers", "AsAnyGetNativeSize");

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(getNativeSizeHelper));
            codeStream.Emit(ILOpcode.dup);
            codeStream.EmitStLoc(lSize);
            codeStream.Emit(ILOpcode.localloc);
            codeStream.Emit(ILOpcode.dup);
            StoreNativeValue(codeStream);
            codeStream.EmitLdc(0);
            codeStream.EmitLdLoc(lSize);
            codeStream.Emit(ILOpcode.initblk);

            codeStream.EmitLabel(lNull);
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadManagedValue(codeStream);
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                Context.GetHelperEntryPoint("InteropHelpers", "AsAnyMarshalManagedToNative")));

            codeStream.EmitLabel(lNull);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadNativeValue(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, _ilCodeStreams.Emitter.NewToken(
                Context.GetHelperEntryPoint("InteropHelpers", "AsAnyMarshalNativeToManaged")));

            codeStream.EmitLabel(lNull);
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            // Only do cleanup if it is IN
            if (!In)
            {
                return;
            }

            ILEmitter emitter = _ilCodeStreams.Emitter;
            ILCodeLabel lNull = emitter.NewCodeLabel();

            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.brfalse, lNull);

            LoadNativeValue(codeStream);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                Context.GetHelperEntryPoint("InteropHelpers", "AsAnyCleanupNative")));

            codeStream.EmitLabel(lNull);
        }
    }

    class ComInterfaceMarshaller : Marshaller
    {
        protected override void AllocAndTransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;

            LoadManagedValue(codeStream);
            var parameterType = this.ManagedParameterType;
            if (parameterType.IsByRef)
            {
                parameterType = ((ByRefType)this.ManagedParameterType).ParameterType;
            }

            CustomAttributeValue<TypeDesc>? guidAttributeValue = (parameterType as EcmaType)?
                .GetDecodedCustomAttribute("System.Runtime.InteropServices", "GuidAttribute");
            if (guidAttributeValue != null)
            {
                var guidValue = (string)guidAttributeValue.Value.FixedArguments[0].Value;
                Span<byte> bytes = Guid.Parse(guidValue).ToByteArray();
                codeStream.EmitLdc(BinaryPrimitives.ReadInt32LittleEndian(bytes));
                codeStream.EmitLdc(BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(4)));
                codeStream.EmitLdc(BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(6)));
                for (int i = 8; i < 16; i++)
                    codeStream.EmitLdc(bytes[i]);

                MetadataType guidType = Context.SystemModule.GetKnownType("System", "Guid");
                var int32Type = Context.GetWellKnownType(WellKnownType.Int32);
                var int16Type = Context.GetWellKnownType(WellKnownType.Int16);
                var byteType = Context.GetWellKnownType(WellKnownType.Byte);
                var sig = new MethodSignature(
                    MethodSignatureFlags.None,
                    genericParameterCount: 0,
                    returnType: Context.GetWellKnownType(WellKnownType.Void),
                    parameters: new TypeDesc[] { int32Type, int16Type, int16Type, byteType, byteType, byteType, byteType, byteType, byteType, byteType, byteType });
                MethodDesc guidCtorHandleMethod =
                    guidType.GetKnownMethod(".ctor", sig);
                codeStream.Emit(ILOpcode.newobj, emitter.NewToken(guidCtorHandleMethod));

                MethodDesc helper = Context.GetHelperEntryPoint("InteropHelpers", "ConvertManagedComInterfaceToNative");
                codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
            }
            else
            {
                if (!parameterType.IsObject)
                {
                    throw new NotSupportedException();
                }

                MethodDesc helper = Context.GetHelperEntryPoint("InteropHelpers", "ConvertManagedComInterfaceToIUnknown");
                codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
            }

            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;

            LoadNativeValue(codeStream);
            MethodDesc helper = Context.GetHelperEntryPoint("InteropHelpers", "ConvertNativeComInterfaceToManaged");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            throw new NotSupportedException();
        }
    }

    class AnsiBSTRStringMarshaller : Marshaller
    {

        internal override bool CleanupRequired => true;

        internal override void EmitElementCleanup(ILCodeStream codeStream, ILEmitter emitter)
        {
            var helper = InteropTypes.GetMarshal(Context).GetKnownMethod("FreeBSTR", null);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }
        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadManagedValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "StringToAnsiBstrBuffer");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadNativeValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "AnsiBstrBufferToString");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }
    }

    class BSTRStringMarshaller : Marshaller
    {

        internal override bool CleanupRequired => true;

        internal override void EmitElementCleanup(ILCodeStream codeStream, ILEmitter emitter)
        {
            var helper = InteropTypes.GetMarshal(Context).GetKnownMethod("FreeBSTR", null);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }
        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadManagedValue(codeStream);

            var helper = InteropTypes.GetMarshal(Context).GetKnownMethod("StringToBSTR", null);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadNativeValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "BstrBufferToString");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }
    }

    class OleDateTimeMarshaller : Marshaller
    {
        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadManagedValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "DateTimeToOleDateTime");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadNativeValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "OleDateTimeToDateTime");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }
    }

    class OleCurrencyMarshaller : Marshaller
    {
        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadManagedValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "DecimalToOleCurrency");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreNativeValue(codeStream);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;
            LoadNativeValue(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "OleCurrencyToDecimal");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }
    }

    class FailedTypeLoadMarshaller : Marshaller
    {
        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            ThrowHelper.ThrowTypeLoadException(ManagedType);
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            ThrowHelper.ThrowTypeLoadException(ManagedType);
        }
    }

    class VariantMarshaller : Marshaller
    {
        protected override void AllocManagedToNative(ILCodeStream codeStream)
        {
            LoadNativeAddr(codeStream);
            codeStream.Emit(ILOpcode.initobj, _ilCodeStreams.Emitter.NewToken(NativeType));
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            if (this.MarshalDirection == MarshalDirection.Reverse)
            {
                throw new NotSupportedException();
            }

            ILEmitter emitter = _ilCodeStreams.Emitter;

            LoadManagedValue(codeStream);
            LoadNativeAddr(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "ConvertObjectToVariant");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            if (this.MarshalDirection == MarshalDirection.Reverse)
            {
                throw new NotSupportedException();
            }

            ILEmitter emitter = _ilCodeStreams.Emitter;

            LoadNativeAddr(codeStream);

            var helper = Context.GetHelperEntryPoint("InteropHelpers", "VariantToObject");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            StoreManagedValue(codeStream);
        }

        protected override void EmitCleanupManaged(ILCodeStream codeStream)
        {
            // Only do cleanup if it is IN
            if (!In)
            {
                return;
            }

            ILEmitter emitter = _ilCodeStreams.Emitter;

            LoadNativeAddr(codeStream);
            var helper = Context.GetHelperEntryPoint("InteropHelpers", "CleanupVariant");
            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));
        }
    }

    class CustomTypeMarshaller : Marshaller
    {
        private ILLocalVariable MarshallerLocalVariable = (ILLocalVariable)(-1);

        private ILLocalVariable InitializeMarshallerVariable()
        {
            if (MarshallerLocalVariable != (ILLocalVariable)(-1))
            {
                return MarshallerLocalVariable;
            }

            var marshallerType = MarshalAsDescriptor.MarshallerType;
            if (marshallerType.IsGenericDefinition)
            {
                ThrowHelper.ThrowTypeLoadException(marshallerType);
            }

            var customMarshallerType = Context.SystemModule.GetKnownType("System.Runtime.InteropServices", "ICustomMarshaler");
            var getInstanceMethod = marshallerType.GetMethod(
                "GetInstance",
                new MethodSignature(MethodSignatureFlags.Static, 0, customMarshallerType, new[] { Context.GetWellKnownType(WellKnownType.String) }));
            if (ManagedType.IsValueType || ManagedType.IsPointer || ManagedType.IsFunctionPointer)
            {
                ThrowHelper.ThrowMarshalDirectiveException();
            }

            var initializeCustomMarshallerMethod = Context.GetHelperEntryPoint("InteropHelpers", "InitializeCustomMarshaller");

            ILEmitter emitter = _ilCodeStreams.Emitter;
            MarshallerLocalVariable = emitter.NewLocal(customMarshallerType);
            var cookie = MarshalAsDescriptor.Cookie;

            // Custom marshaller initialization should not be caught, so initialize early
            ILCodeStream fnptrLoadStream = _ilCodeStreams.FunctionPointerLoadStream;
            fnptrLoadStream.Emit(ILOpcode.ldtoken, emitter.NewToken(ManagedType));
            fnptrLoadStream.Emit(ILOpcode.ldtoken, emitter.NewToken(marshallerType));
            fnptrLoadStream.Emit(ILOpcode.ldstr, emitter.NewToken(cookie));
            if (getInstanceMethod != null)
            {
                fnptrLoadStream.Emit(ILOpcode.ldftn, emitter.NewToken(getInstanceMethod));
            }
            else
            {
                fnptrLoadStream.EmitLdc(0);
                fnptrLoadStream.Emit(ILOpcode.conv_i);
            }

            fnptrLoadStream.Emit(ILOpcode.call, emitter.NewToken(initializeCustomMarshallerMethod));
            fnptrLoadStream.EmitStLoc(MarshallerLocalVariable);
            return MarshallerLocalVariable;
        }

        protected override void TransformManagedToNative(ILCodeStream codeStream)
        {
            var lMarshaller = InitializeMarshallerVariable();

            var customMarshallerType = Context.SystemModule.GetKnownType("System.Runtime.InteropServices", "ICustomMarshaler");
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var manageToNativeMethod = customMarshallerType.GetKnownMethod(
                "MarshalManagedToNative",
                new MethodSignature(MethodSignatureFlags.None, 0, Context.GetWellKnownType(WellKnownType.IntPtr), new[] { Context.GetWellKnownType(WellKnownType.Object) }));

            codeStream.EmitLdLoc(lMarshaller);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(manageToNativeMethod));
            StoreNativeValue(codeStream);
            if (MarshalDirection == MarshalDirection.Forward)
            {
                if (In && Out)
                {
                    EmitCleanUpManagedData(codeStream);
                }

                EmitCleanUpNativeData(_ilCodeStreams.CleanupCodeStream);
            }
            else
            {
                EmitCleanUpManagedData(codeStream);
            }
        }

        protected override void TransformNativeToManaged(ILCodeStream codeStream)
        {
            var lMarshaller = InitializeMarshallerVariable();

            var customMarshallerType = Context.SystemModule.GetKnownType("System.Runtime.InteropServices", "ICustomMarshaler");
            ILEmitter emitter = _ilCodeStreams.Emitter;
            var marshalNativeToManagedMethod = customMarshallerType.GetKnownMethod(
                "MarshalNativeToManaged",
                new MethodSignature(MethodSignatureFlags.None, 0, Context.GetWellKnownType(WellKnownType.Object), new[] { Context.GetWellKnownType(WellKnownType.IntPtr) }));

            codeStream.EmitLdLoc(lMarshaller);
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(marshalNativeToManagedMethod));
            StoreManagedValue(codeStream);
        }

        protected void EmitCleanUpManagedData(ILCodeStream codeStream)
        {
            var lMarshaller = InitializeMarshallerVariable();

            var customMarshallerType = Context.SystemModule.GetKnownType("System.Runtime.InteropServices", "ICustomMarshaler");
            ILEmitter emitter = _ilCodeStreams.Emitter;

            // Call CleanUpManagedData on cleanup code stream.
            var cleanupManagedDataMethod = customMarshallerType.GetKnownMethod(
                "CleanUpManagedData",
                new MethodSignature(MethodSignatureFlags.None, 0, Context.GetWellKnownType(WellKnownType.Void), new[] { Context.GetWellKnownType(WellKnownType.Object) }));

            codeStream.EmitLdLoc(lMarshaller);
            LoadManagedValue(codeStream);
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(cleanupManagedDataMethod));
        }

        protected void EmitCleanUpNativeData(ILCodeStream codeStream)
        {
            var lMarshaller = InitializeMarshallerVariable();

            var customMarshallerType = Context.SystemModule.GetKnownType("System.Runtime.InteropServices", "ICustomMarshaler");
            ILEmitter emitter = _ilCodeStreams.Emitter;

            // Call CleanUpNativeData on cleanup code stream.
            var cleanupNativeDataMethod = customMarshallerType.GetKnownMethod(
                "CleanUpNativeData",
                new MethodSignature(MethodSignatureFlags.None, 0, Context.GetWellKnownType(WellKnownType.Void), new[] { Context.GetWellKnownType(WellKnownType.IntPtr) }));

            codeStream.EmitLdLoc(lMarshaller);
            LoadNativeValue(codeStream);
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(cleanupNativeDataMethod));
        }
    }

    class BlittableValueClassByRefReturn : Marshaller
    {
        protected override void SetupArgumentsForReturnValueMarshalling()
        {
            ILEmitter emitter = _ilCodeStreams.Emitter;

            _managedHome = new Home(emitter.NewLocal(ManagedParameterType), ManagedParameterType, isByRef: false);
            _nativeHome = new Home(emitter.NewLocal(NativeType), NativeType, isByRef: false);
        }
    }
}
