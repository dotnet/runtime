// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
#if FEATURE_PERFTRACING
    internal static class EventPipePayloadDecoder
    {
        /// <summary>
        /// Given the metadata for an event and an event payload, decode and deserialize the event payload.
        /// </summary>
        internal static object[] DecodePayload(ref EventSource.EventMetadata metadata, ReadOnlySpan<Byte> payload)
        {
            ParameterInfo[] parameters = metadata.Parameters;
            object[] decodedFields = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                // It is possible that an older version of the event was emitted.
                // If this happens, the payload might be missing arguments at the end.
                // We can just leave these unset.
                if (payload.Length <= 0)
                {
                    break;
                }

                Type parameterType = parameters[i].ParameterType;
                if (parameterType == typeof(IntPtr))
                {
                    if (IntPtr.Size == 8)
                    {
                        // Payload is automatically updated to point to the next piece of data.
                        decodedFields[i] = (IntPtr)ReadUnalignedUInt64(ref payload);
                    }
                    else if (IntPtr.Size == 4)
                    {
                        decodedFields[i] = (IntPtr)MemoryMarshal.Read<Int32>(payload);
                        payload = payload.Slice(IntPtr.Size);
                    }
                    else
                    {
                        Debug.Assert(false, "Unsupported pointer size.");
                    }
                }
                else if (parameterType == typeof(int))
                {
                    decodedFields[i] = MemoryMarshal.Read<int>(payload);
                    payload = payload.Slice(sizeof(int));
                }
                else if (parameterType == typeof(uint))
                {
                    decodedFields[i] = MemoryMarshal.Read<uint>(payload);
                    payload = payload.Slice(sizeof(uint));
                }
                else if (parameterType == typeof(long))
                {
                    // Payload is automatically updated to point to the next piece of data.
                    decodedFields[i] = (long)ReadUnalignedUInt64(ref payload);
                }
                else if (parameterType == typeof(ulong))
                {
                    // Payload is automatically updated to point to the next piece of data.
                    decodedFields[i] = ReadUnalignedUInt64(ref payload);
                }
                else if (parameterType == typeof(byte))
                {
                    decodedFields[i] = MemoryMarshal.Read<byte>(payload);
                    payload = payload.Slice(sizeof(byte));
                }
                else if (parameterType == typeof(sbyte))
                {
                    decodedFields[i] = MemoryMarshal.Read<sbyte>(payload);
                    payload = payload.Slice(sizeof(sbyte));
                }
                else if (parameterType == typeof(short))
                {
                    decodedFields[i] = MemoryMarshal.Read<short>(payload);
                    payload = payload.Slice(sizeof(short));
                }
                else if (parameterType == typeof(ushort))
                {
                    decodedFields[i] = MemoryMarshal.Read<ushort>(payload);
                    payload = payload.Slice(sizeof(ushort));
                }
                else if (parameterType == typeof(float))
                {
                    decodedFields[i] = MemoryMarshal.Read<float>(payload);
                    payload = payload.Slice(sizeof(float));
                }
                else if (parameterType == typeof(double))
                {
                    // Payload is automatically updated to point to the next piece of data.
                    Int64 doubleBytes = (Int64)ReadUnalignedUInt64(ref payload);
                    decodedFields[i] = BitConverter.Int64BitsToDouble(doubleBytes);
                }
                else if (parameterType == typeof(bool))
                {
                    // The manifest defines a bool as a 32bit type (WIN32 BOOL), not 1 bit as CLR Does.
                    decodedFields[i] = (MemoryMarshal.Read<int>(payload) == 1);
                    payload = payload.Slice(sizeof(int));
                }
                else if (parameterType == typeof(Guid))
                {
                    // Payload is automatically updated to point to the next piece of data.
                    decodedFields[i] = ReadUnalignedGuid(ref payload);
                }
                else if (parameterType == typeof(char))
                {
                    decodedFields[i] = MemoryMarshal.Read<char>(payload);
                    payload = payload.Slice(sizeof(char));
                }
                else if (parameterType == typeof(string))
                {
                    ReadOnlySpan<char> charPayload = MemoryMarshal.Cast<byte, char>(payload);
                    int charCount = charPayload.IndexOf('\0');
                    string val = new string(charCount >= 0 ? charPayload.Slice(0, charCount) : charPayload);
                    payload = payload.Slice((val.Length + 1) * sizeof(char));
                    decodedFields[i] = val;
                }
                else
                {
                    Debug.Assert(false, "Unsupported type encountered.");
                }
            }

            return decodedFields;
        }

        private static UInt64 ReadUnalignedUInt64(ref ReadOnlySpan<byte> payload)
        {
            UInt64 val = 0;
            if (BitConverter.IsLittleEndian)
            {
                val |= MemoryMarshal.Read<UInt32>(payload);
                payload = payload.Slice(sizeof(UInt32));
                val |= (MemoryMarshal.Read<UInt32>(payload) << sizeof(UInt32));
                payload = payload.Slice(sizeof(UInt32));
            }
            else
            {
                val |= (MemoryMarshal.Read<UInt32>(payload) << sizeof(UInt32));
                payload = payload.Slice(sizeof(UInt32));
                val |= MemoryMarshal.Read<UInt32>(payload);
                payload = payload.Slice(sizeof(UInt32));
            }

            return val;
        }

        private static Guid ReadUnalignedGuid(ref ReadOnlySpan<byte> payload)
        {
            const int sizeOfGuid = 16;
            byte[] guidBytes = new byte[sizeOfGuid];
            if (BitConverter.IsLittleEndian)
            {
                for (int i = sizeOfGuid - 1; i >= 0; i--)
                {
                    guidBytes[i] = MemoryMarshal.Read<byte>(payload);
                    payload = payload.Slice(sizeof(byte));
                }
            }
            else
            {
                for (int i = 0; i < sizeOfGuid; i++)
                {
                    guidBytes[i] = MemoryMarshal.Read<byte>(payload);
                    payload = payload.Slice(sizeof(byte));
                }
            }

            return new Guid(guidBytes);
        }
    }
#endif // FEATURE_PERFTRACING
}
