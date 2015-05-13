// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FlushTests
    {
        [Fact]
        public static void Flush()
        {
            const int length = 1000;
            using (var manager = new UmsManager(FileAccess.Write, length))
            {
                UnmanagedMemoryStream stream = manager.Stream;

                stream.Flush();
                Assert.True(stream.FlushAsync(new CancellationToken(true)).IsCanceled);
                Assert.True(stream.FlushAsync().Status == TaskStatus.RanToCompletion);
            }

            using (var stream = new DerivedUnmanagedMemoryStream())
            {
                Assert.Throws<ObjectDisposedException>(() => stream.Flush());

                Task t = stream.FlushAsync();
                Assert.True(t.IsFaulted);
                Assert.IsType<ObjectDisposedException>(t.Exception.InnerException);
            }
        }

    }
}
