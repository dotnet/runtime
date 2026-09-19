// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Common.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class GetPwUidTests
    {
        [Fact]
        public void ShouldRetryGetUserNameFromPasswd_UserNotFound_ReturnsFalse()
        {
            Assert.False(Interop.Sys.ShouldRetryGetUserNameFromPasswd(-1));
        }

        [Fact]
        public void ShouldRetryGetUserNameFromPasswd_BufferTooSmall_ReturnsTrue()
        {
            int error = Interop.Error.ERANGE.Info().RawErrno;

            Assert.True(Interop.Sys.ShouldRetryGetUserNameFromPasswd(error));
        }

        [Fact]
        public void ShouldRetryGetUserNameFromPasswd_UnexpectedError_ReturnsFalse()
        {
            int error = Interop.Error.EIO.Info().RawErrno;

            Assert.False(Interop.Sys.ShouldRetryGetUserNameFromPasswd(error));
        }
    }
}
