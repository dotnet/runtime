// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_Exists
{
    [Fact]
    public static void Exists_NullAsPath_ReturnsFalse()
    {
        bool result = Directory.Exists((string)null);

        Assert.False(result);
    }

    [Fact]
    public static void Exists_EmptyAsPath_ReturnsFalse()
    {
        bool result = Directory.Exists(string.Empty);

        Assert.False(result);
    }

    [Fact]
    public static void Exists_DoesCaseInsensitiveInvariantComparions()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            var paths = new string[] { directory.Path,
                                       directory.Path.ToUpperInvariant(),
                                       directory.Path.ToLowerInvariant() };


            foreach (string path in paths)
            {
                bool result = Directory.Exists(path);

                Assert.True(result, path);
            }
        }
    }

    [Fact]
    public static void Exists_NonExistentValidPathAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetValidPathComponentNames();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_ExistentValidPathAsPath_ReturnsTrue()
    {
        var components = IOInputs.GetValidPathComponentNames();

        foreach (string component in components)
        {
            using (TemporaryDirectory directory = new TemporaryDirectory())
            {
                string path = Path.Combine(directory.Path, component);
                Directory.CreateDirectory(path);

                bool result = Directory.Exists(path);

                Assert.True(result, path);
            }
        }
    }


    [Fact]
    public static void Exists_NonSignificantWhiteSpaceAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetNonSignificantTrailingWhiteSpace();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }


    [Fact]
    public static void Exists_ExistingDirectoryWithNonSignificantTrailingWhiteSpaceAsPath_ReturnsTrue()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            var components = IOInputs.GetNonSignificantTrailingWhiteSpace();

            foreach (string component in components)
            {
                string path = directory.Path + component;

                bool result = Directory.Exists(path);

                Assert.True(result, path);
            }
        }
    }

    [Fact]
    public static void Exists_PathWithInvalidCharactersAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsWithInvalidCharacters();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_PathWithAlternativeDataStreams_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsWithAlternativeDataStreams();
        foreach (var path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    [OuterLoop]
    public static void Exists_PathWithReservedDeviceNameAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsWithReservedDeviceNames();
        foreach (var path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_UncPathWithoutShareNameAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetUncPathsWithoutShareName();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_DirectoryEqualToMaxDirectory_ReturnsTrue()
    {   // Creates directories up to the maximum directory length all at once
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            PathInfo path = IOServices.GetPath(directory.Path, IOInputs.MaxDirectory, maxComponent: 10);

            Directory.CreateDirectory(path.FullPath);

            bool result = Directory.Exists(path.FullPath);

            Assert.True(result, path.FullPath);
        }
    }

    [Fact]
    public static void Exists_DirectoryWithComponentLongerThanMaxComponentAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsWithComponentLongerThanMaxComponent();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_DirectoryLongerThanMaxDirectoryAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsLongerThanMaxDirectory();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_DirectoryLongerThanMaxPathAsPath_ReturnsFalse()
    {
        var paths = IOInputs.GetPathsLongerThanMaxPath();

        foreach (string path in paths)
        {
            bool result = Directory.Exists(path);

            Assert.False(result, path);
        }
    }

    [Fact]
    public static void Exists_NotReadyDriveAsPath_ReturnsFalse()
    {
        var drive = IOServices.GetNotReadyDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
            return;
        }

        bool result = Directory.Exists(drive);

        Assert.False(result);
    }

    [Fact]
    public static void Exists_SubdirectoryOnNotReadyDriveAsPath_ReturnsFalse()
    {
        var drive = IOServices.GetNotReadyDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
            return;
        }

        bool result = Directory.Exists(Path.Combine(drive, "Subdirectory"));

        Assert.False(result);
    }

    [Fact]
    public static void Exists_NonExistentDriveAsPath_ReturnsFalse()
    {
        var drive = IOServices.GetNonExistentDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a non-existent drive.");
            return;
        }

        bool result = Directory.Exists(drive);

        Assert.False(result);
    }

    [Fact]
    public static void Exists_SubdirectoryOnNonExistentDriveAsPath_ReturnsFalse()
    {
        var drive = IOServices.GetNonExistentDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a non-existent drive.");
            return;
        }

        bool result = Directory.Exists(Path.Combine(drive, "Subdirectory"));

        Assert.False(result);
    }


    [Fact]
    public static void Exists_FileWithoutTrailingSlashAsPath_ReturnsFalse()
    {
        using (TemporaryFile file = new TemporaryFile())
        {
            string path = IOServices.RemoveTrailingSlash(file.Path);

            bool result = Directory.Exists(path);

            Assert.False(result);
        }
    }

    [Fact]
    public static void Exists_FileWithTrailingSlashAsPath_ReturnsFalse()
    {
        using (TemporaryFile file = new TemporaryFile())
        {
            string path = IOServices.AddTrailingSlashIfNeeded(file.Path);

            bool result = Directory.Exists(path);

            Assert.False(result);
        }
    }

    [Fact]
    public static void Exists_ExistingDirectoryWithoutTrailingSlashAsPath_ReturnsTrue()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            string path = IOServices.RemoveTrailingSlash(directory.Path);

            bool result = Directory.Exists(path);

            Assert.True(result);
        }
    }

    [Fact]
    public static void Exists_ExistingDirectoryWithTrailingSlashAsPath_ReturnsTrue()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            string path = IOServices.AddTrailingSlashIfNeeded(directory.Path);

            bool result = Directory.Exists(path);

            Assert.True(result);
        }
    }

    [Fact]
    public static void Exists_DotAsPath_ReturnsTrue()
    {
        bool result = Directory.Exists(TestInfo.CurrentDirectory + @"\.");

        Assert.True(result);
    }

    [Fact]
    public static void Exists_DotDotAsPath_ReturnsTrue()
    {
        bool result = Directory.Exists(TestInfo.CurrentDirectory + @"\..");

        Assert.True(result);
    }

#if !TEST_WINRT // WinRT cannot access root
    /*
    [Fact]
    [ActiveIssue(1220)] // SetCurrentDirectory
    public static void Exists_DotDotAsPath_WhenCurrentDirectoryIsRoot_ReturnsTrue()
    {
        string root = Path.GetPathRoot(Directory.GetCurrentDirectory());

        using (CurrentDirectoryContext context = new CurrentDirectoryContext(root))
        {
            bool result = Directory.Exists("..");

            Assert.True(result);
        }
    }
    */
#endif

    [Fact]
    public static void Exists_DirectoryGetCurrentDirectoryAsPath_ReturnsTrue()
    {
        bool result = Directory.Exists(Directory.GetCurrentDirectory());

        Assert.True(result);
    }
}