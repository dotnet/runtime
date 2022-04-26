// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Internal.TypeSystem;
using Internal.TypeSystem.Interop;
using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    public enum StructMarshallingThunkType : byte
    {
        ManagedToNative = 1,
        NativeToManaged = 2,
        Cleanup = 4
    }

    public struct InlineArrayCandidate
    {
        public readonly MetadataType ElementType;
        public readonly uint Length;

        public InlineArrayCandidate(MetadataType type, uint length)
        {
            ElementType = type;
            Length = length;
        }
    }

    public partial class StructMarshallingThunk : ILStubMethod
    {
        internal readonly MetadataType ManagedType;
        internal readonly NativeStructType NativeType;
        internal readonly StructMarshallingThunkType ThunkType;
        private  InteropStateManager _interopStateManager;
        private TypeDesc _owningType;

        public StructMarshallingThunk(TypeDesc owningType, MetadataType managedType, StructMarshallingThunkType thunkType, InteropStateManager interopStateManager)
        {
            _owningType = owningType;
            ManagedType = managedType;
            _interopStateManager = interopStateManager;
            NativeType = _interopStateManager.GetStructMarshallingNativeType(managedType);
            ThunkType = thunkType;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return ManagedType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        private MethodSignature _signature;
        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    TypeDesc[] parameters = null;
                    switch (ThunkType)
                    {
                        case StructMarshallingThunkType.ManagedToNative:
                            parameters = new TypeDesc[] {
                                ManagedType.IsValueType ? (TypeDesc)ManagedType.MakeByRefType() : ManagedType,
                                NativeType.MakeByRefType()
                            };
                            break;
                        case StructMarshallingThunkType.NativeToManaged:
                            parameters = new TypeDesc[] {
                                NativeType.MakeByRefType(),
                                ManagedType.IsValueType ? (TypeDesc)ManagedType.MakeByRefType() : ManagedType
                            };
                            break;
                        case StructMarshallingThunkType.Cleanup:
                            parameters = new TypeDesc[] {
                                NativeType.MakeByRefType()
                            };
                            break;
                        default:
                            System.Diagnostics.Debug.Fail("Unexpected Struct marshalling thunk type");
                            break;
                    }
                    _signature = new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), parameters);
                }
                return _signature;
            }
        }

        private string NamePrefix
        {
            get
            {
                switch (ThunkType)
                {
                    case StructMarshallingThunkType.ManagedToNative:
                        return "ManagedToNative";
                    case StructMarshallingThunkType.NativeToManaged:
                        return "NativeToManaged";
                    case StructMarshallingThunkType.Cleanup:
                        return "Cleanup";
                    default:
                        System.Diagnostics.Debug.Fail("Unexpected Struct marshalling thunk type");
                        return string.Empty;
                }
            }
        }

        public override string Name
        {
            get
            {
                return NamePrefix + "__" + ((MetadataType)ManagedType).Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return NamePrefix + "__" + ManagedType.DiagnosticName;
            }
        }

        private Marshaller[] InitializeMarshallers()
        {
            Debug.Assert(_interopStateManager != null);

            int numInstanceFields = 0;
            foreach (var field in ManagedType.GetFields())
            {
                if (field.IsStatic)
                    continue;
                numInstanceFields++;
            }

            Marshaller[] marshallers = new Marshaller[numInstanceFields];

            PInvokeFlags flags = new PInvokeFlags();
            if (ManagedType.PInvokeStringFormat == PInvokeStringFormat.UnicodeClass || ManagedType.PInvokeStringFormat == PInvokeStringFormat.AutoClass)
            {
                flags.CharSet = CharSet.Unicode;
            }
            else
            {
                flags.CharSet = CharSet.Ansi;
            }

            int index = 0;

            foreach (FieldDesc field in ManagedType.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                marshallers[index] = Marshaller.CreateMarshaller(field.FieldType,
                                                                    null,   /* parameterIndex */
                                                                    null,   /* customModifierData */
                                                                    MarshallerType.Field,
                                                                    field.GetMarshalAsDescriptor(),
                                                                    (ThunkType == StructMarshallingThunkType.NativeToManaged) ? MarshalDirection.Reverse : MarshalDirection.Forward,
                                                                    marshallers,
                                                                    _interopStateManager,
                                                                    index,
                                                                    flags,
                                                                    isIn: true,     /* Struct fields are considered as IN within the helper*/
                                                                    isOut: false,
                                                                    isReturn: false);
                index++;
            }

            return marshallers;
        }

        private MethodIL EmitMarshallingIL(PInvokeILCodeStreams pInvokeILCodeStreams)
        {
            Marshaller[] marshallers = InitializeMarshallers();

            ILEmitter emitter = pInvokeILCodeStreams.Emitter;

            IEnumerator<FieldDesc> nativeEnumerator = NativeType.GetFields().GetEnumerator();

            int index = 0;
            foreach (var managedField in ManagedType.GetFields())
            {
                if (managedField.IsStatic)
                {
                    continue;
                }

                bool notEmpty = nativeEnumerator.MoveNext();
                Debug.Assert(notEmpty == true);

                var nativeField = nativeEnumerator.Current;
                Debug.Assert(nativeField != null);
                bool isInlineArray = nativeField.FieldType is InlineArrayType;
                //
                // Field marshallers expects the value of the fields to be 
                // loaded on the stack. We load the value on the stack
                // before calling the marshallers.
                // Only exception is ByValArray marshallers. Since they can
                // only be used for field marshalling, they load/store values 
                // directly from arguments.
                //

                if (isInlineArray)
                {
                    var byValMarshaller = marshallers[index++] as ByValArrayMarshaller;

                    Debug.Assert(byValMarshaller != null);

                    byValMarshaller.EmitMarshallingIL(pInvokeILCodeStreams, managedField, nativeField);
                }
                else
                {
                    if (ThunkType == StructMarshallingThunkType.ManagedToNative)
                    {
                        LoadFieldValueFromArg(0, managedField, pInvokeILCodeStreams);
                    }
                    else if (ThunkType == StructMarshallingThunkType.NativeToManaged)
                    {
                        LoadFieldValueFromArg(0, nativeField, pInvokeILCodeStreams);
                    }

                    marshallers[index++].EmitMarshallingIL(pInvokeILCodeStreams);

                    if (ThunkType == StructMarshallingThunkType.ManagedToNative)
                    {
                        StoreFieldValueFromArg(1, nativeField, pInvokeILCodeStreams);
                    }
                    else if (ThunkType == StructMarshallingThunkType.NativeToManaged)
                    {
                        StoreFieldValueFromArg(1, managedField, pInvokeILCodeStreams);
                    }
                }
            }

            Debug.Assert(!nativeEnumerator.MoveNext());

            pInvokeILCodeStreams.UnmarshallingCodestream.Emit(ILOpcode.ret);
            return emitter.Link(this);
        }

        private MethodIL EmitCleanupIL(PInvokeILCodeStreams pInvokeILCodeStreams)
        {
            Marshaller[] marshallers = InitializeMarshallers();
            ILEmitter emitter = pInvokeILCodeStreams.Emitter;
            ILCodeStream codeStream = pInvokeILCodeStreams.MarshallingCodeStream;
            IEnumerator<FieldDesc> nativeEnumerator = NativeType.GetFields().GetEnumerator();
            int index = 0;
            foreach (var managedField in ManagedType.GetFields())
            {
                if (managedField.IsStatic)
                {
                    continue;
                }

                bool notEmpty = nativeEnumerator.MoveNext();
                Debug.Assert(notEmpty == true);

                var nativeField = nativeEnumerator.Current;
                Debug.Assert(nativeField != null);

                if (marshallers[index].CleanupRequired)
                {
                    LoadFieldValueFromArg(0, nativeField, pInvokeILCodeStreams);
                    marshallers[index].EmitElementCleanup(codeStream, emitter);
                }
                index++;
            }

            pInvokeILCodeStreams.UnmarshallingCodestream.Emit(ILOpcode.ret);
            return emitter.Link(this);
        }

        public override MethodIL EmitIL()
        {
            try
            {
                PInvokeILCodeStreams pInvokeILCodeStreams = new PInvokeILCodeStreams();

                if (ThunkType == StructMarshallingThunkType.Cleanup)
                {
                    return EmitCleanupIL(pInvokeILCodeStreams);
                }
                else
                {
                    return EmitMarshallingIL(pInvokeILCodeStreams);
                }
            }
            catch (NotSupportedException)
            {
                string message = "Struct '" + ((MetadataType)ManagedType).Name +
                    "' requires marshalling that is not yet supported by this compiler.";
                return MarshalHelpers.EmitExceptionBody(message, this);
            }
            catch (InvalidProgramException ex)
            {
                Debug.Assert(!String.IsNullOrEmpty(ex.Message));
                return MarshalHelpers.EmitExceptionBody(ex.Message, this);
            }
        }

        /// <summary>
        /// Loads the value of field of a struct at argument index argIndex to stack
        /// </summary>
        private void LoadFieldValueFromArg(int argIndex, FieldDesc field, PInvokeILCodeStreams pInvokeILCodeStreams)
        {
            ILCodeStream stream = pInvokeILCodeStreams.MarshallingCodeStream;
            ILEmitter emitter = pInvokeILCodeStreams.Emitter;
            stream.EmitLdArg(argIndex);
            stream.Emit(ILOpcode.ldfld, emitter.NewToken(field));
        }

        private void StoreFieldValueFromArg(int argIndex, FieldDesc field, PInvokeILCodeStreams pInvokeILCodeStreams)
        {
            ILCodeStream stream = pInvokeILCodeStreams.MarshallingCodeStream;
            ILEmitter emitter = pInvokeILCodeStreams.Emitter;
            Internal.IL.Stubs.ILLocalVariable var = emitter.NewLocal(field.FieldType);

            stream.EmitStLoc(var);

            stream.EmitLdArg(argIndex);
            stream.EmitLdLoc(var);
            stream.Emit(ILOpcode.stfld, emitter.NewToken(field));
        }
    }
}
