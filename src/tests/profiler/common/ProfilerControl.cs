// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.NETCore.Client;

namespace Profiler.Tests
{
    // TODO: remove this too when DiagnosticsClient is updated
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

    /// <summary>
    /// Used by managed profilees to control the profiler
    /// </summary>
    public static class ProfilerControlHelpers
    {
        public static void AttachProfilerToSelf(Guid profilerGuid, string profilerPath)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            client.AttachProfiler(TimeSpan.MaxValue, profilerGuid, profilerPath, null);
        }

        private static object MakeHeader(byte commandSet, byte commandId)
        {
            Type commandEnumType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.DiagnosticsServerCommandSet");
            object enumCommandSet = Enum.ToObject(commandEnumType, commandSet);
            Type ipcHeaderType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcHeader");
            object ipcHeader = Activator.CreateInstance(ipcHeaderType, new object[] { enumCommandSet, commandId });
            return ipcHeader;
        }

        private static object MakeMessage(object ipcHeader, byte[] payload)
        {
                Type ipcMessageType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcMessage");
                object ipcMessage = Activator.CreateInstance(ipcMessageType, new object[] { ipcHeader, payload });
                return ipcMessage;
        }

        private static object MakeStartupProfilerMessage(Guid profilerGuid, string profilerPath)
        {
            // public IpcMessage(IpcHeader header, byte[] payload = null)
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(profilerGuid.ToByteArray());
                writer.WriteString(profilerPath);

                writer.Flush();
                byte[] payload = stream.ToArray();
                
                object ipcHeader = MakeHeader(3, 2);
                return MakeMessage(ipcHeader, payload);
            }
        }

        private static object MakeResumeRuntimeMessage()
        {
            object ipcHeader = MakeHeader(4, 1);
            return MakeMessage(ipcHeader, new byte[0]);
        }

        private static Type GetPrivateType(string typeName)
        {
            return typeof(DiagnosticsClient).Assembly.GetType(typeName);
        }

        private static void SendMessage(object processId, object message)
        {
            Type ipcClientType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcClient");
            Type ipcMessageType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcMessage");
            MethodInfo clientSendMessage = ipcClientType.GetMethod("SendMessage", new Type[] { typeof(int), ipcMessageType } );
            clientSendMessage.Invoke(null, new object[] { processId, message });
        }

        public static void SetStartupProfilerViaIPC(Guid profilerGuid, string profilerPath, int processId)
        {
            MethodInfo startupProfiler = typeof(DiagnosticsClient).GetMethod("SetStartupProfiler", BindingFlags.Public);
            if (startupProfiler != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports SetStartupProfiler, please remove this nonsense and replace it with the calls commented out below.");
                // DiagnosticsClient client = new DiagnosticsClient(processId);
                // client.SetStartupProfiler(profilerGuid, profilerPath);
                // client.ResumeRuntime();
            }

            DiagnosticsClient client = new DiagnosticsClient(processId);
            // TODO: remove all this code when DiagnosticsClient version is updated...
            // FieldInfo endpoint = typeof(DiagnosticsClient).GetField("_endpoint", BindingFlags.NonPublic | BindingFlags.Instance);
            // object ipcEndpoint = endpoint.GetValue(client);
            
            // Send StartupProfiler command
            object ipcMessage = MakeStartupProfilerMessage(profilerGuid, profilerPath);
            SendMessage(processId, ipcMessage);

            // Send ResumeRuntime command
            ipcMessage = MakeResumeRuntimeMessage();
            SendMessage(processId, ipcMessage);
        }



        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            return client.StartEventPipeSession(providers, /* requestRunDown */ false);
        }
    }
}
