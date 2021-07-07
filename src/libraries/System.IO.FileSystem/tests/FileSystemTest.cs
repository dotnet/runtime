// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemTest : FileCleanupTestBase
    {
        public static readonly byte[] TestBuffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };

        protected const TestPlatforms CaseInsensitivePlatforms = TestPlatforms.Windows | TestPlatforms.OSX;
        protected const TestPlatforms CaseSensitivePlatforms = TestPlatforms.AnyUnix & ~TestPlatforms.OSX;

        public static bool AreAllLongPathsAvailable => PathFeatures.AreAllLongPathsAvailable();

        public static bool LongPathsAreNotBlocked => !PathFeatures.AreLongPathsBlocked();

        public static bool UsingNewNormalization => !PathFeatures.IsUsingLegacyPathNormalization();

        public static bool ReservedDeviceNamesAreBlocked => PlatformDetection.IsWindows && !PlatformDetection.IsWindows10OrLater;

        public static TheoryData<string> PathsWithInvalidColons = TestData.PathsWithInvalidColons;
        public static TheoryData<string> PathsWithInvalidCharacters = TestData.PathsWithInvalidCharacters;
        public static TheoryData<char> TrailingCharacters = TestData.TrailingCharacters;
        public static TheoryData ValidPathComponentNames = IOInputs.GetValidPathComponentNames().ToTheoryData();
        public static TheoryData SimpleWhiteSpace = IOInputs.GetSimpleWhiteSpace().ToTheoryData();
        public static TheoryData WhiteSpace = IOInputs.GetWhiteSpace().ToTheoryData();
        public static TheoryData UncPathsWithoutShareName = IOInputs.GetUncPathsWithoutShareName().ToTheoryData();
        public static TheoryData PathsWithReservedDeviceNames = IOInputs.GetPathsWithReservedDeviceNames().ToTheoryData();
        public static TheoryData PathsWithColons = IOInputs.GetPathsWithColons().ToTheoryData();
        public static TheoryData PathsWithComponentLongerThanMaxComponent = IOInputs.GetPathsWithComponentLongerThanMaxComponent().ToTheoryData();
        public static TheoryData ControlWhiteSpace = IOInputs.GetControlWhiteSpace().ToTheoryData();
        public static TheoryData NonControlWhiteSpace = IOInputs.GetNonControlWhiteSpace().ToTheoryData();

        public static TheoryData<string> TrailingSeparators
        {
            get
            {
                var data = new TheoryData<string>()
                {
                    "",
                    "" + Path.DirectorySeparatorChar,
                    "" + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar
                };

                if (PlatformDetection.IsWindows)
                {
                    data.Add("" + Path.AltDirectorySeparatorChar);
                }

                return data;
            }
        }

        /// <summary>
        /// In some cases (such as when running without elevated privileges),
        /// the symbolic link may fail to create. Only run this test if it creates
        /// links successfully.
        /// </summary>
        protected static bool CanCreateSymbolicLinks => s_canCreateSymbolicLinks.Value;

        private static readonly Lazy<bool> s_canCreateSymbolicLinks = new Lazy<bool>(() =>
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
        });

        public static string GetNamedPipeServerStreamName()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                return @"LOCAL\" + Guid.NewGuid().ToString("N");
            }
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Do a test action against read only file system (for Unix).
        /// </summary>
        /// <param name="testAction">Test action to perform. The string argument will be read only directory.</param>
        /// <param name="subDirectoryName">Optional subdirectory to create.</param>
        protected void ReadOnly_FileSystemHelper(Action<string> testAction, string subDirectoryName = null)
        {
            // Set up read only file system
            // Set up the source directory
            string sourceDirectory = GetTestFilePath();
            if (subDirectoryName == null)
            {
                Directory.CreateDirectory(sourceDirectory);
            }
            else
            {
                string sourceSubDirectory = Path.Combine(sourceDirectory, subDirectoryName);
                Directory.CreateDirectory(sourceSubDirectory);
            }

            // Set up the target directory and mount as a read only
            string readOnlyDirectory = GetTestFilePath();
            Directory.CreateDirectory(readOnlyDirectory);

            Assert.Equal(0, AdminHelpers.RunAsSudo($"mount --bind {sourceDirectory} {readOnlyDirectory}"));

            try
            {
                Assert.Equal(0, AdminHelpers.RunAsSudo($"mount -o remount,ro,bind {sourceDirectory} {readOnlyDirectory}"));
                testAction(readOnlyDirectory);
            }
            finally
            {
                // Clean up test environment
                Assert.Equal(0, AdminHelpers.RunAsSudo($"umount {readOnlyDirectory}"));
            }
        }

        /// <summary>
        /// Determines whether the file system is case sensitive by creating a file in the specified folder and observing the result.
        /// </summary>
        /// <remarks>
        /// Ideally we'd use something like pathconf with _PC_CASE_SENSITIVE, but that is non-portable,
        /// not supported on Windows or Linux, etc. For now, this function creates a tmp file with capital letters
        /// and then tests for its existence with lower-case letters.  This could return invalid results in corner
        /// cases where, for example, different file systems are mounted with differing sensitivities.
        /// </remarks>
        protected static bool GetIsCaseSensitiveByProbing(string probingDirectory)
        {
            string pathWithUpperCase = Path.Combine(probingDirectory, "CASESENSITIVETEST" + Guid.NewGuid().ToString("N"));
            using (new FileStream(pathWithUpperCase, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose))
            {
                string lowerCased = pathWithUpperCase.ToLowerInvariant();
                return !File.Exists(lowerCased);
            }
        }
    }
}
