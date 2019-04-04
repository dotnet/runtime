// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal partial class JsonClassInfo
    {
        private void AddProperty(Type propertyType, PropertyInfo propertyInfo, Type classType, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonInfo = CreateProperty(propertyType, propertyType, propertyInfo, classType, options);

            if (propertyInfo != null)
            {
                string propertyName = propertyInfo.Name;

                // At this point propertyName is valid UTF16, so just call the simple UTF16->UTF8 encoder.
                byte[] propertyNameBytes = Encoding.UTF8.GetBytes(propertyName);
                jsonInfo._name = propertyNameBytes;

                // Cache the escaped name.
                int valueIdx = JsonWriterHelper.NeedsEscaping(propertyNameBytes);
                if (valueIdx == -1)
                {
                    jsonInfo._escapedName = propertyNameBytes;
                }
                else
                {
                    int length = JsonWriterHelper.GetMaxEscapedLength(propertyNameBytes.Length, valueIdx);

                    byte[] tempArray = ArrayPool<byte>.Shared.Rent(length);

                    JsonWriterHelper.EscapeString(propertyNameBytes, tempArray, valueIdx, out int written);
                    jsonInfo._escapedName = new byte[written];
                    tempArray.CopyTo(jsonInfo._escapedName, 0);

                    // We clear the array because it is "user data" (although a property name).
                    new Span<byte>(tempArray, 0, written).Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }

                _propertyRefs.Add(new PropertyRef(GetKey(propertyNameBytes), jsonInfo));
            }
            else
            {
                // A single property or an IEnumerable
                _propertyRefs.Add(new PropertyRef(0, jsonInfo));
            }
        }

        internal JsonPropertyInfo CreateProperty(Type declaredPropertyType, Type runtimePropertyType, PropertyInfo propertyInfo, Type parentClassType, JsonSerializerOptions options)
        {
            Type collectionElementType = null;
            ClassType propertyClassType = GetClassType(runtimePropertyType);
            if (propertyClassType == ClassType.Enumerable)
            {
                collectionElementType = GetElementType(runtimePropertyType);
                // todo: if collectionElementType is object, create loosely-typed collection (JsonArray).
            }

            // Create the JsonPropertyInfo<TType, TProperty>
            Type propertyInfoClassType;
            if (runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingPropertyType = Nullable.GetUnderlyingType(runtimePropertyType);
                propertyInfoClassType = typeof(JsonPropertyInfoNullable<,>).MakeGenericType(parentClassType, underlyingPropertyType);
            }
            else
            {
                // For now we only support polymorphism with base type == typeof(object).
                Debug.Assert(declaredPropertyType == runtimePropertyType || declaredPropertyType == typeof(object));
                propertyInfoClassType = typeof(JsonPropertyInfoNotNullable<,,>).MakeGenericType(parentClassType, declaredPropertyType, runtimePropertyType);
            }

            JsonPropertyInfo jsonInfo = (JsonPropertyInfo)Activator.CreateInstance(
                propertyInfoClassType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new object[] { parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, collectionElementType, options },
                culture: null);

            return jsonInfo;
        }

        internal JsonPropertyInfo CreatePolymorphicProperty(JsonPropertyInfo property, Type runtimePropertyType, JsonSerializerOptions options)
        {
            // For now we only support typeof(object) for polymorphism.
            Debug.Assert(property?.DeclaredPropertyType == typeof(object));
            Debug.Assert(runtimePropertyType != typeof(object));

            JsonPropertyInfo runtimeProperty = CreateProperty(property.DeclaredPropertyType, runtimePropertyType, property?.PropertyInfo, Type, options);
            runtimeProperty._name = property._name;
            runtimeProperty._escapedName = property._escapedName;

            return runtimeProperty;
        }
    }
}
