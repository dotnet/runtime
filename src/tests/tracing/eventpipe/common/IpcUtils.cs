// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

// modified version of same code in dotnet/diagnostics for testing
namespace Tracing.Tests.Common
{
    public static class Utils
    {
        public static readonly string DiagnosticsMonitorAddressEnvKey = "DOTNET_DiagnosticsMonitorAddress";
        public static readonly string DiagnosticsMonitorPauseOnStartEnvKey = "DOTNET_DiagnosticsMonitorPauseOnStart";

        public static async Task<T> WaitTillTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (completedTask == task)
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException("Task timed out");
            }
        }

        public static async Task WaitTillTimeout(Task task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (completedTask == task)
            {
                cts.Cancel();
                return;
            }
            else
            {
                throw new TimeoutException("Task timed out");
            }
        }

        public static async Task<bool> RunSubprocess(Assembly currentAssembly, Dictionary<string,string> environment, Func<Task> beforeExecution = null, Func<int, Task> duringExecution = null, Func<Task> afterExecution = null)
        {
            bool fSuccess = true;
            using (var process = new Process())
            {
                if (beforeExecution != null)
                    await beforeExecution();

                var stdoutSb = new StringBuilder();
                var stderrSb = new StringBuilder();

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                foreach ((string key, string value) in environment)
                    process.StartInfo.Environment.Add(key, value);
                process.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo.Arguments = new Uri(currentAssembly.CodeBase).LocalPath + " 0";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;

                Logger.logger.Log($"running sub-process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                DateTime startTime = DateTime.Now;
                process.OutputDataReceived += new DataReceivedEventHandler((s,e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stdoutSb.Append($"\n\t{(DateTime.Now - startTime).TotalSeconds,5:f1}s: {e.Data}");
                    }
                });

                process.ErrorDataReceived += new DataReceivedEventHandler((s,e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stderrSb.Append($"\n\t{(DateTime.Now - startTime).TotalSeconds,5:f1}s: {e.Data}");
                    }
                });

                process.EnableRaisingEvents = true;
                fSuccess &= process.Start();
                if (!fSuccess)
                    throw new Exception("Failed to start subprocess");
                StreamWriter subprocesssStdIn = process.StandardInput;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Logger.logger.Log($"subprocess started: {fSuccess}");
                Logger.logger.Log($"subprocess PID: {process.Id}");

                bool fGotToEnd = false;
                process.Exited += (s, e) => 
                {
                    Logger.logger.Log("================= Subprocess Exited =================");
                    if (!fGotToEnd)
                    {
                        Logger.logger.Log($"- Exit code: {process.ExitCode}");
                        Logger.logger.Log($"Subprocess stdout: {stdoutSb.ToString()}");
                        Logger.logger.Log($"Subprocess stderr: {stderrSb.ToString()}");
                    }
                };

                while (!EventPipeClient.ListAvailablePorts().Contains(process.Id))
                {
                    Logger.logger.Log($"Standard Diagnostics Server connection not created yet -> try again in 100 ms");
                    await Task.Delay(100);
                }

                try
                {
                    if (duringExecution != null)
                        await duringExecution(process.Id);
                    fGotToEnd = true;
                    Logger.logger.Log($"Sending 'exit' to subprocess stdin");
                    subprocesssStdIn.WriteLine("exit");
                    subprocesssStdIn.Close();
                    while (!process.WaitForExit(5000))
                    {
                        Logger.logger.Log("Subprocess didn't exit in 5 seconds!");
                    }
                    Logger.logger.Log($"SubProcess exited - Exit code: {process.ExitCode}");
                }
                catch (Exception e)
                {
                    Logger.logger.Log(e.ToString());
                    Logger.logger.Log($"Calling process.Kill()");
                    process.Kill();
                    fSuccess=false;
                }
                finally
                {
                    Logger.logger.Log($"Subprocess stdout: {stdoutSb.ToString()}");
                    Logger.logger.Log($"Subprocess stderr: {stderrSb.ToString()}");
                }


                if (afterExecution != null)
                    await afterExecution();
            }

            return fSuccess;
        }

        public static void Assert(bool predicate, string message = "")
        {
            if (!predicate)
                throw new Exception(message);
        }
    }

    public class IpcHeader
    {
        IpcHeader() { }

        public IpcHeader(byte commandSet, byte commandId)
        {
            CommandSet = (byte)commandSet;
            CommandId = commandId;
        }

        // the number of bytes for the DiagnosticsIpc::IpcHeader type in native code
        public static readonly UInt16 HeaderSizeInBytes = 20;
        private static readonly UInt16 MagicSizeInBytes = 14;

        public byte[] Magic = DotnetIpcV1; // byte[14] in native code
        public UInt16 Size = HeaderSizeInBytes;
        public byte CommandSet;
        public byte CommandId;
        public UInt16 Reserved = 0x0000;


        // Helper expression to quickly get V1 magic string for comparison
        // should be 14 bytes long
        public static byte[] DotnetIpcV1 => Encoding.ASCII.GetBytes("DOTNET_IPC_V1" + '\0');

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                Debug.Assert(Magic.Length == MagicSizeInBytes);
                writer.Write(Size);
                writer.Write(CommandSet);
                writer.Write(CommandId);
                writer.Write((UInt16)0x0000);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static IpcHeader TryParse(BinaryReader reader)
        {
            IpcHeader header = new IpcHeader
            {
                Magic = reader.ReadBytes(MagicSizeInBytes),
                Size = reader.ReadUInt16(),
                CommandSet = reader.ReadByte(),
                CommandId = reader.ReadByte(),
                Reserved = reader.ReadUInt16()
            };

            return header;
        }

        override public string ToString()
        {
            return $"{{ Magic={Magic}; Size={Size}; CommandSet={CommandSet}; CommandId={CommandId}; Reserved={Reserved} }}";
        }
    }

    public class IpcMessage
    {
        public IpcMessage()
        { }

        public IpcMessage(IpcHeader header, byte[] payload)
        {
            Payload = payload;
            Header = header;
        }

        public IpcMessage(byte commandSet, byte commandId, byte[] payload = null)
        : this(new IpcHeader(commandSet, commandId), payload)
        {
        }

        public byte[] Payload { get; private set; } = null;
        public IpcHeader Header { get; private set; } = default;

        public byte[] Serialize()
        { 
            byte[] serializedData = null;
            // Verify things will fit in the size capacity
            Header.Size = checked((UInt16)(IpcHeader.HeaderSizeInBytes + (Payload?.Length ?? 0))); ;
            byte[] headerBytes = Header.Serialize();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(headerBytes);
                if (Payload != null)
                    writer.Write(Payload);
                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static IpcMessage Parse(Stream stream)
        {
            IpcMessage message = new IpcMessage();
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                message.Header = IpcHeader.TryParse(reader);
                message.Payload = reader.ReadBytes(message.Header.Size - IpcHeader.HeaderSizeInBytes);
                return message;
            }
        }

        override public string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{{ Header={Header.ToString()}; ");
            if (Payload != null)
            {
                sb.Append("Payload=[ ");
                foreach (byte b in Payload)
                    sb.Append($"0x{b:X2} ");
                sb.Append(" ]");
            }
            sb.Append("}");

            return sb.ToString();
        }
    }

    public class IpcClient
    {
        public static IpcMessage SendMessage(Stream stream, IpcMessage message)
        {
            using (stream)
            {
                Write(stream, message);
                return Read(stream);
            }
        }

        public static Stream SendMessage(Stream stream, IpcMessage message, out IpcMessage response)
        {
            Write(stream, message);
            response = Read(stream);
            return stream;
        }

        private static void Write(Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        private static void Write(Stream stream, IpcMessage message)
        {
            Write(stream, message.Serialize());
        }

        private static IpcMessage Read(Stream stream)
        {
            return IpcMessage.Parse(stream);
        }
    }

    public class ConnectionHelper
    {
        private static string IpcRootPath { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\\.\pipe\" : Path.GetTempPath();
        public static Stream GetStandardTransport(int processId)
        {
            try 
            {
                var process = Process.GetProcessById(processId);
            }
            catch (System.ArgumentException)
            {
                throw new Exception($"Process {processId} is not running.");
            }
            catch (System.InvalidOperationException)
            {
                throw new Exception($"Process {processId} seems to be elevated.");
            }
 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string pipeName = $"dotnet-diagnostic-{processId}";
                var namedPipe = new NamedPipeClientStream(
                    ".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
                namedPipe.Connect(3);
                return namedPipe;
            }
            else
            {
                string ipcPort;
                try
                {
                    ipcPort = Directory.GetFiles(IpcRootPath, $"dotnet-diagnostic-{processId}-*-socket") // Try best match.
                                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                                .FirstOrDefault();
                    if (ipcPort == null)
                    {
                        throw new Exception($"Process {processId} not running compatible .NET Core runtime.");
                    }
                }
                catch (InvalidOperationException)
                {
                    throw new Exception($"Process {processId} not running compatible .NET Core runtime.");
                }
                string path = Path.Combine(IpcRootPath, ipcPort);
                var remoteEP = new UnixDomainSocketEndPoint(path);

                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(remoteEP);
                return new NetworkStream(socket, ownsSocket: true);
            }
        }
    }
}
