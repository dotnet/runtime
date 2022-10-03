// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    // Run these tests on their own as they use a lot of disk space
    [Collection(nameof(DisableParallelization))]
    public partial class StreamReaderTests_Serial : FileCleanupTestBase
    {
        [OuterLoop("It creates 1GB file")]
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public async Task ReadToEndAsync_WithCancellation()
        {
            string path = GetTestFilePath();
            CreateLargeFile(path);

            using StreamReader reader = File.OpenText(path);
            using CancellationTokenSource cts = new();
            var token = cts.Token;

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                Task<string> readToEndTask = reader.ReadToEndAsync(token);

                // This is a time-sensitive test where the cancellation needs to happen before the async read completes.
                // A sleep may be too long a delay, so spin-wait for a very short duration before canceling.
                SpinWait spinner = default;
                while (!spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce(sleep1Threshold: -1);
                }

                cts.Cancel();
                await readToEndTask;
            });
            Assert.Equal(token, ex.CancellationToken);
        }

        private void CreateLargeFile(string path)
        {
            const string sentence = "A very large file used for testing StreamReader cancellation. 0123456789012345678901234567890123456789.";
            const int repeatCount = 10_000_000;
            Encoding encoding = Encoding.UTF8;

            using FileStream fs = File.OpenWrite(path);
            long fileSize = encoding.GetByteCount(sentence) * repeatCount;

            try
            {
                fs.SetLength(fileSize);
            }
            catch (IOException)
            {
                throw new SkipTestException($"Unable to run {ReadToEndAsync_WithCancellation} due to lack of available disk space");
            }

            using StreamWriter streamWriter = new StreamWriter(fs, encoding);
            for (int i = 0; i < repeatCount; i++)
            {
                streamWriter.WriteLine(sentence);
            }
        }
    }
}
