// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class AppExecLinks : BaseSymbolicLinks
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.HasUsableAppExecLinksDirectory))]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveAppExecLinkTargets(bool returnFinalTarget)
        {
            string windowsAppsDir = Path.Join(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Microsoft", "WindowsApps");
            var appExecLinkPaths = new FileSystemEnumerable<string?>(
                                   windowsAppsDir,
                                   (ref FileSystemEntry entry) => entry.ToFullPath(),
                                   new EnumerationOptions { RecurseSubdirectories = true })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                    FileSystemName.MatchesWin32Expression("*.exe", entry.FileName) &&
                    entry.Attributes.HasFlag(FileAttributes.ReparsePoint)
            };

            foreach (string appExecLinkPath in appExecLinkPaths)
            {
                FileInfo linkInfo = new(appExecLinkPath);
                Assert.Equal(0, linkInfo.Length);
                Assert.True(linkInfo.Attributes.HasFlag(FileAttributes.ReparsePoint));

                string? linkTarget = linkInfo.LinkTarget;
                Assert.NotNull(linkTarget);
                Assert.NotEqual(appExecLinkPath, linkTarget);

                FileSystemInfo? targetInfoFromFileInfo = linkInfo.ResolveLinkTarget(returnFinalTarget);
                VerifyFileInfo(targetInfoFromFileInfo);

                FileSystemInfo? targetInfoFromFile = File.ResolveLinkTarget(appExecLinkPath, returnFinalTarget);
                VerifyFileInfo(targetInfoFromFile);
            }

            void VerifyFileInfo(FileSystemInfo? info)
            {
                Assert.True(info is FileInfo);
                if (info.Exists) // The target may not exist, that's ok
                {
                    Assert.True(((FileInfo)info).Length > 0);
                    Assert.False(info.Attributes.HasFlag(FileAttributes.ReparsePoint));
                }
            }
        }
    }
}
