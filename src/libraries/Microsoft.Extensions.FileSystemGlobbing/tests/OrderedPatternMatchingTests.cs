// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class OrderedPatternMatchingTests : PatternMatchingTests
    {
        public OrderedPatternMatchingTests() => IsOrdered = true;

        [Fact]
        public void ComplexPatternSequence_SimpleFilters()
        {
            var scenario = new FileSystemGlobbingTestContext(@"c:/project/", IsOrdered)
                .Include("A/*")
                .Exclude("A/B/*")
                .Include("C/*")
                .Files(
                    "A/Program.cs",
                    "A/B/foo.txt",
                    "C/README.md"
                )
                .Execute();

            scenario.AssertExact(
                "A/Program.cs",
                "C/README.md"
            );
        }

        [Fact]
        public void ComplexPatternSequence_DeeplyNestedFilters()
        {
            var scenario = new FileSystemGlobbingTestContext(@"c:/project/", IsOrdered)
                .Include("**/*.cs")                                   // Include all .cs files
                .Exclude("**/obj/**/*")                               // Exclude all in obj/
                .Include("lib/generated/**/*.cs")                     // Re-include generated code
                .Exclude("**/*Tests.cs")                              // Exclude unit tests
                .Include("tests/manual/*Tests.cs")                    // Re-include manual test files
                .Exclude("legacy/**/*")                               // Exclude legacy code
                .Files(
                    "Program.cs",
                    "obj/Temp.cs",
                    "lib/generated/AutoGen.cs",
                    "Tests/Unit/MathTests.cs",
                    "tests/manual/VisualTests.cs",
                    "legacy/OldStuff.cs",
                    "README.md"
                )
                .Execute();

            scenario.AssertExact(
                "Program.cs",
                "lib/generated/AutoGen.cs",
                "tests/manual/VisualTests.cs"
            );
        }

        [Fact]
        public void MixOfPatternsAcrossVariousFileTypes()
        {
            var scenario = new FileSystemGlobbingTestContext(@"c:/assets/", IsOrdered)
                .Include("**/*")                                       // Include everything
                .Exclude("**/*.tmp")                                  // Remove temp files
                .Exclude("**/*.bak")                                  // Remove backups
                .Include("backup/important/*.bak")                    // Bring back important backups
                .Exclude("temp/**/*")                                 // Remove all in temp dir
                .Include("temp/keep/*")                               // But allow files in temp/keep
                .Files(
                    "image.png",
                    "video.mp4",
                    "doc.tmp",
                    "settings.bak",
                    "backup/important/settings.bak",
                    "temp/scratch.txt",
                    "temp/keep/notes.txt"
                )
                .Execute();

            scenario.AssertExact(
                "image.png",
                "video.mp4",
                "backup/important/settings.bak",
                "temp/keep/notes.txt"
            );
        }

        [Fact]
        public void ComplexIncludesThenGlobalExcludeOverrides()
        {
            var scenario = new FileSystemGlobbingTestContext(@"c:/data/", IsOrdered)
                .Include("**/*.json")
                .Include("**/*.csv")
                .Include("metrics/*.xml")
                .Include("raw/**/*.bin")
                .Exclude("**/sensitive/**/*")
                .Exclude("**/*.bak")
                .Files(
                    "dataset.csv",
                    "config.json",
                    "metrics/stats.xml",
                    "raw/input/data.bin",
                    "sensitive/backup/config.bak",
                    "sensitive/raw/logs.json"
                )
                .Execute();

            scenario.AssertExact(
                "dataset.csv",
                "config.json",
                "metrics/stats.xml",
                "raw/input/data.bin"
            );
        }
    }
}
