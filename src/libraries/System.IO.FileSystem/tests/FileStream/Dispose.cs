// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Dispose : FileSystemTest
    {
        [Fact]
        public void DisposeClosesHandle()
        {
            SafeFileHandle handle;
            using(FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                handle = fs.SafeFileHandle;
            }

            Assert.True(handle.IsClosed);
        }

        private class MyFileStream : FileStream
        {
            public MyFileStream(string path, FileMode mode)
                : base(path, mode)
            { }

            public MyFileStream(SafeFileHandle handle, FileAccess access, Action<bool> disposeMethod) : base(handle, access)
            {
                DisposeMethod = disposeMethod;
            }

            public Action<bool> DisposeMethod { get; set; }

            protected override void Dispose(bool disposing)
            {
                Action<bool> disposeMethod = DisposeMethod;

                if (disposeMethod != null)
                    disposeMethod(disposing);

                base.Dispose(disposing);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Dispose_CallsVirtualDisposeTrueArg_ThrowsDuringFlushWriteBuffer_DisposeThrows()
        {
            RemoteExecutor.Invoke(() =>
            {
                string fileName = GetTestFilePath();
                using (FileStream fscreate = new FileStream(fileName, FileMode.Create))
                {
                    fscreate.WriteByte(0);
                }
                bool writeDisposeInvoked = false;
                Action<bool> writeDisposeMethod = _ => writeDisposeInvoked = true;
                using (var fsread = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    Action act = () => // separate method to avoid JIT lifetime-extension issues
                    {
                        using (var fswrite = new MyFileStream(fsread.SafeFileHandle, FileAccess.Write, writeDisposeMethod))
                        {
                            fswrite.WriteByte(0);

                            // Normal dispose should call Dispose(true). Throws due to FS trying to flush write buffer
                            Assert.Throws<UnauthorizedAccessException>(() => fswrite.Dispose());
                            Assert.True(writeDisposeInvoked, "Expected Dispose(true) to be called from Dispose()");
                            writeDisposeInvoked = false;

                            // Only throws on first Dispose call
                            fswrite.Dispose();
                            Assert.True(writeDisposeInvoked, "Expected Dispose(true) to be called from Dispose()");
                            writeDisposeInvoked = false;
                        }
                        Assert.True(writeDisposeInvoked, "Expected Dispose(true) to be called from Dispose() again");
                        writeDisposeInvoked = false;
                    };
                    act();

                    for (int i = 0; i < 2; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    Assert.False(writeDisposeInvoked, "Expected finalizer to have been suppressed");
                }
            }).Dispose();
        }

        private static bool IsPreciseGcSupportedAndRemoteExecutorSupported => PlatformDetection.IsPreciseGcSupported && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(IsPreciseGcSupportedAndRemoteExecutorSupported))]
        public void NoDispose_CallsVirtualDisposeFalseArg_ThrowsDuringFlushWriteBuffer_FinalizerWontThrow()
        {
            RemoteExecutor.Invoke(() =>
            {
                string fileName = GetTestFilePath();
                using (FileStream fscreate = new FileStream(fileName, FileMode.Create))
                {
                    fscreate.WriteByte(0);
                }
                bool writeDisposeInvoked = false;
                Action<bool> writeDisposeMethod = (disposing) =>
                {
                    writeDisposeInvoked = true;
                    Assert.False(disposing, "Expected false arg to Dispose(bool)");
                };
                using (var fsread = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    Action act = () => // separate method to avoid JIT lifetime-extension issues
                    {
                        var fswrite = new MyFileStream(fsread.SafeFileHandle, FileAccess.Write, writeDisposeMethod);
                        fswrite.WriteByte(0);
                    };
                    act();

                    // Dispose is not getting called here.
                    // instead, make sure finalizer gets called and doesnt throw exception
                    for (int i = 0; i < 2; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    Assert.True(writeDisposeInvoked, "Expected finalizer to be invoked but not throw exception");
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void Dispose_CallsVirtualDispose_TrueArg()
        {
            bool disposeInvoked = false;

            Action act = () => // separate method to avoid JIT lifetime-extension issues
            {
                using (MyFileStream fs = new MyFileStream(GetTestFilePath(), FileMode.Create))
                {
                    fs.DisposeMethod = (disposing) =>
                    {
                        disposeInvoked = true;
                        Assert.True(disposing, "Expected true arg to Dispose(bool)");
                    };

                    // Normal dispose should call Dispose(true)
                    fs.Dispose();
                    Assert.True(disposeInvoked, "Expected Dispose(true) to be called from Dispose()");

                    disposeInvoked = false;
                }

                // Second dispose leaving the using should still call dispose
                Assert.True(disposeInvoked, "Expected Dispose(true) to be called from Dispose() again");
                disposeInvoked = false;
            };
            act();

            // Make sure we suppressed finalization
            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.False(disposeInvoked, "Expected finalizer to have been suppressed");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void Finalizer_CallsVirtualDispose_FalseArg()
        {
            bool disposeInvoked = false;

            Action act = () => // separate method to avoid JIT lifetime-extension issues
            {
                new MyFileStream(GetTestFilePath(), FileMode.Create)
                {
                    DisposeMethod = (disposing) =>
                    {
                        disposeInvoked = true;
                        Assert.False(disposing, "Expected false arg to Dispose(bool)");
                    }
                };
            };
            act();

            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.True(disposeInvoked, "Expected finalizer to be invoked and set called");
        }

        [Fact]
        public void DisposeFlushesWriteBuffer()
        {
            string fileName = GetTestFilePath();
            using(FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(buffer.Length, fs.Length);
                fs.Read(buffer, 0, buffer.Length);
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void FinalizeFlushesWriteBuffer()
        {
            string fileName = GetTestFilePath();

            // use a separate method to be sure that fs isn't rooted at time of GC.
            Action leakFs = () =>
            {
                // we must specify useAsync:false, otherwise the finalizer just kicks off an async write.
                FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: false);
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs = null;
            };
            leakFs();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (FileStream fsr = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(buffer.Length, fsr.Length);
                fsr.Read(buffer, 0, buffer.Length);
                Assert.Equal(TestBuffer, buffer);
            }
        }
    }
}
