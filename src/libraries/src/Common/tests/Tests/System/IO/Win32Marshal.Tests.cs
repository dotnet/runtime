// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tests.System.IO
{
    public class Win32MarshalTests
    {
        [Theory]
        [InlineData("", Interop.Errors.ERROR_ALREADY_EXISTS, "")]
        [InlineData("foo", Interop.Errors.ERROR_ALREADY_EXISTS, "IO_AlreadyExists_Name")]
        [InlineData("", Interop.Errors.ERROR_INVALID_PARAMETER, "")]
        [InlineData("foo", Interop.Errors.ERROR_INVALID_PARAMETER, "")]
        [InlineData("", Interop.Errors.ERROR_SHARING_VIOLATION, "IO_SharingViolation_NoFileName")]
        [InlineData("foo", Interop.Errors.ERROR_SHARING_VIOLATION, "IO_SharingViolation_File")]
        [InlineData("", Interop.Errors.ERROR_FILE_EXISTS, "")]
        [InlineData("foo", Interop.Errors.ERROR_FILE_EXISTS, "IO_FileExists_Name")]
        // This is a random error we don't explicitly check
        [InlineData("", Interop.Errors.ERROR_INVALID_SID, "")]
        [InlineData("foo", Interop.Errors.ERROR_INVALID_SID, "")]
        public void IOExceptionErrors(string path, int errorCode, string error)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<IOException>(exception);
            Assert.Equal(Win32Marshal.MakeHRFromErrorCode(errorCode), exception.HResult);
            if (!string.IsNullOrEmpty(error))
            {
                Assert.StartsWith(error, exception.Message);
                if (!string.IsNullOrEmpty(path))
                {
                    Assert.EndsWith(path, exception.Message);
                }
            }
        }

        [Theory]
        [InlineData("", Interop.Errors.ERROR_FILE_NOT_FOUND, "IO_FileNotFound")]
        [InlineData("foo", Interop.Errors.ERROR_FILE_NOT_FOUND, "IO_FileNotFound_FileName")]
        public void FileNotFoundErrors(string path, int errorCode, string error)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<FileNotFoundException>(exception);

            Assert.StartsWith(error, exception.Message);
            if (!string.IsNullOrEmpty(path))
            {
                Assert.EndsWith(path, exception.Message);
            }
        }

        [Theory]
        [InlineData("", Interop.Errors.ERROR_PATH_NOT_FOUND, "IO_PathNotFound_NoPathName")]
        [InlineData("foo", Interop.Errors.ERROR_PATH_NOT_FOUND, "IO_PathNotFound_Path")]
        public void DirectoryNotFoundErrors(string path, int errorCode, string error)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<DirectoryNotFoundException>(exception);

            Assert.StartsWith(error, exception.Message);
            if (!string.IsNullOrEmpty(path))
            {
                Assert.EndsWith(path, exception.Message);
            }
        }

        [Theory]
        [InlineData("", Interop.Errors.ERROR_ACCESS_DENIED, "UnauthorizedAccess_IODenied_NoPathName")]
        [InlineData("foo", Interop.Errors.ERROR_ACCESS_DENIED, "UnauthorizedAccess_IODenied_Path")]
        public void UnauthorizedAccessErrors(string path, int errorCode, string error)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<UnauthorizedAccessException>(exception);

            Assert.StartsWith(error, exception.Message);
            if (!string.IsNullOrEmpty(path))
            {
                Assert.EndsWith(path, exception.Message);
            }
        }

        [Theory]
        [InlineData("", Interop.Errors.ERROR_FILENAME_EXCED_RANGE)]
        [InlineData("foo", Interop.Errors.ERROR_FILENAME_EXCED_RANGE)]
        public void PathTooLongErrors(string path, int errorCode)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<PathTooLongException>(exception);
            Assert.StartsWith("IO_PathTooLong", exception.Message);
        }

        [Theory]
        [InlineData("", Interop.Errors.ERROR_OPERATION_ABORTED)]
        [InlineData("foo", Interop.Errors.ERROR_OPERATION_ABORTED)]
        public void OperationCancelledErrors(string path, int errorCode)
        {
            var exception = Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            Assert.IsType<OperationCanceledException>(exception);
        }
    }
}
