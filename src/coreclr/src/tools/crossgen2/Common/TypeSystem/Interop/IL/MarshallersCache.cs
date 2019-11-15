// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem.Interop
{
    internal sealed class MethodMarshallersCache : LockFreeReaderHashtable<MethodDesc, MethodMarshallersCache.MethodMarshallers>
    {
        protected override int GetKeyHashCode(MethodDesc key)
        {
            return key.GetHashCode();
        }
        protected override int GetValueHashCode(MethodMarshallers value)
        {
            return value.Method.GetHashCode();
        }
        protected override bool CompareKeyToValue(MethodDesc key, MethodMarshallers value)
        {
            return Object.ReferenceEquals(key, value.Method);
        }
        protected override bool CompareValueToValue(MethodMarshallers value1, MethodMarshallers value2)
        {
            return Object.ReferenceEquals(value1.Method, value2.Method);
        }
        protected override MethodMarshallers CreateValueFromKey(MethodDesc key)
        {
            Debug.Assert(key.IsPInvoke);

            return new MethodMarshallers(key, InitializeMarshallers(key, key.GetPInvokeMethodMetadata().Flags)); 
        }
        private static Marshaller[] InitializeMarshallers(MethodDesc targetMethod, PInvokeFlags flags)
        {
            MarshalDirection direction = MarshalDirection.Forward;
            MethodSignature methodSig = targetMethod.Signature;

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
                marshallers[i] = Marshaller.CreateMarshaller(parameterType,
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

        internal class MethodMarshallers
        {
            internal MethodMarshallers(MethodDesc targetMethod, Marshaller[] marshallers)
            {
                Method = targetMethod;
                Marshallers = marshallers;
            }

            public readonly MethodDesc Method;
            public readonly Marshaller[] Marshallers;
        }
    }
}
