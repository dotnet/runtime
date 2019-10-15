// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public partial class PipeResetTests : IDisposable
    {
        [Fact]
        public async Task LengthIsReseted()
        {
           var source = new byte[] { 1, 2, 3 };

           await _pipe.Writer.WriteAsync(source);

           _pipe.Reader.Complete();
           _pipe.Writer.Complete();

           _pipe.Reset();

           Assert.Equal(0, _pipe.Length);
        }
    }
}
