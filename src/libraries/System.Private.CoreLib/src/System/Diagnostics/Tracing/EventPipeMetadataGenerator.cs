// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using EventMetadata = System.Diagnostics.Tracing.EventSource.EventMetadata;

namespace System.Diagnostics.Tracing
{
#if FEATURE_PERFTRACING
    internal sealed class EventPipeMetadataGenerator
    {
        private enum MetadataTag
        {
            Opcode = 1,
            ParameterPayload = 2
        }

        public static EventPipeMetadataGenerator Instance = new EventPipeMetadataGenerator();

        private EventPipeMetadataGenerator() { }

        public byte[]? GenerateEventMetadata(EventMetadata eventMetadata)
        {
            ParameterInfo[] parameters = eventMetadata.Parameters;
            EventParameterInfo[] eventParams = new EventParameterInfo[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                EventParameterInfo.GetTypeInfoFromType(parameters[i].ParameterType, out TraceLoggingTypeInfo? paramTypeInfo);
                eventParams[i].SetInfo(parameters[i].Name!, parameters[i].ParameterType, paramTypeInfo);
            }

            return GenerateMetadata(
                eventMetadata.Descriptor.EventId,
                eventMetadata.Name,
                eventMetadata.Descriptor.Keywords,
                eventMetadata.Descriptor.Level,
                eventMetadata.Descriptor.Version,
                (EventOpcode)eventMetadata.Descriptor.Opcode,
                eventParams);
        }

        public byte[]? GenerateEventMetadata(
            int eventId,
            string eventName,
            EventKeywords keywords,
            EventLevel level,
            uint version,
            EventOpcode opcode,
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

            return GenerateMetadata(eventId, eventName, (long)keywords, (uint)level, version, opcode, eventParams);
        }

        internal unsafe byte[]? GenerateMetadata(
            int eventId,
            string eventName,
            long keywords,
            uint level,
            uint version,
            EventOpcode opcode,
            EventParameterInfo[] parameters)
        {
            byte[]? metadata = null;
            bool hasV2ParameterTypes = false;
            try
            {
                // eventID          : 4 bytes
                // eventName        : (eventName.Length + 1) * 2 bytes
                // keywords         : 8 bytes
                // eventVersion     : 4 bytes
                // level            : 4 bytes
                // parameterCount   : 4 bytes
                uint v1MetadataLength = 24 + ((uint)eventName.Length + 1) * 2;
                uint v2MetadataLength = 0;
                uint defaultV1MetadataLength = v1MetadataLength;

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
                    uint pMetadataLength;
                    if (!parameter.GetMetadataLength(out pMetadataLength))
                    {
                        // The call above may return false which means it is an unsupported type for V1.
                        // If that is the case we use the v2 blob for metadata instead
                        hasV2ParameterTypes = true;
                        break;
                    }

                    v1MetadataLength += (uint)pMetadataLength;
                }


                if (hasV2ParameterTypes)
                {
                    v1MetadataLength = defaultV1MetadataLength;

                    // V2 length is the parameter count (4 bytes) plus the size of the params
                    v2MetadataLength = 4;
                    foreach (EventParameterInfo parameter in parameters)
                    {
                        uint pMetadataLength;
                        if (!parameter.GetMetadataLengthV2(out pMetadataLength))
                        {
                            // We ran in to an unsupported type, return empty event metadata
                            parameters = Array.Empty<EventParameterInfo>();
                            v1MetadataLength = defaultV1MetadataLength;
                            v2MetadataLength = 0;
                            hasV2ParameterTypes = false;
                            break;
                        }

                        v2MetadataLength += (uint)pMetadataLength;
                    }
                }

                // Optional opcode length needs 1 byte for the opcode + 5 bytes for the tag (4 bytes size, 1 byte kind)
                uint opcodeMetadataLength = opcode == EventOpcode.Info ? 0u : 6u;
                // Optional V2 metadata needs the size of the params + 5 bytes for the tag (4 bytes size, 1 byte kind)
                uint v2MetadataPayloadLength = v2MetadataLength == 0 ? 0 : v2MetadataLength + 5;
                uint totalV2MetadataLength = v2MetadataPayloadLength + opcodeMetadataLength;
                uint totalMetadataLength = v1MetadataLength + totalV2MetadataLength;
                metadata = new byte[totalMetadataLength];

                // Write metadata: metadataHeaderLength, eventID, eventName, keywords, eventVersion, level,
                //                 parameterCount, param1..., optional extended metadata
                fixed (byte* pMetadata = metadata)
                {
                    uint offset = 0;

                    WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (uint)eventId);
                    fixed (char* pEventName = eventName)
                    {
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (byte*)pEventName, ((uint)eventName.Length + 1) * 2);
                    }
                    WriteToBuffer(pMetadata, totalMetadataLength, ref offset, keywords);
                    WriteToBuffer(pMetadata, totalMetadataLength, ref offset, version);
                    WriteToBuffer(pMetadata, totalMetadataLength, ref offset, level);

                    if (hasV2ParameterTypes)
                    {
                        // If we have unsupported types, the V1 metadata must be empty. Write 0 count of params.
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, 0);
                    }
                    else
                    {
                        // Without unsupported V1 types we can write all the params now.
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (uint)parameters.Length);
                        foreach (var parameter in parameters)
                        {
                            if (!parameter.GenerateMetadata(pMetadata, ref offset, totalMetadataLength))
                            {
                                // If we fail to generate metadata for any parameter, we should return the "default" metadata without any parameters
                                return GenerateMetadata(eventId, eventName, keywords, level, version, opcode, Array.Empty<EventParameterInfo>());
                            }
                        }
                    }

                    Debug.Assert(offset == v1MetadataLength);

                    if (opcode != EventOpcode.Info)
                    {
                        // Size of opcode
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, 1);
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (byte)MetadataTag.Opcode);
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (byte)opcode);
                    }

                    if (hasV2ParameterTypes)
                    {
                        // Write the V2 supported metadata now
                        // Starting with the size of the V2 payload
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, v2MetadataLength);
                        // Now the tag to identify it as a V2 parameter payload
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (byte)MetadataTag.ParameterPayload);
                        // Then the count of parameters
                        WriteToBuffer(pMetadata, totalMetadataLength, ref offset, (uint)parameters.Length);
                        // Finally the parameters themselves
                        foreach (var parameter in parameters)
                        {
                            if (!parameter.GenerateMetadataV2(pMetadata, ref offset, totalMetadataLength))
                            {
                                // If we fail to generate metadata for any parameter, we should return the "default" metadata without any parameters
                                return GenerateMetadata(eventId, eventName, keywords, level, version, opcode, Array.Empty<EventParameterInfo>());
                            }
                        }
                    }

                    Debug.Assert(totalMetadataLength == offset);
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

        internal static unsafe void WriteToBuffer<T>(byte* buffer, uint bufferLength, ref uint offset, T value) where T : unmanaged
        {
            Debug.Assert(bufferLength >= (offset + sizeof(T)));
            *(T*)(buffer + offset) = value;
            offset += (uint)sizeof(T);
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

            if (!GetMetadataLengthForNamedTypeV2(name, typeInfo, out uint length))
            {
                return false;
            }

            EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, length);

            // Write the property name.
            fixed (char *pPropertyName = name)
            {
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)pPropertyName, ((uint)name.Length + 1) * 2);
            }

            return GenerateMetadataForTypeV2(typeInfo, pMetadataBlob, ref offset, blobSize);
        }

        private static unsafe bool GenerateMetadataForTypeV2(TraceLoggingTypeInfo? typeInfo, byte* pMetadataBlob, ref uint offset, uint blobSize)
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

                TraceLoggingTypeInfo? elementTypeInfo;
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

        internal static bool GetTypeInfoFromType(Type? type, out TraceLoggingTypeInfo? typeInfo)
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
                typeInfo = null;
                return false;
            }
        }

        internal bool GetMetadataLength(out uint size)
        {
            size = 0;

            TypeCode typeCode = GetTypeCodeExtended(ParameterType);
            if (typeCode == TypeCode.Object)
            {
                if (!(TypeInfo is InvokeTypeInfo typeInfo))
                {
                    return false;
                }

                // Each nested struct is serialized as:
                //     TypeCode.Object      : 4 bytes
                //     Number of properties : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                size += sizeof(uint)  // TypeCode
                     + sizeof(uint); // Property count

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = typeInfo.properties;
                if (properties != null)
                {
                    foreach (PropertyAnalysis prop in properties)
                    {
                        size += GetMetadataLengthForProperty(prop);
                    }
                }

                // For simplicity when writing a reader, we write a NULL char
                // after the metadata for a top-level struct (for its name) so that
                // readers don't have do special case the outer-most struct.
                size += sizeof(char);
            }
            else
            {
                size += (uint)(sizeof(uint) + ((ParameterName.Length + 1) * 2));
            }

            return true;
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
            // see https://github.com/dotnet/runtime/issues/9629#issuecomment-361749750 for more
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

        internal bool GetMetadataLengthV2(out uint size)
        {
            return GetMetadataLengthForNamedTypeV2(ParameterName, TypeInfo, out size);
        }

        private static bool GetMetadataLengthForTypeV2(TraceLoggingTypeInfo? typeInfo, out uint size)
        {
            size = 0;
            if (typeInfo == null)
            {
                return false;
            }

            if (typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Struct is serialized as:
                //     TypeCode.Object      : 4 bytes
                //     Number of properties : 4 bytes
                //     Property description 0...N
                size += sizeof(uint)  // TypeCode
                     + sizeof(uint); // Property count

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if (properties != null)
                {
                    foreach (PropertyAnalysis prop in properties)
                    {
                        if (!GetMetadataLengthForNamedTypeV2(prop.name, prop.typeInfo, out uint typeSize))
                        {
                            return false;
                        }

                        size += typeSize;
                    }
                }
            }
            else if (typeInfo is EnumerableTypeInfo enumerableTypeInfo)
            {
                // IEnumerable<T> is serialized as:
                //     TypeCode            : 4 bytes
                //     ElementType         : N bytes
                size += sizeof(uint);
                if (!GetMetadataLengthForTypeV2(enumerableTypeInfo.ElementInfo, out uint typeSize))
                {
                    return false;
                }

                size += typeSize;
            }
            else if (typeInfo is ScalarArrayTypeInfo arrayTypeInfo)
            {
                TraceLoggingTypeInfo? elementTypeInfo;
                if (!arrayTypeInfo.DataType.HasElementType
                    || !GetTypeInfoFromType(arrayTypeInfo.DataType.GetElementType(), out elementTypeInfo))
                {
                    return false;
                }

                size += sizeof(uint);
                if (!GetMetadataLengthForTypeV2(elementTypeInfo, out uint typeSize))
                {
                    return false;
                }

                size += typeSize;
            }
            else
            {
                size += (uint)sizeof(uint);
            }

            return true;
        }

        private static bool GetMetadataLengthForNamedTypeV2(string name, TraceLoggingTypeInfo? typeInfo, out uint size)
        {
            // Named type is serialized
            //     SizeOfTypeDescription    : 4 bytes
            //     Name                     : NULL-terminated UTF16 string
            //     Type                     : N bytes
            size = (uint)(sizeof(uint) +
                   ((name.Length + 1) * 2));

            if (!GetMetadataLengthForTypeV2(typeInfo, out uint typeSize))
            {
                return false;
            }

            size += typeSize;
            return true;
        }
    }

#endif // FEATURE_PERFTRACING
}
