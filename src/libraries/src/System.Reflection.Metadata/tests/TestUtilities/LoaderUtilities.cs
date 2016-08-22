// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Reflection.PortableExecutable;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    internal unsafe static class LoaderUtilities
    {
        public static void LoadPEAndValidate(byte[] peImage, Action<PEReader> validator)
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, peImage);

            using (SafeLibraryHandle libHandle = global::Interop.mincore.LoadLibraryExW(tempFile, IntPtr.Zero, 0))
            {
                byte* peImagePtr = (byte*)global::Interop.mincore.GetModuleHandle(Path.GetFileName(tempFile));

                Assert.True(peImagePtr != null);
                Assert.Equal('M', (char)peImagePtr[0]);
                Assert.Equal('Z', (char)peImagePtr[1]);

                using (var peReader = new PEReader(peImagePtr, int.MaxValue, isLoadedImage: true))
                {
                    validator(peReader);
                }
            }

            File.Delete(tempFile);
        }
    }
}
