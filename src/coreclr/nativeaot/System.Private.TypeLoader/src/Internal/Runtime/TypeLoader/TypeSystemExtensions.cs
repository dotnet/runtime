// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.TypeSystem.NativeFormat;
#if ECMA_METADATA_SUPPORT
using Internal.TypeSystem.Ecma;
#endif
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;
using Internal.TypeSystem.NoMetadata;
using System.Reflection.Runtime.General;

namespace Internal.TypeSystem.NativeFormat
{
    // When SUPPORTS_NATIVE_METADATA_TYPE_LOADING is not set we may see compile errors from using statements.
    // Add a namespace definition for Internal.TypeSystem.NativeFormat
}

namespace Internal.Runtime.TypeLoader
{
    internal static class TypeDescExtensions
    {
        public static bool CanShareNormalGenericCode(this TypeDesc type)
        {
            return (type != type.ConvertToCanonForm(CanonicalFormKind.Specific));
        }

        public static bool IsGeneric(this TypeDesc type)
        {
            DefType typeAsDefType = type as DefType;
            return typeAsDefType != null && typeAsDefType.HasInstantiation;
        }

        public static DefType GetClosestDefType(this TypeDesc type)
        {
            if (type is DefType)
                return (DefType)type;
            else
                return type.BaseType;
        }
    }

    internal static class MethodDescExtensions
    {
        public static bool CanShareNormalGenericCode(this InstantiatedMethod method)
        {
            return (method != method.GetCanonMethodTarget(CanonicalFormKind.Specific));
        }
    }

    internal static class RuntimeHandleExtensions
    {
        public static bool IsNull(this RuntimeTypeHandle rtth)
        {
            return RuntimeAugments.GetRuntimeTypeHandleRawValue(rtth) == IntPtr.Zero;
        }

        public static unsafe bool IsDynamic(this RuntimeFieldHandle rtfh)
        {
            IntPtr rtfhValue = *(IntPtr*)&rtfh;
            return (rtfhValue.ToInt64() & 0x1) == 0x1;
        }

        public static unsafe bool IsDynamic(this RuntimeMethodHandle rtfh)
        {
            IntPtr rtfhValue = *(IntPtr*)&rtfh;
            return (rtfhValue.ToInt64() & 0x1) == 0x1;
        }
    }

    public static partial class RuntimeSignatureHelper
    {
        public static bool TryCreate(MethodDesc method, out RuntimeSignature methodSignature)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            if (typicalMethod is TypeSystem.NativeFormat.NativeFormatMethod)
            {
                TypeSystem.NativeFormat.NativeFormatMethod nativeFormatMethod = (TypeSystem.NativeFormat.NativeFormatMethod)typicalMethod;
                methodSignature = RuntimeSignature.CreateFromMethodHandle(nativeFormatMethod.MetadataUnit.RuntimeModule, nativeFormatMethod.Handle.ToInt());
                return true;
            }
#if ECMA_METADATA_SUPPORT
            if (typicalMethod is TypeSystem.Ecma.EcmaMethod)
            {
                unsafe
                {
                    TypeSystem.Ecma.EcmaMethod ecmaMethod = (TypeSystem.Ecma.EcmaMethod)typicalMethod;
                    methodSignature = RuntimeSignature.CreateFromMethodHandle(new IntPtr(ecmaMethod.Module.RuntimeModuleInfo.DynamicModulePtr), System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(ecmaMethod.Handle));
                }
                return true;
            }
#endif
#endif
            methodSignature = default(RuntimeSignature);
            return false;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public static MethodDesc ToMethodDesc(this RuntimeMethodHandle rmh, TypeSystemContext typeSystemContext)
        {
            RuntimeTypeHandle declaringTypeHandle;
            MethodNameAndSignature nameAndSignature;
            RuntimeTypeHandle[] genericMethodArgs;

            if (!TypeLoaderEnvironment.Instance.TryGetRuntimeMethodHandleComponents(rmh, out declaringTypeHandle, out nameAndSignature, out genericMethodArgs))
            {
                return null;
            }

            QMethodDefinition methodHandle;
            if (!TypeLoaderEnvironment.Instance.TryGetMetadataForTypeMethodNameAndSignature(declaringTypeHandle, nameAndSignature, out methodHandle))
            {
                return null;
            }

            TypeDesc declaringType = typeSystemContext.ResolveRuntimeTypeHandle(declaringTypeHandle);

            TypeDesc declaringTypeDefinition = declaringType.GetTypeDefinition();
            MethodDesc typicalMethod = null;
            if (methodHandle.IsNativeFormatMetadataBased)
            {
                var nativeFormatType = (NativeFormatType)declaringTypeDefinition;
                typicalMethod = nativeFormatType.MetadataUnit.GetMethod(methodHandle.NativeFormatHandle, nativeFormatType);
            }
            else if (methodHandle.IsEcmaFormatMetadataBased)
            {
                var ecmaFormatType = (EcmaType)declaringTypeDefinition;
                typicalMethod = ecmaFormatType.EcmaModule.GetMethod(methodHandle.EcmaFormatHandle);
            }
            Debug.Assert(typicalMethod != null);

            MethodDesc methodOnInstantiatedType = typicalMethod;
            if (declaringType != declaringTypeDefinition)
                methodOnInstantiatedType = typeSystemContext.GetMethodForInstantiatedType(typicalMethod, (InstantiatedType)declaringType);

            MethodDesc instantiatedMethod = methodOnInstantiatedType;
            if (genericMethodArgs != null)
            {
                Debug.Assert(genericMethodArgs.Length > 0);
                Instantiation genericMethodInstantiation = typeSystemContext.ResolveRuntimeTypeHandles(genericMethodArgs);
                typeSystemContext.GetInstantiatedMethod(methodOnInstantiatedType, genericMethodInstantiation);
            }

            return instantiatedMethod;
        }
#endif
    }
}
