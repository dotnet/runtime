// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;
using System.IO;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides support for design-time license context serialization.
    /// </summary>
    public class DesigntimeLicenseContextSerializer
    {
        // Not creatable.
        private DesigntimeLicenseContextSerializer()
        {
        }

        /// <summary>
        /// Serializes the licenses within the specified design-time license context
        /// using the specified key and output stream.
        /// </summary>
        public static void Serialize(Stream o, string cryptoKey, DesigntimeLicenseContext context)
        {
            if (LocalAppContextSwitches.BinaryFormatterEnabled)
            {
                IFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Issue https://github.com/dotnet/runtime/issues/39293 tracks finding an alternative to BinaryFormatter
                formatter.Serialize(o, new object[] { cryptoKey, context._savedLicenseKeys });
#pragma warning restore SYSLIB0011
            }
            else
            {
                using (BinaryWriter writer = new BinaryWriter(o, encoding: Text.Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(-1); // flag to identify BinaryWriter
                    writer.Write(cryptoKey);
                    writer.Write(context._savedLicenseKeys.Count);
                    foreach (DictionaryEntry keyAndValue in context._savedLicenseKeys)
                    {
                        writer.Write(keyAndValue.Key.ToString());
                        writer.Write(keyAndValue.Value.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// During deserialization, the stream passed in may be binary formatted or may have used binary writer. This is quick test to discern between them.
        /// </summary>
        internal static bool StreamIsBinaryFormatted(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new Exception("Expected a seek-able stream");
            }
            bool ret = true;

            // For binary formatter, the first byte is the SerializationHeaderRecord and has a value 0
            int firstByte = stream.ReadByte();
            if (firstByte != 0)
            {
                ret = false;
            }

            stream.Seek(-1, SeekOrigin.Current);
            return ret;
        }

        internal static void Deserialize(Stream o, string cryptoKey, RuntimeLicenseContext context)
        {
            if (StreamIsBinaryFormatted(o))
            {
                try
                {
#pragma warning disable SYSLIB0011 // Issue https://github.com/dotnet/runtime/issues/39293 tracks finding an alternative to BinaryFormatter
                    IFormatter formatter = new BinaryFormatter();

                    object obj = formatter.Deserialize(o);
#pragma warning restore SYSLIB0011

                    if (obj is object[] value)
                    {
                        if (value[0] is string && (string)value[0] == cryptoKey)
                        {
                            context._savedLicenseKeys = (Hashtable)value[1];
                        }
                    }
                }
                catch (System.NotSupportedException exception)
                {
                    if (!LocalAppContextSwitches.BinaryFormatterEnabled)
                    {
                        throw new System.NotSupportedException(exception.Message + " Turn on the EnableUnsafeBinaryFormatterSerialization flag to continue using BinaryFormatter");
                    }
                    else
                    {
                        throw exception;
                    }
                }
            }
            else
            {
                using (BinaryReader reader = new BinaryReader(o, encoding: Text.Encoding.UTF8, leaveOpen: true))
                {
                    int binaryWriterIdentifer = reader.ReadInt32();
                    string streamCryptoKey = reader.ReadString();
                    int numEntries = reader.ReadInt32();
                    if (streamCryptoKey == cryptoKey)
                    {
                        for (int i = 0; i < numEntries; i++)
                        {
                            context._savedLicenseKeys.Add(reader.ReadString(), reader.ReadString());
                        }
                    }
                }
            }
        }
    }
}
