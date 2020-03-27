// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Extensions.CommandLineUtils
{
    public class DotNetMuxerTests
    {
        [Fact]
        public void FindsTheMuxer()
        {
            var muxerPath = DotNetMuxer.MuxerPath;
            Assert.NotNull(muxerPath);
            Assert.True(File.Exists(muxerPath), "The file did not exist");
            Assert.True(Path.IsPathRooted(muxerPath), "The path should be rooted");
            Assert.Equal("dotnet", Path.GetFileNameWithoutExtension(muxerPath), ignoreCase: true);
        }
    }
}
#endif
