// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using Xunit;
using System.Threading.Tasks;

namespace System.IO.Pipes.Tests
{
    public abstract class Perf_PipeTest : PipeTestBase
    {
        [Benchmark]
        [InlineData(1000000)]
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
