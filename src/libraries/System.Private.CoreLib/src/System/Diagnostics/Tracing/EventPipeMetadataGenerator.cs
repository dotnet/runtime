// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Reflection;
using EventMetadata = System.Diagnostics.Tracing.EventSource.EventMetadata;

namespace System.Diagnostics.Tracing
{
#if FEATURE_PERFTRACING
    internal sealed class EventPipeMetadataGenerator
    {
        public static EventPipeMetadataGenerator Instance = new EventPipeMetadataGenerator();

        private EventPipeMetadataGenerator() { }

        public byte[]? GenerateEventMetadata(EventMetadata eventMetadata)
        {
            ParameterInfo[] parameters = eventMetadata.Parameters;
            EventParameterInfo[] eventParams = new EventParameterInfo[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                eventParams[i].SetInfo(parameters[i].Name!, parameters[i].ParameterType);
            }

            return GenerateMetadata(
                eventMetadata.Descriptor.EventId,
                eventMetadata.Name,
                eventMetadata.Descriptor.Keywords,
                eventMetadata.Descriptor.Level,
                eventMetadata.Descriptor.Version,
                eventParams);
        }

        public byte[]? GenerateEventMetadata(
            int eventId,
            string eventName,
            EventKeywords keywords,
            EventLevel level,
            uint version,
            TraceLoggingEventTypes eventTypes)
        {
            TraceLoggingTypeInfo[] typeInfos = eventTypes.typeInfos;
            string[]? paramNames = eventTypes.paramNames;
            EventParameterInfo[] eventParams = new EventParameterInfo[typeInfos.Length];
            for (int i = 0; i < typeInfos.Length; i++)
            {
                string paramName = string.Empty;
                if (paramNames != null)
                {
                    paramName = paramNames[i];
                }
                eventParams[i].SetInfo(paramName, typeInfos[i].DataType, typeInfos[i]);
            }

            return GenerateMetadata(eventId, eventName, (long)keywords, (uint)level, version, eventParams);
        }

        internal unsafe byte[]? GenerateMetadata(
            int eventId,
            string eventName,
            long keywords,
            uint level,
            uint version,
            EventParameterInfo[] parameters)

        {
            // TODO: I would like this to not allocate 2x what it needs to
            byte[]? metadataV1 = GenerateMetadata(eventId, eventName, keywords, level, version, parameters,
                out bool hasUnsupportedParamTypes);
            byte[]? metadataV2 = null;
            if (hasUnsupportedParamTypes)
            {
                if (metadataV1 == null)
                {
                    // We bailed on metadata generation so we can't trust anything
                    return null;
                }

                metadataV2 = GenerateMetadataV2(eventId, eventName, keywords, level, version, parameters);
                if (metadataV2 != null)
                {
                    // Append the new metadata to the end of the original metadata.
                    byte[] temp = new byte[metadataV1.Length + metadataV2.Length];
                    Array.Copy(metadataV1, 0, temp, 0, metadataV1.Length);
                    Array.Copy(metadataV2, 0, temp, metadataV1.Length, metadataV2.Length);

                    metadataV1 = temp;
                }
            }

            return metadataV1;
        }

        private unsafe byte[]? GenerateMetadata(
            int eventId,
            string eventName,
            long keywords,
            uint level,
            uint version,
            EventParameterInfo[] parameters,
            out bool hasUnsupportedParameterTypes)
        {
            byte[]? metadata = null;
            hasUnsupportedParameterTypes = false;
            try
            {
                // eventID          : 4 bytes
                // eventName        : (eventName.Length + 1) * 2 bytes
                // keywords         : 8 bytes
                // eventVersion     : 4 bytes
                // level            : 4 bytes
                // parameterCount   : 4 bytes
                uint metadataLength = 24 + ((uint)eventName.Length + 1) * 2;
                uint defaultMetadataLength = metadataLength;

                // Check for an empty payload.
                // Write<T> calls with no arguments by convention have a parameter of
                // type NullTypeInfo which is serialized as nothing.
                if ((parameters.Length == 1) && (parameters[0].ParameterType == typeof(EmptyStruct)))
                {
                    parameters = Array.Empty<EventParameterInfo>();
                }

                // Increase the metadataLength for parameters.
                foreach (EventParameterInfo parameter in parameters)
                {
                    int pMetadataLength = parameter.GetMetadataLength();
                    // The call above may return -1 which means we failed to get the metadata length.
                    // We then return a default metadata blob (with parameterCount of 0) to prevent it from generating malformed metadata.
                    if (pMetadataLength < 0)
                    {
                        hasUnsupportedParameterTypes = true;
                        parameters = Array.Empty<EventParameterInfo>();
                        metadataLength = defaultMetadataLength;
                        break;
                    }
                    metadataLength += (uint)pMetadataLength;
                }

                metadata = new byte[metadataLength];

                // Write metadata: eventID, eventName, keywords, eventVersion, level, parameterCount, param1 type, param1 name...
                fixed (byte* pMetadata = metadata)
                {
                    uint offset = 0;
                    WriteToBuffer(pMetadata, metadataLength, ref offset, (uint)eventId);
                    fixed (char* pEventName = eventName)
                    {
                        WriteToBuffer(pMetadata, metadataLength, ref offset, (byte*)pEventName, ((uint)eventName.Length + 1) * 2);
                    }
                    WriteToBuffer(pMetadata, metadataLength, ref offset, keywords);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, version);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, level);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, (uint)parameters.Length);
                    foreach (EventParameterInfo parameter in parameters)
                    {
                        if (!parameter.GenerateMetadata(pMetadata, ref offset, metadataLength))
                        {                            hasUnsupportedParameterTypes = true;
                            return GenerateMetadata(eventId, eventName, keywords, level, version, Array.Empty<EventParameterInfo>(), out bool unused);
                        }
                    }
                    Debug.Assert(metadataLength == offset);
                }
            }
            catch
            {
                // If a failure occurs during metadata generation, make sure that we don't return
                // malformed metadata.  Instead, return a null metadata blob.
                // Consumers can either build in knowledge of the event or skip it entirely.
                metadata = null;
            }

            return metadata;
        }

        private unsafe byte[]? GenerateMetadataV2(
            int eventId,
            string eventName,
            long keywords,
            uint level,
            uint version,
            EventParameterInfo[] parameters)
        {
            byte[]? metadata = null;
            try
            {
                // header area
                // metadataHeaderLength  : 4 bytes
                // eventID               : 4 bytes
                // eventName             : (eventName.Length + 1) * 2 bytes
                // keywords              : 8 bytes
                // eventVersion          : 4 bytes
                // level                 : 4 bytes
                uint metadataLength = 24 + ((uint)eventName.Length + 1) * 2;
                uint metadataHeaderLength = metadataLength;

                // "V2" identifier       : 6 bytes
                metadataLength += 6;

                // parameter area
                // parameterCount        : 4 bytes
                // parameters            : N bytes
                metadataLength += 4;

                // Check for an empty payload.
                // Write<T> calls with no arguments by convention have a parameter of
                // type NullTypeInfo which is serialized as nothing.
                if ((parameters.Length == 1) && (parameters[0].ParameterType == typeof(EmptyStruct)))
                {
                    parameters = Array.Empty<EventParameterInfo>();
                }

                // Increase the metadataLength for parameters.
                foreach (var parameter in parameters)
                {
                    int pMetadataLength = parameter.GetMetadataLengthV2();
                    // TODO: handle errors
                    metadataLength += (uint)pMetadataLength;
                }

                metadata = new byte[metadataLength];

                // Write metadata: metadataHeaderLength, eventID, eventName, keywords, eventVersion, level,
                //                 parameterCount, param1...
                fixed (byte* pMetadata = metadata)
                {
                    uint offset = 0;
                    // Write "V2" in to the stream to indicate that we are adding V2 metadata
                    WriteToBuffer(pMetadata, metadataLength, ref offset, 'V');
                    WriteToBuffer(pMetadata, metadataLength, ref offset, '2');
                    WriteToBuffer(pMetadata, metadataLength, ref offset, '\0');
                    WriteToBuffer(pMetadata, metadataLength, ref offset, metadataHeaderLength);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, (uint)eventId);
                    fixed (char* pEventName = eventName)
                    {
                        WriteToBuffer(pMetadata, metadataLength, ref offset, (byte*)pEventName, ((uint)eventName.Length + 1) * 2);
                    }
                    WriteToBuffer(pMetadata, metadataLength, ref offset, keywords);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, version);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, level);
                    WriteToBuffer(pMetadata, metadataLength, ref offset, (uint)parameters.Length);
                    foreach (var parameter in parameters)
                    {
                        if (!parameter.GenerateMetadataV2(pMetadata, ref offset, metadataLength))
                        {
                            // If we fail to generate metadata for any parameter fallback to the V1 metadata
                            return null;
                        }
                    }

                    Debug.Assert(metadataLength == offset);
                }
            }
            catch
            {
                // If a failure occurs during metadata generation, make sure that we don't return
                // malformed metadata.  Instead, return a null metadata blob.
                // Consumers can either build in knowledge of the event or skip it entirely.
                metadata = null;
            }

            return metadata;
        }

        // Copy src to buffer and modify the offset.
        // Note: We know the buffer size ahead of time to make sure no buffer overflow.
        internal static unsafe void WriteToBuffer(byte* buffer, uint bufferLength, ref uint offset, byte* src, uint srcLength)
        {
            Debug.Assert(bufferLength >= (offset + srcLength));
            for (int i = 0; i < srcLength; i++)
            {
                *(byte*)(buffer + offset + i) = *(byte*)(src + i);
            }
            offset += srcLength;
        }

        // Copy uint value to buffer.
        internal static unsafe void WriteToBuffer(byte* buffer, uint bufferLength, ref uint offset, uint value)
        {
            Debug.Assert(bufferLength >= (offset + 4));
            *(uint*)(buffer + offset) = value;
            offset += 4;
        }

        // Copy long value to buffer.
        internal static unsafe void WriteToBuffer(byte* buffer, uint bufferLength, ref uint offset, long value)
        {
            Debug.Assert(bufferLength >= (offset + 8));
            *(long*)(buffer + offset) = value;
            offset += 8;
        }

        // Copy char value to buffer.
        internal static unsafe void WriteToBuffer(byte* buffer, uint bufferLength, ref uint offset, char value)
        {
            Debug.Assert(bufferLength >= (offset + 2));
            *(char*)(buffer + offset) = value;
            offset += 2;
        }
    }

    internal struct EventParameterInfo
    {
        internal string ParameterName;
        internal Type ParameterType;
        internal TraceLoggingTypeInfo? TypeInfo;

        internal void SetInfo(string name, Type type, TraceLoggingTypeInfo? typeInfo = null)
        {
            ParameterName = name;
            ParameterType = type;
            TypeInfo = typeInfo;
        }

        internal unsafe bool GenerateMetadata(byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            TypeCode typeCode = GetTypeCodeExtended(ParameterType);
            if (typeCode == TypeCode.Object)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object              : 4 bytes
                //     Number of properties         : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)TypeCode.Object);

                if (!(TypeInfo is InvokeTypeInfo invokeTypeInfo))
                {
                    return false;
                }

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    // Write the count of serializable properties.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);

                    foreach (PropertyAnalysis prop in properties)
                    {
                        if (!GenerateMetadataForProperty(prop, pMetadataBlob, ref offset, blobSize))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // This struct has zero serializable properties so we just write the property count.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)0);
                }

                // Top-level structs don't have a property name, but for simplicity we write a NULL-char to represent the name.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, '\0');
            }
            else
            {
                // Write parameter type.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCode);

                // Write parameter name.
                fixed (char* pParameterName = ParameterName)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)pParameterName, ((uint)ParameterName.Length + 1) * 2);
                }
            }
            return true;
        }

        private static unsafe bool GenerateMetadataForProperty(PropertyAnalysis property, byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            Debug.Assert(property != null);
            Debug.Assert(pMetadataBlob != null);

            // Check if this property is a nested struct.
            if (property.typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object              : 4 bytes
                //     Number of properties         : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)TypeCode.Object);

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    // Write the count of serializable properties.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);

                    foreach (PropertyAnalysis prop in properties)
                    {
                        if (!GenerateMetadataForProperty(prop, pMetadataBlob, ref offset, blobSize))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // This struct has zero serializable properties so we just write the property count.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)0);
                }

                // Write the property name.
                fixed (char* pPropertyName = property.name)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)pPropertyName, ((uint)property.name.Length + 1) * 2);
                }
            }
            else
            {
                // Each primitive type is serialized as:
                //     TypeCode : 4 bytes
                //     PropertyName : NULL-terminated string
                TypeCode typeCode = GetTypeCodeExtended(property.typeInfo.DataType);

                // EventPipe does not support this type.  Throw, which will cause no metadata to be registered for this event.
                if (typeCode == TypeCode.Object)
                {
                    return false;
                }

                // Write the type code.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCode);

                // Write the property name.
                fixed (char* pPropertyName = property.name)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)pPropertyName, ((uint)property.name.Length + 1) * 2);
                }
            }
            return true;
        }

        internal unsafe bool GenerateMetadataV2(byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            if (TypeInfo == null)
                return false;
            return GenerateMetadataForNamedTypeV2(ParameterName, TypeInfo, pMetadataBlob, ref offset, blobSize);
        }

        private static unsafe bool GenerateMetadataForNamedTypeV2(string name, TraceLoggingTypeInfo typeInfo, byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            Debug.Assert(pMetadataBlob != null);

            uint length = GetMetadataLengthForNamedTypeV2(name, typeInfo);
            EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, length);

            // Write the property name.
            fixed (char *pPropertyName = name)
            {
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte *)pPropertyName, ((uint)name.Length + 1) * 2);
            }

            return GenerateMetadataForTypeV2(typeInfo, pMetadataBlob, ref offset, blobSize);
        }

        private static unsafe bool GenerateMetadataForTypeV2(TraceLoggingTypeInfo typeInfo, byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            Debug.Assert(typeInfo != null);
            Debug.Assert(pMetadataBlob != null);

            // Check if this type is a nested struct.
            if (typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object              : 4 bytes
                //     Number of properties         : 4 bytes
                //     Property description 0...N
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)TypeCode.Object);

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    // Write the count of serializable properties.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);

                    foreach (PropertyAnalysis prop in properties)
                    {
                        if (!GenerateMetadataForNamedTypeV2(prop.name, prop.typeInfo, pMetadataBlob, ref offset, blobSize))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // This struct has zero serializable properties so we just write the property count.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)0);
                }
            }
            else if (typeInfo is EnumerableTypeInfo enumerableTypeInfo)
            {
                // Each enumerable is serialized as:
                //     TypeCode.Array               : 4 bytes
                //     ElementType                  : N bytes
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, EventPipeTypeCodeArray);
                GenerateMetadataForTypeV2(enumerableTypeInfo.ElementInfo, pMetadataBlob, ref offset, blobSize);
            }
            else if (typeInfo is ScalarArrayTypeInfo arrayTypeInfo)
            {
                // Each enumerable is serialized as:
                //     TypeCode.Array               : 4 bytes
                //     ElementType                  : N bytes
                if (!arrayTypeInfo.DataType.HasElementType)
                {
                    return false;
                }

                TraceLoggingTypeInfo elementTypeInfo;
                if (!GetTypeInfoFromType(arrayTypeInfo.DataType.GetElementType(), out elementTypeInfo))
                {
                    return false;
                }

                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, EventPipeTypeCodeArray);
                GenerateMetadataForTypeV2(elementTypeInfo, pMetadataBlob, ref offset, blobSize);
            }
            else
            {
                // Each primitive type is serialized as:
                //     TypeCode : 4 bytes
                TypeCode typeCode = GetTypeCodeExtended(typeInfo.DataType);

                // EventPipe does not support this type.  Throw, which will cause no metadata to be registered for this event.
                if (typeCode == TypeCode.Object)
                {
                    return false;
                }

                // Write the type code.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCode);
            }
            return true;
        }

        internal static bool GetTypeInfoFromType(Type? type, out TraceLoggingTypeInfo typeInfo)
        {
            if (type == typeof(bool))
            {
                typeInfo = ScalarTypeInfo.Boolean();
                return true;
            }
            else if (type == typeof(byte))
            {
                typeInfo = ScalarTypeInfo.Byte();
                return true;
            }
            else if (type == typeof(sbyte))
            {
                typeInfo = ScalarTypeInfo.SByte();
                return true;
            }
            else if (type == typeof(char))
            {
                typeInfo = ScalarTypeInfo.Char();
                return true;
            }
            else if (type == typeof(short))
            {
                typeInfo = ScalarTypeInfo.Int16();
                return true;
            }
            else if (type == typeof(ushort))
            {
                typeInfo = ScalarTypeInfo.UInt16();
                return true;
            }
            else if (type == typeof(int))
            {
                typeInfo = ScalarTypeInfo.Int32();
                return true;
            }
            else if (type == typeof(uint))
            {
                typeInfo = ScalarTypeInfo.UInt32();
                return true;
            }
            else if (type == typeof(long))
            {
                typeInfo = ScalarTypeInfo.Int64();
                return true;
            }
            else if (type == typeof(ulong))
            {
                typeInfo = ScalarTypeInfo.UInt64();
                return true;
            }
            else if (type == typeof(IntPtr))
            {
                typeInfo = ScalarTypeInfo.IntPtr();
                return true;
            }
            else if (type == typeof(UIntPtr))
            {
                typeInfo = ScalarTypeInfo.UIntPtr();
                return true;
            }
            else if (type == typeof(float))
            {
                typeInfo = ScalarTypeInfo.Single();
                return true;
            }
            else if (type == typeof(double))
            {
                typeInfo = ScalarTypeInfo.Double();
                return true;
            }
            else if (type == typeof(Guid))
            {
                typeInfo = ScalarTypeInfo.Guid();
                return true;
            }
            else
            {
                typeInfo = new NullTypeInfo();
                return false;
            }
        }

        internal int GetMetadataLength()
        {
            int ret = 0;

            TypeCode typeCode = GetTypeCodeExtended(ParameterType);
            if (typeCode == TypeCode.Object)
            {
                if (!(TypeInfo is InvokeTypeInfo typeInfo))
                {
                    return -1;
                }

                // Each nested struct is serialized as:
                //     TypeCode.Object      : 4 bytes
                //     Number of properties : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                ret += sizeof(uint)  // TypeCode
                     + sizeof(uint); // Property count

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = typeInfo.properties;
                if (properties != null)
                {
                    foreach (PropertyAnalysis prop in properties)
                    {
                        ret += (int)GetMetadataLengthForProperty(prop);
                    }
                }

                // For simplicity when writing a reader, we write a NULL char
                // after the metadata for a top-level struct (for its name) so that
                // readers don't have do special case the outer-most struct.
                ret += sizeof(char);
            }
            else
            {
                ret += (int)(sizeof(uint) + ((ParameterName.Length + 1) * 2));
            }

            return ret;
        }

        private static uint GetMetadataLengthForProperty(PropertyAnalysis property)
        {
            Debug.Assert(property != null);

            uint ret = 0;

            // Check if this property is a nested struct.
            if (property.typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object      : 4 bytes
                //     Number of properties : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                ret += sizeof(uint)  // TypeCode
                     + sizeof(uint); // Property count

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    foreach (PropertyAnalysis prop in properties)
                    {
                        ret += GetMetadataLengthForProperty(prop);
                    }
                }

                // Add the size of the property name.
                ret += (uint)((property.name.Length + 1) * 2);
            }
            else
            {
                ret += (uint)(sizeof(uint) + ((property.name.Length + 1) * 2));
            }

            return ret;
        }

        // Array is not part of TypeCode, we decided to use 19 to represent it. (18 is the last type code value, string)
        private const int EventPipeTypeCodeArray = 19;

        private static TypeCode GetTypeCodeExtended(Type parameterType)
        {
            // Guid is not part of TypeCode, we decided to use 17 to represent it, as it's the "free slot"
            // see https://github.com/dotnet/coreclr/issues/16105#issuecomment-361749750 for more
            const TypeCode GuidTypeCode = (TypeCode)17;

            if (parameterType == typeof(Guid)) // Guid is not a part of TypeCode enum
                return GuidTypeCode;

            // IntPtr and UIntPtr are converted to their non-pointer types.
            if (parameterType == typeof(IntPtr))
                return IntPtr.Size == 4 ? TypeCode.Int32 : TypeCode.Int64;

            if (parameterType == typeof(UIntPtr))
                return UIntPtr.Size == 4 ? TypeCode.UInt32 : TypeCode.UInt64;

            return Type.GetTypeCode(parameterType);
        }

        internal int GetMetadataLengthV2()
        {
            return (int)GetMetadataLengthForNamedTypeV2(ParameterName, TypeInfo);
        }

        //TODO: error handling for bad types
        private static uint GetMetadataLengthForTypeV2(TraceLoggingTypeInfo? typeInfo)
        {
            uint ret = 0;
            if (typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Struct is serialized as:
                //     TypeCode.Object      : 4 bytes
                //     Number of properties : 4 bytes
                //     Property description 0...N
                ret += sizeof(uint)  // TypeCode
                     + sizeof(uint); // Property count

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    foreach (PropertyAnalysis prop in properties)
                    {
                        ret += GetMetadataLengthForNamedTypeV2(prop.name, prop.typeInfo);
                    }
                }
            }
            else if (typeInfo is EnumerableTypeInfo enumerableTypeInfo)
            {
                // IEnumerable<T> is serialized as:
                //     TypeCode            : 4 bytes
                //     ElementType         : N bytes
                ret += sizeof(uint)
                     + GetMetadataLengthForTypeV2(enumerableTypeInfo.ElementInfo);
            }
            else if (typeInfo is ScalarArrayTypeInfo arrayTypeInfo)
            {
                TraceLoggingTypeInfo elementTypeInfo;
                if (arrayTypeInfo.DataType.HasElementType
                    && GetTypeInfoFromType(arrayTypeInfo.DataType.GetElementType(), out elementTypeInfo))
                {
                    ret += sizeof(uint)
                         + GetMetadataLengthForTypeV2(elementTypeInfo);
                }
            }
            else
            {
                ret += (uint)sizeof(uint);
            }
            return ret;
        }

        private static uint GetMetadataLengthForNamedTypeV2(string name, TraceLoggingTypeInfo? typeInfo)
        {
            // Named type is serialized
            //     SizeOfTypeDescription    : 4 bytes
            //     Name                     : NULL-terminated UTF16 string
            //     Type                     : N bytes
            return (uint)(sizeof(uint) +
                   ((name.Length + 1) * 2)) +
                   GetMetadataLengthForTypeV2(typeInfo);
        }
    }

#endif // FEATURE_PERFTRACING
}
