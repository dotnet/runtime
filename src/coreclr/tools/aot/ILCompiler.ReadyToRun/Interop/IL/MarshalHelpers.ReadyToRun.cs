// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Interop
{
    public static partial class MarshalHelpers
    {
        private static bool IsBlittableMarshallerKind(MarshallerKind kind)
        {
            return kind is MarshallerKind.Enum or MarshallerKind.BlittableValue or MarshallerKind.BlittableStruct or MarshallerKind.UnicodeChar or MarshallerKind.VoidReturn;
        }

        private static MarshallerKind[] GetMarshallerKinds(
            MethodSignature methodSig,
            PInvokeFlags flags,
            ParameterMetadata[] parameterMetadataArray,
            bool runtimeMarshallingEnabled)
        {
            MarshallerKind[] marshallers = new MarshallerKind[methodSig.Length + 1];

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
                    bool isAnsi = flags.CharSet switch
                    {
                        CharSet.Ansi => true,
                        CharSet.Unicode => false,
                        CharSet.Auto => !parameterType.Context.Target.IsWindows,
                        _ => true
                    };
                    marshallers[i] = MarshalHelpers.GetMarshallerKind(parameterType,
                                                        parameterIndex,
                                                        methodSig.GetEmbeddedSignatureData(),
                                                        parameterMetadata.MarshalAsDescriptor,
                                                        parameterMetadata.Return,
                                                        isAnsi,
                                                        MarshallerType.Argument,
                                                        out _);
                }
                else
                {
                    marshallers[i] = MarshalHelpers.GetDisabledMarshallerKind(parameterType, false);
                }
            }

            return marshallers;
        }

        private static MarshallerKind[] GetMarshallersForMethod(MethodDesc targetMethod)
        {
            Debug.Assert(targetMethod.IsPInvoke);
            return GetMarshallerKinds(
                targetMethod.Signature,
                targetMethod.GetPInvokeMethodMetadata().Flags,
                targetMethod.GetParameterMetadata(),
                MarshalHelpers.IsRuntimeMarshallingEnabled(((MetadataType)targetMethod.OwningType).Module));
        }

        private static MarshallerKind[] GetMarshallersForSignature(MethodSignature methodSig, ParameterMetadata[] paramMetadata, ModuleDesc moduleContext)
        {
            return GetMarshallerKinds(
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
                if (!IsBlittableMarshallerKind(marshallers[i]))
                    return true;
            }

            return false;
        }

        public static bool IsMarshallingRequired(MethodSignature methodSig, ParameterMetadata[] paramMetadata, ModuleDesc moduleContext)
        {
            MarshallerKind[] marshallers = GetMarshallersForSignature(methodSig, paramMetadata, moduleContext);
            for (int i = 0; i < marshallers.Length; i++)
            {
                if (!IsBlittableMarshallerKind(marshallers[i]))
                    return true;
            }

            return false;
        }
    }
}
