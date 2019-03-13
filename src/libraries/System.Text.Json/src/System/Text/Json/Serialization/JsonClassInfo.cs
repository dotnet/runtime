// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization
{
    internal sealed partial class JsonClassInfo
    {
        // The length of the property name embedded in the key (in bytes).
        private const int PropertyNameKeyLength = 6;

        private readonly List<PropertyRef> _property_refs = new List<PropertyRef>();
        private readonly List<PropertyRef> _property_refs_sorted = new List<PropertyRef>();

        internal delegate object ConstructorDelegate();
        internal ConstructorDelegate CreateObject { get; private set; }

        internal ClassType ClassType { get; private set; }

        // If enumerable, the JsonClassInfo for the element type.
        internal JsonClassInfo ElementClassInfo { get; private set; }

        public Type Type { get; private set; }

        internal JsonClassInfo(Type type, JsonSerializerOptions options)
        {
            Type = type;
            ClassType = GetClassType(type);

            CreateObject = options.ClassMaterializerStrategy.CreateConstructor(type);

            // Ignore properties on enumerable.
            if (ClassType == ClassType.Object)
            {
                foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // For now we only support public getters\setters
                    if (propertyInfo.GetMethod?.IsPublic == true ||
                        propertyInfo.SetMethod?.IsPublic == true)
                    {
                        AddProperty(propertyInfo.PropertyType, propertyInfo, type, options);
                    }
                }
            }
            else if (ClassType == ClassType.Enumerable)
            {
                // Add a single property that maps to the class type so we can have policies applied.
                AddProperty(type, propertyInfo : null, type, options);

                // Create a ClassInfo that maps to the element type which is used for (de)serialization and policies.
                Type elementType = GetElementType(type);
                ElementClassInfo = options.GetOrAddClass(elementType);
            }
            else
            {
                Debug.Assert(ClassType == ClassType.Value);

                // Add a single property that maps to the class type so we can have policies applied.
                AddProperty(type, propertyInfo: null, type, options);
            }
        }

        internal JsonPropertyInfo GetProperty(ReadOnlySpan<byte> propertyName, int propertyIndex)
        {
            ulong key = GetKey(propertyName);
            JsonPropertyInfo info = null;

            // First try sorted lookup.
            int count = _property_refs_sorted.Count;
            if (count != 0)
            {
                int iForward = propertyIndex;
                int iBackward = propertyIndex - 1;
                while (iForward < count || (iBackward >= 0 && iBackward < count))
                {
                    if (iForward < count)
                    {
                        if (TryIsPropertyRefEqual(_property_refs_sorted, propertyName, key, iForward, out info))
                        {
                            return info;
                        }
                        ++iForward;
                    }

                    if (iBackward >= 0)
                    {
                        if (TryIsPropertyRefEqual(_property_refs_sorted, propertyName, key, iBackward, out info))
                        {
                            return info;
                        }
                        --iBackward;
                    }
                }
            }

            // Then try fallback
            for (int i = 0; i < _property_refs.Count; i++)
            {
                if (TryIsPropertyRefEqual(_property_refs, propertyName, key, i, out info))
                {
                    break;
                }
            }

            if (info != null)
            {
                _property_refs_sorted.Add(new PropertyRef(key, info));
            }

            return info;
        }

        internal JsonPropertyInfo GetPolicyProperty()
        {
            Debug.Assert(_property_refs.Count == 1);
            return _property_refs[0].Info;
        }

        internal JsonPropertyInfo GetProperty(int index)
        {
            Debug.Assert(index < _property_refs.Count);
            return _property_refs[index].Info;
        }

        internal int PropertyCount
        {
            get
            {
                return _property_refs.Count;
            }
        }

        private static bool TryIsPropertyRefEqual(List<PropertyRef> list, ReadOnlySpan<byte> propertyName, ulong key, int index, out JsonPropertyInfo info)
        {
            if (key == list[index].Key)
            {
                if (propertyName.Length <= PropertyNameKeyLength ||
                    // We compare the whole name, although we could skip the first 6 bytes (but it's likely not any faster)
                    propertyName.SequenceEqual((ReadOnlySpan<byte>)list[index].Info._name))
                {
                    info = list[index].Info;
                    return true;
                }
            }

            info = null;
            return false;
        }

        private static ulong GetKey(ReadOnlySpan<byte> propertyName)
        {
            Debug.Assert(propertyName.Length > 0);

            ulong key;
            int length = propertyName.Length;

            // Embed the propertyName in the first 6 bytes of the key.
            if (length > 3)
            {
                key = MemoryMarshal.Read<uint>(propertyName);
                if (length > 4)
                {
                    key |= (ulong)propertyName[4] << 32;
                }
                if (length > 5)
                {
                    key |= (ulong)propertyName[5] << 40;
                }
            }
            else if (length > 1)
            {
                key = MemoryMarshal.Read<ushort>(propertyName);
                if (length > 2)
                {
                    key |= (ulong)propertyName[2] << 16;
                }
            }
            else
            {
                key = propertyName[0];
            }

            // Embed the propertyName length in the last two bytes.
            key |= (ulong)propertyName.Length << 48;
            return key;

        }

        private static Type GetElementType(Type propertyType)
        {
            Type elementType = null;
            if (typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                elementType = propertyType.GetElementType();
                if (elementType == null)
                {
                    if (propertyType.IsGenericType)
                    {
                        elementType = propertyType.GetGenericArguments()[0];
                    }
                    else
                    {
                        // Unable to determine collection type; attempt to use object which will be used to create loosely-typed collection.
                        return typeof(object);
                    }
                }
            }

            return elementType;
        }

        internal static ClassType GetClassType(Type type)
        {
            Debug.Assert(type != null);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            // A Type is considered a value if it implements IConvertible.
            if (typeof(IConvertible).IsAssignableFrom(type))
                return ClassType.Value;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ClassType.Enumerable;

            return ClassType.Object;
        }
    }
}
