// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetTimes_String : File_GetSetTimes
    {
        protected override bool CanBeReadOnly => true;

        protected override string CreateSymlink(string path, string pathToTarget) => File.CreateSymbolicLink(path, pathToTarget).FullName;

        protected override void SetCreationTime(string path, DateTime creationTime) => File.SetCreationTime(path, creationTime);

        protected override DateTime GetCreationTime(string path) => File.GetCreationTime(path);

        protected override void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => File.SetCreationTimeUtc(path, creationTimeUtc);

        protected override DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);

        protected override void SetLastAccessTime(string path, DateTime lastAccessTime) => File.SetLastAccessTime(path, lastAccessTime);

        protected override DateTime GetLastAccessTime(string path) => File.GetLastAccessTime(path);

        protected override void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

        protected override DateTime GetLastAccessTimeUtc(string path) => File.GetLastAccessTimeUtc(path);

        protected override void SetLastWriteTime(string path, DateTime lastWriteTime) => File.SetLastWriteTime(path, lastWriteTime);

        protected override DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

        protected override void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

        protected override DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PageFileHasTimes()
        {
            // Typically there is a page file on the C: drive, if not, don't bother trying to track it down.
            string pageFilePath = Directory.EnumerateFiles(@"C:\", "pagefile.sys", new EnumerationOptions
            {
                AttributesToSkip = 0
            }).FirstOrDefault();
            if (pageFilePath != null)
            {
                Assert.All(TimeFunctions(), (item) =>
                {
                    var time = item.Getter(pageFilePath);
                    Assert.NotEqual(DateTime.FromFileTime(0), time);
                });
            }
        }
    }
}
