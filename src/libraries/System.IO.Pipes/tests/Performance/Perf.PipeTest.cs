// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;
using System.Threading.Tasks;

namespace System.IO.Pipes.Tests
{
    public abstract class Perf_PipeTest : PipeTestBase
    {
        [Benchmark(Skip = "https://github.com/dotnet/corefx/issues/18290")]
        [InlineData(1000000)]
        [ActiveIssue(18290, TestPlatforms.AnyUnix)]
        public async Task ReadWrite(int size)
        {
            Random rand = new Random(314);
            byte[] sent = new byte[size];
            byte[] received = new byte[size];
            rand.NextBytes(sent);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (ServerClientPair pair = CreateServerClientPair())
                using (iteration.StartMeasurement())
                {
                    Task write = Task.Run(() => pair.writeablePipe.Write(sent, 0, sent.Length));
                    pair.readablePipe.Read(received, 0, size);
                    await write;
                }
            }
        }
    }
}
