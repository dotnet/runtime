// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Net.Primitives.Tests
{
    public static class IPAddressPerformanceTests
    {
        public static readonly object[][] TestAddresses =
        {
            new object[] { new byte[] { 0x8f, 0x18, 0x14, 0x24 } },
            new object[] { new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 } },
        };

        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void TryWriteBytes(byte[] address)
        {
            var ip = new IPAddress(address);
            var bytes = new byte[address.Length];
            var bytesSpan = new Span<byte>(bytes);
            int bytesWritten = 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        ip.TryWriteBytes(bytesSpan, out bytesWritten); ip.TryWriteBytes(bytesSpan, out bytesWritten);
                        ip.TryWriteBytes(bytesSpan, out bytesWritten); ip.TryWriteBytes(bytesSpan, out bytesWritten);
                        ip.TryWriteBytes(bytesSpan, out bytesWritten); ip.TryWriteBytes(bytesSpan, out bytesWritten);
                        ip.TryWriteBytes(bytesSpan, out bytesWritten); ip.TryWriteBytes(bytesSpan, out bytesWritten);
                    }
                }
            }
        }

        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void GetAddressBytes(byte[] address)
        {
            var ip = new IPAddress(address);
            byte[] bytes;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        bytes = ip.GetAddressBytes(); bytes = ip.GetAddressBytes();
                        bytes = ip.GetAddressBytes(); bytes = ip.GetAddressBytes();
                        bytes = ip.GetAddressBytes(); bytes = ip.GetAddressBytes();
                    }
                }
            }
        }

        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void Ctor_Bytes(byte[] address)
        {
            IPAddress ip;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        ip = new IPAddress(address); ip = new IPAddress(address);
                        ip = new IPAddress(address); ip = new IPAddress(address);
                        ip = new IPAddress(address); ip = new IPAddress(address);
                        ip = new IPAddress(address); ip = new IPAddress(address);
                    }
                }
            }
        }
 
        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void Ctor_Span(byte[] address)
        {
            var span = new ReadOnlySpan<byte>(address);
            IPAddress ip;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        ip = new IPAddress(span); ip = new IPAddress(span);
                        ip = new IPAddress(span); ip = new IPAddress(span);
                        ip = new IPAddress(span); ip = new IPAddress(span);
                        ip = new IPAddress(span); ip = new IPAddress(span);
                    }
                }
            }
        }

        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void TryFormat(byte[] address)
        {
            const int INET6_ADDRSTRLEN = 65;
            var buffer = new char[INET6_ADDRSTRLEN];
            var result = new Span<char>(buffer);
            int charsWritten;

            var ip = new IPAddress(address);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        ip.TryFormat(result, out charsWritten);
                    }
                }
            }
        }
 
        [Benchmark]
        [MeasureGCCounts]
        [MemberData(nameof(TestAddresses))]
        public static void ToString(byte[] address)
        {
            var ip = new IPAddress(address);

            string result;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; ++i)
                    {
                        result = ip.ToString(); result = ip.ToString();
                        result = ip.ToString(); result = ip.ToString();
                        result = ip.ToString(); result = ip.ToString();
                        result = ip.ToString(); result = ip.ToString();
                        result = ip.ToString(); result = ip.ToString();
                        result = ip.ToString(); result = ip.ToString();
                    }
                }
            }
        }
    }
}
