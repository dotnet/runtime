// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.SqlServer.TDS.ColMetadata;

namespace Microsoft.SqlServer.TDS.Row
{
    /// <summary>
    /// Base class for token that corresponds to the row of data
    /// </summary>
    public abstract class TDSRowTokenBase : TDSPacketToken
    {
        /// <summary>
        /// Metadata associated with the row
        /// </summary>
        public TDSColMetadataToken Metadata { get; set; }

        /// <summary>
        /// Values
        /// </summary>
        public IList<object> Data { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSRowTokenBase(TDSColMetadataToken metadata)
        {
            // Store metadata to be able to serialize values properly
            Metadata = metadata;

            // Create data container
            Data = new List<object>();
        }

        /// <summary>
        /// Inflate a particular column from the stream
        /// </summary>
        /// <param name="source">Stream to inflate the column from</param>
        /// <param name="column">Metadata about the column</param>
        /// <returns>TRUE if inflation is complete</returns>
        protected virtual object InflateColumn(Stream source, TDSColumnData column)
        {
            // Dispatch further reading based on the type
            switch (column.DataType)
            {
                case TDSDataType.Null:
                    {
                        // No data associated with it
                        return null;
                    }
                case TDSDataType.Bit:
                case TDSDataType.Int1:
                    {
                        // Bit, 1 byte data representation
                        return (byte)source.ReadByte();
                    }
                case TDSDataType.Int2:
                    {
                        // SmallInt, 2 byte data representation
                        return unchecked((short)TDSUtilities.ReadUShort(source));
                    }
                case TDSDataType.Int4:
                    {
                        // Int, 4 byte data representation
                        return unchecked((int)TDSUtilities.ReadUInt(source));
                    }
                case TDSDataType.Float8:
                    {
                        // Float (8 byte data representation)
                        return BitConverter.ToDouble(BitConverter.GetBytes(TDSUtilities.ReadULong(source)), 0);
                    }
                case TDSDataType.Int8:
                    {
                        // BigInt (8 byte data representation)
                        return unchecked((long)TDSUtilities.ReadULong(source));
                    }
                case TDSDataType.BitN:
                    {
                        // Read length
                        byte length = (byte)source.ReadByte();

                        // Check if null
                        if (length == 0)
                        {
                            // No data
                            return null;
                        }

                        // Bit, 1 byte data representation
                        return (byte)source.ReadByte();
                    }
                case TDSDataType.IntN:
                    {
                        // Read length
                        byte length = (byte)source.ReadByte();

                        // Check integer length
                        switch (length)
                        {
                            case 0:
                                {
                                    // No data
                                    return null;
                                }
                            case 1:
                                {
                                    // Bit data
                                    return (byte)source.ReadByte();
                                }
                            case 2:
                                {
                                    // Short data
                                    return unchecked((short)TDSUtilities.ReadUShort(source));
                                }
                            case 4:
                                {
                                    // Integer data
                                    return unchecked((int)TDSUtilities.ReadUInt(source));
                                }
                            case 8:
                                {
                                    // Integer data
                                    return unchecked((long)TDSUtilities.ReadULong(source));
                                }
                            default:
                                {
                                    // We don't know how to inflate this integer
                                    throw new InvalidDataException(string.Format("Unable to inflate integer of {0} bytes", length));
                                }
                        }
                    }
                case TDSDataType.NumericN:
                    {
                        // Read length
                        byte length = (byte)source.ReadByte();

                        // Check if null
                        if (length == 0)
                        {
                            // No data
                            return null;
                        }

                        // Allocate value container
                        byte[] guidBytes = new byte[length];

                        // Read value
                        source.Read(guidBytes, 0, guidBytes.Length);

                        // Instantiate type
                        return guidBytes;
                    }
                case TDSDataType.Guid:
                    {
                        // Read the length
                        byte length = (byte)source.ReadByte();

                        // Check if we have any data
                        if (length == 0x0000)
                        {
                            // No data
                            return null;
                        }

                        // Allocate value container
                        byte[] guidBytes = new byte[length];

                        // Read value
                        source.Read(guidBytes, 0, guidBytes.Length);

                        // Convert to type
                        return new Guid(guidBytes);
                    }
                case TDSDataType.BigVarChar:
                case TDSDataType.BigChar:
                    {
                        // Read length
                        ushort length = TDSUtilities.ReadUShort(source);

                        // Check if we have any data
                        if (length == 0xFFFF)
                        {
                            // No data
                            return null;
                        }

                        // Allocate value container
                        byte[] textBytes = new byte[length];

                        // Read value
                        source.Read(textBytes, 0, textBytes.Length);

                        // Convert to type
                        return Encoding.ASCII.GetString(textBytes);
                    }
                case TDSDataType.NVarChar:
                    {
                        // Check if MAX type
                        if ((column.DataTypeSpecific as TDSShilohVarCharColumnSpecific).Length == 0xFFFF)
                        {
                            // Read the length of partialy length-prefixed type
                            ulong length = TDSUtilities.ReadULong(source);

                            // Check the value
                            if (length == 0xFFFFFFFFFFFFFFFF)
                            {
                                // There's no data
                                return null;
                            }
                            else if (length == 0xFFFFFFFFFFFFFFFE)
                            {
                                throw new NotImplementedException("UNKNOWN_PLP_LEN is not implemented yet");
                            }
                            else
                            {
                                // Allocate a memory stream
                                MemoryStream chunks = new MemoryStream();

                                // Size of the last chunk
                                uint chunkLength = 0;

                                // Iterate until we read the whole data
                                do
                                {
                                    // Read the length of the chunk
                                    chunkLength = TDSUtilities.ReadUInt(source);

                                    // Allocate chunk container
                                    byte[] chunk = new byte[chunkLength];

                                    // Read value
                                    source.Read(chunk, 0, chunk.Length);

                                    // Append into the stream
                                    chunks.Write(chunk, 0, chunk.Length);
                                }
                                while (chunkLength > 0);

                                // Convert to byte array
                                byte[] byteString = chunks.ToArray();

                                // Serialize the data into the string
                                return Encoding.Unicode.GetString(byteString, 0, byteString.Length);
                            }
                        }
                        else
                        {
                            // Read length
                            ushort length = TDSUtilities.ReadUShort(source);

                            // Check if we have any data
                            if (length == 0xFFFF)
                            {
                                // No data
                                return null;
                            }

                            // Read the whole string at once
                            return TDSUtilities.ReadString(source, (ushort)length);
                        }
                    }
                case TDSDataType.BigVarBinary:
                    {
                        byte[] bytes = null;

                        // Check if MAX type
                        if ((ushort)column.DataTypeSpecific == 0xFFFF)
                        {
                            // Read the length of partialy length-prefixed type
                            ulong length = TDSUtilities.ReadULong(source);

                            // Check the value
                            if (length == 0xFFFFFFFFFFFFFFFF)
                            {
                                // There's no data
                                return null;
                            }
                            else if (length == 0xFFFFFFFFFFFFFFFE)
                            {
                                throw new NotImplementedException("UNKNOWN_PLP_LEN is not implemented yet");
                            }
                            else
                            {
                                // Allocate a memory stream
                                MemoryStream chunks = new MemoryStream();

                                // Size of the last chunk
                                uint chunkLength = 0;

                                // Iterate until we read the whole data
                                do
                                {
                                    // Read the length of the chunk
                                    chunkLength = TDSUtilities.ReadUInt(source);

                                    // Allocate chunk container
                                    byte[] chunk = new byte[chunkLength];

                                    // Read value
                                    source.Read(chunk, 0, chunk.Length);

                                    // Append into the stream
                                    chunks.Write(chunk, 0, chunk.Length);
                                }
                                while (chunkLength > 0);

                                // Serialize to byte array
                                bytes = chunks.ToArray();
                            }
                        }
                        else
                        {
                            // Read length
                            ushort length = TDSUtilities.ReadUShort(source);

                            // Check if we have any data
                            if (length == 0xFFFF)
                            {
                                // No data
                                return null;
                            }

                            // Allocate value container
                            bytes = new byte[length];

                            // Read value
                            source.Read(bytes, 0, bytes.Length);
                        }

                        // Convert to type
                        return bytes;
                    }
                case TDSDataType.BigBinary:
                    {
                        // Read length
                        ushort length = TDSUtilities.ReadUShort(source);

                        // Check if we have any data
                        if (length == 0xFFFF)
                        {
                            // No data
                            return null;
                        }

                        // Allocate value container
                        byte[] bytes = new byte[length];

                        // Read value
                        source.Read(bytes, 0, bytes.Length);

                        // Convert to type
                        return bytes;
                    }
                default:
                    {
                        // We don't know this type
                        throw new NotImplementedException(string.Format("Unrecognized data type {0} for inflation", column.DataType));
                    }
            }
        }

        /// <summary>
        /// Deflate the column into the stream
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        /// <param name="column">Column metadata</param>
        /// <param name="data">Column value</param>
        protected virtual void DeflateColumn(Stream destination, TDSColumnData column, object data)
        {
            // Dispatch further reading based on the type
            switch (column.DataType)
            {
                case TDSDataType.Null:
                    {
                        // No data associated with it
                        break;
                    }
                case TDSDataType.Bit:
                    {
                        destination.WriteByte((byte)((bool)data ? 1 : 0));
                        break;
                    }
                case TDSDataType.Int1:
                    {
                        // Bit, 1 byte data representation
                        destination.WriteByte((byte)data);
                        break;
                    }
                case TDSDataType.Int2:
                    {
                        // SmallInt, 2 byte data representation
                        TDSUtilities.WriteUShort(destination, unchecked((ushort)((short)data)));
                        break;
                    }
                case TDSDataType.Int4:
                    {
                        // Int, 4 byte data representation
                        TDSUtilities.WriteUInt(destination, unchecked((uint)((int)data)));
                        break;
                    }
                case TDSDataType.Float8:
                    {
                        // Float (8 byte data representation)
                        byte[] floatBytes = BitConverter.GetBytes((double)data);
                        destination.Write(floatBytes, 0, floatBytes.Length);
                        break;
                    }
                case TDSDataType.Int8:
                    {
                        // BigInt (8 byte data representation)
                        TDSUtilities.WriteULong(destination, unchecked((ulong)((long)data)));
                        break;
                    }
                case TDSDataType.BitN:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            destination.WriteByte(0);
                        }
                        else
                        {
                            // One byte data
                            destination.WriteByte(1);

                            // Data
                            destination.WriteByte((byte)((bool)data ? 1 : 0));
                        }

                        break;
                    }
                case TDSDataType.IntN:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            destination.WriteByte(0);
                        }
                        else if (data is byte)
                        {
                            // One-byte data
                            destination.WriteByte(1);

                            // Bit data
                            destination.WriteByte((byte)data);
                        }
                        else if (data is short)
                        {
                            // One-byte data
                            destination.WriteByte(2);

                            // Short data
                            TDSUtilities.WriteUShort(destination, unchecked((ushort)(short)data));
                        }
                        else if (data is int)
                        {
                            // One-byte data
                            destination.WriteByte(4);

                            // Integer data
                            TDSUtilities.WriteUInt(destination, unchecked((uint)(int)data));
                        }
                        else if (data is long)
                        {
                            // One-byte data
                            destination.WriteByte(8);

                            // Long data
                            TDSUtilities.WriteULong(destination, unchecked((ulong)(long)data));
                        }
                        else
                        {
                            // We don't know how to deflate this integer
                            throw new InvalidDataException(string.Format("Unable to deflate integer of type {0}", data.GetType().FullName));
                        }

                        break;
                    }
                case TDSDataType.Guid:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            destination.WriteByte(0);
                        }
                        else
                        {
                            // Get bytes
                            byte[] guidBytes = ((Guid)data).ToByteArray();

                            // One byte data length
                            destination.WriteByte((byte)guidBytes.Length);

                            // Data
                            destination.Write(guidBytes, 0, guidBytes.Length);
                        }

                        break;
                    }
                case TDSDataType.BigChar:
                case TDSDataType.BigVarChar:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            TDSUtilities.WriteUShort(destination, 0xFFFF);
                        }
                        else
                        {
                            // Get bytes
                            byte[] textBytes = Encoding.ASCII.GetBytes((string)data);

                            // One data length
                            TDSUtilities.WriteUShort(destination, (ushort)textBytes.Length);

                            // Data
                            destination.Write(textBytes, 0, textBytes.Length);
                        }

                        break;
                    }
                case TDSDataType.NVarChar:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            TDSUtilities.WriteUShort(destination, 0xFFFF);
                        }
                        else
                        {
                            // Get bytes
                            byte[] textBytes = Encoding.Unicode.GetBytes((string)data);

                            // One data length
                            TDSUtilities.WriteUShort(destination, (ushort)textBytes.Length);

                            // Data
                            destination.Write(textBytes, 0, textBytes.Length);
                        }

                        break;
                    }
                case TDSDataType.BigVarBinary:
                case TDSDataType.BigBinary:
                    {
                        // Check if data is available
                        if (data == null)
                        {
                            // No data
                            TDSUtilities.WriteUShort(destination, 0xFFFF);
                        }
                        else
                        {
                            // Get bytes
                            byte[] bytes = (byte[])data;

                            // One data length
                            TDSUtilities.WriteUShort(destination, (ushort)bytes.Length);

                            // Data
                            destination.Write(bytes, 0, bytes.Length);
                        }

                        break;
                    }
                default:
                    {
                        // We don't know this type
                        throw new NotImplementedException(string.Format("Unrecognized data type {0} for deflation", column.DataType));
                    }
            }
        }
    }
}
