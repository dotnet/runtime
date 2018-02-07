// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var directory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing space ")).Dispose();
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing period.")).Dispose();

            var files = directory.GetFiles();
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

            var directory = Directory.CreateDirectory(GetTestFilePath());
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing space ")).Dispose();
            File.Create(@"\\?\" + Path.Combine(directory.FullName, "Trailing period.")).Dispose();

            // With just a path name, the trailing space/period will get eaten, so we
            // can't delete without prepending- they won't "exist".
            var paths = Directory.GetFiles(directory.FullName);
            Assert.All(paths, p => Assert.False(File.Exists(p)));

            var files = directory.GetFiles();
            Assert.Equal(2, files.Count());
            Assert.All(files, f => Assert.True(f.Exists));
            foreach (var f in files)
                f.Refresh();
            Assert.All(files, f => Assert.True(f.Exists));
            foreach (var f in files)
            {
                f.Delete();
                f.Refresh();
            }
            Assert.All(files, f => Assert.False(f.Exists));
        }
    }
}
