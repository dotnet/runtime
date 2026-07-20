// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Runtime.Loader.Tests
{
    public class TpaLoadFailureTest
    {
        private static string GetAssemblyPath(string assemblyName)
        {
            string appDir = Path.GetDirectoryName(AssemblyPathHelper.GetAssemblyLocation(typeof(TpaLoadFailureTest).Assembly));
            return Path.Combine(appDir, assemblyName + ".dll");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseMissingAssembly() => BindFailureTest.Missing.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseLockedAssembly() => BindFailureTest.Locked.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseCorruptAssembly() => BindFailureTest.Corrupt.TestClass.GetMessage();

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles), nameof(PlatformDetection.IsNotMobile))]
        public void NotFound_ExceptionContainsAssemblyPath()
        {
            // The Missing assembly is listed in deps.json (so the host adds it to the TPA
            // list) but deleted from the output directory by the RemoveBindFailureTestAssemblies
            // MSBuild target, so it will not be found at runtime.
            const string assemblyName = "System.Runtime.Loader.Test.BindFailure.Missing";
            string dllPath = GetAssemblyPath(assemblyName);
            Assert.False(File.Exists(dllPath), $"Test assembly should not be present at {dllPath}");

            var ex = Assert.Throws<FileNotFoundException>(() => UseMissingAssembly());
            Assert.NotNull(ex.FileName);
            Assert.Contains(assemblyName, ex.FileName);
            Assert.NotNull(ex.FusionLog);
            Assert.Contains(dllPath, ex.FusionLog);
            Assert.Contains(HResults.COR_E_FILENOTFOUND.ToString("X8"), ex.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.HasAssemblyFiles))]
        public void SharingViolation_ExceptionContainsPathAndHResult()
        {
            // The Locked assembly is listed in deps.json but deleted from the output
            // directory by the RemoveBindFailureTestAssemblies MSBuild target.
            // We write a file and lock it before the load attempt.
            const string assemblyName = "System.Runtime.Loader.Test.BindFailure.Locked";
            string dllPath = GetAssemblyPath(assemblyName);
            Assert.False(File.Exists(dllPath), $"Test assembly should not be present at {dllPath}");

            try
            {
                File.WriteAllBytes(dllPath, new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

                using FileStream _ = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.None);

                var ex = Assert.Throws<FileNotFoundException>(() => UseLockedAssembly());
                Assert.NotNull(ex.FileName);
                Assert.Contains(assemblyName, ex.FileName);
                Assert.NotNull(ex.FusionLog);
                Assert.Contains(dllPath, ex.FusionLog);
                Assert.Contains(HResults.ERROR_SHARING_VIOLATION.ToString("X8"), ex.FusionLog);
            }
            finally
            {
                File.Delete(dllPath);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles), nameof(PlatformDetection.IsNotMobile))]
        public void Corrupt_ExceptionContainsPathAndHResult()
        {
            const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);

            // The Corrupt assembly is listed in deps.json but deleted from the output
            // directory by the RemoveBindFailureTestAssemblies MSBuild target.
            // We write a corrupt file in its place before the load attempt.
            const string assemblyName = "System.Runtime.Loader.Test.BindFailure.Corrupt";
            string dllPath = GetAssemblyPath(assemblyName);
            Assert.False(File.Exists(dllPath), $"Test assembly should not be present at {dllPath}");

            try
            {
                File.WriteAllBytes(dllPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00 });

                var ex = Assert.Throws<FileNotFoundException>(() => UseCorruptAssembly());
                Assert.NotNull(ex.FileName);
                Assert.Contains(assemblyName, ex.FileName);
                Assert.NotNull(ex.FusionLog);
                Assert.Contains(dllPath, ex.FusionLog);
                Assert.Contains(COR_E_ASSEMBLYEXPECTED.ToString("X8"), ex.FusionLog);
            }
            finally
            {
                File.Delete(dllPath);
            }
        }
    }
}
