// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using TestLibrary;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests.IpcProtocolValidation
{
    public class IpcProtocolValidation
    {
        private const uint DiagnosticsIpcError_BadEncoding = 0x80131384;

        /// <summary>
        /// Validates that the diagnostic server properly rejects an IPC AttachProfiler command
        /// when client_data_len exceeds the actual remaining payload bytes.
        /// This tests the validation at line 73 in ds-profiler-protocol.c:
        ///     !(buffer_cursor_len >= instance->client_data_len)
        /// </summary>
        [ActiveIssue("IPC tracing tests are not supported", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static void AttachProfilerCommand_TruncatedClientData_ReturnsError()
        {
            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            Logger.logger.Log($"Test PID: {pid}");

            // Construct a malformed AttachProfiler command payload where client_data_len
            // claims more data than actually exists in the remaining buffer.
            //
            // AttachProfiler payload structure:
            //   uint32_t attach_timeout
            //   GUID profiler_guid (16 bytes)
            //   string profiler_path (uint32_t length + UTF-16 chars)
            //   uint32_t client_data_len
            //   uint8_t[] client_data (client_data_len bytes)
            //
            // We will set client_data_len to a large value but provide zero actual bytes.

            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // attach_timeout (4 bytes)
                writer.Write((uint)1000);

                // profiler_guid (16 bytes) - using a fake GUID
                writer.Write(Guid.NewGuid().ToByteArray());

                // profiler_path - UTF-16 string with length prefix
                // Format: uint32_t char_count followed by UTF-16 encoded string (including null terminator)
                string profilerPath = "C:\\fake\\profiler.dll";
                int charCount = profilerPath.Length + 1; // include null terminator
                writer.Write((uint)charCount);
                writer.Write(Encoding.Unicode.GetBytes(profilerPath + '\0'));

                // client_data_len - claim we have 1000 bytes of client data
                // but don't actually include any bytes after this
                writer.Write((uint)1000);

                // Intentionally do NOT write any client_data bytes
                // This creates a truncated payload where client_data_len > remaining buffer

                writer.Flush();
                payload = stream.ToArray();
            }

            Logger.logger.Log($"Payload size: {payload.Length} bytes");
            Logger.logger.Log($"Payload claims client_data_len of 1000 but provides 0 bytes");

            // Send the malformed AttachProfiler command
            // CommandSet = 0x03 (Profiler), CommandId = 0x01 (AttachProfiler)
            Stream transport = ConnectionHelper.GetStandardTransport(pid);
            var message = new IpcMessage(0x03, 0x01, payload);
            Logger.logger.Log($"Sending malformed AttachProfiler command...");
            IpcMessage response = IpcClient.SendMessage(transport, message);
            Logger.logger.Log($"Received response: CommandSet=0x{response.Header.CommandSet:X2}, CommandId=0x{response.Header.CommandId:X2}");

            // Validate that the server returns an error response
            // Error responses have CommandSet = 0xFF and CommandId = 0xFF
            Utils.Assert(response.Header.CommandSet == 0xFF,
                $"Response CommandSet must indicate error (0xFF). Received: 0x{response.Header.CommandSet:X2}");
            Utils.Assert(response.Header.CommandId == 0xFF,
                $"Response CommandId must indicate error (0xFF). Received: 0x{response.Header.CommandId:X2}");

            // Parse the error code from the payload
            // Error response payload: uint32_t error_code
            Utils.Assert(response.Payload.Length >= 4,
                $"Error response payload must contain at least 4 bytes for error code. Received: {response.Payload.Length} bytes");

            uint errorCode = BitConverter.ToUInt32(response.Payload, 0);
            Logger.logger.Log($"Error code: 0x{errorCode:X8}");

            // Validate it's a BadEncoding error (DS_IPC_E_BAD_ENCODING = 0x80131384)
            Utils.Assert(errorCode == DiagnosticsIpcError_BadEncoding,
                $"Error code must be DS_IPC_E_BAD_ENCODING (0x{DiagnosticsIpcError_BadEncoding:X8}). Received: 0x{errorCode:X8}");

            Logger.logger.Log("Test PASSED: Diagnostic server correctly rejected truncated AttachProfiler payload");
        }

        /// <summary>
        /// Validates that the diagnostic server properly rejects an IPC StartupProfiler command
        /// when the profiler_path string is truncated (string_len > actual remaining bytes).
        /// </summary>
        [ActiveIssue("IPC tracing tests are not supported", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static void StartupProfilerCommand_TruncatedString_ReturnsError()
        {
            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            Logger.logger.Log($"Test PID: {pid}");

            // Construct a malformed StartupProfiler command payload where profiler_path
            // string length claims more characters than actually exist.
            //
            // StartupProfiler payload structure:
            //   GUID profiler_guid (16 bytes)
            //   string profiler_path (uint32_t length + UTF-16 chars)
            //
            // We will set the string length to a large value but provide fewer actual characters.

            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // profiler_guid (16 bytes) - using a fake GUID
                writer.Write(Guid.NewGuid().ToByteArray());

                // profiler_path - claim length is 1000 chars but only provide a few bytes
                writer.Write((uint)1000); // claim 1000 characters
                writer.Write(Encoding.Unicode.GetBytes("AB")); // but only provide 2 characters (4 bytes)

                writer.Flush();
                payload = stream.ToArray();
            }

            Logger.logger.Log($"Payload size: {payload.Length} bytes");
            Logger.logger.Log($"Payload claims profiler_path has 1000 chars but provides only 2");

            // Send the malformed StartupProfiler command
            // CommandSet = 0x03 (Profiler), CommandId = 0x02 (StartupProfiler)
            Stream transport = ConnectionHelper.GetStandardTransport(pid);
            var message = new IpcMessage(0x03, 0x02, payload);
            Logger.logger.Log($"Sending malformed StartupProfiler command...");
            IpcMessage response = IpcClient.SendMessage(transport, message);
            Logger.logger.Log($"Received response: CommandSet=0x{response.Header.CommandSet:X2}, CommandId=0x{response.Header.CommandId:X2}");

            // Validate that the server returns an error response
            Utils.Assert(response.Header.CommandSet == 0xFF,
                $"Response CommandSet must indicate error (0xFF). Received: 0x{response.Header.CommandSet:X2}");
            Utils.Assert(response.Header.CommandId == 0xFF,
                $"Response CommandId must indicate error (0xFF). Received: 0x{response.Header.CommandId:X2}");

            // Parse the error code from the payload
            Utils.Assert(response.Payload.Length >= 4,
                $"Error response payload must contain at least 4 bytes for error code. Received: {response.Payload.Length} bytes");

            uint errorCode = BitConverter.ToUInt32(response.Payload, 0);
            Logger.logger.Log($"Error code: 0x{errorCode:X8}");

            // Validate it's a BadEncoding error
            Utils.Assert(errorCode == DiagnosticsIpcError_BadEncoding,
                $"Error code must be DS_IPC_E_BAD_ENCODING (0x{DiagnosticsIpcError_BadEncoding:X8}). Received: 0x{errorCode:X8}");

            Logger.logger.Log("Test PASSED: Diagnostic server correctly rejected truncated StartupProfiler payload");
        }

        /// <summary>
        /// Validates that the diagnostic server properly rejects an IPC AttachProfiler command
        /// when the profiler_guid field is truncated (not enough bytes for a GUID).
        /// </summary>
        [ActiveIssue("IPC tracing tests are not supported", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static void AttachProfilerCommand_TruncatedGuid_ReturnsError()
        {
            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            Logger.logger.Log($"Test PID: {pid}");

            // Construct a malformed AttachProfiler command payload where the GUID is truncated.
            // Only provide 8 bytes instead of 16 for the GUID.

            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // attach_timeout (4 bytes)
                writer.Write((uint)1000);

                // Truncated profiler_guid - only 8 bytes instead of 16
                writer.Write((ulong)0x1234567890ABCDEF);

                // Don't write any more data - this creates a truncated payload

                writer.Flush();
                payload = stream.ToArray();
            }

            Logger.logger.Log($"Payload size: {payload.Length} bytes (expected at least 20 for timeout + guid)");
            Logger.logger.Log($"Payload truncated in profiler_guid field");

            // Send the malformed AttachProfiler command
            Stream transport = ConnectionHelper.GetStandardTransport(pid);
            var message = new IpcMessage(0x03, 0x01, payload);
            Logger.logger.Log($"Sending malformed AttachProfiler command (truncated GUID)...");
            IpcMessage response = IpcClient.SendMessage(transport, message);
            Logger.logger.Log($"Received response: CommandSet=0x{response.Header.CommandSet:X2}, CommandId=0x{response.Header.CommandId:X2}");

            // Validate that the server returns an error response
            Utils.Assert(response.Header.CommandSet == 0xFF,
                $"Response CommandSet must indicate error (0xFF). Received: 0x{response.Header.CommandSet:X2}");
            Utils.Assert(response.Header.CommandId == 0xFF,
                $"Response CommandId must indicate error (0xFF). Received: 0x{response.Header.CommandId:X2}");

            // Parse the error code from the payload
            Utils.Assert(response.Payload.Length >= 4,
                $"Error response payload must contain at least 4 bytes for error code. Received: {response.Payload.Length} bytes");

            uint errorCode = BitConverter.ToUInt32(response.Payload, 0);
            Logger.logger.Log($"Error code: 0x{errorCode:X8}");

            // Validate it's a BadEncoding error
            Utils.Assert(errorCode == DiagnosticsIpcError_BadEncoding,
                $"Error code must be DS_IPC_E_BAD_ENCODING (0x{DiagnosticsIpcError_BadEncoding:X8}). Received: 0x{errorCode:X8}");

            Logger.logger.Log("Test PASSED: Diagnostic server correctly rejected truncated AttachProfiler payload (GUID)");
        }
    }
}
