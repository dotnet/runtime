// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Microsoft.SqlServer.TDS.Login7;
using Microsoft.SqlServer.TDS.PreLogin;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Internal utilities
    /// </summary>
    public static class TDSUtilities
    {
        /// <summary>
        /// Object lock for log writer
        /// </summary>
        private static Object s_logWriterLock = new Object();

        /// <summary>
        /// Read unsigned long from the stream
        /// </summary>
        internal static ulong ReadULong(Stream source)
        {
            return (ulong)(source.ReadByte()
                + (source.ReadByte() << 8)
                + (source.ReadByte() << 16)
                + (source.ReadByte() << 24)
                + (source.ReadByte() << 32)
                + (source.ReadByte() << 40)
                + (source.ReadByte() << 48)
                + (source.ReadByte() << 56));
        }

        /// <summary>
        /// Write unsigned long into the stream
        /// </summary>
        internal static void WriteULong(Stream destination, ulong value)
        {
            destination.WriteByte((byte)(value & 0xff));
            destination.WriteByte((byte)((value >> 8) & 0xff));
            destination.WriteByte((byte)((value >> 16) & 0xff));
            destination.WriteByte((byte)((value >> 24) & 0xff));
            destination.WriteByte((byte)((value >> 32) & 0xff));
            destination.WriteByte((byte)((value >> 40) & 0xff));
            destination.WriteByte((byte)((value >> 48) & 0xff));
            destination.WriteByte((byte)((value >> 56) & 0xff));
        }

        /// <summary>
        /// Read unsigned integer from the stream
        /// </summary>
        internal static uint ReadUInt(Stream source)
        {
            return (uint)(source.ReadByte())
                + (uint)(source.ReadByte() << 8)
                + (uint)(source.ReadByte() << 16)
                + (uint)(source.ReadByte() << 24);
        }

        /// <summary>
        /// Write unsigned integer into the stream
        /// </summary>
        public static void WriteUInt(Stream destination, uint value)
        {
            unchecked
            {
                destination.WriteByte((byte)value);
                destination.WriteByte((byte)(value >> 8));
                destination.WriteByte((byte)(value >> 16));
                destination.WriteByte((byte)(value >> 24));
            }
        }

        /// <summary>
        /// Read signed integer from the packet
        /// </summary>
        internal static int ReadInt(Stream source)
        {
            return (int)(source.ReadByte())
                + (int)(source.ReadByte() << 8)
                + (int)(source.ReadByte() << 16)
                + (int)(source.ReadByte() << 24);
        }

        /// <summary>
        /// Write signed integer into the stream
        /// </summary>
        internal static void WriteInt(Stream destination, int value)
        {
            destination.WriteByte((byte)value);
            destination.WriteByte((byte)(value >> 8));
            destination.WriteByte((byte)(value >> 16));
            destination.WriteByte((byte)(value >> 24));
        }

        /// <summary>
        /// Read unsigned short from the packet
        /// </summary>
        internal static ushort ReadUShort(Stream source)
        {
            return (ushort)(source.ReadByte() + (ushort)(source.ReadByte() << 8));
        }

        /// <summary>
        /// Write unsigned short into the stream
        /// </summary>
        internal static void WriteUShort(Stream destination, ushort value)
        {
            destination.WriteByte(unchecked((byte)value));
            destination.WriteByte((byte)(value >> 8));
        }

        /// <summary>
        /// Write unsigned short into the stream in network byte order (big-endian)
        /// </summary>
        internal static void WriteUShortBigEndian(Stream destination, ushort value)
        {
            destination.WriteByte((byte)(value >> 8));
            destination.WriteByte((byte)value);
        }

        /// <summary>
        /// Read string from the packet
        /// </summary>
        internal static string ReadString(Stream source, ushort length)
        {
            // Check if any data will be read
            if (length == 0)
            {
                // Instead of returning an empty string later we just return NULL
                return null;
            }

            // Allocate buffer
            byte[] byteString = new byte[length];

            // Read into a byte buffer
            source.Read(byteString, 0, byteString.Length);

            // Convert
            return Encoding.Unicode.GetString(byteString, 0, byteString.Length);
        }

        /// <summary>
        /// Write string from into the packet
        /// </summary>
        internal static void WriteString(Stream destination, string value)
        {
            // Check if value is null
            if (string.IsNullOrEmpty(value))
            {
                // There's nothing to write
                return;
            }

            // Convert
            byte[] byteString = Encoding.Unicode.GetBytes(value);

            // Write into a the stream
            destination.Write(byteString, 0, byteString.Length);
        }

        /// <summary>
        /// Read a password string and decrypt it
        /// </summary>
        internal static string ReadPasswordString(Stream source, ushort length)
        {
            // Allocate buffer
            byte[] byteString = new byte[length];

            // Read into a byte buffer
            source.Read(byteString, 0, byteString.Length);

            // Perform password decryption
            for (int i = 0; i < byteString.Length; i++)
            {
                // XOR first
                byteString[i] ^= 0xA5;

                // Swap 4 high bits with 4 low bits
                byteString[i] = (byte)(((byteString[i] & 0xf0) >> 4) | ((byteString[i] & 0xf) << 4));
            }

            // Convert
            return Encoding.Unicode.GetString(byteString, 0, byteString.Length);
        }

        /// <summary>
        /// Write password string encrypted into the packet
        /// </summary>
        internal static void WritePasswordString(Stream destination, string value)
        {
            // Check if value is null
            if (string.IsNullOrEmpty(value))
            {
                // There's nothing to write
                return;
            }

            // Convert
            byte[] byteString = Encoding.Unicode.GetBytes(value);

            // Perform password decryption
            for (int i = 0; i < byteString.Length; i++)
            {
                // Swap 4 high bits with 4 low bits
                byteString[i] = (byte)(((byteString[i] & 0xf0) >> 4) | ((byteString[i] & 0xf) << 4));

                // XOR
                byteString[i] ^= 0xA5;
            }

            // Write into a the stream
            destination.Write(byteString, 0, byteString.Length);
        }

        /// <summary>
        /// Generate an encryption response based on the client request and server setting
        /// </summary>
        /// <param name="client">A value received from the client</param>
        /// <param name="server">Configuration of the server</param>
        public static TDSPreLoginTokenEncryptionType GetEncryptionResponse(TDSPreLoginTokenEncryptionType client, TDSPreLoginTokenEncryptionType server)
        {
            // Check each equivalence class
            if (client == TDSPreLoginTokenEncryptionType.NotSupported)
            {
                // Check server response
                if (server == TDSPreLoginTokenEncryptionType.Off || server == TDSPreLoginTokenEncryptionType.NotSupported)
                {
                    return TDSPreLoginTokenEncryptionType.NotSupported;
                }
                else
                {
                    return TDSPreLoginTokenEncryptionType.Required;
                }
            }
            else if (client == TDSPreLoginTokenEncryptionType.Off)
            {
                // Check corresponding server
                if (server == TDSPreLoginTokenEncryptionType.NotSupported)
                {
                    return TDSPreLoginTokenEncryptionType.NotSupported;
                }
                else if (server == TDSPreLoginTokenEncryptionType.Off)
                {
                    return TDSPreLoginTokenEncryptionType.Off;
                }
                else
                {
                    return TDSPreLoginTokenEncryptionType.Required;
                }
            }
            else if (client == TDSPreLoginTokenEncryptionType.On)
            {
                // Check server
                if (server == TDSPreLoginTokenEncryptionType.Off || server == TDSPreLoginTokenEncryptionType.On || server == TDSPreLoginTokenEncryptionType.Required)
                {
                    return TDSPreLoginTokenEncryptionType.On;
                }
                else
                {
                    throw new ArgumentException("Server is configured to not support encryption", "server");
                }
            }

            // This case is not documented so pick a default
            return TDSPreLoginTokenEncryptionType.Off;
        }

        /// <summary>
        /// Convert indications of encryption support by client and server into expected behavior
        /// </summary>
        public static TDSEncryptionType ResolveEncryption(TDSPreLoginTokenEncryptionType client, TDSPreLoginTokenEncryptionType server)
        {
            // Check each equivalence class
            if (client == TDSPreLoginTokenEncryptionType.NotSupported)
            {
                // Check server response
                if (server == TDSPreLoginTokenEncryptionType.Off || server == TDSPreLoginTokenEncryptionType.NotSupported)
                {
                    return TDSEncryptionType.Off;
                }
                else
                {
                    // Encrypt login only
                    return TDSEncryptionType.LoginOnly;
                }
            }
            else if (client == TDSPreLoginTokenEncryptionType.Off)
            {
                // Check corresponding server
                if (server == TDSPreLoginTokenEncryptionType.NotSupported)
                {
                    // Encryption should be turned off
                    return TDSEncryptionType.Off;
                }
                else if (server == TDSPreLoginTokenEncryptionType.Off)
                {
                    // We encrypt only login packet
                    return TDSEncryptionType.LoginOnly;
                }
            }
            else if (client == TDSPreLoginTokenEncryptionType.On)
            {
                // Check server
                if (server == TDSPreLoginTokenEncryptionType.NotSupported || server == TDSPreLoginTokenEncryptionType.Off)
                {
                    // This is an error case, however existing client stacks treat this as login-only encryption
                    return TDSEncryptionType.LoginOnly;
                }
            }

            // Full encryption is required
            return TDSEncryptionType.Full;
        }

        /// <summary>
        /// Log object content into destination
        /// </summary>
        /// <param name="log">Destination</param>
        /// <param name="prefix">Prefix the output with</param>
        /// <param name="instance">Object to log</param>
        public static void Log(TextWriter log, string prefix, object instance)
        {
            // Check log validity
            if (log == null)
            {
                // Don't log anything
                return;
            }

            // Check if null
            if (instance == null)
            {
                SerializedWriteLineToLog(log, String.Format("{0}: <null>", prefix));

                return;
            }

            // Get object type
            Type objectType = instance.GetType();

            // Check if simple type
            if (objectType.IsEnum
                || instance is bool
                || instance is string
                || instance is int
                || instance is uint
                || instance is byte
                || instance is sbyte
                || instance is short
                || instance is ushort
                || instance is long
                || instance is ulong
                || instance is double
                || instance is float
                || instance is Version)
            {
                SerializedWriteLineToLog(log, String.Format("{0}: {1}", prefix, instance));

                return;
            }


            // Check declaring type
            if (objectType.IsGenericType || (objectType.BaseType != null && objectType.BaseType.IsGenericType))  // IList<T>
            {
                int index = 0;

                // Log values
                foreach (object o in (instance as System.Collections.IEnumerable))
                {
                    Log(log, string.Format("{0}[{1}]", prefix, index++), o);
                }

                // Check if we logged anything
                if (index == 0)
                {
                    SerializedWriteLineToLog(log, String.Format("{0}: <empty>", prefix));
                }
            }
            else if (objectType.IsArray)
            {
                // Prepare prefix
                string preparedLine = String.Format("{0}: [", prefix);

                // Log values
                foreach (object o in (instance as Array))
                {
                    preparedLine += String.Format("{0:X} ", o);
                }

                // Finish the line
                preparedLine += "]";

                // Move to the next line
                SerializedWriteLineToLog(log, preparedLine);
            }

            // Iterate all public properties
            foreach (PropertyInfo info in objectType.GetProperties())
            {
                // Check if this is an indexer
                if (info.GetIndexParameters().Length > 0 || !info.DeclaringType.Assembly.Equals(Assembly.GetExecutingAssembly()))
                {
                    // We ignore indexers
                    continue;
                }

                // Get property value
                object value = info.GetValue(instance, null);

                // Log each property
                Log(log, string.Format("{0}.{1}.{2}", prefix, objectType.Name, info.Name), value);
            }

            // Flush to destination
            lock (s_logWriterLock)
            {
                log.Flush();
            }
        }

        /// <summary>
        /// Serialized write line to destination
        /// </summary>
        /// <param name="log">Destination</param>
        /// <param name="text">Text to log</param>        
        public static void SerializedWriteLineToLog(TextWriter log, string text)
        {
            lock (s_logWriterLock)
            {
                log.WriteLine(string.Format("[{0}] {1}", DateTime.Now, text));
            }
        }
    }
}
