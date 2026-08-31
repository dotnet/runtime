// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class TrimmedPaths : FileSystemTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsAreFound_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to find them
            // and retain the filename in the info classes and string results.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing space ")).Dispose();
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing period.")).Dispose();

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Count());
            FSAssert.EqualWhenOrdered(new string[] { "Trailing space ", "Trailing period." }, files.Select(f => f.Name));

            var paths = Directory.GetFiles(directory.FullName);
            Assert.Equal(2, paths.Count());
            FSAssert.EqualWhenOrdered(new string[] { "Trailing space ", "Trailing period." }, paths.Select(p => Path.GetFileName(p)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsDeletion_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to delete them
            // from the info class.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing space ")).Dispose();
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing period.")).Dispose();

            // With just a path name, the trailing space/period will get eaten, so we
            // can't delete without prepending- they won't "exist".
            var paths = Directory.GetFiles(directory.FullName);
            Assert.All(paths, p => Assert.False(File.Exists(p)));

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Count());
            Assert.All(files, f => Assert.True(f.Exists));
            foreach (FileInfo f in files)
                f.Refresh();
            Assert.All(files, f => Assert.True(f.Exists));
            foreach (FileInfo f in files)
            {
                f.Delete();
                f.Refresh();
            }
            Assert.All(files, f => Assert.False(f.Exists));

            foreach (FileInfo f in files)
            {
                f.Create().Dispose();
                f.Refresh();
            }
            Assert.All(files, f => Assert.True(f.Exists));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsOpen_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to open them
            // from the info class when enumerating.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string fileOne = Path.Join(directory.FullName, "Trailing space ");
            string fileTwo = Path.Join(directory.FullName, "Trailing period.");
            File.Create(@"\\?\" + fileOne).Dispose();
            File.Create(@"\\?\" + fileTwo).Dispose();

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Length);
            foreach (FileInfo fi in directory.GetFiles())
            {
                // Shouldn't throw hitting any of the Open overloads
                using (FileStream stream = fi.Open(FileMode.Open))
                { }
                using (FileStream stream = fi.Open(FileMode.Open, FileAccess.Read))
                { }
                using (FileStream stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                { }
                using (FileStream stream = fi.OpenRead())
                { }
                using (FileStream stream = fi.OpenWrite())
                { }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsText_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to open readers
            // and writers from the info class when enumerating.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string fileOne = Path.Join(directory.FullName, "Trailing space ");
            string fileTwo = Path.Join(directory.FullName, "Trailing period.");
            File.WriteAllText(@"\\?\" + fileOne, "space");
            File.WriteAllText(@"\\?\" + fileTwo, "period");

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Length);
            foreach (FileInfo fi in directory.GetFiles())
            {
                using (StreamReader reader = fi.OpenText())
                {
                    string content = reader.ReadToEnd();
                    if (fi.FullName.EndsWith(fileOne))
                    {
                        Assert.Equal("space", content);
                    }
                    else if (fi.FullName.EndsWith(fileTwo))
                    {
                        Assert.Equal("period", content);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected name '{fi.FullName}'");
                    }
                }

                using (StreamWriter writer = fi.CreateText())
                {
                    writer.Write("foo");
                }

                using (StreamReader reader = fi.OpenText())
                {
                    Assert.Equal("foo", reader.ReadToEnd());
                }

                using (StreamWriter writer = fi.AppendText())
                {
                    writer.Write("bar");
                }

                using (StreamReader reader = fi.OpenText())
                {
                    Assert.Equal("foobar", reader.ReadToEnd());
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsCopyTo_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to copy them
            // without the special syntax from the info class when enumerating.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string fileOne = Path.Join(directory.FullName, "Trailing space ");
            string fileTwo = Path.Join(directory.FullName, "Trailing period.");
            File.Create(@"\\?\" + fileOne).Dispose();
            File.Create(@"\\?\" + fileTwo).Dispose();

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Length);
            foreach (FileInfo fi in directory.GetFiles())
            {
                FileInfo newInfo = fi.CopyTo(Path.Join(directory.FullName, GetTestFileName()));
                Assert.True(newInfo.Exists);
                FileInfo newerInfo = fi.CopyTo(Path.Join(directory.FullName, GetTestFileName()), overwrite: true);
                Assert.True(newerInfo.Exists);
            }

            Assert.Equal(6, directory.GetFiles().Length);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsReplace_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to replace them
            // from the info class when enumerating.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string fileOne = Path.Join(directory.FullName, "Trailing space ");
            string fileTwo = Path.Join(directory.FullName, "Trailing period.");
            File.WriteAllText(@"\\?\" + fileOne, "space");
            File.WriteAllText(@"\\?\" + fileTwo, "period");

            FileInfo[] files = directory.GetFiles();
            Assert.Equal(2, files.Length);

            FileInfo destination = new FileInfo(Path.Join(directory.FullName, GetTestFileName()));
            destination.Create().Dispose();

            foreach (FileInfo fi in files)
            {
                fi.Replace(destination.FullName, null);
                using (StreamReader reader = destination.OpenText())
                {
                    string content = reader.ReadToEnd();
                    if (fi.FullName.EndsWith(fileOne))
                    {
                        Assert.Equal("space", content);
                    }
                    else if (fi.FullName.EndsWith(fileTwo))
                    {
                        Assert.Equal("period", content);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected name '{fi.FullName}'");
                    }
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TrimmedPathsMoveTo_Windows()
        {
            // Trailing spaces and periods are eaten when normalizing in Windows, making them impossible
            // to access without using the \\?\ device syntax. We should, however, be able to move them
            // without the special syntax from the info class when enumerating.

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo spaceDirectory = Directory.CreateDirectory(Path.Join(@"\\?\", directory.FullName, "Trailing space "));
            DirectoryInfo periodDirectory = Directory.CreateDirectory(Path.Join(@"\\?\", directory.FullName, "Trailing period."));
            string spaceFile = Path.Join(spaceDirectory.FullName, "space");
            string periodFile = Path.Join(periodDirectory.FullName, "period");
            File.Create(spaceFile).Dispose();
            File.Create(periodFile).Dispose();

            DirectoryInfo[] directories = directory.GetDirectories();
            Assert.Equal(2, directories.Length);
            foreach (DirectoryInfo di in directories)
            {
                if (di.Name == "Trailing space ")
                {
                    di.MoveTo(Path.Join(directory.FullName, "WasSpace"));
                }
                else if (di.Name == "Trailing period.")
                {
                    di.MoveTo(Path.Join(directory.FullName, "WasPeriod"));
                }
                else
                {
                    Assert.Fail($"Found unexpected name '{di.Name}'");
                }
            }

            directories = directory.GetDirectories();
            Assert.Equal(2, directories.Length);
            foreach (DirectoryInfo di in directories)
            {
                if (di.Name == "WasSpace")
                {
                    FileInfo fi = di.GetFiles().Single();
                    Assert.Equal("space", fi.Name);
                }
                else if (di.Name == "WasPeriod")
                {
                    FileInfo fi = di.GetFiles().Single();
                    Assert.Equal("period", fi.Name);
                }
                else
                {
                    Assert.Fail($"Found unexpected name '{di.Name}'");
                }
            }
        }
    }
}
