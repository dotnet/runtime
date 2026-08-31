// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Windows-specific tests for the constructors for NamedPipeClientStream
    /// </summary>
    public partial class NamedPipeTest_CreateClient
    {
        [Fact]
        public static void EmptyStringPipeName_Throws_ArgumentException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeClientStream(".", "", PipeAccessRights.FullControl, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void NullServerName_Throws_ArgumentNullException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", PipeAccessRights.FullControl, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void EmptyStringServerName_Throws_ArgumentException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new NamedPipeClientStream("", "client1", PipeAccessRights.FullControl, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void ReservedPipeName_Throws_ArgumentOutOfRangeException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(".", "anonymous", PipeAccessRights.FullControl, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Theory]
        [InlineData(0)]  // No bits set
        [InlineData(32)] // Invalid bit
        [InlineData(32 + (int)PipeAccessRights.ReadData)] // ReadData plus an invalid bit
        [InlineData(32 + (int)PipeAccessRights.WriteData)] // WriteData plus an invalid bit
        [InlineData((int)PipeAccessRights.WriteAttributes)] // Missing ReadData and WriteData (no direction can be determined)
        public static void InvalidPipeAccessRights_Throws_ArgumentOutOfRangeException(int rights)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("desiredAccessRights", () => new NamedPipeClientStream(".", "client1", (PipeAccessRights)rights, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void InvalidPipeOptions_Throws_ArgumentOutOfRangeException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeClientStream(".", "client1", PipeAccessRights.FullControl, (PipeOptions)255, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void InvalidImpersonationLevel_Throws_ArgumentOutOfRangeException_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("impersonationLevel", () => new NamedPipeClientStream(".", "client1", PipeAccessRights.FullControl, PipeOptions.None, (TokenImpersonationLevel)999, HandleInheritability.None));
        }

        [Fact]
        public static void NamedPipeClientStream_InvalidHandleInerhitability_WithAccessRights()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeAccessRights.FullControl, 0, TokenImpersonationLevel.Delegation, HandleInheritability.None - 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeAccessRights.FullControl, 0, TokenImpersonationLevel.Delegation, HandleInheritability.Inheritable + 1));
        }
    }
}
