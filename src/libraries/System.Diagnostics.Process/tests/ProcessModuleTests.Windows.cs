// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessModuleTests : ProcessTestBase
    {
        [ConditionalFact(typeof(PathFeatures), nameof(PathFeatures.AreAllLongPathsAvailable))]
        public void LongModuleFileNamesAreSupported()
        {
            // To be able to test Long Path support for ProcessModule.FileName we need a .dll that has a path > 260 chars.
            // Since Long Paths support can be disabled (see the ConditionalFact attribute usage above),
            // we just copy "LongName.dll" from bin to a temp directory with a long name and load it from there.
            // Loading from new path is possible because the type exposed by the assembly is not referenced in any explicit way.
            const string libraryName = "LongPath.dll";
            const int minPathLength = 261;

            string testBinPath = Path.GetDirectoryName(typeof(ProcessModuleTests).Assembly.Location);
            string libraryToCopy = Path.Combine(testBinPath, libraryName);
            Assert.True(File.Exists(libraryToCopy), $"{libraryName} was not present in bin folder '{testBinPath}'");

            string directoryWithLongName = Path.Combine(TestDirectory, new string('a', Math.Max(1, minPathLength - TestDirectory.Length)));
            Directory.CreateDirectory(directoryWithLongName);

            string longNamePath = Path.Combine(directoryWithLongName, libraryName);
            Assert.True(longNamePath.Length > minPathLength);

            File.Copy(libraryToCopy, longNamePath);
            Assert.True(File.Exists(longNamePath));

            IntPtr moduleHandle = Interop.Kernel32.LoadLibrary(longNamePath);
            if (moduleHandle == IntPtr.Zero)
            {
                return; // we've failed to load the module, we can't test module paths
            }

            try
            {
                string[] modulePaths = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(module => module.FileName).ToArray();
                Assert.Contains(longNamePath, modulePaths);
            }
            finally
            {
                Interop.Kernel32.FreeLibrary(moduleHandle);
            }
        }
    }
}
