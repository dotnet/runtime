// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

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
                case MarshallerKind.BlittableArray:
                    return new BlittableArrayMarshaller();
                case MarshallerKind.Bool:
                case MarshallerKind.CBool:
                    return new BooleanMarshaller();
                case MarshallerKind.AnsiString:
                    return new AnsiStringMarshaller();
                case MarshallerKind.SafeHandle:
                    return new SafeHandleMarshaller();
                case MarshallerKind.UnicodeString:
                    return new UnicodeStringMarshaller();
                case MarshallerKind.VoidReturn:
                    return new VoidReturnMarshaller();
                case MarshallerKind.FunctionPointer:
                    return new DelegateMarshaller();
                default:
                    // ensures we don't throw during create marshaller. We will throw NSE
                    // during EmitIL which will be handled.
                    return new NotSupportedMarshaller();
            }
        }

        public static Marshaller[] GetMarshallersForMethod(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);

            MarshalDirection direction = MarshalDirection.Forward;
            MethodSignature methodSig = targetMethod.Signature;
            PInvokeFlags flags = targetMethod.GetPInvokeMethodMetadata().Flags;

            ParameterMetadata[] parameterMetadataArray = targetMethod.GetParameterMetadata();
            Marshaller[] marshallers = new Marshaller[methodSig.Length + 1];
            ParameterMetadata parameterMetadata;

            for (int i = 0, parameterIndex = 0; i < marshallers.Length; i++)
            {
                Debug.Assert(parameterIndex == parameterMetadataArray.Length || i <= parameterMetadataArray[parameterIndex].Index);
                if (parameterIndex == parameterMetadataArray.Length || i < parameterMetadataArray[parameterIndex].Index)
                {
                    // if we don't have metadata for the parameter, create a dummy one
                    parameterMetadata = new ParameterMetadata(i, ParameterMetadataAttributes.None, null);
                }
                else
                {
                    Debug.Assert(i == parameterMetadataArray[parameterIndex].Index);
                    parameterMetadata = parameterMetadataArray[parameterIndex++];
                }

                TypeDesc parameterType = (i == 0) ? methodSig.ReturnType : methodSig[i - 1];  //first item is the return type
                marshallers[i] = CreateMarshaller(parameterType,
                                                    parameterIndex,
                                                    methodSig.GetEmbeddedSignatureData(),
                                                    MarshallerType.Argument,
                                                    parameterMetadata.MarshalAsDescriptor,
                                                    direction,
                                                    marshallers,
                                                    parameterMetadata.Index,
                                                    flags,
                                                    parameterMetadata.In,
                                                    parameterMetadata.Out,
                                                    parameterMetadata.Return);
            }

            return marshallers;
        }

        public static bool IsMarshallingRequired(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);

            if (targetMethod.IsUnmanagedCallersOnly)
                return true;

            PInvokeMetadata metadata = targetMethod.GetPInvokeMethodMetadata();
            PInvokeFlags flags = metadata.Flags;

            if (flags.SetLastError)
                return true;

            if (!flags.PreserveSig)
                return true;

            if (MarshalHelpers.ShouldCheckForPendingException(targetMethod.Context.Target, metadata))
                return true;

            var marshallers = GetMarshallersForMethod(targetMethod);
            for (int i = 0; i < marshallers.Length; i++)
            {
                if (marshallers[i].IsMarshallingRequired())
                    return true;
            }

            return false;
        }

        public static bool IsMarshallingRequired(MethodSignature methodSig, ParameterMetadata[] paramMetadata)
        {
            for (int i = 0, paramIndex = 0; i < methodSig.Length + 1; i++)
            {
                ParameterMetadata parameterMetadata = (paramIndex == paramMetadata.Length || i < paramMetadata[paramIndex].Index) ?
                    new ParameterMetadata(i, ParameterMetadataAttributes.None, null) :
                    paramMetadata[paramIndex++];

                TypeDesc parameterType = (i == 0) ? methodSig.ReturnType : methodSig[i - 1];  //first item is the return type

                MarshallerKind marshallerKind = MarshalHelpers.GetMarshallerKind(
                    parameterType,
                    parameterIndex: i,
                    customModifierData: methodSig.GetEmbeddedSignatureData(),
                    parameterMetadata.MarshalAsDescriptor,
                    parameterMetadata.Return,
                    isAnsi: true,
                    MarshallerType.Argument,
                    out MarshallerKind elementMarshallerKind);

                if (IsMarshallingRequired(marshallerKind))
                    return true;
            }

            return false;
        }
    }
}
