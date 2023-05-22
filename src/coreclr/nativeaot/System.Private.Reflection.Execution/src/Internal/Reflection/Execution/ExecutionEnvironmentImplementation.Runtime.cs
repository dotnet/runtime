// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.Runtime.Augments;

using Internal.Reflection.Core.Execution;
using Internal.Reflection.Execution.FieldAccessors;
using Internal.Reflection.Execution.MethodInvokers;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================
    // These ExecutionEnvironment entrypoints provide basic runtime allocation and policy services to
    // Reflection. Our implementation merely forwards to System.Private.CoreLib.
    //==========================================================================================================
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        public sealed override object NewObject(RuntimeTypeHandle typeHandle)
        {
            return RuntimeAugments.NewObject(typeHandle);
        }

        public sealed override Array NewArray(RuntimeTypeHandle typeHandleForArrayType, int count)
        {
            return RuntimeAugments.NewArray(typeHandleForArrayType, count);
        }

        public sealed override Array NewMultiDimArray(RuntimeTypeHandle typeHandleForArrayType, int[] lengths, int[] lowerBounds)
        {
            return RuntimeAugments.NewMultiDimArray(typeHandleForArrayType, lengths, lowerBounds);
        }

        public sealed override RuntimeTypeHandle ProjectionTypeForArrays
        {
            get
            {
                return RuntimeAugments.ProjectionTypeForArrays;
            }
        }

        public sealed override bool IsAssignableFrom(RuntimeTypeHandle dstType, RuntimeTypeHandle srcType)
        {
            return RuntimeAugments.IsAssignableFrom(dstType, srcType);
        }

        public sealed override bool TryGetBaseType(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle baseTypeHandle)
        {
            return RuntimeAugments.TryGetBaseType(typeHandle, out baseTypeHandle);
        }

        public sealed override IEnumerable<RuntimeTypeHandle> TryGetImplementedInterfaces(RuntimeTypeHandle typeHandle)
        {
            return RuntimeAugments.TryGetImplementedInterfaces(typeHandle);
        }

        public sealed override void VerifyInterfaceIsImplemented(RuntimeTypeHandle typeHandle, RuntimeTypeHandle ifaceHandle)
        {
            if (RuntimeAugments.IsInterface(typeHandle))
            {
                throw new ArgumentException(SR.Argument_InterfaceMap);
            }

            if (!RuntimeAugments.IsInterface(ifaceHandle))
            {
                throw new ArgumentException(SR.Arg_MustBeInterface);
            }

            if (RuntimeAugments.IsAssignableFrom(ifaceHandle, typeHandle))
            {
                return;
            }

            throw new ArgumentException(SR.Arg_NotFoundIFace);
        }

        public sealed override void GetInterfaceMap(Type instanceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType, out MethodInfo[] interfaceMethods, out MethodInfo[] targetMethods)
        {
            MethodInfo[] ifaceMethods = interfaceType.GetMethods();
            var tMethods = new MethodInfo[ifaceMethods.Length];
            for (int i = 0; i < ifaceMethods.Length; i++)
            {
                var invoker = (VirtualMethodInvoker)GetMethodInvoker(ifaceMethods[i]);

                IntPtr classRtMethodHandle = invoker.ResolveTarget(instanceType.TypeHandle);
                if (classRtMethodHandle == IntPtr.Zero)
                {
                    goto notFound;
                }

                MethodBase methodBase = RuntimeAugments.Callbacks.GetMethodBaseFromStartAddressIfAvailable(classRtMethodHandle);
                if (methodBase == null)
                {
                    goto notFound;
                }

                tMethods[i] = (MethodInfo)methodBase;
                continue;

            notFound:
                if (instanceType.IsAbstract)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.Arg_InterfaceMapMustNotBeAbstract, interfaceType.FullName, instanceType.FullName));
                }

                throw new NotSupportedException();
            }

            interfaceMethods = ifaceMethods;
            targetMethods = tMethods;
        }

        //==============================================================================================
        // Miscellaneous
        //==============================================================================================
        public sealed override FieldAccessor CreateLiteralFieldAccessor(object value, RuntimeTypeHandle fieldTypeHandle)
        {
            return new LiteralFieldAccessor(value, fieldTypeHandle);
        }

        public sealed override void GetEnumInfo(RuntimeTypeHandle typeHandle, out string[] names, out object[] values, out bool isFlags)
        {
            // Handle the weird case of an enum type nested under a generic type that makes the
            // enum itself generic
            RuntimeTypeHandle typeDefHandle = typeHandle;
            if (RuntimeAugments.IsGenericType(typeHandle))
            {
                typeDefHandle = RuntimeAugments.GetGenericDefinition(typeHandle);
            }

            QTypeDefinition qTypeDefinition = ReflectionExecution.ExecutionEnvironment.GetMetadataForNamedType(typeDefHandle);

            if (qTypeDefinition.IsNativeFormatMetadataBased)
            {
                NativeFormatEnumInfo.GetEnumValuesAndNames(
                    qTypeDefinition.NativeFormatReader,
                    qTypeDefinition.NativeFormatHandle,
                    out values,
                    out names,
                    out isFlags);
                return;
            }
#if ECMA_METADATA_SUPPORT
            if (qTypeDefinition.IsEcmaFormatMetadataBased)
            {
                return EcmaFormatEnumInfo.Create<TUnderlyingValue>(typeHandle, qTypeDefinition.EcmaFormatReader, qTypeDefinition.EcmaFormatHandle);
            }
#endif
            names = Array.Empty<string>();
            values = Array.Empty<object>();
            isFlags = false;
            return;
        }

        public override IntPtr GetDynamicInvokeThunk(MethodInvoker invoker)
        {
            return ((MethodInvokerWithMethodInvokeInfo)invoker).MethodInvokeInfo.InvokeThunk
                ;
        }
    }
}
