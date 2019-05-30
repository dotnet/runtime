// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void AllUsersApplicationDataFolderTest()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SpecialDirectories.AllUsersApplicationData);
        }

        [Fact]
        public static void CurrentUserApplicationDataFolderTest()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SpecialDirectories.CurrentUserApplicationData);
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
            Assert.Equal(TrimSeparators(System.IO.Path.GetTempPath()), TrimSeparators(SpecialDirectories.Temp));
        }

        private static string TrimSeparators(string s)
        {
            return s.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
    }
}
