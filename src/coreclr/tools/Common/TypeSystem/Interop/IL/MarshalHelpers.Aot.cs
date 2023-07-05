// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Interop
{
    public static partial class MarshalHelpers
    {
        public static bool IsStructMarshallingRequired(TypeDesc typeDesc)
        {
            if (typeDesc is ByRefType)
            {
                typeDesc = typeDesc.GetParameterType();
            }

            typeDesc = typeDesc.UnderlyingType;

            // TODO: There are primitive types which require marshalling, such as bool, char.
            if (typeDesc.IsPrimitive)
            {
                return false;
            }

            MetadataType type = typeDesc as MetadataType;
            if (type == null)
            {
                return false;
            }

            //
            // For struct marshalling it is required to have either Sequential
            // or Explicit layout. For Auto layout the P/Invoke marshalling code
            // will throw appropriate error message.
            //
            if (!type.HasLayout())
                return false;

            if (!type.IsValueType)
                return true;

            // If it is not blittable we will need struct marshalling
            return !MarshalUtils.IsBlittableType(type);
        }

        internal static TypeDesc GetNativeMethodParameterType(TypeDesc type, MarshalAsDescriptor marshalAs, InteropStateManager interopStateManager, bool isReturn, bool isAnsi)
        {
            MarshallerKind elementMarshallerKind;
            MarshallerKind marshallerKind = GetMarshallerKind(type,
                                                null,   /* parameterIndex */
                                                null,   /* customModifierData */
                                                marshalAs,
                                                isReturn,
                                                isAnsi,
                                                MarshallerType.Argument,
                                                out elementMarshallerKind);

            return GetNativeTypeFromMarshallerKind(type,
                marshallerKind,
                elementMarshallerKind,
                interopStateManager,
                marshalAs);
        }

        internal static TypeDesc GetNativeStructFieldType(TypeDesc type, MarshalAsDescriptor marshalAs, InteropStateManager interopStateManager, bool isAnsi)
        {
            MarshallerKind elementMarshallerKind;
            MarshallerKind marshallerKind = GetMarshallerKind(type,
                                                null,   /* parameterIndex */
                                                null,   /* customModifierData */
                                                marshalAs,
                                                false,  /*  isReturn */
                                                isAnsi, /*    isAnsi */
                                                MarshallerType.Field,
                                                out elementMarshallerKind);

            return GetNativeTypeFromMarshallerKind(type,
                marshallerKind,
                elementMarshallerKind,
                interopStateManager,
                marshalAs);
        }

        internal static InlineArrayCandidate GetInlineArrayCandidate(TypeDesc managedElementType, MarshallerKind elementMarshallerKind, InteropStateManager interopStateManager, MarshalAsDescriptor marshalAs)
        {
            TypeDesc nativeType = GetNativeTypeFromMarshallerKind(
                                                managedElementType,
                                                elementMarshallerKind,
                                                MarshallerKind.Unknown,
                                                interopStateManager,
                                                null);

            var elementNativeType = nativeType as MetadataType;
            if (elementNativeType == null)
            {
                Debug.Assert(nativeType.IsPointer || nativeType.IsFunctionPointer);

                // If it is a pointer type we will create InlineArray for IntPtr
                elementNativeType = (MetadataType)managedElementType.Context.GetWellKnownType(WellKnownType.IntPtr);
            }
            Debug.Assert(marshalAs != null && marshalAs.SizeConst.HasValue);

            // if SizeConst is not specified, we will default to 1.
            // the marshaller will throw appropriate exception
            uint size = 1;
            if (marshalAs.SizeConst.HasValue)
            {
                size = marshalAs.SizeConst.Value;
            }
            return new InlineArrayCandidate(elementNativeType, size);

        }

        //TODO: https://github.com/dotnet/corert/issues/2675
        // This exception messages need to localized
        // TODO: Log as warning
        public static MethodIL EmitExceptionBody(string message, MethodDesc method)
        {
            ILEmitter emitter = new ILEmitter();

            TypeSystemContext context = method.Context;
            MethodSignature ctorSignature = new MethodSignature(0, 0, context.GetWellKnownType(WellKnownType.Void),
                new TypeDesc[] { context.GetWellKnownType(WellKnownType.String) });
            MethodDesc exceptionCtor = InteropTypes.GetMarshalDirectiveException(context).GetKnownMethod(".ctor", ctorSignature);

            ILCodeStream codeStream = emitter.NewCodeStream();
            codeStream.Emit(ILOpcode.ldstr, emitter.NewToken(message));
            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(exceptionCtor));
            codeStream.Emit(ILOpcode.throw_);

            return new PInvokeILStubMethodIL((ILStubMethodIL)emitter.Link(method), isStubRequired: true);
        }
    }
}
