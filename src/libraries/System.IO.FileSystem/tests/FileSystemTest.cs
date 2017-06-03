// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemTest : RemoteExecutorTestBase
    {
        public static readonly byte[] TestBuffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };

        protected const TestPlatforms CaseInsensitivePlatforms = TestPlatforms.Windows | TestPlatforms.OSX;
        protected const TestPlatforms CaseSensitivePlatforms = TestPlatforms.AnyUnix & ~TestPlatforms.OSX;

        public static bool AreAllLongPathsAvailable => PathFeatures.AreAllLongPathsAvailable();

        public static bool LongPathsAreNotBlocked => !PathFeatures.AreLongPathsBlocked();

        public static bool UsingNewNormalization => !PathFeatures.IsUsingLegacyPathNormalization();

        public static TheoryData<string> PathsWithInvalidColons = TestData.PathsWithInvalidColons;
        public static TheoryData<string> PathsWithInvalidCharacters = TestData.PathsWithInvalidCharacters;
        public static TheoryData<char> TrailingCharacters = TestData.TrailingCharacters;

        /// <summary>
        /// In some cases (such as when running without elevated privileges),
        /// the symbolic link may fail to create. Only run this test if it creates
        /// links successfully.
        /// </summary>
        protected static bool CanCreateSymbolicLinks => s_canCreateSymbolicLinks.Value;

        private static readonly Lazy<bool> s_canCreateSymbolicLinks = new Lazy<bool>(() =>
        {
            try
            {
                // Verify file symlink creation
                string path = Path.GetTempFileName();
                string linkPath = path + ".link";
                bool success = MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false);
                try { File.Delete(path); } catch { }
                try { File.Delete(linkPath); } catch { }

                // Verify directory symlink creation
                path = Path.GetTempFileName();
                linkPath = path + ".link";
                success = success && MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true);
                try { Directory.Delete(path); } catch { }
                try { Directory.Delete(linkPath); } catch { }

                return success;
            }
            catch
            {
                // Problems with Process.Start (used by CreateSymbolicLinks) on some platforms
                // https://github.com/dotnet/corefx/issues/19909
                return false;
            }
        });
    }
}
