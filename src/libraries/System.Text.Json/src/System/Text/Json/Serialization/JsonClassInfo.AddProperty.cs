// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal partial class JsonClassInfo
    {
        private void AddProperty(Type propertyType, PropertyInfo propertyInfo, Type classType, JsonSerializerOptions options)
        {
            Type collectionElementType = null;
            ClassType propertyClassType = GetClassType(propertyType);
            if (propertyClassType == ClassType.Enumerable)
            {
                collectionElementType = GetElementType(propertyType);
                // todo: if collectionElementType is object, create loosely-typed collection (JsonArray).
            }

            // Create the JsonPropertyInfo<TType, TProperty>
            Type genericPropertyType = typeof(JsonPropertyInfo<,>).MakeGenericType(classType, propertyType);
            JsonPropertyInfo jsonInfo = (JsonPropertyInfo)Activator.CreateInstance(
                genericPropertyType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new object[] { classType, propertyType, propertyInfo, collectionElementType, options },
                culture: null);

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
                    ArrayPool<byte>.Shared.Return(tempArray, clearArray: true);
                }

                _property_refs.Add(new PropertyRef(GetKey(propertyNameBytes), jsonInfo));
            }
            else
            {
                // A single property or an IEnumerable
                _property_refs.Add(new PropertyRef(0, jsonInfo));
            }
        }
    }
}
