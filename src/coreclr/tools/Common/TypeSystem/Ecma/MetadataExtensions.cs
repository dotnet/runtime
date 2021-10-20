// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Internal.TypeSystem.Ecma
{
    public static class MetadataExtensions
    {
        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this EcmaType This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(metadataReader.GetTypeDefinition(This.Handle).GetCustomAttributes(),
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This.EcmaModule));
        }

        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this EcmaMethod This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(metadataReader.GetMethodDefinition(This.Handle).GetCustomAttributes(),
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This.Module));
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(this EcmaMethod This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;
            var attributeHandles = metadataReader.GetMethodDefinition(This.Handle).GetCustomAttributes();
            foreach (var attributeHandle in attributeHandles)
            {
                if (IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This.Module));
                }
            }
        }

        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this EcmaField This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(metadataReader.GetFieldDefinition(This.Handle).GetCustomAttributes(),
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This.Module));
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(this EcmaField This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;
            var attributeHandles = metadataReader.GetFieldDefinition(This.Handle).GetCustomAttributes();
            foreach (var attributeHandle in attributeHandles)
            {
                if (IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This.Module));
                }
            }
        }

        public static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(this EcmaAssembly This,
            string attributeNamespace, string attributeName)
        {
            var metadataReader = This.MetadataReader;
            var attributeHandles = metadataReader.GetAssemblyDefinition().GetCustomAttributes();
            foreach (var attributeHandle in attributeHandles)
            {
                if (IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    yield return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(This));
                }
            }
        }

        public static CustomAttributeHandle GetCustomAttributeHandle(this MetadataReader metadataReader, CustomAttributeHandleCollection customAttributes,
            string attributeNamespace, string attributeName)
        {
            foreach (var attributeHandle in customAttributes)
            {
                if (IsEqualCustomAttributeName(attributeHandle, metadataReader, attributeNamespace, attributeName))
                {
                    return attributeHandle;
                }
            }

            return default(CustomAttributeHandle);
        }

        private static bool IsEqualCustomAttributeName(CustomAttributeHandle attributeHandle, MetadataReader metadataReader, 
            string attributeNamespace, string attributeName)
        {
            StringHandle namespaceHandle, nameHandle;
            if (!metadataReader.GetAttributeNamespaceAndName(attributeHandle, out namespaceHandle, out nameHandle))
                return false;

            return metadataReader.StringComparer.Equals(namespaceHandle, attributeNamespace)
                && metadataReader.StringComparer.Equals(nameHandle, attributeName);
        }

        public static bool GetAttributeNamespaceAndName(this MetadataReader metadataReader, CustomAttributeHandle attributeHandle,
            out StringHandle namespaceHandle, out StringHandle nameHandle)
        {
            EntityHandle attributeType, attributeCtor;
            if (!GetAttributeTypeAndConstructor(metadataReader, attributeHandle, out attributeType, out attributeCtor))
            {
                namespaceHandle = default(StringHandle);
                nameHandle = default(StringHandle);
                return false;
            }

            return GetAttributeTypeNamespaceAndName(metadataReader, attributeType, out namespaceHandle, out nameHandle);
        }

        public static bool GetAttributeTypeAndConstructor(this MetadataReader metadataReader, CustomAttributeHandle attributeHandle,
            out EntityHandle attributeType, out EntityHandle attributeCtor)
        {
            attributeCtor = metadataReader.GetCustomAttribute(attributeHandle).Constructor;

            if (attributeCtor.Kind == HandleKind.MemberReference)
            {
                attributeType = metadataReader.GetMemberReference((MemberReferenceHandle)attributeCtor).Parent;
                return true;
            }
            else if (attributeCtor.Kind == HandleKind.MethodDefinition)
            {
                attributeType = metadataReader.GetMethodDefinition((MethodDefinitionHandle)attributeCtor).GetDeclaringType();
                return true;
            }
            else
            {
                // invalid metadata
                attributeType = default(EntityHandle);
                return false;
            }
        }

        public static bool GetAttributeTypeNamespaceAndName(this MetadataReader metadataReader, EntityHandle attributeType,
            out StringHandle namespaceHandle, out StringHandle nameHandle)
        {
            namespaceHandle = default(StringHandle);
            nameHandle = default(StringHandle);

            if (attributeType.Kind == HandleKind.TypeReference)
            {
                TypeReference typeRefRow = metadataReader.GetTypeReference((TypeReferenceHandle)attributeType);
                HandleKind handleType = typeRefRow.ResolutionScope.Kind;

                // Nested type?
                if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
                    return false;

                nameHandle = typeRefRow.Name;
                namespaceHandle = typeRefRow.Namespace;
                return true;
            }
            else if (attributeType.Kind == HandleKind.TypeDefinition)
            {
                var def = metadataReader.GetTypeDefinition((TypeDefinitionHandle)attributeType);

                // Nested type?
                if (IsNested(def.Attributes))
                    return false;

                nameHandle = def.Name;
                namespaceHandle = def.Namespace;
                return true;
            }
            else
            {
                // unsupported metadata
                return false;
            }
        }

        public static PInvokeFlags GetDelegatePInvokeFlags(this EcmaType type)
        {
            PInvokeFlags flags = new PInvokeFlags(PInvokeAttributes.PreserveSig);
            
            if (!type.IsDelegate)
            {
                return flags;
            }

            var customAttributeValue = type.GetDecodedCustomAttribute(
                               "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");

            if (customAttributeValue == null)
            {
                return flags;
            }

            if (!customAttributeValue.HasValue)
            {
                return flags;
            }

            if (customAttributeValue.Value.FixedArguments.Length == 1)
            {
                CallingConvention callingConvention = (CallingConvention)customAttributeValue.Value.FixedArguments[0].Value;

                switch (callingConvention)
                {
                    case CallingConvention.StdCall:
                        flags.UnmanagedCallingConvention = MethodSignatureFlags.UnmanagedCallingConventionStdCall;
                        break;
                    case CallingConvention.Cdecl:
                        flags.UnmanagedCallingConvention = MethodSignatureFlags.UnmanagedCallingConventionCdecl;
                        break;
                    case CallingConvention.ThisCall:
                        flags.UnmanagedCallingConvention = MethodSignatureFlags.UnmanagedCallingConventionThisCall;
                        break;
                    case CallingConvention.Winapi:
                        // Platform default
                        break;
                }
            }

            foreach (var namedArgument in customAttributeValue.Value.NamedArguments)
            {
                if (namedArgument.Name == "CharSet")
                {
                    flags.CharSet = (CharSet)namedArgument.Value;
                }
                else if (namedArgument.Name == "BestFitMapping")
                {
                    flags.BestFitMapping = (bool)namedArgument.Value;
                }
                else if (namedArgument.Name == "SetLastError")
                {
                    flags.SetLastError = (bool)namedArgument.Value;
                }
                else if (namedArgument.Name == "ThrowOnUnmappableChar")
                {
                    flags.ThrowOnUnmappableChar = (bool)namedArgument.Value;
                }
            }
            return flags;
        }

        // This mask is the fastest way to check if a type is nested from its flags,
        // but it should not be added to the BCL enum as its semantics can be misleading.
        // Consider, for example, that (NestedFamANDAssem & NestedMask) == NestedFamORAssem.
        // Only comparison of the masked value to 0 is meaningful, which is different from
        // the other masks in the enum.
        private const TypeAttributes NestedMask = (TypeAttributes)0x00000006;

        public static bool IsNested(this TypeAttributes flags)
        {
            return (flags & NestedMask) != 0;
        }

        public static bool IsRuntimeSpecialName(this MethodAttributes flags)
        {
            return (flags & (MethodAttributes.SpecialName | MethodAttributes.RTSpecialName))
                == (MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }

        public static bool IsPublic(this MethodAttributes flags)
        {
            return (flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
        }
    }
}
