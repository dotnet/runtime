// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics.Tracing
{
#if FEATURE_PERFTRACING
    internal static class EventPipePayloadDecoder
    {
        /// <summary>
        /// Given the metadata for an event and an event payload, decode and deserialize the event payload.
        /// </summary>
        internal static object[] DecodePayload(ref EventSource.EventMetadata metadata, ReadOnlySpan<byte> payload)
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
                        decodedFields[i] = (IntPtr)BinaryPrimitives.ReadInt64LittleEndian(payload);
                    }
                    else
                    {
                        decodedFields[i] = (IntPtr)BinaryPrimitives.ReadInt32LittleEndian(payload);
                    }
                    payload = payload.Slice(IntPtr.Size);
                }
                else if (parameterType == typeof(int))
                {
                    decodedFields[i] = BinaryPrimitives.ReadInt32LittleEndian(payload);
                    payload = payload.Slice(sizeof(int));
                }
                else if (parameterType == typeof(uint))
                {
                    decodedFields[i] = BinaryPrimitives.ReadUInt32LittleEndian(payload);
                    payload = payload.Slice(sizeof(uint));
                }
                else if (parameterType == typeof(long))
                {
                    decodedFields[i] = BinaryPrimitives.ReadInt64LittleEndian(payload);
                    payload = payload.Slice(sizeof(long));
                }
                else if (parameterType == typeof(ulong))
                {
                    decodedFields[i] = BinaryPrimitives.ReadUInt64LittleEndian(payload);
                    payload = payload.Slice(sizeof(ulong));
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
                    decodedFields[i] = BinaryPrimitives.ReadInt16LittleEndian(payload);
                    payload = payload.Slice(sizeof(short));
                }
                else if (parameterType == typeof(ushort))
                {
                    decodedFields[i] = BinaryPrimitives.ReadUInt16LittleEndian(payload);
                    payload = payload.Slice(sizeof(ushort));
                }
                else if (parameterType == typeof(float))
                {
                    decodedFields[i] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload));
                    payload = payload.Slice(sizeof(float));
                }
                else if (parameterType == typeof(double))
                {
                    decodedFields[i] = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(payload));
                    payload = payload.Slice(sizeof(double));
                }
                else if (parameterType == typeof(bool))
                {
                    // The manifest defines a bool as a 32bit type (WIN32 BOOL), not 1 bit as CLR Does.
                    decodedFields[i] = (BinaryPrimitives.ReadInt32LittleEndian(payload) == 1);
                    payload = payload.Slice(sizeof(int));
                }
                else if (parameterType == typeof(Guid))
                {
                    const int sizeOfGuid = 16;
                    decodedFields[i] = new Guid(payload.Slice(0, sizeOfGuid));
                    payload = payload.Slice(sizeOfGuid);
                }
                else if (parameterType == typeof(char))
                {
                    decodedFields[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(payload);
                    payload = payload.Slice(sizeof(char));
                }
                else if (parameterType == typeof(string))
                {
                    ReadOnlySpan<char> charPayload = MemoryMarshal.Cast<byte, char>(payload);
                    int charCount = charPayload.IndexOf('\0');
                    if (charCount < 0)
                    {
                        payload = default;
                    }
                    else
                    {
                        charPayload = charPayload.Slice(0, charCount);
                        payload = payload.Slice((charCount + 1) * sizeof(char));
                    }
                    decodedFields[i] = BitConverter.IsLittleEndian ? new string(charPayload) : Encoding.Unicode.GetString(MemoryMarshal.Cast<char, byte>(charPayload));
                }
                else
                {
                    Debug.Assert(false, "Unsupported type encountered.");
                }
            }

            return decodedFields;
        }
    }
#endif // FEATURE_PERFTRACING
}
