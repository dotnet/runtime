// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Runtime.Loader.Tests
{
    public class TpaLoadFailureTest
    {
        private static string GetAssemblyPath(string assemblyFileName)
        {
            string appDir = Path.GetDirectoryName(AssemblyPathHelper.GetAssemblyLocation(typeof(TpaLoadFailureTest).Assembly));
            return Path.Combine(appDir, assemblyFileName);
        }

        // Ensure the DLL is not present on disk. The assembly is still listed in deps.json
        // so the host adds it to the TPA list, but the physical file is absent.
        private static void EnsureAssemblyRemoved(string dllPath)
        {
            if (File.Exists(dllPath))
                File.Delete(dllPath);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseMissingAssembly() => global::BindFailureTest.Missing.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseLockedAssembly() => global::BindFailureTest.Locked.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseCorruptAssembly() => global::BindFailureTest.Corrupt.TestClass.GetMessage();

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles))]
        public void NotFound_ExceptionContainsAssemblyPath()
        {
            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Missing.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            EnsureAssemblyRemoved(dllPath);

            var ex = Assert.Throws<FileNotFoundException>(() => UseMissingAssembly());
            Assert.Contains("System.Runtime.Loader.Test.BindFailure.Missing", ex.FusionLog);
            Assert.Contains(dllPath, ex.FusionLog);
            Assert.Contains(HResults.COR_E_FILENOTFOUND.ToString("X8"), ex.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.HasAssemblyFiles))]
        public void SharingViolation_ExceptionContainsPathAndHResult()
        {
            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Locked.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            Assert.True(File.Exists(dllPath), $"Test assembly not found at {dllPath}");

            using FileStream _ = new FileStream(dllPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var ex = Assert.Throws<FileNotFoundException>(() => UseLockedAssembly());
            Assert.Contains(dllPath, ex.FusionLog);
            Assert.Contains(HResults.ERROR_SHARING_VIOLATION.ToString("X8"), ex.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles))]
        public void Corrupt_ExceptionContainsPathAndHResult()
        {
            const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);

            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Corrupt.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            EnsureAssemblyRemoved(dllPath);

            try
            {
                File.WriteAllBytes(dllPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00 });

                var ex = Assert.Throws<FileNotFoundException>(() => UseCorruptAssembly());
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
