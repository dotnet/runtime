// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;

// This is to work around having to wait for an update to the DiagnosticsClient nuget before adding
// a test. I really hope this isn't permanent
// TODO: remove all this code when DiagnosticsClient version is updated...
namespace Profiler.Tests
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteString(this BinaryWriter @this, string value)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.Write(value != null ? (value.Length + 1) : 0);
            if (value != null)
                @this.Write(Encoding.Unicode.GetBytes(value + '\0'));
        }

    }

    public class DiagnosticsIPCWorkaround
    {
        private int _processId;

        public DiagnosticsIPCWorkaround(int processId)
        {
            _processId = processId;

        }

        public void SetStartupProfiler(Guid profilerGuid, string profilerPath)
        {
            MethodInfo startupProfiler = typeof(DiagnosticsClient).GetMethod("SetStartupProfiler", BindingFlags.Public);
            if (startupProfiler != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports SetStartupProfiler, please remove this and use the real code.");
            }
            
            Console.WriteLine("Sending startup profiler message.");
            
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(profilerGuid.ToByteArray());
                writer.WriteString(profilerPath);

                writer.Flush();
                byte[] payload = stream.ToArray();
                
                var message = new IpcMessage(0x03, 0x02, payload);
                Console.WriteLine($"Sent: {message.ToString()}");
                IpcMessage response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(_processId), message);
                Console.WriteLine($"Received: {response.ToString()}");
            }

            Console.WriteLine("Finished sending startup profiler message.");
        }

        public bool SetEnvironmentVariable(string name, string val)
        {
            MethodInfo setEnvironmentVariable = typeof(DiagnosticsClient).GetMethod("SetEnvironmentVariable", BindingFlags.Public);
            if (setEnvironmentVariable != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports SetEnvironmentVariable, please remove this and use the real code.");
            }

            Console.WriteLine($"Sending SetEnvironmentVariable message name={name} value={val ?? "NULL"}.");
            
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteString(name);
                writer.WriteString(val);

                writer.Flush();
                byte[] payload = stream.ToArray();
                
                var message = new IpcMessage(0x04, 0x03, payload);
                Console.WriteLine($"Sent: {message.ToString()}");
                IpcMessage response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(_processId), message);
                Console.WriteLine($"Received: {response.ToString()}");

                if (response.Header.CommandSet != 255 || response.Header.CommandId != 0)
                {
                    Console.WriteLine($"SetEnvironmentVariable failed.");
                    return false;
                }
            }

            Console.WriteLine($"Finished sending SetEnvironmentVariable message.");

            return true;
        }

        private static string ReadString(byte[] buffer)
        {
            int index = 0;
            // Length of the string of UTF-16 characters
            int length = (int)BitConverter.ToUInt32(buffer, index);
            index += sizeof(UInt32);

            int size = (int)length * sizeof(char);
            // The string contains an ending null character; remove it before returning the value
            string value = Encoding.Unicode.GetString(buffer, index, size).Substring(0, length - 1);
            index += size;
            return value;
        }
    }
}