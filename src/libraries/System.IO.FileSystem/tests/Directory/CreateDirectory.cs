// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_CreateDirectory
{
    [Fact]
    public static void CreateDirectory_NullAsPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Directory.CreateDirectory((string)null);
        });
    }

    [Fact]
    public static void CreateDirectory_EmptyAsPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>              // BUG 995784: Not setting the parameter name
        {
            Directory.CreateDirectory(string.Empty);
        });
    }

    [Fact]
    public static void CreateDirectory_NonSignificantWhiteSpaceAsPath_ThrowsArgumentException()
    {
        var paths = IOInputs.GetNonSignificantTrailingWhiteSpace();
        foreach (var path in paths)
        {
            Assert.Throws<ArgumentException>(() =>          // BUG 995784: Not setting the parameter name
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_PathWithInvalidCharactersAsPath_ThrowsArgumentException()
    {
        var paths = IOInputs.GetPathsWithInvalidCharacters();
        foreach (var path in paths)
        {
            Assert.Throws<ArgumentException>(() =>          // BUG 995784: Not setting the parameter name
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_PathWithAlternativeDataStreams_ThrowsNotSupportedException()
    {
        var paths = IOInputs.GetPathsWithAlternativeDataStreams();
        foreach (var path in paths)
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    [OuterLoop]
    public static void CreateDirectory_PathWithReservedDeviceNameAsPath_ThrowsDirectoryNotFoundException()
    {   // Throws DirectoryNotFoundException, when the behavior really should be an invalid path
        var paths = IOInputs.GetPathsWithReservedDeviceNames();
        foreach (var path in paths)
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_UncPathWithoutShareNameAsPath_ThrowsArgumentException()
    {
        var paths = IOInputs.GetUncPathsWithoutShareName();
        foreach (var path in paths)
        {
            Assert.Throws<ArgumentException>(() =>      // BUG 995784: Not setting the parameter name
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_DirectoryWithComponentLongerThanMaxComponentAsPath_ThrowsPathTooLongException()
    {
        // While paths themselves can be up to 260 characters including trailing null, file systems
        // limit each components of the path to a total of 255 characters.

        var paths = IOInputs.GetPathsWithComponentLongerThanMaxComponent();

        foreach (string path in paths)
        {
            Assert.Throws<PathTooLongException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_DirectoryLongerThanMaxDirectoryAsPath_ThrowsPathTooLongException()
    {
        var paths = IOInputs.GetPathsLongerThanMaxDirectory();
        foreach (var path in paths)
        {
            Assert.Throws<PathTooLongException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_DirectoryLongerThanMaxPathAsPath_ThrowsPathTooLongException()
    {
        var paths = IOInputs.GetPathsLongerThanMaxPath();
        foreach (var path in paths)
        {
            Assert.Throws<PathTooLongException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_NotReadyDriveAsPath_ThrowsDirectoryNotFoundException()
    {   // Behavior is suspect, should really have thrown IOException similar to the SubDirectory case
        var drive = IOServices.GetNotReadyDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
            return;
        }

        Assert.Throws<DirectoryNotFoundException>(() =>
        {
            Directory.CreateDirectory(drive);
        });
    }

    [Fact]
    public static void CreateDirectory_SubdirectoryOnNotReadyDriveAsPath_ThrowsIOException()
    {
        var drive = IOServices.GetNotReadyDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a not-ready drive, such as CD-Rom with no disc inserted.");
            return;
        }

        // 'Device is not ready'
        Assert.Throws<IOException>(() =>
        {
            Directory.CreateDirectory(Path.Combine(drive, "Subdirectory"));
        });
    }

    [Fact]
    public static void CreateDirectory_NonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
    {
        var drive = IOServices.GetNonExistentDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a non-existent drive.");
            return;
        }


        Assert.Throws<DirectoryNotFoundException>(() =>
        {
            Directory.CreateDirectory(drive);
        });
    }

    [Fact]
    public static void CreateDirectory_SubdirectoryOnNonExistentDriveAsPath_ThrowsDirectoryNotFoundException()
    {
        var drive = IOServices.GetNonExistentDrive();
        if (drive == null)
        {
            Console.WriteLine("Skipping test. Unable to find a non-existent drive.");
            return;
        }

        Assert.Throws<DirectoryNotFoundException>(() =>
        {
            Directory.CreateDirectory(Path.Combine(drive, "Subdirectory"));
        });
    }

    [Fact]
    public static void CreateDirectory_FileWithoutTrailingSlashAsPath_ThrowsIOException()
    {   // VSWhidbey #104049
        using (TemporaryFile file = new TemporaryFile())
        {
            string path = IOServices.RemoveTrailingSlash(file.Path);

            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }


    [Fact]
    public static void CreateDirectory_FileWithTrailingSlashAsPath_ThrowsIOException()
    {
        using (TemporaryFile file = new TemporaryFile())
        {
            string path = IOServices.AddTrailingSlashIfNeeded(file.Path);

            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
    }

    [Fact]
    public static void CreateDirectory_ExistingDirectoryWithoutTrailingSlashAsPath_DoesNotThrow()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            string path = IOServices.RemoveTrailingSlash(directory.Path);

            DirectoryInfo result = Directory.CreateDirectory(path);

            Assert.Equal(path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
    }

    [Fact]
    public static void CreateDirectory_ExistingDirectoryWithTrailingSlashAsPath_DoesNotThrow()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            string path = IOServices.AddTrailingSlashIfNeeded(directory.Path);

            DirectoryInfo result = Directory.CreateDirectory(path);

            Assert.Equal(path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
    }

    [Fact]
    public static void CreateDirectory_DotWithoutTrailingSlashAsPath_DoesNotThrow()
    {
        DirectoryInfo result = Directory.CreateDirectory(TestInfo.CurrentDirectory + @"\."); // "Current" directory

        Assert.True(Directory.Exists(result.FullName));
    }

    [Fact]
    public static void CreateDirectory_DotWithTrailingSlashAsPath_DoesNotThrow()
    {
        DirectoryInfo result = Directory.CreateDirectory(TestInfo.CurrentDirectory + @"\.\"); // "Current" directory

        Assert.True(Directory.Exists(result.FullName));
    }

    [Fact]
    public static void CreateDirectory_DotDotWithoutTrailingSlashAsPath_DoesNotThrow()
    {
        DirectoryInfo result = Directory.CreateDirectory(TestInfo.CurrentDirectory + @"\..");    // "Parent" of current directory

        Assert.True(Directory.Exists(result.FullName));
    }

    [Fact]
    public static void CreateDirectory_DotDotWithTrailingSlashAsPath_DoesNotThrow()
    {
        DirectoryInfo result = Directory.CreateDirectory(TestInfo.CurrentDirectory + @"\..\");    // "Parent" of current directory

        Assert.True(Directory.Exists(result.FullName));
    }

#if !TEST_WINRT // Cannot set current directory to root from appcontainer with it's default ACL
    /*
    [Fact]
    [ActiveIssue(1220)] // SetCurrentDirectory
    public static void CreateDirectory_DotDotAsPath_WhenCurrentDirectoryIsRoot_DoesNotThrow()
    {
        string root = Path.GetPathRoot(Directory.GetCurrentDirectory());

        using (CurrentDirectoryContext context = new CurrentDirectoryContext(root))
        {
            DirectoryInfo result = Directory.CreateDirectory("..");

            Assert.True(Directory.Exists(result.FullName));
            Assert.Equal(root, result.FullName);
        }
    }
    */
#endif

    [Fact]
    public static void CreateDirectory_NonSignificantTrailingWhiteSpace_TreatsAsNonSignificant()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            var components = IOInputs.GetNonSignificantTrailingWhiteSpace();

            foreach (string component in components)
            {
                string path = directory.Path + component;

                DirectoryInfo result = Directory.CreateDirectory(path);

                Assert.True(Directory.Exists(result.FullName));
                Assert.Equal(directory.Path, result.FullName);
            }
        }
    }

    [Fact]
    public static void CreateDirectory_ReadOnlyFileAsPath_ThrowsIOException()
    {
        using (TemporaryFile file = new TemporaryFile())
        {
            file.IsReadOnly = true;

            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(file.Path);
            });
        }
    }

#if !TEST_WINRT // TODO: Enable once we implement WinRT file attributes
    [Fact]
    public static void CreateDirectory_ReadOnlyExistingDirectoryAsPath_DoesNotThrow()
    {   // DevDivBugs #33833 
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            directory.IsReadOnly = true;

            DirectoryInfo result = Directory.CreateDirectory(directory.Path);

            Assert.Equal(directory.Path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
    }


    [Fact]
    public static void CreateDirectory_HiddenExistingDirectoryAsPath_DoesNotThrow()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            directory.IsHidden = true;

            DirectoryInfo result = Directory.CreateDirectory(directory.Path);

            Assert.Equal(directory.Path, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
    }
#endif

    [Fact]
    public static void CreateDirectory_DirectoryEqualToMaxDirectory_CanBeCreated()
    {   // Recursively creates directories right up to the maximum directory length ("247 chars not including null")
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            PathInfo path = IOServices.GetPath(directory.Path, IOInputs.MaxDirectory);

            // First create 'C:\AAA...AA', followed by 'C:\AAA...AAA\AAA...AAA', etc
            foreach (string subpath in path.SubPaths)
            {
                DirectoryInfo result = Directory.CreateDirectory(subpath);

                Assert.Equal(subpath, result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            }
        }
    }

    [Fact]
    public static void CreateDirectory_DirectoryEqualToMaxDirectory_CanBeCreatedAllAtOnce()
    {   // Creates directories up to the maximum directory length all at once
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            PathInfo path = IOServices.GetPath(directory.Path, IOInputs.MaxDirectory, maxComponent: 10);

            DirectoryInfo result = Directory.CreateDirectory(path.FullPath);

            Assert.Equal(path.FullPath, result.FullName);
            Assert.True(Directory.Exists(result.FullName));
        }
    }

    [Fact]
    public static void CreateDirectory_ValidPathWithTrailingSlash_CanBeCreated()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            var components = IOInputs.GetValidPathComponentNames();

            foreach (var component in components)
            {
                string path = IOServices.AddTrailingSlashIfNeeded(directory.Path + "\\" + component);

                DirectoryInfo result = Directory.CreateDirectory(path);

                Assert.Equal(path, result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            }
        }
    }

    [Fact]
    public static void CreateDirectory_ValidPathWithoutTrailingSlashAsPath_CanBeCreated()
    {
        using (TemporaryDirectory directory = new TemporaryDirectory())
        {
            var components = IOInputs.GetValidPathComponentNames();

            foreach (var component in components)
            {
                string path = directory.Path + @"\" + component;

                DirectoryInfo result = Directory.CreateDirectory(path);

                Assert.Equal(path, result.FullName);
                Assert.True(Directory.Exists(result.FullName));
            }
        }
    }
}