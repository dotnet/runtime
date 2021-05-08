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

        public bool GetEnvironmentVariable(string name, out string val)
        {
            val = String.Empty;

            MethodInfo getEnvironmentVariable = typeof(DiagnosticsClient).GetMethod("GetEnvironmentVariable", BindingFlags.Public);
            if (getEnvironmentVariable != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports GetEnvironmentVariable, please remove this and use the real code.");
            }

            Console.WriteLine($"Sending GetEnvironmentVariable message name={name}.");
            
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteString(name);

                writer.Flush();
                byte[] payload = stream.ToArray();
                
                var message = new IpcMessage(0x04, 0x03, payload);
                Console.WriteLine($"Sent: {message.ToString()}");
                IpcMessage response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(_processId), message);
                Console.WriteLine($"Received: {response.ToString()}");

                if (response.Header.CommandSet == 255)
                {
                    Console.WriteLine($"GetEnvironmentVariable failed.");
                    return false;
                }

                val = Encoding.Unicode.GetString(response.Payload);
            }

            Console.WriteLine($"Finished sending GetEnvironmentVariable message value={val}.");

            return true;
        }

        public bool SetEnvironmentVariable(string name, string val)
        {
            val = String.Empty;

            MethodInfo setEnvironmentVariable = typeof(DiagnosticsClient).GetMethod("SetEnvironmentVariable", BindingFlags.Public);
            if (setEnvironmentVariable != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports SetEnvironmentVariable, please remove this and use the real code.");
            }

            Console.WriteLine($"Sending SetEnvironmentVariable message name={name} value={val}.");
            
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

                if (response.Header.CommandSet == 255)
                {
                    Console.WriteLine($"SetEnvironmentVariable failed.");
                    return false;
                }
            }

            Console.WriteLine($"Finished sending SetEnvironmentVariable message.");

            return true;
        }
    }
}