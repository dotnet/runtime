// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;
using Internal.Reflection.Execution.FieldAccessors;
using Internal.Reflection.Execution.MethodInvokers;
using Internal.Reflection.Execution.PayForPlayExperience;
using Internal.Reflection.Extensions.NonPortable;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================
    // These ExecutionEnvironment entrypoints provide basic runtime allocation and policy services to
    // Reflection. Our implementation merely forwards to System.Private.CoreLib.
    //==========================================================================================================
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
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

                MethodBase methodBase = ReflectionExecution.GetMethodBaseFromStartAddressIfAvailable(classRtMethodHandle);
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

        public override IntPtr GetDynamicInvokeThunk(MethodBaseInvoker invoker)
        {
            return ((MethodInvokerWithMethodInvokeInfo)invoker).MethodInvokeInfo.InvokeThunk;
        }

        public override MethodInfo GetDelegateMethod(Delegate del)
        {
            return DelegateMethodInfoRetriever.GetDelegateMethodInfo(del);
        }

        public override MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress)
        {
            return ReflectionExecution.GetMethodBaseFromStartAddressIfAvailable(methodStartAddress);
        }

        public override IntPtr GetStaticClassConstructionContext(RuntimeTypeHandle typeHandle)
        {
            return TypeLoaderEnvironment.GetStaticClassConstructionContext(typeHandle);
        }

        // Obtain it lazily to avoid using RuntimeAugments.Callbacks before it is initialized
        public override AssemblyBinder AssemblyBinder => AssemblyBinderImplementation.Instance;

        public override Exception CreateMissingMetadataException(Type pertainant)
        {
            return MissingMetadataExceptionCreator.Create(pertainant);
        }

        public override Exception CreateNonInvokabilityException(MemberInfo pertainant)
        {
            string resourceName = SR.Object_NotInvokable;

            if (pertainant is MethodBase methodBase)
            {
                resourceName = methodBase.IsConstructedGenericMethod ? SR.MakeGenericMethod_NoMetadata : SR.Object_NotInvokable;
                if (methodBase is ConstructorInfo)
                {
                    Type declaringType = methodBase.DeclaringType;
                    if (declaringType.BaseType == typeof(MulticastDelegate))
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_CannotInvokeDelegateCtor);
                }
            }

            string pertainantString = MissingMetadataExceptionCreator.ComputeUsefulPertainantIfPossible(pertainant);
            return new NotSupportedException(SR.Format(resourceName, pertainantString ?? "?"));
        }
    }
}
