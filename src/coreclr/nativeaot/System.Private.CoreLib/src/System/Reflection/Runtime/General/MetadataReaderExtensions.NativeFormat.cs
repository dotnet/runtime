// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Runtime.Assemblies;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.LowLevelLinq;
using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Runtime.Augments;

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
                return handle.IsNull(reader);
            if (handle.IsNull(reader))
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

            LowLevelList<Type> customModifiers = new LowLevelList<Type>();
            do
            {
                NativeFormatModifiedType modifiedType = handle.ToModifiedTypeHandle(reader).GetModifiedType(reader);
                if (optional == modifiedType.IsOptional)
                {
                    Type customModifier = modifiedType.ModifierType.Resolve(reader, typeContext).ToType();
                    customModifiers.Insert(0, customModifier);
                }

                handle = modifiedType.Type;
                handleType = handle.HandleType;
            }
            while (handleType == HandleType.ModifiedType);
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

        private static Exception ParseBoxedEnumConstantValue(this ConstantBoxedEnumValueHandle handle, MetadataReader reader, out object value)
        {
            ConstantBoxedEnumValue record = handle.GetConstantBoxedEnumValue(reader);

            Exception? exception = null;
            Type? enumType = record.Type.TryResolve(reader, new TypeContext(null, null), ref exception)?.ToType();
            if (enumType == null)
            {
                value = null;
                return exception;
            }

            if (!enumType.IsEnum)
                throw new BadImageFormatException();

            Type underlyingType = Enum.GetUnderlyingType(enumType);

            // Now box the value as the specified enum type.
            unsafe
            {
                switch (record.Value.HandleType)
                {
                    case HandleType.ConstantByteValue:
                        {
                            if (underlyingType != typeof(byte))
                                throw new BadImageFormatException();

                            byte v = record.Value.ToConstantByteValueHandle(reader).GetConstantByteValue(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantSByteValue:
                        {
                            if (underlyingType != typeof(sbyte))
                                throw new BadImageFormatException();

                            sbyte v = record.Value.ToConstantSByteValueHandle(reader).GetConstantSByteValue(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantInt16Value:
                        {
                            if (underlyingType != typeof(short))
                                throw new BadImageFormatException();

                            short v = record.Value.ToConstantInt16ValueHandle(reader).GetConstantInt16Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantUInt16Value:
                        {
                            if (underlyingType != typeof(ushort))
                                throw new BadImageFormatException();

                            ushort v = record.Value.ToConstantUInt16ValueHandle(reader).GetConstantUInt16Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantInt32Value:
                        {
                            if (underlyingType != typeof(int))
                                throw new BadImageFormatException();

                            int v = record.Value.ToConstantInt32ValueHandle(reader).GetConstantInt32Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantUInt32Value:
                        {
                            if (underlyingType != typeof(uint))
                                throw new BadImageFormatException();

                            uint v = record.Value.ToConstantUInt32ValueHandle(reader).GetConstantUInt32Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantInt64Value:
                        {
                            if (underlyingType != typeof(long))
                                throw new BadImageFormatException();

                            long v = record.Value.ToConstantInt64ValueHandle(reader).GetConstantInt64Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    case HandleType.ConstantUInt64Value:
                        {
                            if (underlyingType != typeof(ulong))
                                throw new BadImageFormatException();

                            ulong v = record.Value.ToConstantUInt64ValueHandle(reader).GetConstantUInt64Value(reader).Value;
                            value = RuntimeAugments.Box(enumType.TypeHandle, (IntPtr)(&v));
                            return null;
                        }
                    default:
                        throw new BadImageFormatException();
                }
            }
        }

        public static object? ParseConstantValue(this Handle handle, MetadataReader reader)
        {
            object? value;
            Exception exception = handle.TryParseConstantValue(reader, out value);
            if (exception != null)
                throw exception;
            return value;
        }

        public static object ParseConstantNumericValue(this Handle handle, MetadataReader reader)
        {
            switch (handle.HandleType)
            {
                case HandleType.ConstantBooleanValue:
                    return handle.ToConstantBooleanValueHandle(reader).GetConstantBooleanValue(reader).Value;
                case HandleType.ConstantCharValue:
                    return handle.ToConstantCharValueHandle(reader).GetConstantCharValue(reader).Value;
                case HandleType.ConstantByteValue:
                    return handle.ToConstantByteValueHandle(reader).GetConstantByteValue(reader).Value;
                case HandleType.ConstantSByteValue:
                    return handle.ToConstantSByteValueHandle(reader).GetConstantSByteValue(reader).Value;
                case HandleType.ConstantInt16Value:
                    return handle.ToConstantInt16ValueHandle(reader).GetConstantInt16Value(reader).Value;
                case HandleType.ConstantUInt16Value:
                    return handle.ToConstantUInt16ValueHandle(reader).GetConstantUInt16Value(reader).Value;
                case HandleType.ConstantInt32Value:
                    return handle.ToConstantInt32ValueHandle(reader).GetConstantInt32Value(reader).Value;
                case HandleType.ConstantUInt32Value:
                    return handle.ToConstantUInt32ValueHandle(reader).GetConstantUInt32Value(reader).Value;
                case HandleType.ConstantInt64Value:
                    return handle.ToConstantInt64ValueHandle(reader).GetConstantInt64Value(reader).Value;
                case HandleType.ConstantUInt64Value:
                    return handle.ToConstantUInt64ValueHandle(reader).GetConstantUInt64Value(reader).Value;
                case HandleType.ConstantSingleValue:
                    return handle.ToConstantSingleValueHandle(reader).GetConstantSingleValue(reader).Value;
                case HandleType.ConstantDoubleValue:
                    return handle.ToConstantDoubleValueHandle(reader).GetConstantDoubleValue(reader).Value;
                default:
                    throw new BadImageFormatException();
            }
        }

        public static Exception TryParseConstantValue(this Handle handle, MetadataReader reader, out object? value)
        {
            HandleType handleType = handle.HandleType;
            switch (handleType)
            {
                case HandleType.ConstantBooleanValue:
                case HandleType.ConstantCharValue:
                case HandleType.ConstantByteValue:
                case HandleType.ConstantSByteValue:
                case HandleType.ConstantInt16Value:
                case HandleType.ConstantUInt16Value:
                case HandleType.ConstantInt32Value:
                case HandleType.ConstantUInt32Value:
                case HandleType.ConstantInt64Value:
                case HandleType.ConstantUInt64Value:
                case HandleType.ConstantSingleValue:
                case HandleType.ConstantDoubleValue:
                    value = handle.ParseConstantNumericValue(reader);
                    return null;
                case HandleType.ConstantStringValue:
                    value = handle.ToConstantStringValueHandle(reader).GetConstantStringValue(reader).Value;
                    return null;
                case HandleType.TypeDefinition:
                case HandleType.TypeReference:
                case HandleType.TypeSpecification:
                    {
                        Exception? exception = null;
                        Type? type = handle.TryResolve(reader, new TypeContext(null, null), ref exception)?.ToType();
                        value = type;
                        return (value == null) ? exception : null;
                    }
                case HandleType.ConstantReferenceValue:
                    value = null;
                    return null;
                case HandleType.ConstantBoxedEnumValue:
                    {
                        return handle.ToConstantBoxedEnumValueHandle(reader).ParseBoxedEnumConstantValue(reader, out value);
                    }
                default:
                    {
                        Exception? exception;
                        value = handle.TryParseConstantArray(reader, out exception);
                        if (value == null)
                            return exception;
                        return null;
                    }
            }
        }

        private static Array TryParseConstantArray(this Handle handle, MetadataReader reader, out Exception? exception)
        {
            exception = null;

            HandleType handleType = handle.HandleType;
            switch (handleType)
            {
                case HandleType.ConstantBooleanArray:
                    return handle.ToConstantBooleanArrayHandle(reader).GetConstantBooleanArray(reader).Value.ToArray();

                case HandleType.ConstantCharArray:
                    return handle.ToConstantCharArrayHandle(reader).GetConstantCharArray(reader).Value.ToArray();

                case HandleType.ConstantByteArray:
                    return handle.ToConstantByteArrayHandle(reader).GetConstantByteArray(reader).Value.ToArray();

                case HandleType.ConstantSByteArray:
                    return handle.ToConstantSByteArrayHandle(reader).GetConstantSByteArray(reader).Value.ToArray();

                case HandleType.ConstantInt16Array:
                    return handle.ToConstantInt16ArrayHandle(reader).GetConstantInt16Array(reader).Value.ToArray();

                case HandleType.ConstantUInt16Array:
                    return handle.ToConstantUInt16ArrayHandle(reader).GetConstantUInt16Array(reader).Value.ToArray();

                case HandleType.ConstantInt32Array:
                    return handle.ToConstantInt32ArrayHandle(reader).GetConstantInt32Array(reader).Value.ToArray();

                case HandleType.ConstantUInt32Array:
                    return handle.ToConstantUInt32ArrayHandle(reader).GetConstantUInt32Array(reader).Value.ToArray();

                case HandleType.ConstantInt64Array:
                    return handle.ToConstantInt64ArrayHandle(reader).GetConstantInt64Array(reader).Value.ToArray();

                case HandleType.ConstantUInt64Array:
                    return handle.ToConstantUInt64ArrayHandle(reader).GetConstantUInt64Array(reader).Value.ToArray();

                case HandleType.ConstantSingleArray:
                    return handle.ToConstantSingleArrayHandle(reader).GetConstantSingleArray(reader).Value.ToArray();

                case HandleType.ConstantDoubleArray:
                    return handle.ToConstantDoubleArrayHandle(reader).GetConstantDoubleArray(reader).Value.ToArray();

                case HandleType.ConstantEnumArray:
                    return TryParseConstantEnumArray(handle.ToConstantEnumArrayHandle(reader), reader, out exception);

                case HandleType.ConstantStringArray:
                    {
                        HandleCollection constantHandles = handle.ToConstantStringArrayHandle(reader).GetConstantStringArray(reader).Value;
                        string[] elements = new string[constantHandles.Count];
                        int i = 0;
                        foreach (Handle constantHandle in constantHandles)
                        {
                            object? elementValue;
                            exception = constantHandle.TryParseConstantValue(reader, out elementValue);
                            if (exception != null)
                                return null;
                            elements[i] = (string)elementValue!;
                            i++;
                        }
                        return elements;
                    }

                case HandleType.ConstantHandleArray:
                    {
                        HandleCollection constantHandles = handle.ToConstantHandleArrayHandle(reader).GetConstantHandleArray(reader).Value;
                        object?[] elements = new object[constantHandles.Count];
                        int i = 0;
                        foreach (Handle constantHandle in constantHandles)
                        {
                            exception = constantHandle.TryParseConstantValue(reader, out elements[i]);
                            if (exception != null)
                                return null;
                            i++;
                        }
                        return elements;
                    }
                default:
                    throw new BadImageFormatException();
            }
        }

        private static Array TryParseConstantEnumArray(this ConstantEnumArrayHandle handle, MetadataReader reader, out Exception? exception)
        {
            exception = null;

            ConstantEnumArray enumArray = handle.GetConstantEnumArray(reader);
            Type? elementType = enumArray.ElementType.TryResolve(reader, new TypeContext(null, null), ref exception)?.ToType();
            if (exception != null)
                return null;

            switch (enumArray.Value.HandleType)
            {
                case HandleType.ConstantByteArray:
                    return enumArray.Value.ToConstantByteArrayHandle(reader).GetConstantByteArray(reader).Value.ToArray(elementType);

                case HandleType.ConstantSByteArray:
                    return enumArray.Value.ToConstantSByteArrayHandle(reader).GetConstantSByteArray(reader).Value.ToArray(elementType);

                case HandleType.ConstantInt16Array:
                    return enumArray.Value.ToConstantInt16ArrayHandle(reader).GetConstantInt16Array(reader).Value.ToArray(elementType);

                case HandleType.ConstantUInt16Array:
                    return enumArray.Value.ToConstantUInt16ArrayHandle(reader).GetConstantUInt16Array(reader).Value.ToArray(elementType);

                case HandleType.ConstantInt32Array:
                    return enumArray.Value.ToConstantInt32ArrayHandle(reader).GetConstantInt32Array(reader).Value.ToArray(elementType);

                case HandleType.ConstantUInt32Array:
                    return enumArray.Value.ToConstantUInt32ArrayHandle(reader).GetConstantUInt32Array(reader).Value.ToArray(elementType);

                case HandleType.ConstantInt64Array:
                    return enumArray.Value.ToConstantInt64ArrayHandle(reader).GetConstantInt64Array(reader).Value.ToArray(elementType);

                case HandleType.ConstantUInt64Array:
                    return enumArray.Value.ToConstantUInt64ArrayHandle(reader).GetConstantUInt64Array(reader).Value.ToArray(elementType);

                default:
                    throw new BadImageFormatException();
            }
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

        public static Handle[] ToArray(this HandleCollection collection)
        {
            int count = collection.Count;
            Handle[] result = new Handle[count];
            int i = 0;
            foreach (Handle element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        public static bool[] ToArray(this BooleanCollection collection)
        {
            int count = collection.Count;
            bool[] result = new bool[count];
            int i = 0;
            foreach (bool element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        public static char[] ToArray(this CharCollection collection)
        {
            int count = collection.Count;
            char[] result = new char[count];
            int i = 0;
            foreach (char element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        public static float[] ToArray(this SingleCollection collection)
        {
            int count = collection.Count;
            float[] result = new float[count];
            int i = 0;
            foreach (float element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        public static double[] ToArray(this DoubleCollection collection)
        {
            int count = collection.Count;
            double[] result = new double[count];
            int i = 0;
            foreach (double element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static byte[] ToArray(this ByteCollection collection, Type enumType = null)
        {
            int count = collection.Count;
            byte[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (byte[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new byte[count];
            }
            int i = 0;
            foreach (byte element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static sbyte[] ToArray(this SByteCollection collection, Type enumType = null)
        {
            int count = collection.Count;
            sbyte[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (sbyte[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new sbyte[count];
            }
            int i = 0;
            foreach (sbyte element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static ushort[] ToArray(this UInt16Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            ushort[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (ushort[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new ushort[count];
            }
            int i = 0;
            foreach (ushort element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static short[] ToArray(this Int16Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            short[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (short[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new short[count];
            }
            int i = 0;
            foreach (short element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static uint[] ToArray(this UInt32Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            uint[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (uint[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new uint[count];
            }
            int i = 0;
            foreach (uint element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static int[] ToArray(this Int32Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            int[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (int[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new int[count];
            }
            int i = 0;
            foreach (int element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static ulong[] ToArray(this UInt64Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            ulong[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (ulong[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new ulong[count];
            }
            int i = 0;
            foreach (ulong element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures we have array types referenced from custom attribute blobs")]
        public static long[] ToArray(this Int64Collection collection, Type enumType = null)
        {
            int count = collection.Count;
            long[] result;
            if (enumType != null)
            {
                Debug.Assert(enumType.IsEnum);
                result = (long[])Array.CreateInstance(enumType, count);
            }
            else
            {
                result = new long[count];
            }
            int i = 0;
            foreach (long element in collection)
            {
                result[i++] = element;
            }
            Debug.Assert(i == count);
            return result;
        }
    }
}
