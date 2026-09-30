// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.Assemblies;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.LowLevelLinq;
using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;

using NativeFormatAssemblyFlags = global::Internal.Metadata.NativeFormat.AssemblyFlags;
using NativeFormatModifiedType = global::Internal.Metadata.NativeFormat.ModifiedType;

namespace System.Reflection.Runtime.General
{
    //
    // Collect various metadata reading tasks for better chunking...
    //
    [CLSCompliant(false)]
    public static class NativeFormatMetadataReaderExtensions
    {
        public static bool StringOrNullEquals(this ConstantStringValueHandle handle, string valueOrNull, MetadataReader reader)
        {
            if (valueOrNull == null)
                return handle.IsNil;
            if (handle.IsNil)
                return false;
            return handle.StringEquals(valueOrNull, reader);
        }

        public static int AsInt(this MethodHandle methodHandle)
        {
            unsafe
            {
                return *(int*)&methodHandle;
            }
        }

        public static bool IsNamespaceDefinitionHandle(this Handle handle, MetadataReader reader)
        {
            HandleType handleType = handle.HandleType;
            return handleType == HandleType.NamespaceDefinition;
        }

        public static bool IsNamespaceReferenceHandle(this Handle handle, MetadataReader reader)
        {
            HandleType handleType = handle.HandleType;
            return handleType == HandleType.NamespaceReference;
        }

        // Conversion where a invalid handle type indicates bad metadata rather a mistake by the caller.
        public static NamespaceReferenceHandle ToExpectedNamespaceReferenceHandle(this Handle handle, MetadataReader reader)
        {
            try
            {
                return handle.ToNamespaceReferenceHandle(reader);
            }
            catch (ArgumentException)
            {
                throw new BadImageFormatException();
            }
        }

        // Return any custom modifiers modifying the passed-in type and whose required/optional bit matches the passed in boolean.
        // Because this is intended to service the GetCustomModifiers() apis, this helper will always return a freshly allocated array
        // safe for returning to api callers.
        internal static Type[] GetCustomModifiers(this Handle handle, MetadataReader reader, TypeContext typeContext, bool optional)
        {
            HandleType handleType = handle.HandleType;
            Debug.Assert(handleType == HandleType.TypeDefinition || handleType == HandleType.TypeReference || handleType == HandleType.TypeSpecification || handleType == HandleType.ModifiedType);
            if (handleType != HandleType.ModifiedType)
                return Array.Empty<Type>();

            ArrayBuilder<Type> customModifiers = default;
            do
            {
                NativeFormatModifiedType modifiedType = handle.ToModifiedTypeHandle(reader).GetModifiedType(reader);
                if (optional == modifiedType.IsOptional)
                {
                    Type customModifier = modifiedType.ModifierType.Resolve(reader, typeContext).ToType();
                    customModifiers.Add(customModifier);
                }

                handle = modifiedType.Type;
                handleType = handle.HandleType;
            }
            while (handleType == HandleType.ModifiedType);
            customModifiers.AsSpan().Reverse();
            return customModifiers.ToArray();
        }

        public static Handle SkipCustomModifiers(this Handle handle, MetadataReader reader)
        {
            HandleType handleType = handle.HandleType;
            Debug.Assert(handleType == HandleType.TypeDefinition || handleType == HandleType.TypeReference || handleType == HandleType.TypeSpecification || handleType == HandleType.ModifiedType);
            if (handleType != HandleType.ModifiedType)
                return handle;

            do
            {
                NativeFormatModifiedType modifiedType = handle.ToModifiedTypeHandle(reader).GetModifiedType(reader);
                handle = modifiedType.Type;
                handleType = handle.HandleType;
            }
            while (handleType == HandleType.ModifiedType);

            return handle;
        }

        public static MethodSignature ParseMethodSignature(this Handle handle, MetadataReader reader)
        {
            return handle.ToMethodSignatureHandle(reader).GetMethodSignature(reader);
        }

        //
        // Used to split methods between DeclaredMethods and DeclaredConstructors.
        //
        public static bool IsConstructor(this MethodHandle methodHandle, MetadataReader reader)
        {
            Method method = methodHandle.GetMethod(reader);
            return IsConstructor(ref method, reader);
        }

        // This is specially designed for a hot path so we make some compromises in the signature:
        //
        //     - "method" is passed by reference even though no side-effects are intended.
        //
        public static bool IsConstructor(ref Method method, MetadataReader reader)
        {
            if ((method.Flags & (MethodAttributes.RTSpecialName | MethodAttributes.SpecialName)) != (MethodAttributes.RTSpecialName | MethodAttributes.SpecialName))
                return false;

            ConstantStringValueHandle nameHandle = method.Name;
            return nameHandle.StringEquals(ConstructorInfo.ConstructorName, reader) || nameHandle.StringEquals(ConstructorInfo.TypeConstructorName, reader);
        }

        public static object? ParseConstantValue(this Handle handle, MetadataReader reader)
        {
            return handle.HandleType switch
            {
                HandleType.ConstantBooleanValue => handle.ToConstantBooleanValueHandle(reader).GetConstantBooleanValue(reader).Value,
                HandleType.ConstantCharValue => handle.ToConstantCharValueHandle(reader).GetConstantCharValue(reader).Value,
                HandleType.ConstantByteValue => handle.ToConstantByteValueHandle(reader).GetConstantByteValue(reader).Value,
                HandleType.ConstantSByteValue => handle.ToConstantSByteValueHandle(reader).GetConstantSByteValue(reader).Value,
                HandleType.ConstantInt16Value => handle.ToConstantInt16ValueHandle(reader).GetConstantInt16Value(reader).Value,
                HandleType.ConstantUInt16Value => handle.ToConstantUInt16ValueHandle(reader).GetConstantUInt16Value(reader).Value,
                HandleType.ConstantInt32Value => handle.ToConstantInt32ValueHandle(reader).GetConstantInt32Value(reader).Value,
                HandleType.ConstantUInt32Value => handle.ToConstantUInt32ValueHandle(reader).GetConstantUInt32Value(reader).Value,
                HandleType.ConstantInt64Value => handle.ToConstantInt64ValueHandle(reader).GetConstantInt64Value(reader).Value,
                HandleType.ConstantUInt64Value => handle.ToConstantUInt64ValueHandle(reader).GetConstantUInt64Value(reader).Value,
                HandleType.ConstantSingleValue => handle.ToConstantSingleValueHandle(reader).GetConstantSingleValue(reader).Value,
                HandleType.ConstantDoubleValue => handle.ToConstantDoubleValueHandle(reader).GetConstantDoubleValue(reader).Value,
                HandleType.ConstantStringValue => handle.ToConstantStringValueHandle(reader).GetConstantStringValue(reader).Value,
                HandleType.ConstantReferenceValue => null,
                _ => throw new BadImageFormatException()
            };
        }

        public static Handle GetAttributeTypeHandle(this CustomAttribute customAttribute,
                                                    MetadataReader reader)
        {
            HandleType constructorHandleType = customAttribute.Constructor.HandleType;

            if (constructorHandleType == HandleType.QualifiedMethod)
                return customAttribute.Constructor.ToQualifiedMethodHandle(reader).GetQualifiedMethod(reader).EnclosingType;
            else if (constructorHandleType == HandleType.MemberReference)
                return customAttribute.Constructor.ToMemberReferenceHandle(reader).GetMemberReference(reader).Parent;
            else
                throw new BadImageFormatException();
        }

        //
        // Lightweight check to see if a custom attribute's is of a well-known type.
        //
        // This check performs without instantiating the Type object and bloating memory usage. On the flip side,
        // it doesn't check on whether the type is defined in a paricular assembly. The desktop CLR typically doesn't
        // check this either so this is useful from a compat perspective as well.
        //
        public static bool IsCustomAttributeOfType(this CustomAttributeHandle customAttributeHandle,
                                                   MetadataReader reader,
                                                   ReadOnlySpan<string> namespaceParts,
                                                   string name)
        {
            Handle typeHandle = customAttributeHandle.GetCustomAttribute(reader).GetAttributeTypeHandle(reader);
            HandleType handleType = typeHandle.HandleType;
            if (handleType == HandleType.TypeDefinition)
            {
                TypeDefinition typeDefinition = typeHandle.ToTypeDefinitionHandle(reader).GetTypeDefinition(reader);
                if (!typeDefinition.Name.StringEquals(name, reader))
                    return false;
                NamespaceDefinitionHandle nsHandle = typeDefinition.NamespaceDefinition;
                int idx = namespaceParts.Length;
                while (idx-- != 0)
                {
                    string namespacePart = namespaceParts[idx];
                    NamespaceDefinition namespaceDefinition = nsHandle.GetNamespaceDefinition(reader);
                    if (!namespaceDefinition.Name.StringOrNullEquals(namespacePart, reader))
                        return false;
                    if (!namespaceDefinition.ParentScopeOrNamespace.IsNamespaceDefinitionHandle(reader))
                        return false;
                    nsHandle = namespaceDefinition.ParentScopeOrNamespace.ToNamespaceDefinitionHandle(reader);
                }
                if (!nsHandle.GetNamespaceDefinition(reader).Name.StringOrNullEquals(null, reader))
                    return false;
                return true;
            }
            else if (handleType == HandleType.TypeReference)
            {
                TypeReference typeReference = typeHandle.ToTypeReferenceHandle(reader).GetTypeReference(reader);
                if (!typeReference.TypeName.StringEquals(name, reader))
                    return false;
                if (!typeReference.ParentNamespaceOrType.IsNamespaceReferenceHandle(reader))
                    return false;
                NamespaceReferenceHandle nsHandle = typeReference.ParentNamespaceOrType.ToNamespaceReferenceHandle(reader);
                int idx = namespaceParts.Length;
                while (idx-- != 0)
                {
                    string namespacePart = namespaceParts[idx];
                    NamespaceReference namespaceReference = nsHandle.GetNamespaceReference(reader);
                    if (!namespaceReference.Name.StringOrNullEquals(namespacePart, reader))
                        return false;
                    if (!namespaceReference.ParentScopeOrNamespace.IsNamespaceReferenceHandle(reader))
                        return false;
                    nsHandle = namespaceReference.ParentScopeOrNamespace.ToNamespaceReferenceHandle(reader);
                }
                if (!nsHandle.GetNamespaceReference(reader).Name.StringOrNullEquals(null, reader))
                    return false;
                return true;
            }
            else if (handleType == HandleType.TypeSpecification)
                return false;
            else
                throw new NotSupportedException();
        }


        public static string ToNamespaceName(this NamespaceDefinitionHandle namespaceDefinitionHandle, MetadataReader reader)
        {
            string ns = "";
            for (; ; )
            {
                NamespaceDefinition currentNamespaceDefinition = namespaceDefinitionHandle.GetNamespaceDefinition(reader);
                string name = currentNamespaceDefinition.Name.GetStringOrNull(reader);
                if (name != null)
                {
                    if (ns.Length != 0)
                        ns = "." + ns;
                    ns = name + ns;
                }
                Handle nextHandle = currentNamespaceDefinition.ParentScopeOrNamespace;
                HandleType handleType = nextHandle.HandleType;
                if (handleType == HandleType.ScopeDefinition)
                    break;
                if (handleType == HandleType.NamespaceDefinition)
                {
                    namespaceDefinitionHandle = nextHandle.ToNamespaceDefinitionHandle(reader);
                    continue;
                }

                throw new BadImageFormatException();
            }
            return ns;
        }

        public static IEnumerable<NamespaceDefinitionHandle> GetTransitiveNamespaces(this MetadataReader reader, IEnumerable<NamespaceDefinitionHandle> namespaceHandles)
        {
            foreach (NamespaceDefinitionHandle namespaceHandle in namespaceHandles)
            {
                yield return namespaceHandle;

                NamespaceDefinition namespaceDefinition = namespaceHandle.GetNamespaceDefinition(reader);
                foreach (NamespaceDefinitionHandle childNamespaceHandle in GetTransitiveNamespaces(reader, namespaceDefinition.NamespaceDefinitions.AsEnumerable()))
                    yield return childNamespaceHandle;
            }
        }

        public static IEnumerable<TypeDefinitionHandle> GetTopLevelTypes(this MetadataReader reader, IEnumerable<NamespaceDefinitionHandle> namespaceHandles)
        {
            foreach (NamespaceDefinitionHandle namespaceHandle in namespaceHandles)
            {
                NamespaceDefinition namespaceDefinition = namespaceHandle.GetNamespaceDefinition(reader);
                foreach (TypeDefinitionHandle typeDefinitionHandle in namespaceDefinition.TypeDefinitions)
                {
                    yield return typeDefinitionHandle;
                }
            }
        }

        public static IEnumerable<TypeDefinitionHandle> GetTransitiveTypes(this MetadataReader reader, IEnumerable<TypeDefinitionHandle> typeDefinitionHandles, bool publicOnly)
        {
            foreach (TypeDefinitionHandle typeDefinitionHandle in typeDefinitionHandles)
            {
                TypeDefinition typeDefinition = typeDefinitionHandle.GetTypeDefinition(reader);

                if (publicOnly)
                {
                    TypeAttributes visibility = typeDefinition.Flags & TypeAttributes.VisibilityMask;
                    if (visibility != TypeAttributes.Public && visibility != TypeAttributes.NestedPublic)
                        continue;
                }

                yield return typeDefinitionHandle;

                foreach (TypeDefinitionHandle nestedTypeDefinitionHandle in GetTransitiveTypes(reader, typeDefinition.NestedTypes.AsEnumerable(), publicOnly))
                    yield return nestedTypeDefinitionHandle;
            }
        }

        /// <summary>
        /// Reverse len characters in a StringBuilder starting at offset index
        /// </summary>
        private static void ReverseStringInStringBuilder(StringBuilder builder, int index, int len)
        {
            int back = index + len - 1;
            int front = index;
            while (front < back)
            {
                char temp = builder[front];
                builder[front] = builder[back];
                builder[back] = temp;
                front++;
                back--;
            }
        }

        public static string ToFullyQualifiedTypeName(this NamespaceReferenceHandle namespaceReferenceHandle, string typeName, MetadataReader reader)
        {
            StringBuilder fullName = new StringBuilder(64);
            NamespaceReference namespaceReference;
            for (; ; )
            {
                namespaceReference = namespaceReferenceHandle.GetNamespaceReference(reader);
                string namespacePart = namespaceReference.Name.GetStringOrNull(reader);
                if (namespacePart == null)
                    break;
                fullName.Append('.');
                int index = fullName.Length;
                fullName.Append(namespacePart);
                ReverseStringInStringBuilder(fullName, index, namespacePart.Length);
                namespaceReferenceHandle = namespaceReference.ParentScopeOrNamespace.ToExpectedNamespaceReferenceHandle(reader);
            }
            ReverseStringInStringBuilder(fullName, 0, fullName.Length);
            fullName.Append(typeName);
            return fullName.ToString();
        }

        public static IEnumerable<NamespaceDefinitionHandle> AsEnumerable(this NamespaceDefinitionHandleCollection collection)
        {
            foreach (NamespaceDefinitionHandle handle in collection)
                yield return handle;
        }

        public static IEnumerable<TypeDefinitionHandle> AsEnumerable(this TypeDefinitionHandleCollection collection)
        {
            foreach (TypeDefinitionHandle handle in collection)
                yield return handle;
        }

        public static byte[] ToArray(this ByteCollection collection)
        {
            int count = collection.Count;
            byte[] result = new byte[count];
            int i = 0;
            foreach (byte element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }
    }
}
