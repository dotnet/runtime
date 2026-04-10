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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseMissingAssembly() => global::BindFailureTest.Missing.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseLockedAssembly() => global::BindFailureTest.Locked.TestClass.GetMessage();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string UseCorruptAssembly() => global::BindFailureTest.Corrupt.TestClass.GetMessage();

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles))]
        public void NotFound_ExceptionContainsAssemblyPath()
        {
            // The Missing assembly is referenced at compile time but not copied to the output
            // directory (Private=false), so it will not be found at runtime.
            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Missing.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            Assert.False(File.Exists(dllPath), $"Test assembly should not be present at {dllPath}");

            var ex = Assert.Throws<FileNotFoundException>(() => UseMissingAssembly());
            string exString = ex.ToString();
            Assert.Contains("System.Runtime.Loader.Test.BindFailure.Missing", exString);
            Assert.Contains(dllPath, exString);
            Assert.Contains(HResults.COR_E_FILENOTFOUND.ToString("X8"), exString);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.HasAssemblyFiles))]
        public void SharingViolation_ExceptionContainsPathAndHResult()
        {
            // The Locked assembly is copied to the output directory so we can lock it.
            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Locked.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            Assert.True(File.Exists(dllPath), $"Test assembly not found at {dllPath}");

            using FileStream lockStream = new FileStream(dllPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var ex = Assert.Throws<FileNotFoundException>(() => UseLockedAssembly());

            string exString = ex.ToString();
            Assert.Contains(dllPath, exString);
            Assert.Contains(HResults.ERROR_SHARING_VIOLATION.ToString("X8"), exString);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.HasAssemblyFiles))]
        public void Corrupt_ExceptionContainsPathAndHResult()
        {
            const int COR_E_ASSEMBLYEXPECTED = unchecked((int)0x80131018);

            // The Corrupt assembly is referenced at compile time but not copied to the output
            // directory (Private=false). We write a corrupt file in its place.
            const string assemblyFileName = "System.Runtime.Loader.Test.BindFailure.Corrupt.dll";
            string dllPath = GetAssemblyPath(assemblyFileName);
            Assert.False(File.Exists(dllPath), $"Test assembly should not be present at {dllPath}");

            try
            {
                File.WriteAllBytes(dllPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00 });

                var ex = Assert.Throws<FileNotFoundException>(() => UseCorruptAssembly());

                string exString = ex.ToString();
                Assert.Contains(dllPath, exString);
                Assert.Contains(COR_E_ASSEMBLYEXPECTED.ToString("X8"), exString);
            }
            finally
            {
                try { File.Delete(dllPath); } catch { }
            }
        }
    }
}
