// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static System.Environment;
using Xunit;

namespace Microsoft.VisualBasic.FileIO.Tests
{
    public class SpecialDirectoriesTests
    {
        private static void CheckSpecialFolder(SpecialFolder folder, Func<string> getSpecialDirectory)
        {
            var path = Environment.GetFolderPath(folder);
            if (string.IsNullOrEmpty(path))
            {
                Assert.Throws<System.IO.DirectoryNotFoundException>(getSpecialDirectory);
            }
            else
            {
                Assert.Equal(TrimSeparators(path), TrimSeparators(getSpecialDirectory()));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Supported on netfx")]
        public static void AllUsersApplicationDataFolderTest()
        {
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => SpecialDirectories.AllUsersApplicationData);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Supported on netfx")]
        public static void CurrentUserApplicationDataFolderTest()
        {
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => SpecialDirectories.CurrentUserApplicationData);
        }

        [Fact]
        public static void DesktopFolderTest()
        {
            CheckSpecialFolder(SpecialFolder.Desktop, () => SpecialDirectories.Desktop);
        }

        [Fact]
        public static void MyDocumentsFolderTest()
        {
            if (PlatformDetection.IsWindowsNanoServer)
            {
                Assert.Throws<System.IO.DirectoryNotFoundException>(() => SpecialDirectories.MyDocuments);
            }
            else
            {
                CheckSpecialFolder(SpecialFolder.MyDocuments, () => SpecialDirectories.MyDocuments);
            }
        }

        [Fact]
        public static void MyMusicFolderTest()
        {
            CheckSpecialFolder(SpecialFolder.MyMusic, () => SpecialDirectories.MyMusic);
        }

        [Fact]
        public static void MyPicturesFolderTest()
        {
            CheckSpecialFolder(SpecialFolder.MyPictures, () => SpecialDirectories.MyPictures);
        }

        [Fact]
        public static void ProgramFilesFolderTest()
        {
            CheckSpecialFolder(SpecialFolder.ProgramFiles, () => SpecialDirectories.ProgramFiles);
        }

        [Fact]
        public static void ProgramsFolderTest()
        {
            CheckSpecialFolder(SpecialFolder.Programs, () => SpecialDirectories.Programs);
        }

        [Fact]
        public static void TempFolderTest()
        {
            // On Nano Server >=1809 the temp path's case is changed during the normalization.
            Assert.Equal(TrimSeparators(System.IO.Path.GetTempPath()), TrimSeparators(SpecialDirectories.Temp), ignoreCase: PlatformDetection.IsWindowsNanoServer);
        }

        private static string TrimSeparators(string s)
        {
            return s.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
    }
}
