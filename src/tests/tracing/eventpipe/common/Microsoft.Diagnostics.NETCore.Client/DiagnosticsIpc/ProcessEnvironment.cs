// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal class ProcessEnvironmentHelper
    {
        private const int CopyBufferSize = (16 << 10) /* 16KiB */;

        private ProcessEnvironmentHelper() {}
        public static ProcessEnvironmentHelper Parse(byte[] payload)
        {
            ProcessEnvironmentHelper helper = new ProcessEnvironmentHelper();

            helper.ExpectedSizeInBytes = BitConverter.ToUInt32(payload, 0);
            helper.Future = BitConverter.ToUInt16(payload, 4);

            return helper;
        }

        public Dictionary<string, string> ReadEnvironment(Stream continuation)
        {
            using var memoryStream = new MemoryStream();
            continuation.CopyTo(memoryStream, CopyBufferSize);
            return ReadEnvironmentCore(memoryStream);
        }

        public async Task<Dictionary<string,string>> ReadEnvironmentAsync(Stream continuation, CancellationToken token = default(CancellationToken))
        {
            using var memoryStream = new MemoryStream();
            await continuation.CopyToAsync(memoryStream, CopyBufferSize, token);
            return ReadEnvironmentCore(memoryStream);
        }

        private Dictionary<string, string> ReadEnvironmentCore(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] envBlock = stream.ToArray();

            if (envBlock.Length != (long)ExpectedSizeInBytes)
                throw new ApplicationException($"ProcessEnvironment continuation length did not match expected length. Expected: {ExpectedSizeInBytes} bytes, Received: {envBlock.Length} bytes");

            var env = new Dictionary<string, string>();
            int cursor = 0;
            UInt32 nElements = BitConverter.ToUInt32(envBlock, cursor);
            cursor += sizeof(UInt32);
            while (cursor < envBlock.Length)
            {
                string pair = IpcHelpers.ReadString(envBlock, ref cursor);
                int equalsIdx = pair.IndexOf('=');
                env[pair.Substring(0, equalsIdx)] = equalsIdx != pair.Length - 1 ? pair.Substring(equalsIdx + 1) : "";
            }

            return env;
        }


        private UInt32 ExpectedSizeInBytes { get; set; }
        private UInt16 Future { get; set; }
    }
}