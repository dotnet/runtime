// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public partial class RandomAccess_Dup2 : FileSystemTest
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void WriteCanUseFileDescriptorAboveCurrentLimit()
        {
            RemoteExecutor.Invoke(() =>
            {
                const ulong MaximumFileDescriptorLimit = 4_096;

                Assert.Equal(0, Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out Interop.Sys.RLimit limits));

                limits.CurrentLimit = Math.Min(limits.CurrentLimit, MaximumFileDescriptorLimit);
                Assert.InRange(limits.CurrentLimit, 2UL, (ulong)int.MaxValue);
                Assert.Equal(0, Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, ref limits));

                int maxAllowedFileDescriptor = checked((int)limits.CurrentLimit - 1);
                string filePath = Path.GetTempFileName();

                try
                {
                    using (SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        Assert.Equal(maxAllowedFileDescriptor, dup2((int)fileHandle.DangerousGetHandle(), maxAllowedFileDescriptor));

                        limits.CurrentLimit--;
                        Assert.Equal(0, Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, ref limits));

                        using (SafeFileHandle duplicatedHandle = new SafeFileHandle(new IntPtr(maxAllowedFileDescriptor), ownsHandle: true))
                        {
                            RandomAccess.Write(duplicatedHandle, new byte[] { 1 }, fileOffset: 0);
                        }

                        byte[] buffer = new byte[1];
                        Assert.Equal(1, RandomAccess.Read(fileHandle, buffer, fileOffset: 0));
                        Assert.Equal(new byte[] { 1 }, buffer);
                    }
                }
                finally
                {
                    File.Delete(filePath);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int dup2(int oldFileDescriptor, int newFileDescriptor);
    }
}
