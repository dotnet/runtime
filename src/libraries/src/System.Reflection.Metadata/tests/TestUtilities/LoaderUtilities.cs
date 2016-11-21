// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Reflection.PortableExecutable;
using Xunit;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Tests
{
    internal unsafe static class LoaderUtilities
    {
        public static void LoadPEAndValidate(byte[] peImage, Action<PEReader> validator, bool useStream = false)
        {
            using (var tempFile = new TempFile(Path.GetTempFileName()))
            {
                File.WriteAllBytes(tempFile.Path, peImage);

                using (SafeLibraryHandle libHandle = global::Interop.Kernel32.LoadLibraryExW(tempFile.Path, IntPtr.Zero, 0))
                {
                    byte* peImagePtr = (byte*)global::Interop.Kernel32.GetModuleHandle(Path.GetFileName(tempFile.Path));

                    Assert.True(peImagePtr != null);
                    Assert.Equal('M', (char)peImagePtr[0]);
                    Assert.Equal('Z', (char)peImagePtr[1]);

                    using (var peReader = useStream ?
                        new PEReader(new ReadOnlyUnmanagedMemoryStream(peImagePtr, int.MaxValue), PEStreamOptions.IsLoadedImage) :
                        new PEReader(peImagePtr, int.MaxValue, isLoadedImage: true))
                    {
                        validator(peReader);
                    }
                }
            }
        }
    }
}
