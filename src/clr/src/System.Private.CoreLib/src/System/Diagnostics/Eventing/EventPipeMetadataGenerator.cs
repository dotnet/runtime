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
            for(int i = 0; i < parameters.Length; i++)
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
            for(int i = 0; i < typeInfos.Length; i++)
            {
                string paramName = string.Empty;
                if(paramNames != null)
                {
                    paramName = paramNames[i];
                }
                eventParams[i].SetInfo(paramName, typeInfos[i].DataType, typeInfos[i]);
            }

            return GenerateMetadata(eventId, eventName, (long)keywords, (uint)level, version, eventParams);
        }

        private unsafe byte[]? GenerateMetadata(
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
                foreach (var parameter in parameters)
                {
                    int pMetadataLength = parameter.GetMetadataLength();
                    // The call above may return -1 which means we failed to get the metadata length. 
                    // We then return a default metadata blob (with parameterCount of 0) to prevent it from generating malformed metadata.
                    if (pMetadataLength < 0)
                    {
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
                    foreach (var parameter in parameters)
                    {
                        if(!parameter.GenerateMetadata(pMetadata, ref offset, metadataLength))
                        {
                            // If we fail to generate metadata for any parameter, we should return the "default" metadata without any parameters
                            return GenerateMetadata(eventId, eventName, keywords, level, version, Array.Empty<EventParameterInfo>());
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
        internal static unsafe void WriteToBuffer(byte *buffer, uint bufferLength, ref uint offset, byte *src, uint srcLength)
        {
            Debug.Assert(bufferLength >= (offset + srcLength));
            for (int i = 0; i < srcLength; i++)
            {
                *(byte *)(buffer + offset + i) = *(byte *)(src + i);
            }
            offset += srcLength;
        }

        // Copy uint value to buffer.
        internal static unsafe void WriteToBuffer(byte *buffer, uint bufferLength, ref uint offset, uint value)
        {
            Debug.Assert(bufferLength >= (offset + 4));
            *(uint *)(buffer + offset) = value;
            offset += 4;
        }

        // Copy long value to buffer.
        internal static unsafe void WriteToBuffer(byte *buffer, uint bufferLength, ref uint offset, long value)
        {
            Debug.Assert(bufferLength >= (offset + 8));
            *(long *)(buffer + offset) = value;
            offset += 8;
        }

        // Copy char value to buffer.
        internal static unsafe void WriteToBuffer(byte *buffer, uint bufferLength, ref uint offset, char value)
        {
            Debug.Assert(bufferLength >= (offset + 2));
            *(char *)(buffer + offset) = value;
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
            if(typeCode == TypeCode.Object)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object              : 4 bytes
                //     Number of properties         : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)TypeCode.Object);

                if(!(TypeInfo is InvokeTypeInfo invokeTypeInfo))
                {
                    return false;
                }

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if(properties != null)
                {
                    // Write the count of serializable properties.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);

                    foreach(PropertyAnalysis prop in properties)
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
                fixed (char *pParameterName = ParameterName)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte *)pParameterName, ((uint)ParameterName.Length + 1) * 2);
                }
            }
            return true;
        }

        private static unsafe bool GenerateMetadataForProperty(PropertyAnalysis property, byte* pMetadataBlob, ref uint offset, uint blobSize)
        {
            Debug.Assert(property != null);
            Debug.Assert(pMetadataBlob != null);

            // Check if this property is a nested struct.
            if(property.typeInfo is InvokeTypeInfo invokeTypeInfo)
            {
                // Each nested struct is serialized as:
                //     TypeCode.Object              : 4 bytes
                //     Number of properties         : 4 bytes
                //     Property description 0...N
                //     Nested struct property name  : NULL-terminated string.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)TypeCode.Object);

                // Get the set of properties to be serialized.
                PropertyAnalysis[]? properties = invokeTypeInfo.properties;
                if(properties != null)
                {
                    // Write the count of serializable properties.
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);

                    foreach(PropertyAnalysis prop in properties)
                    {
                        if(!GenerateMetadataForProperty(prop, pMetadataBlob, ref offset, blobSize))
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
                fixed(char *pPropertyName = property.name)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte *)pPropertyName, ((uint)property.name.Length + 1) * 2);
                }
            }
            else
            {
                // Each primitive type is serialized as:
                //     TypeCode : 4 bytes
                //     PropertyName : NULL-terminated string
                TypeCode typeCode = GetTypeCodeExtended(property.typeInfo.DataType);

                // EventPipe does not support this type.  Throw, which will cause no metadata to be registered for this event.
                if(typeCode == TypeCode.Object)
                {
                    return false;
                }

                // Write the type code.
                EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCode);

                // Write the property name.
                fixed(char *pPropertyName = property.name)
                {
                    EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte *)pPropertyName, ((uint)property.name.Length + 1) * 2);
                }
            }
            return true;
        }

        internal int GetMetadataLength()
        {
            int ret = 0;

            TypeCode typeCode = GetTypeCodeExtended(ParameterType);
            if(typeCode == TypeCode.Object)
            {
                if(!(TypeInfo is InvokeTypeInfo typeInfo))
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
                if(properties != null)
                {
                    foreach(PropertyAnalysis prop in properties)
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
            if(property.typeInfo is InvokeTypeInfo invokeTypeInfo)
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
                if(properties != null)
                {
                    foreach(PropertyAnalysis prop in properties)
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
    }

#endif // FEATURE_PERFTRACING
}
