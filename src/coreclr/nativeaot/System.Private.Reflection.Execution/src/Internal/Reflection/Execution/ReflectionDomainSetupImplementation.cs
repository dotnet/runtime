// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;

using global::Internal.Reflection.Core;
using global::Internal.Reflection.Execution.PayForPlayExperience;

namespace Internal.Reflection.Execution
{
    //=========================================================================================================================
    // The setup information for the reflection domain used for Project N's "classic reflection".
    //=========================================================================================================================
    internal sealed class ReflectionDomainSetupImplementation : ReflectionDomainSetup
    {
        public ReflectionDomainSetupImplementation()
        {
        }

        // Obtain it lazily to avoid using RuntimeAugments.Callbacks before it is initialized
        public sealed override AssemblyBinder AssemblyBinder => AssemblyBinderImplementation.Instance;

        public sealed override Exception CreateMissingMetadataException(TypeInfo pertainant)
        {
            return MissingMetadataExceptionCreator.Create(pertainant);
        }

        public sealed override Exception CreateMissingMetadataException(Type pertainant)
        {
            return MissingMetadataExceptionCreator.Create(pertainant);
        }

        public sealed override Exception CreateMissingMetadataException(TypeInfo pertainant, string nestedTypeName)
        {
            return MissingMetadataExceptionCreator.Create(pertainant, nestedTypeName);
        }

        public sealed override Exception CreateNonInvokabilityException(MemberInfo pertainant)
        {
            string resourceName = SR.Object_NotInvokable;

            if (pertainant is MethodBase methodBase)
            {
                resourceName = methodBase.IsConstructedGenericMethod ? SR.MakeGenericMethod_NoMetadata : SR.Object_NotInvokable;
                if (methodBase is ConstructorInfo)
                {
                    TypeInfo declaringTypeInfo = methodBase.DeclaringType.GetTypeInfo();
                    if (typeof(Delegate).GetTypeInfo().IsAssignableFrom(declaringTypeInfo))
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_CannotInvokeDelegateCtor);
                }
            }

            string pertainantString = MissingMetadataExceptionCreator.ComputeUsefulPertainantIfPossible(pertainant);
            return new MissingRuntimeArtifactException(SR.Format(resourceName, pertainantString ?? "?"));
        }

        public sealed override Exception CreateMissingArrayTypeException(Type elementType, bool isMultiDim, int rank)
        {
            return MissingMetadataExceptionCreator.CreateMissingArrayTypeException(elementType, isMultiDim, rank);
        }

        public sealed override Exception CreateMissingConstructedGenericTypeException(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            return MissingMetadataExceptionCreator.CreateMissingConstructedGenericTypeException(genericTypeDefinition, genericTypeArguments);
        }
    }
}
