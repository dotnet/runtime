// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Tests;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public static class RemoveRedundantSegmentsTests
    {
        private static readonly string[] s_Prefixes = new string[] { "", @"\\?\", @"\\.\" };
        private static readonly string[] s_UncPrefixes = new string[] { @"\\", @"\\?\UNC\", @"\\.\UNC\" };

        // Null or empty tests
        public static TheoryData<string, string> NullOrEmptyData => new TheoryData<string, string>
        {
            { null,   null },
            { "",     ""   },
            { " ",    ""   },
            { "    ", ""   }
        };

        // Normal paths
        public static TheoryData<string, string> WindowsNormalData => new TheoryData<string, string>
        {
            // A '\' inside a string that is prefixed with @ is actually passed as an escaped backward slash "\\"
            { @"C:",               @"C:" },
            { @"C:\",              @"C:\" },
            { @"C:\Users",         @"C:\Users" },
            { @"C:\Users\",        @"C:\Users\" },
            { @"C:\Users\myuser",  @"C:\Users\myuser" },
            { @"C:\Users\myuser\", @"C:\Users\myuser\" },
        };
        public static TheoryData<string, string> UnixNormalData => new TheoryData<string, string>
        {
            // AltDirectorySeparatorChar gets normalized to DirectorySeparatorChar, if they are not the same in the current platform
            { @"/",                @"/" },
            { @"/home",            @"/home" },
            { @"/home/",           @"/home/" },
            { @"/home/myuser",     @"/home/myuser" },
            { @"/home/myuser/",    @"/home/myuser/" },
        };

        // Paths with '..' to indicate parent backtracking
        public static TheoryData<string, string> WindowsValidParentBacktrackingData => new TheoryData<string, string>
        {
            { @"C:\..",                        @"C:\" },
            { @"C:\..\",                       @"C:\" },
            { @"C:\..\Users",                  @"C:\Users" },
            { @"C:\..\Users\",                 @"C:\Users\" },
            { @"C:\Users\..",                  @"C:\" },
            { @"C:\Users\..\",                 @"C:\" },
            { @"C:\Users\..\..",               @"C:\" },
            { @"C:\Users\..\..\",              @"C:\" },
            { @"C:\Users\..\myuser",           @"C:\myuser" },
            { @"C:\Users\..\myuser\",          @"C:\myuser\" },
            { @"C:\Users\..\..\myuser",        @"C:\myuser" },
            { @"C:\Users\..\..\myuser\",       @"C:\myuser\" },
            { @"C:\Users\myuser\..",           @"C:\Users" },
            { @"C:\Users\myuser\..\",          @"C:\Users\" },
            { @"C:\Users\myuser\..\..",        @"C:\" },
            { @"C:\Users\myuser\..\..\",       @"C:\" },
            { @"C:\Users\..\myuser\..",        @"C:\" },
            { @"C:\Users\..\myuser\..\",       @"C:\" },
            { @"C:\Users\..\myuser\..\..",     @"C:\" },
            { @"C:\Users\..\myuser\..\..\",    @"C:\" },
            { @"C:\Users\..\myuser\..\..\..",  @"C:\" },
            { @"C:\Users\..\myuser\..\..\..\", @"C:\" },
            { @"C:\Users\..\..\myuser\..\..",  @"C:\" },
            { @"C:\Users\..\..\myuser\..\..\", @"C:\" },
        };
        public static TheoryData<string, string> UnixValidParentBacktrackingData => new TheoryData<string, string>
        {
            { @"/home/..",                  @"/" },
            { @"/home/../",                 @"/" },
            { @"/home/../myuser",           @"/myuser" },
            { @"/home/../myuser/",          @"/myuser/" },
            { @"/home/myuser/..",           @"/home" },
            { @"/home/myuser/../",          @"/home/" },
            { @"/home/myuser/../..",        @"/" },
            { @"/home/myuser/../../",       @"/" },
            { @"/home/../myuser/..",        @"/" },
            { @"/home/../myuser/../",       @"/" },
        };

        // Paths with '.' to indicate current directory
        public static TheoryData<string, string> WindowsValidCurrentDirectoryData => new TheoryData<string, string>
        {
            { @"C:\.",                     @"C:\" },
            { @"C:\.\",                    @"C:\" },
            { @"C:\.\.",                   @"C:\" },
            { @"C:\.\.\",                  @"C:\" },
            { @"C:\.\Users",               @"C:\Users" },
            { @"C:\.\Users\",              @"C:\Users\" },
            { @"C:\.\.\Users",             @"C:\Users" },
            { @"C:\.\.\Users\",            @"C:\Users\" },
            { @"C:\Users\.",               @"C:\Users" },
            { @"C:\Users\.\",              @"C:\Users\" },
            { @"C:\Users\.\.",             @"C:\Users" },
            { @"C:\Users\.\.\",            @"C:\Users\" },
            { @"C:\.\Users\myuser",        @"C:\Users\myuser" },
            { @"C:\.\Users\myuser\",       @"C:\Users\myuser\" },
            { @"C:\.\Users\.\myuser",      @"C:\Users\myuser" },
            { @"C:\.\Users\.\myuser\",     @"C:\Users\myuser\" },
            { @"C:\.\Users\.\myuser\.",    @"C:\Users\myuser" },
            { @"C:\.\Users\.\myuser\.\",   @"C:\Users\myuser\" },
            { @"C:\Users\.\.\myuser\.\.",  @"C:\Users\myuser" },
            { @"C:\Users\.\.\myuser\.\.\", @"C:\Users\myuser\" },
        };

        public static TheoryData<string, string> UnixValidCurrentDirectoryData => new TheoryData<string, string>
        {
            { @"/.",                    @"/" },
            { @"/./",                   @"/" },
            { @"/./.",                  @"/" },
            { @"/././",                 @"/" },
            { @"/./home",               @"/home" },
            { @"/./home/",              @"/home/" },
            { @"/././home",             @"/home" },
            { @"/././home/",            @"/home/" },
            { @"/home/.",               @"/home" },
            { @"/home/./",              @"/home/" },
            { @"/home/./.",             @"/home" },
            { @"/home/././",            @"/home/" },
            { @"/./home/myuser",        @"/home/myuser" },
            { @"/./home/myuser/",       @"/home/myuser/" },
            { @"/./home/./myuser",      @"/home/myuser" },
            { @"/./home/./myuser/",     @"/home/myuser/" },
            { @"/./home/./myuser/.",    @"/home/myuser" },
            { @"/./home/./myuser/./",   @"/home/myuser/" },
            { @"/home/././myuser/./.",  @"/home/myuser" },
            { @"/home/././myuser/././", @"/home/myuser/" },
        };

        // Combined '.' and '..'
        public static TheoryData<string, string> WindowsCombinedRedundantData => new TheoryData<string, string>
        {
            { @"C:\.\..",               @"C:\" },
            { @"C:\.\..\",              @"C:\" },
            { @"C:\..\.",               @"C:\" },
            { @"C:\..\.\",              @"C:\" },
            { @"C:\.\..\.",             @"C:\" },
            { @"C:\.\..\.\",            @"C:\" },
            { @"C:\..\.\.",             @"C:\" },
            { @"C:\..\.\.\",            @"C:\" },
            { @"C:\.\..\..",            @"C:\" },
            { @"C:\.\..\..\",           @"C:\" },
            { @"C:\..\.\..",            @"C:\" },
            { @"C:\..\.\..\",           @"C:\" },
            { @"C:\Users\.\..",         @"C:\" },
            { @"C:\Users\.\..\",        @"C:\" },
            { @"C:\Users\..\.",         @"C:\" },
            { @"C:\Users\..\.\",        @"C:\" },
            { @"C:\.\Users\..",         @"C:\" },
            { @"C:\.\Users\..\",        @"C:\" },
            { @"C:\..\Users\.",         @"C:\Users" },
            { @"C:\..\Users\.\",        @"C:\Users\" },
            { @"C:\.\..\Users",         @"C:\Users" },
            { @"C:\.\..\Users\",        @"C:\Users\" },
            { @"C:\..\.\Users",         @"C:\Users" },
            { @"C:\..\.\Users\",        @"C:\Users\" },
            { @"C:\.\Users\myuser\..",  @"C:\Users" },
            { @"C:\.\Users\myuser\..\", @"C:\Users\" },
            { @"C:\..\Users\myuser\.",  @"C:\Users\myuser" },
            { @"C:\..\Users\myuser\.\", @"C:\Users\myuser\" },
            { @"C:\.\Users\..\myuser",  @"C:\myuser" },
            { @"C:\.\Users\..\myuser\", @"C:\myuser\" },
            { @"C:\..\Users\.\myuser",  @"C:\Users\myuser" },
            { @"C:\..\Users\.\myuser\", @"C:\Users\myuser\" },
        };

        public static TheoryData<string, string> UnixCombinedRedundantData => new TheoryData<string, string>
        {
            { @"/home/./..",         @"/" },
            { @"/home/./../",        @"/" },
            { @"/home/../.",         @"/" },
            { @"/home/.././",        @"/" },
            { @"/./home/..",         @"/" },
            { @"/./home/../",        @"/" },
            { @"/./home/myuser/..",  @"/home" },
            { @"/./home/myuser/../", @"/home/" },
            { @"/./home/../myuser",  @"/myuser" },
            { @"/./home/../myuser/", @"/myuser/" },
        };

        public static TheoryData<string, string, string> DifferentBehaviorCurrentDirectoryData => new TheoryData<string, string, string>
        {
            // Path                   Unix              Windows
            { @"/./home",             @"/home",         @"home" },
            { @"/././home/",          @"/home/",        @"home\" },
            { @"/home/.",             @"/home",         @"home" },
            { @"/home/./",            @"/home/",        @"home\" },
            { @"/home/./.",           @"/home",         @"home" },
            { @"/home/././",          @"/home/",        @"home\" },
            { @"/./home/./",          @"/home/",        @"home\" },
            { @"/./home/./.",         @"/home",         @"home" },
            { @"/./home/././",        @"/home/",        @"home\" },
            { @"/./home/././folder",  @"/home/folder",  @"home\folder" },
            { @"/./home/././folder/", @"/home/folder/", @"home\folder\" },
        };

        public static TheoryData<string, string, string> DifferentBehaviorBacktrackingData => new TheoryData<string, string, string>
        {
            // Path                         Unix              Windows
            { @"/home/../myuser/../..",     @"/",             @".." },
            { @"/home/../myuser/../../",    @"/",             @"..\" },
            { @"/home/../myuser/../../..",  @"/",             @"..\.." },
            { @"/home/../myuser/../../../", @"/",             @"..\..\" },
            { @"/home/../..",               @"/",             @".." },
            { @"/home/../../",              @"/",             @"..\" },
            { @"/home/../../myuser",        @"/myuser",       @"..\myuser" },
            { @"/home/../../myuser/",       @"/myuser/",      @"..\myuser\" },
            { @"/home/../../myuser/../..",  @"/",             @"..\.." },
            { @"/home/../../myuser/../../", @"/",             @"..\..\" },
            { @"/..",                       @"/",             @"\.." },
            { @"/../",                      @"/",             @"\..\" },
            { @"/../home",                  @"/home",         @"\..\home" },
            { @"/../home/",                 @"/home/",        @"\..\home\" },
            { @"/../home/./myuser",         @"/home/myuser",  @"\..\home\myuser" },
            { @"/../home/./myuser/",        @"/home/myuser/", @"\..\home\myuser\" },
        };

        // Combined '.' and '..'
        public static TheoryData<string, string, string> DifferentBehaviorCombinedRedundantData => new TheoryData<string, string, string>
        {
            // Path                         Unix                 Windows
            { @"/./..",                       @"/",             @".." },
            { @"/./../",                      @"/",             @"..\" },
            { @"/../.",                       @"/",             @"\.." },
            { @"/.././",                      @"/",             @"\..\" },
            { @"/./../.",                     @"/",             @".." },
            { @"/./.././",                    @"/",             @"..\" },
            { @"/.././.",                     @"/",             @"\.." },
            { @"/../././",                    @"/",             @"\..\" },
            { @"/./../..",                    @"/",             @"..\.." },
            { @"/./../../",                   @"/",             @"..\..\" },
            { @"/.././..",                    @"/",             @"\..\.." },
            { @"/.././../",                   @"/",             @"\..\..\" },
            { @"/../home/.",                  @"/home",         @"\..\home" },
            { @"/../home/./",                 @"/home/",        @"\..\home\" },
            { @"/./../home",                  @"/home",         @"..\home" },
            { @"/./../home/",                 @"/home/",        @"..\home\" },
            { @"/.././home",                  @"/home",         @"\..\home" },
            { @"/.././home/",                 @"/home/",        @"\..\home\" },
            { @"/../home/myuser/.",           @"/home/myuser",  @"\..\home\myuser" },
            { @"/../home/myuser/./",          @"/home/myuser/", @"\..\home\myuser\" },
            { @"/../home/myuser/./../",       @"/home/",        @"\..\home\" },
            { @"/../home/myuser/./../folder", @"/home/folder",  @"\..\home\folder" },
        };


        // Duplicate separators
        public static TheoryData<string, string> WindowsDuplicateSeparatorsData => new TheoryData<string, string>
        {
            { @"C:\\",                 @"C:\" },
            { @"C:\\\",                @"C:\" },
            { @"C://",                 @"C:\" },
            { @"C:///",                @"C:\" },
            { @"C:\/",                 @"C:\" },
            { @"C:/\",                 @"C:\" },
            { @"C:\/\",                @"C:\" },
            { @"C:/\\",                @"C:\" },
            { @"C:\//",                @"C:\" },
            { @"C:/\/",                @"C:\" },
            { @"C:\\Users",            @"C:\Users" },
            { @"C:\\\Users",           @"C:\Users" },
            { @"C:\\Users\\\",         @"C:\Users\" },
            { @"C:\\\Users\\",         @"C:\Users\" },
            { @"C:\\Users\\/\",        @"C:\Users\" },
            { @"C:\\\Users\/\",        @"C:\Users\" },
            { @"C:\/Users",            @"C:\Users" },
            { @"C:\/Users/",           @"C:\Users\" },
            { @"C:\/Users/",           @"C:\Users\" },
            { @"C:\\Users\\.\",        @"C:\Users\" },
            { @"C:\\\Users\..\\",      @"C:\" },
            { @"C:\\Users\\./.\",      @"C:\Users\" },
            { @"C:\.\\Users\/./\\",    @"C:\Users\" },
            { @"C:\\.\Users\/../\\./", @"C:\" },
        };

        public static TheoryData<string, string> UnixDuplicateSeparatorsData => new TheoryData<string, string>
        {
            { @"/home/",                   @"/home/" },
            { @"/home\\\",                 @"/home/" },
            { @"/home//",                  @"/home/" },
            { @"/home///",                 @"/home/" },
            { @"/home\/",                  @"/home/" },
            { @"/home/\",                  @"/home/" },
            { @"/home\/\",                 @"/home/" },
            { @"/home/\\",                 @"/home/" },
            { @"/home\//",                 @"/home/" },
            { @"/home/\/",                 @"/home/" },
            { @"/home\\myuser",            @"/home/myuser" },
            { @"/home\\\myuser",           @"/home/myuser" },
            { @"/home\\myuser\\\",         @"/home/myuser/" },
            { @"/home\\\myuser\\",         @"/home/myuser/" },
            { @"/home\\myuser\\/\",        @"/home/myuser/" },
            { @"/home\\\myuser\/\",        @"/home/myuser/" },
            { @"/home\/myuser",            @"/home/myuser" },
            { @"/home\/myuser/",           @"/home/myuser/" },
            { @"/home\/myuser/",           @"/home/myuser/" },
            { @"/home\\myuser\\.\",        @"/home/myuser/" },
            { @"/home\\\myuser\..\\",      @"/home/" },
            { @"/home\\myuser\\./.\",      @"/home/myuser/" },
            { @"/home\.\\myuser\/./\\",    @"/home/myuser/" },
            { @"/home\\.\myuser\/../\\./", @"/home/" },
        };

        // Network locations - "Server\Share" always stays. UNC prefixes get prepended in the tests.
        public static TheoryData<string, string> UncData => new TheoryData<string, string>
        {
            { @"Server\Share\git\runtime",              @"Server\Share\git\runtime"},
            { @"Server\Share\\git\runtime",             @"Server\Share\git\runtime"},
            { @"Server\Share\git\\runtime",             @"Server\Share\git\runtime"},
            { @"Server\Share\git\.\runtime\.\\",        @"Server\Share\git\runtime\"},
            { @"Server\Share\git\runtime",              @"Server\Share\git\runtime"},
            { @"Server\Share\git\..\runtime",           @"Server\Share\runtime"},
            { @"Server\Share\git\runtime\..\",          @"Server\Share\git\"},
            { @"Server\Share\git\runtime\..\..\..\",    @"Server\Share\"},
            { @"Server\Share\git\runtime\..\..\.\",     @"Server\Share\"},
            { @"Server\Share\git\..\.\runtime\temp\..", @"Server\Share\runtime"},
            { @"Server\Share\git\..\\\.\..\runtime",    @"Server\Share\runtime"},
            { @"Server\Share\git\runtime\",             @"Server\Share\git\runtime\"},
            { @"Server\Share\git\temp\..\runtime\",     @"Server\Share\git\runtime\"},
        };

        // Paths that are not rooted
        public static TheoryData<string, string> UnqualifiedPathsData => new TheoryData<string, string>
        {
            { @"Users\myuser\..\",             @"Users\" },
            { @"Users\myuser\..",              @"Users" },
            { @"Users\..\..",                  @".." },
            { @"Users\..\..\",                 @"..\" },
            { @"myuser\..\",                   @"" },
            { @"myuser",                       @"myuser" },
            { @".\myuser",                     @"myuser" },
            { @".\myuser\",                    @"myuser\" },
            { @".\.\myuser",                   @"myuser" },
            { @".\.\myuser\",                  @"myuser\" },
            { @"..\myuser",                    @"..\myuser" },
            { @"..\myuser\",                   @"..\myuser\" },
            { @"..\\myuser\",                  @"..\myuser\" },
            { @"..\myuser\..",                 @".." },
            { @"..\first\..\second",           @"..\second" },
            { @"..\first\..\..\second\..",     @"..\.." },
            { @"..\first\..\..\second\..\",    @"..\..\" },
            { @"..\first\..\..\second\..",     @"..\.." },
            { @"..\first\..\..\second\..\..",  @"..\..\.." },
            { @"..\first\..\..\second\..\..\", @"..\..\..\" },
        };

        public static TheoryData<string, string> ValidEdgecasesData => new TheoryData<string, string>
        {
            { @"C:\Users\myuser\folder.with\one\dot",   @"C:\Users\myuser\folder.with\one\dot" },
            { @"C:\Users\myuser\folder..with\two\dots", @"C:\Users\myuser\folder..with\two\dots" },
            { @"C:\Users\.folder\startswithdot",        @"C:\Users\.folder\startswithdot" },
            { @"C:\Users\folder.\endswithdot",          @"C:\Users\folder.\endswithdot" },
            { @"C:\Users\..folder\startswithtwodots",   @"C:\Users\..folder\startswithtwodots" },
            { @"C:\Users\folder..\endswithtwodots",     @"C:\Users\folder..\endswithtwodots" },
            { @"C:\Users\myuser\this\is\a\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\long\path\but\it\should\not\matter\extraword\..\", @"C:\Users\myuser\this\is\a\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\really\long\path\but\it\should\not\matter\" },
            { @"C:\Users\myuser\this_is_a_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_long_foldername\but_it_should_not_matter\extraword\..\", @"C:\Users\myuser\this_is_a_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_really_long_foldername\but_it_should_not_matter\" },
        };

        [Theory]
        [MemberData(nameof(NullOrEmptyData))]
        [MemberData(nameof(UnqualifiedPathsData))]
        [MemberData(nameof(ValidEdgecasesData))]
        public static void SpecialCases_String(string path, string expected) => TestRedundantSegments(path, expected);

        [Theory]
        [MemberData(nameof(NullOrEmptyData))]
        [MemberData(nameof(UnqualifiedPathsData))]
        [MemberData(nameof(ValidEdgecasesData))]
        public static void SpecialCases_Span(string path, string expected) => TestRedundantSegments(path.AsSpan(), expected);

        [Theory]
        [MemberData(nameof(UnqualifiedPathsData))]
        [MemberData(nameof(ValidEdgecasesData))]
        public static void SpecialCases_True_Try(string path, string expected) => TestTryRedundantSegments(path, expected, true, expected.Length);

        [Theory]
        [MemberData(nameof(NullOrEmptyData))]
        public static void SpecialCases_False_Try(string path, string expected) => TestTryRedundantSegments(path, expected, false, 0);

        [Theory]
        [MemberData(nameof(DifferentBehaviorCurrentDirectoryData))]
        [MemberData(nameof(DifferentBehaviorBacktrackingData))]
        [MemberData(nameof(DifferentBehaviorCombinedRedundantData))]
        public static void DifferentBehavior_String(string path, string expectedUnix, string expectedWindows)
        {
            string expected = (PlatformDetection.IsWindows) ? expectedWindows : expectedUnix;
            TestRedundantSegments(path, expected);
        }

        [Theory]
        [MemberData(nameof(DifferentBehaviorCurrentDirectoryData))]
        [MemberData(nameof(DifferentBehaviorBacktrackingData))]
        [MemberData(nameof(DifferentBehaviorCombinedRedundantData))]
        public static void DifferentBehavior_Span(string path, string expectedUnix, string expectedWindows)
        {
            string expected = (PlatformDetection.IsWindows) ? expectedWindows : expectedUnix;
            TestRedundantSegments(path.AsSpan(), expected);
        }

        [Theory]
        [MemberData(nameof(DifferentBehaviorCurrentDirectoryData))]
        [MemberData(nameof(DifferentBehaviorBacktrackingData))]
        [MemberData(nameof(DifferentBehaviorCombinedRedundantData))]
        public static void DifferentBehavior_Try(string path, string expectedUnix, string expectedWindows)
        {
            string expected = (PlatformDetection.IsWindows) ? expectedWindows : expectedUnix;
            TestTryRedundantSegments(path, expected, true, expected.Length);
        }

        [Theory]
        [MemberData(nameof(WindowsNormalData))]
        [MemberData(nameof(WindowsValidParentBacktrackingData))]
        [MemberData(nameof(WindowsValidCurrentDirectoryData))]
        [MemberData(nameof(WindowsCombinedRedundantData))]
        [MemberData(nameof(WindowsDuplicateSeparatorsData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsValid_String(string path, string expected)
        {
            foreach (string prefix in s_Prefixes)
            {
                TestRedundantSegments(prefix + path, prefix + expected);
            }
        }

        [Theory]
        [MemberData(nameof(WindowsNormalData))]
        [MemberData(nameof(WindowsValidParentBacktrackingData))]
        [MemberData(nameof(WindowsValidCurrentDirectoryData))]
        [MemberData(nameof(WindowsCombinedRedundantData))]
        [MemberData(nameof(WindowsDuplicateSeparatorsData))]
        public static void WindowsValid_Span(string path, string expected)
        {
            foreach (string prefix in s_Prefixes)
            {
                TestRedundantSegments((prefix + path).AsSpan(), prefix + expected);
            }
        }

        [Theory]
        [MemberData(nameof(WindowsNormalData))]
        [MemberData(nameof(WindowsValidParentBacktrackingData))]
        [MemberData(nameof(WindowsValidCurrentDirectoryData))]
        [MemberData(nameof(WindowsCombinedRedundantData))]
        [MemberData(nameof(WindowsDuplicateSeparatorsData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsValid_Try(string path, string expected)
        {
            foreach (string prefix in s_Prefixes)
            {
                TestTryRedundantSegments(prefix + path, prefix + expected, true, (prefix + expected).Length);
            }
        }


        [Theory]
        [MemberData(nameof(UncData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsUnc_String(string path, string expected)
        {
            foreach (string prefix in s_UncPrefixes)
            {
                TestRedundantSegments(prefix + path, prefix + expected);
            }
        }

        [Theory]
        [MemberData(nameof(UncData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsUnc_Span(string path, string expected)
        {
            foreach (string prefix in s_UncPrefixes)
            {
                TestRedundantSegments((prefix + path).AsSpan(), prefix + expected);
            }
        }

        [Theory]
        [MemberData(nameof(UncData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsUnc_Try(string path, string expected)
        {
            foreach (string prefix in s_UncPrefixes)
            {
                TestTryRedundantSegments(prefix + path, prefix + expected, true, (prefix + expected).Length);
            }
        }


        // The expected string is returned in Unix with '/' as separator.
        // The trailing '/' is considered the root, hence it's a fully qualified path.

        [Theory]
        [MemberData(nameof(UnixNormalData))]
        [MemberData(nameof(UnixValidParentBacktrackingData))]
        [MemberData(nameof(UnixValidCurrentDirectoryData))]
        [MemberData(nameof(UnixCombinedRedundantData))]
        [MemberData(nameof(UnixDuplicateSeparatorsData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void UnixValid_String(string path, string expected)
        {
            if (!PlatformDetection.IsWindows)
            {
                expected = expected.Replace('\\', '/');
            }
            TestRedundantSegments(path, expected);
        }

        [Theory]
        [MemberData(nameof(UnixNormalData))]
        [MemberData(nameof(UnixValidParentBacktrackingData))]
        [MemberData(nameof(UnixValidCurrentDirectoryData))]
        [MemberData(nameof(UnixCombinedRedundantData))]
        [MemberData(nameof(UnixDuplicateSeparatorsData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void UnixValid_Span(string path, string expected)
        {
            if (!PlatformDetection.IsWindows)
            {
                expected = expected.Replace('\\', '/');
            }
            TestRedundantSegments(path.AsSpan(), expected);
        }

        [Theory]
        [MemberData(nameof(UnixNormalData))]
        [MemberData(nameof(UnixValidParentBacktrackingData))]
        [MemberData(nameof(UnixValidCurrentDirectoryData))]
        [MemberData(nameof(UnixCombinedRedundantData))]
        [MemberData(nameof(UnixDuplicateSeparatorsData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void UnixValid_Try(string path, string expected)
        {
            if (!PlatformDetection.IsWindows)
            {
                expected = expected.Replace('\\', '/');
            }
            TestTryRedundantSegments(path, expected, true, expected.Length);
        }



        [Fact]
        public static void DestinationTooSmall_Try()
        {
            Span<char> actualDestination = stackalloc char[1];
            bool actualReturn = Path.TryRemoveRedundantSegments(@"C:\Users\myuser", actualDestination, out int actualCharsWritten);
            string stringDestination = actualDestination.Slice(0, actualCharsWritten).ToString();
            Assert.False(actualReturn);
            Assert.Equal(0, actualCharsWritten);
            Assert.Equal(0, stringDestination.Length);
        }


        // Helper methods

        private static void TestTryRedundantSegments(string path, string expected, bool expectedReturn, int expectedCharsWritten)
        {
            Span<char> actualDestination = stackalloc char[(path != null) ? path.Length : 1];
            bool actualReturn = Path.TryRemoveRedundantSegments(path.AsSpan(), actualDestination, out int actualCharsWritten);
            Assert.Equal(expectedReturn, actualReturn);
            Assert.Equal(expected ?? string.Empty, actualDestination.Slice(0, actualCharsWritten).ToString());
            Assert.Equal(expectedCharsWritten, actualCharsWritten);
        }

        private static void TestRedundantSegments(ReadOnlySpan<char> path, string expected)
        {
            string actual = Path.RemoveRedundantSegments(path);
            Assert.Equal(expected ?? string.Empty, actual);
        }

        private static void TestRedundantSegments(string path, string expected)
        {
            string actual = Path.RemoveRedundantSegments(path);
            Assert.Equal(expected, actual);
        }

    }
}
