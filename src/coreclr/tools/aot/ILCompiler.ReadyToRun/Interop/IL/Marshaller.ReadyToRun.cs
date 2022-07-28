// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Interop
{
    partial class Marshaller
    {
        protected static Marshaller CreateMarshaller(MarshallerKind kind)
        {
            // ReadyToRun only supports emitting IL for blittable types
            switch (kind)
            {
                case MarshallerKind.Enum:
                case MarshallerKind.BlittableValue:
                case MarshallerKind.BlittableStruct:
                case MarshallerKind.UnicodeChar:
                    return new BlittableValueMarshaller();
                case MarshallerKind.VoidReturn:
                    return new VoidReturnMarshaller();
                default:
                    // ensures we don't throw during create marshaller. We will throw NSE
                    // during EmitIL which will be handled.
                    return new NotSupportedMarshaller();
            }
        }

        private static Marshaller[] GetMarshallers(
            MethodSignature methodSig,
            PInvokeFlags flags,
            ParameterMetadata[] parameterMetadataArray,
            bool runtimeMarshallingEnabled)
        {
            Marshaller[] marshallers = new Marshaller[methodSig.Length + 1];

            for (int i = 0, parameterIndex = 0; i < marshallers.Length; i++)
            {
                Debug.Assert(parameterIndex == parameterMetadataArray.Length || i <= parameterMetadataArray[parameterIndex].Index);

                ParameterMetadata parameterMetadata;
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
                if (runtimeMarshallingEnabled)
                {
                    marshallers[i] = CreateMarshaller(parameterType,
                                                        parameterIndex,
                                                        methodSig.GetEmbeddedSignatureData(),
                                                        MarshallerType.Argument,
                                                        parameterMetadata.MarshalAsDescriptor,
                                                        MarshalDirection.Forward,
                                                        marshallers,
                                                        parameterMetadata.Index,
                                                        flags,
                                                        parameterMetadata.In,
                                                        parameterMetadata.Out,
                                                        parameterMetadata.Return);
                }
                else
                {
                    marshallers[i] = CreateDisabledMarshaller(
                        parameterType,
                        parameterIndex,
                        MarshallerType.Argument,
                        MarshalDirection.Forward,
                        marshallers,
                        parameterMetadata.Index,
                        flags,
                        parameterMetadata.Return);
                }
            }

            return marshallers;
        }

        public static Marshaller[] GetMarshallersForMethod(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);
            return GetMarshallers(
                targetMethod.Signature,
                targetMethod.GetPInvokeMethodMetadata().Flags,
                targetMethod.GetParameterMetadata(),
                MarshalHelpers.IsRuntimeMarshallingEnabled(((MetadataType)targetMethod.OwningType).Module));
        }

        public static Marshaller[] GetMarshallersForSignature(MethodSignature methodSig, ParameterMetadata[] paramMetadata, ModuleDesc moduleContext)
        {
            return GetMarshallers(
                methodSig,
                new PInvokeFlags(PInvokeAttributes.None),
                paramMetadata,
                MarshalHelpers.IsRuntimeMarshallingEnabled(moduleContext));
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

        public static bool IsMarshallingRequired(MethodSignature methodSig, ParameterMetadata[] paramMetadata, ModuleDesc moduleContext)
        {
            Marshaller[] marshallers = GetMarshallersForSignature(methodSig, paramMetadata, moduleContext);
            for (int i = 0; i < marshallers.Length; i++)
            {
                if (marshallers[i].IsMarshallingRequired())
                    return true;
            }

            return false;
        }
    }
}
