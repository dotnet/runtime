// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.Diagnostics.NETCore.Client;

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
                throw new Exception("You updated DiagnosticsClient to a version that supports SetStartupProfiler, please remove this nonsense and use the real code.");
            }

            DiagnosticsClient client = new DiagnosticsClient(_processId);

            Console.WriteLine("Sending startup profiler message.");            
            // Send StartupProfiler command
            object ipcMessage = MakeStartupProfilerMessage(profilerGuid, profilerPath);
            SendMessage(_processId, ipcMessage);
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

        private static int GetResponseCommandSet(object response)
        {
            PropertyInfo header = response.GetType().GetProperty("Header");
            object ipcHeader = header.GetValue(response);

            FieldInfo commandId = ipcHeader.GetType().GetField("CommandSet");
            byte id = (byte)commandId.GetValue(ipcHeader);
            return id;
        }

        private static int GetResponseCommandId(object response)
        {
            PropertyInfo header = response.GetType().GetProperty("Header");
            object ipcHeader = header.GetValue(response);

            FieldInfo commandId = ipcHeader.GetType().GetField("CommandId");
            byte id = (byte)commandId.GetValue(ipcHeader);
            return id;
        }

        private static void SendMessage(object processId, object message)
        {
            Type ipcClientType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcClient");
            Type ipcMessageType = GetPrivateType("Microsoft.Diagnostics.NETCore.Client.IpcMessage");
            MethodInfo clientSendMessage = ipcClientType.GetMethod("SendMessage", new Type[] { typeof(int), ipcMessageType } );
            object response = clientSendMessage.Invoke(null, new object[] { processId, message });
            int responseCommandSet = GetResponseCommandSet(response);
            int responseCommandId = GetResponseCommandId(response);
            Console.WriteLine($"SendMessage response CommandSet={responseCommandSet} CommandId={responseCommandId}");
        }
    }
}