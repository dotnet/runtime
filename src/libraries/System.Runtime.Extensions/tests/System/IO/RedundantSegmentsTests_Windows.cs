// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class RedundantSegmentsTests_Windows : RedundantSegmentsTestsBase
    {
        #region Tests

        #region Qualified NoRedundancy

        [Theory]
        [MemberData(nameof(MemberData_DevicePrefix))]
        public void Unmodified(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_NoRedundancy_DriveAndRoot))]
        public void Qualified_NoRedundancy(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_NoRedundancy_DriveAndRoot_EdgeCases))]
        public void Qualified_NoRedundancy_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot))]
        public void Qualified_NoRedundancy_Prefix(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot_EdgeCases))]
        public void Qualified_NoRedundancy_Prefix_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_ServerShare_NoRedundancy))]
        public void Qualified_NoRedundancy_ServerShare(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_ServerShare_NoRedundancy_EdgeCases))]
        public void Qualified_NoRedundancy_ServerShare_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_UNC_NoRedundancy))]
        public void Qualified_NoRedundancy_UNC(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_UNC_NoRedundancy_EdgeCases))]
        public void Qualified_NoRedundancy_UNC_EdgeCases(string original, string expected) => TestAll(original, expected);

        #endregion

        #region Qualified redundant

        [Theory]
        [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_SingleDot))]
        [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot))]
        [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_Combined))]
        public void Qualified_Redundant(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot_EdgeCases))]
        public void Qualified_Redundant_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot))]
        [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot))]
        [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_Combined))]
        public void Qualified_Redundant_Prefix(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot_EdgeCases))]
        public void Qualified_Redundant_Prefix_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_ServerShare_Redundant_SingleDot))]
        [MemberData(nameof(MemberData_ServerShare_Redundant_DoubleDot))]
        [MemberData(nameof(MemberData_ServerShare_Redundant_Combined))]
        public void Qualified_Redundant_ServerShare(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_ServerShare_Redundant_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_ServerShare_Redundant_DoubleDot_EdgeCases))]
        public void Qualified_Redundant_ServerShare_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_UNC_Redundant_SingleDot))]
        [MemberData(nameof(MemberData_UNC_Redundant_DoubleDot))]
        [MemberData(nameof(MemberData_UNC_Redundant_Combined))]
        public void Qualified_Redundant_UNC(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_UNC_Redundant_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_UNC_Redundant_DoubleDot_EdgeCases))]
        public void Qualified_Redundant_UNC_EdgeCases(string original, string expected) => TestAll(original, expected);

        #endregion

        #region Unqualified NoRedundancy

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy))]
        public void Unqualified_NoRedundancy(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_EdgeCases))]
        public void Unqualified_NoRedundancy_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DrivelessRoot))]
        public void Unqualified_NoRedundancy_DrivelessRoot(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DrivelessRoot_EdgeCases))]
        public void Unqualified_NoRedundancy_DrivelessRoot_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DriveRootless))]
        public void Unqualified_NoRedundancy_DriveRootless(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DriveRootless_EdgeCases))]
        public void Unqualified_NoRedundancy_DriveRootless_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless))]
        public void Unqualified_NoRedundancy_Prefix_DriveRootless(string original) => TestAll(original, original);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeCases))]
        public void Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeData(string original, string expected) => TestAll(original, expected);

        #endregion

        #region Unqualified redundant

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_SingleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DoubleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Combined))]
        public void Unqualified_Redundant(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DoubleDot_EdgeCases))]
        public void Unqualified_Redundant_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot))]
        public void Unqualified_Redundant_DrivelessRoot(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot_EdgeCases))]
        public void Unqualified_Redundant_DrivelessRoot_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_SingleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_DoubleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_Combined))]
        public void Unqualified_Redundant_DriveRootless(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_DoubleDot_EdgeCases))]
        public void Unqualified_Redundant_DriveRootless_EdgeCases(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_Combined))]
        public void Unqualified_Redundant_Prefix_DriveRootless(string original, string expected) => TestAll(original, expected);

        [Theory]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot_EdgeCases))]
        [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot_EdgeCases))]
        public void Unqualified_Redundant_Prefix_DriveRootless_EdgeCases(string original, string expected) => TestAll(original, expected);

        #endregion

        #endregion

        #region Test data

        private const string ServerShare = @"\\Server\Share\";
        private const string UNCServerShare = @"UNC\Server\Share\";
        private const string Prefix_Windows_Drive_Rootless = "C:";
        private const string Prefix_Windows_Driveless_Root = @"\";
        private const string Prefix_Windows_Drive_Root = Prefix_Windows_Drive_Rootless + Prefix_Windows_Driveless_Root;
        private static readonly string DevicePrefix = @"\\.\";


        #region Device prefix

        // No matter what the string is, if it's preceded by a device prefix, we don't do anything
        private static readonly string[] Suffixes = new string[]
{
            @"",
            @"\", @"\\",
            @"/", @"//", @"\/", @"/\",
            @".",
            @".\", @".\\",
            @"./", @".//",
            @"\.", @"\\.", @"\.\", @"\\.\\",
            @"/.", @"//.", @"/./", @"//.//",
            @"\.\.", @"\\.\\.", @"\.\.\", @"\\.\\.\\",
            @"\..", @"\\..", @"\..\", @"\\..\\",
            @"\..\..", @"\\..\\..", @"\..\..\", @"\\..\\..\\",
            @"\.\..", @"\\.\\..", @"\.\..\", @"\\.\\..\\",
            @"\..\.", @"\\..\\.", @"\..\.\", @"\\..\\.\\"
        };
        private static readonly string[] ExtendedPrefixes = new string[]
        {
            @"\\?\",
            @"\??\"
        };
        private static readonly string[] TestPaths_DevicePrefix = new string[]
        {
            @"C",
            @"C:",
            @"C:\",
            @"C:/",
            @"C:\folder",
            @"C:/folder",
            @"C:A",
            @"C:A",
            @"C:A\folder",
            @"C:A/folder",
        };
        public static IEnumerable<object[]> MemberData_DevicePrefix =>
            from prefix in ExtendedPrefixes
            from s in TestPaths_DevicePrefix
            from suffix in Suffixes
            select new object[] { prefix + s + suffix };

        private static readonly string[] TestPaths_DevicePrefix_UNC = new string[]
        {
            @"UNC",
            @"UNC\Server",
            @"UNC/Server",
            @"UNC\Server\Share",
            @"UNC/Server/Share",
            @"UNC\Server\Share\folder",
            @"UNC/Server/Share/folder",
        };
        public static IEnumerable<object[]> MemberData_DevicePrefix_UNC =>
            from prefix in ExtendedPrefixes
            from s in TestPaths_DevicePrefix_UNC
            from suffix in Suffixes
            select new object[] { prefix + s + suffix };

        #endregion

        #region No redundancy

        private static readonly string[] TestPaths_NoRedundancy = new string[]
        {
            @"folder",
            @"folder\",
            @"folder\file.txt",
            @"folder\subfolder",
            @"folder\subfolder\",
            @"folder\subfolder\file.txt"
        };
        public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_DriveAndRoot =>
            from s in TestPaths_NoRedundancy
            select new object[] { Prefix_Windows_Drive_Root + s };
        public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot =>
            from s in TestPaths_NoRedundancy
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + s };
        public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveRootless =>
            from s in TestPaths_NoRedundancy
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + s };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DriveRootless =>
            from s in TestPaths_NoRedundancy
            select new object[] { Prefix_Windows_Drive_Rootless + s };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless =>
            from s in TestPaths_NoRedundancy
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + s };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy =>
            from s in TestPaths_NoRedundancy
            select new object[] { s };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DrivelessRoot =>
            from s in TestPaths_NoRedundancy
            select new object[] { Prefix_Windows_Driveless_Root + s };
        public static IEnumerable<object[]> MemberData_ServerShare_NoRedundancy =>
            from s in TestPaths_NoRedundancy
            select new object[] { ServerShare + s };
        public static IEnumerable<object[]> MemberData_UNC_NoRedundancy =>
            from s in TestPaths_NoRedundancy
            select new object[] { DevicePrefix + UNCServerShare + s };

        #endregion

        #region Single dot

        private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_SingleDot = new List<Tuple<string, string, string, string>>
        {
            // The original and qualified strings must get the root string prefixed
            // Original | Qualified | Unqualified | Device prefix
            { @".",      @"",    @".",  @"." },
            { @".\",     @"",    @".\", @".\" },
            { @".\.",    @"",    @".",  @".\" },
            { @".\.\",   @"",    @".\", @".\" },

            { @".\folder",       @"folder",      @".\folder",   @".\folder" },
            { @".\folder\",      @"folder\",     @".\folder\",  @".\folder\" },
            { @".\folder\.",     @"folder",      @".\folder",   @".\folder" },
            { @".\folder\.\",    @"folder\",     @".\folder\",  @".\folder\" },
            { @".\folder\.\.",   @"folder",      @".\folder",   @".\folder" },
            { @".\folder\.\.\",  @"folder\",     @".\folder\",  @".\folder\" },

            { @".\.\folder",         @"folder",      @".\folder",   @".\folder" },
            { @".\.\folder\",        @"folder\",     @".\folder\",  @".\folder\" },
            { @".\.\folder\.",       @"folder",      @".\folder",   @".\folder" },
            { @".\.\folder\.\",      @"folder\",     @".\folder\",  @".\folder\" },
            { @".\.\folder\.\.",     @"folder",      @".\folder",   @".\folder" },
            { @".\.\folder\.\.\",    @"folder\",     @".\folder\",  @".\folder\" },

            { @"folder\.",       @"folder",      @"folder",     @"folder\" },
            { @"folder\.\",      @"folder\",     @"folder\",    @"folder\" },
            { @"folder\.\.",     @"folder",      @"folder",     @"folder\" },
            { @"folder\.\.\",    @"folder\",     @"folder\",    @"folder\" },

            { @"folder\subfolder\.",     @"folder\subfolder",  @"folder\subfolder",     @"folder\subfolder" },
            { @"folder\subfolder\.\",    @"folder\subfolder\", @"folder\subfolder\",    @"folder\subfolder\" },
            { @"folder\subfolder\.\.",   @"folder\subfolder",  @"folder\subfolder",     @"folder\subfolder" },
            { @"folder\subfolder\.\.\",  @"folder\subfolder\", @"folder\subfolder\",    @"folder\subfolder\" },

            { @".\folder\subfolder\.",       @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\subfolder\.\",      @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
            { @".\folder\subfolder\.\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\subfolder\.\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },

            { @".\.\folder\subfolder\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\.\folder\subfolder\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
            { @".\.\folder\subfolder\.\.",   @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\.\folder\subfolder\.\.\",  @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },

            { @".\folder\.\subfolder\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\.\subfolder\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
            { @".\folder\.\subfolder\.\.",   @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\.\subfolder\.\.\",  @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },

            { @".\folder\.\.\subfolder\.",       @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\.\.\subfolder\.\",      @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
            { @".\folder\.\.\subfolder\.\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
            { @".\folder\.\.\subfolder\.\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },

            { @".\file.txt",     @"file.txt",    @".\file.txt",  @".\file.txt" },
            { @".\.\file.txt",   @"file.txt",    @".\file.txt",  @".\file.txt" },

            { @".\folder\file.txt",      @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
            { @".\folder\.\file.txt",    @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
            { @".\folder\.\.\file.txt",  @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },

            { @".\.\folder\file.txt",        @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
            { @".\.\folder\.\file.txt",      @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
            { @".\.\folder\.\.\file.txt",    @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },

            { @"folder\.\file.txt",      @"folder\file.txt",     @"folder\file.txt",  @"folder\file.txt" },
            { @"folder\.\.\file.txt",    @"folder\file.txt",     @"folder\file.txt",  @"folder\file.txt" },

            { @"folder\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @"folder\subfolder\file.txt",  @"folder\subfolder\file.txt" },
            { @"folder\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @"folder\subfolder\file.txt",  @"folder\subfolder\file.txt" },

            { @".\folder\subfolder\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
            { @".\folder\subfolder\.\.\file.txt", @"folder\subfolder\file.txt",  @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },

            { @".\.\folder\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
            { @".\.\folder\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },

            { @".\folder\.\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
            { @".\folder\.\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },

            { @".\.\folder\.\.\subfolder\.\file.txt",      @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
            { @".\.\folder\.\.\subfolder\.\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_ServerShare_Redundant_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
        public static IEnumerable<object[]> MemberData_UNC_Redundant_SingleDot =>
            from t in TestPaths_Redundant_SingleDot
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

        #endregion

        #region Double dot

        private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_DoubleDot = new List<Tuple<string, string, string, string>>
        {
            // The original and qualified strings must get the root string prefixed
            // Original | Qualified | Unqualified | Device prefix
            { @"..",     @"",    @"..",     @".." },
            { @"..\",    @"",    @"..\",    @"..\" },
            { @"..\..",  @"",    @"..\..",  @"..\.." },
            { @"..\..\", @"",    @"..\..\", @"..\..\" },

            { @"..\folder",          @"folder",      @"..\folder",  @"..\folder" },
            { @"..\folder\",         @"folder\",     @"..\folder\", @"..\folder\" },
            { @"..\folder\..",       @"",            @"..",         @"..\" },
            { @"..\folder\..\",      @"",            @"..\",        @"..\" },
            { @"..\folder\..\..",    @"",            @"..\..",      @"..\.." },
            { @"..\folder\..\..\",   @"",            @"..\..\",     @"..\..\" },

            { @"..\..\folder",           @"folder",      @"..\..\folder",   @"..\..\folder" },
            { @"..\..\folder\",          @"folder\",     @"..\..\folder\",  @"..\..\folder\" },
            { @"..\..\folder\..",        @"",            @"..\..",          @"..\.." },
            { @"..\..\folder\..\",       @"",            @"..\..\",         @"..\..\" },
            { @"..\..\folder\..\..",     @"",            @"..\..\..",       @"..\..\.." },
            { @"..\..\folder\..\..\",    @"",            @"..\..\..\",      @"..\..\..\" },

            { @"folder\..",          @"",    @"",       @"folder\.." },
            { @"folder\..\",         @"",    @"",       @"folder\..\" },
            { @"folder\..\..",       @"",    @"..",     @"folder\..\.." },
            { @"folder\..\..\",      @"",    @"..\",    @"folder\..\..\" },
            { @"folder\..\..\..",    @"",    @"..\..",  @"folder\..\..\.." },
            { @"folder\..\..\..\",   @"",    @"..\..\", @"folder\..\..\..\" },

            { @"folder\subfolder\..",        @"folder",      @"folder",     @"folder\" },
            { @"folder\subfolder\..\",       @"folder\",     @"folder\",    @"folder\" },
            { @"folder\subfolder\..\..",     @"",            @"",           @"folder\.." },
            { @"folder\subfolder\..\..\",    @"",            @"",           @"folder\..\" },
            { @"folder\subfolder\..\..\..",  @"",            @"..",         @"folder\..\.." },
            { @"folder\subfolder\..\..\..\", @"",            @"..\",        @"folder\..\..\" },

            { @"..\folder\subfolder\..",         @"folder",      @"..\folder",  @"..\folder" },
            { @"..\folder\subfolder\..\",        @"folder\",     @"..\folder\", @"..\folder\" },
            { @"..\folder\subfolder\..\..",      @"",            @"..",         @"..\" },
            { @"..\folder\subfolder\..\..\",     @"",            @"..\",        @"..\" },
            { @"..\folder\subfolder\..\..\..",   @"",            @"..\..",      @"..\.." },
            { @"..\folder\subfolder\..\..\..\",  @"",            @"..\..\",     @"..\..\" },

            { @"..\folder\..\subfolder\..",          @"",    @"..",         @"..\" },
            { @"..\folder\..\subfolder\..\",         @"",    @"..\",        @"..\" },
            { @"..\folder\..\subfolder\..\..",       @"",    @"..\..",      @"..\.." },
            { @"..\folder\..\subfolder\..\..\",      @"",    @"..\..\",     @"..\..\" },
            { @"..\folder\..\subfolder\..\..\..",    @"",    @"..\..\..",   @"..\..\.." },
            { @"..\folder\..\subfolder\..\..\..\",   @"",    @"..\..\..\",  @"..\..\..\" },

            { @"..\folder\..\..\subfolder\..",           @"",    @"..\..",          @"..\.." },
            { @"..\folder\..\..\subfolder\..\",          @"",    @"..\..\",         @"..\..\" },
            { @"..\folder\..\..\subfolder\..\..",        @"",    @"..\..\..",       @"..\..\.." },
            { @"..\folder\..\..\subfolder\..\..\",       @"",    @"..\..\..\",      @"..\..\..\" },
            { @"..\folder\..\..\subfolder\..\..\..",     @"",    @"..\..\..\..",    @"..\..\..\.." },
            { @"..\folder\..\..\subfolder\..\..\..\",    @"",    @"..\..\..\..\",   @"..\..\..\..\" },

            { @"..\file.txt",    @"file.txt",    @"..\file.txt",    @"..\file.txt" },
            { @"..\..\file.txt", @"file.txt",    @"..\..\file.txt", @"..\..\file.txt" },

            { @"..\folder\file.txt",         @"folder\file.txt",     @"..\folder\file.txt", @"..\folder\file.txt" },
            { @"..\folder\..\file.txt",      @"file.txt",            @"..\file.txt",        @"..\file.txt" },
            { @"..\folder\..\..\file.txt",   @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },

            { @"..\..\folder\file.txt",          @"folder\file.txt",     @"..\..\folder\file.txt",  @"..\..\folder\file.txt" },
            { @"..\..\folder\..\file.txt",       @"file.txt",            @"..\..\file.txt",         @"..\..\file.txt" },
            { @"..\..\folder\..\..\file.txt",    @"file.txt",            @"..\..\..\file.txt",      @"..\..\..\file.txt" },

            { @"folder\..\file.txt",         @"file.txt",    @"file.txt",       @"folder\..\file.txt" },
            { @"folder\..\..\file.txt",      @"file.txt",    @"..\file.txt",    @"folder\..\..\file.txt" },
            { @"folder\..\..\..\file.txt",   @"file.txt",    @"..\..\file.txt", @"folder\..\..\..\file.txt" },

            { @"folder\subfolder\..\file.txt",       @"folder\file.txt",     @"folder\file.txt",    @"folder\file.txt" },
            { @"folder\subfolder\..\..\file.txt",    @"file.txt",            @"file.txt",           @"folder\..\file.txt" },
            { @"folder\subfolder\..\..\..\file.txt", @"file.txt",            @"..\file.txt",        @"folder\..\..\file.txt" },

            { @"..\folder\subfolder\..\file.txt",        @"folder\file.txt",     @"..\folder\file.txt", @"..\folder\file.txt" },
            { @"..\folder\subfolder\..\..\file.txt",     @"file.txt",            @"..\file.txt",        @"..\file.txt" },
            { @"..\folder\subfolder\..\..\..\file.txt",  @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },

            { @"..\folder\..\subfolder\..\file.txt",         @"file.txt",    @"..\file.txt",        @"..\file.txt" },
            { @"..\folder\..\subfolder\..\..\file.txt",      @"file.txt",    @"..\..\file.txt",     @"..\..\file.txt" },
            { @"..\folder\..\subfolder\..\..\..\file.txt",   @"file.txt",    @"..\..\..\file.txt",  @"..\..\..\file.txt" },

            { @"..\folder\..\..\subfolder\..\file.txt",          @"file.txt",    @"..\..\file.txt",         @"..\..\file.txt" },
            { @"..\folder\..\..\subfolder\..\..\file.txt",       @"file.txt",    @"..\..\..\file.txt",      @"..\..\..\file.txt" },
            { @"..\folder\..\..\subfolder\..\..\..\file.txt",    @"file.txt",    @"..\..\..\..\file.txt",   @"..\..\..\..\file.txt" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_ServerShare_Redundant_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
        public static IEnumerable<object[]> MemberData_UNC_Redundant_DoubleDot =>
            from t in TestPaths_Redundant_DoubleDot
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

        #endregion

        #region Combined: single + double dot

        private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_Combined = new List<Tuple<string, string, string, string>>
        {
            // The original and qualified strings must get the root string prefixed
            // Original | Qualified | Unqualified | Device prefix
            { @"..\.",     @"",    @"..",       @"..\" },
            { @"..\.\",    @"",    @"..\",      @"..\" },
            { @"..\..\.",  @"",    @"..\..",    @"..\.." },
            { @"..\..\.\", @"",    @"..\..\",   @"..\..\" },

            { @".\..\.",     @"",    @".\..",       @".\.." },
            { @".\..\.\",    @"",    @".\..\",      @".\..\" },
            { @".\..\..\.",  @"",    @".\..\..",    @".\..\.." },
            { @".\..\..\.\", @"",    @".\..\..\",   @".\..\..\" },

            { @"..\.\file.txt",         @"file.txt",    @"..\file.txt",     @"..\file.txt" },
            { @"..\.\..\file.txt",      @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },
            { @"..\..\.\file.txt",      @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },
            { @"..\.\..\.\file.txt",    @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },

            { @".\..\.\file.txt",         @"file.txt",    @".\..\file.txt",     @".\..\file.txt" },
            { @".\..\.\..\file.txt",      @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },
            { @".\..\..\.\file.txt",      @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },
            { @".\..\.\..\.\file.txt",    @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },

            { @"..\.\folder",          @"folder",      @"..\folder",    @"..\folder" },
            { @"..\.\folder\",         @"folder\",     @"..\folder\",   @"..\folder\" },
            { @"..\.\folder\..",       @"",            @"..",           @"..\" },
            { @"..\.\folder\..\",      @"",            @"..\",          @"..\" },
            { @"..\.\folder\..\..",    @"",            @"..\..",        @"..\.." },
            { @"..\.\folder\..\..\",   @"",            @"..\..\",       @"..\..\" },

            { @"..\folder\.",          @"folder",      @"..\folder",    @"..\folder" },
            { @"..\folder\.\",         @"folder\",     @"..\folder\",   @"..\folder\" },
            { @"..\folder\.\..",       @"",            @"..",           @"..\" },
            { @"..\folder\.\..\",      @"",            @"..\",          @"..\" },
            { @"..\folder\.\..\..",    @"",            @"..\..",        @"..\.." },
            { @"..\folder\.\..\..\",   @"",            @"..\..\",       @"..\..\" },

            { @"folder\.\subfolder\..",        @"folder",      @"folder",   @"folder\" },
            { @"folder\.\subfolder\..\",       @"folder\",     @"folder\",  @"folder\" },
            { @"folder\.\subfolder\..\..",     @"",            @"",         @"folder\.." },
            { @"folder\.\subfolder\..\..\",    @"",            @"",         @"folder\..\" },
            { @"folder\.\subfolder\..\..\..",  @"",            @"..",       @"folder\..\.." },
            { @"folder\.\subfolder\..\..\..\", @"",            @"..\",      @"folder\..\..\" },

            { @".\folder\.\subfolder\..",        @"folder",      @".\folder",   @".\folder" },
            { @".\folder\.\subfolder\..\",       @"folder\",     @".\folder\",  @".\folder\" },
            { @".\folder\.\subfolder\..\..",     @"",            @".",          @".\" },
            { @".\folder\.\subfolder\..\..\",    @"",            @".\",         @".\" },
            { @".\folder\.\subfolder\..\..\..",  @"",            @".\..",       @".\.." },
            { @".\folder\.\subfolder\..\..\..\", @"",            @".\..\",      @".\..\" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item3 };
        public static IEnumerable<object[]> MemberData_ServerShare_Redundant_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
        public static IEnumerable<object[]> MemberData_UNC_Redundant_Combined =>
            from t in TestPaths_Redundant_Combined
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

        #endregion

        #region Edge cases: more than two dots, paths with trailing dot

        private static readonly List<Tuple<string, string, string, string>> TestPaths_NoRedundancy_EdgeCases = new List<Tuple<string, string, string, string>>
        {
            // Original | Qualified | Unqualified | Device prefix
            
            // Trailing more than 2 dots
            { @"...",           @"",                @"",                @"..." },
            { @"...\",          @"...\",            @"...\",            @"...\" },
            { @"folder\...",    @"folder\",         @"folder\",         @"folder\..." },
            { @"folder\...\",   @"folder\...\",     @"folder\...\",     @"folder\...\" },

            { @"....",          @"",                @"",                @"...." },
            { @"....\",         @"....\",           @"....\",           @"....\" },
            { @"folder\....",   @"folder\",         @"folder\",         @"folder\...." },
            { @"folder\....\",  @"folder\....\",    @"folder\....\",    @"folder\....\" },

            // Starting with more than 2 dots
            { @"...\subfolder",             @"...\subfolder",           @"...\subfolder",          @"...\subfolder" },
            { @"...\subfolder\",            @"...\subfolder\",          @"...\subfolder\",         @"...\subfolder\" },
            { @"...\file.txt",              @"...\file.txt",            @"...\file.txt",           @"...\file.txt" },
            { @"...\subfolder\file.txt",    @"...\subfolder\file.txt",  @"...\subfolder\file.txt", @"...\subfolder\file.txt" },

            { @"....\subfolder",            @"....\subfolder",          @"....\subfolder",          @"....\subfolder" },
            { @"....\subfolder\",           @"....\subfolder\",         @"....\subfolder\",         @"....\subfolder\" },
            { @"....\file.txt",             @"....\file.txt",           @"....\file.txt",           @"....\file.txt" },
            { @"....\subfolder\file.txt",   @"....\subfolder\file.txt", @"....\subfolder\file.txt", @"....\subfolder\file.txt" },

            // file/folder ending in dot
            { @"dot.",               @"dot",            @"dot",             @"dot." },
            { @"dot.\",              @"dot\",           @"dot\",            @"dot.\" },
            { @"folder\dot.",        @"folder\dot",     @"folder\dot",      @"folder\dot." },
            { @"folder\dot.\",       @"folder\dot\",    @"folder\dot\",     @"folder\dot.\" },
            { @"dot.\subfolder",     @"dot\subfolder",  @"dot\subfolder",   @"dot.\subfolder" },
            { @"dot.\subfolder\",    @"dot\subfolder\", @"dot\subfolder\",  @"dot.\subfolder\" },

            { @"dot.\file.txt",              @"dot\file.txt",            @"dot\file.txt",           @"dot.\file.txt" },
            { @"dot.\subfolder\file.txt",    @"dot\subfolder\file.txt",  @"dot\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_DriveAndRoot_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DriveRootless_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DrivelessRoot_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_ServerShare_NoRedundancy_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
        public static IEnumerable<object[]> MemberData_UNC_NoRedundancy_EdgeCases =>
            from t in TestPaths_NoRedundancy_EdgeCases
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item4 };

        #endregion

        #region Edge cases + single dot

        private static readonly List<Tuple<string, string, string, string, string>> TestPaths_Redundant_SingleDot_EdgeCases = new List<Tuple<string, string, string, string, string>>
        {
            // The original and qualified strings must get the root string prefixed
            // Original | Qualified | Unqualified | Device unrooted | Device rooted

            // Folder with 3 dots
            { @"...\.",      @"",     @"",      @"...\",  @"..." },
            { @"...\.\",     @"...\", @"...\",  @"...\",  @"...\" },
            { @"...\.\.",    @"",     @"",      @"...\",  @"..." },
            { @"...\.\.\",   @"...\", @"...\",  @"...\",  @"...\" },

            { @"...\subfolder\.",        @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
            { @"...\subfolder\.\",       @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
            { @"...\subfolder\.\.",      @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
            { @"...\subfolder\.\.\",     @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
            { @"...\.\subfolder\.\",     @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
            { @"...\.\subfolder\.\.",    @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
            { @"...\.\subfolder\.\.\",   @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },

            { @"...\.\file.txt",                 @"...\file.txt",              @"...\file.txt",             @"...\file.txt",            @"...\file.txt" },
            { @"...\.\.\file.txt",               @"...\file.txt",              @"...\file.txt",             @"...\file.txt",            @"...\file.txt" },
            { @"...\subfolder\.\file.txt",       @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
            { @"...\subfolder\.\.\file.txt",     @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
            { @"...\.\subfolder\.\file.txt",     @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
            { @"...\.\subfolder\.\.\file.txt",   @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },

            // Folder with 4 dots
            { @"....\.",     @"",      @"",    @"....\",   @"...." },
            { @"....\.\",    @"....\", @"....\",    @"....\",   @"....\" },
            { @"....\.\.",   @"",      @"",    @"....\",   @"...." },
            { @"....\.\.\",  @"....\", @"....\",    @"....\",   @"....\" },

            { @"....\subfolder\.",       @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
            { @"....\subfolder\.\",      @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
            { @"....\subfolder\.\.",     @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
            { @"....\subfolder\.\.\",    @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
            { @"....\.\subfolder\.\",    @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
            { @"....\.\subfolder\.\.",   @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
            { @"....\.\subfolder\.\.\",  @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },

            { @"....\.\file.txt",                @"....\file.txt",              @"....\file.txt",               @"....\file.txt",            @"....\file.txt" },
            { @"....\.\.\file.txt",              @"....\file.txt",              @"....\file.txt",               @"....\file.txt",            @"....\file.txt" },
            { @"....\subfolder\.\file.txt",      @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
            { @"....\subfolder\.\.\file.txt",    @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
            { @"....\.\subfolder\.\file.txt",    @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
            { @"....\.\subfolder\.\.\file.txt",  @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
            
            // file/folder ending in dot
            { @"dot.\.",     @"dot",    @"dot",     @"dot.\",  @"dot." },
            { @"dot.\.\",    @"dot\",   @"dot\",    @"dot.\",  @"dot.\" },
            { @"dot.\.\.",   @"dot",    @"dot",     @"dot.\",  @"dot." },
            { @"dot.\.\.\",  @"dot\",   @"dot\",    @"dot.\",  @"dot.\" },

            { @"dot.\subfolder\.",       @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
            { @"dot.\subfolder\.\",      @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
            { @"dot.\subfolder\.\.",     @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
            { @"dot.\subfolder\.\.\",    @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
            { @"dot.\.\subfolder\.\",    @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
            { @"dot.\.\subfolder\.\.",   @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
            { @"dot.\.\subfolder\.\.\",  @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },

            { @"dot.\.\file.txt",                @"dot\file.txt",              @"dot\file.txt",             @"dot.\file.txt",           @"dot.\file.txt" },
            { @"dot.\.\.\file.txt",              @"dot\file.txt",              @"dot\file.txt",             @"dot.\file.txt",           @"dot.\file.txt" },
            { @"dot.\subfolder\.\file.txt",      @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
            { @"dot.\subfolder\.\.\file.txt",    @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
            { @"dot.\.\subfolder\.\file.txt",    @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
            { @"dot.\.\subfolder\.\.\file.txt",  @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item5 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_ServerShare_Redundant_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
        public static IEnumerable<object[]> MemberData_UNC_Redundant_SingleDot_EdgeCases =>
            from t in TestPaths_Redundant_SingleDot_EdgeCases
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item5 };

        #endregion

        #region Edge cases + double dot

        private static readonly List<Tuple<string, string, string, string, string>> TestPaths_Redundant_DoubleDot_EdgeCases = new List<Tuple<string, string, string, string, string>>
        {
            // Original | Qualified | Unqualified | Device prefix unrooted | Device prefix rooted
            // Folder with 3 dots
            { @"...\..",     @"",    @"",       @"...\..",      @"" },
            { @"...\..\",    @"",    @"",       @"...\..\",     @"" },
            { @"...\..\..",  @"",    @"..",     @"...\..\..",   @"" },
            { @"...\..\..\", @"",    @"..\",    @"...\..\..\",  @"" },

            { @"...\subfolder\..",           @"",        @"",       @"...\",       @"..." },
            { @"...\subfolder\..\",          @"...\",    @"...\",   @"...\",       @"...\" },
            { @"...\subfolder\..\..",        @"",        @"",       @"...\..",     @"" },
            { @"...\subfolder\..\..\",       @"",        @"",       @"...\..\",    @"" },
            { @"...\..\subfolder\..\",       @"",        @"",       @"...\..\",    @"" },
            { @"...\..\subfolder\..\..",     @"",        @"..",     @"...\..\..",  @"" },
            { @"...\..\subfolder\..\..\",    @"",        @"..\",    @"...\..\..\", @"" },

            { @"...\..\file.txt",                    @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
            { @"...\..\..\file.txt",                 @"file.txt",        @"..\file.txt",    @"...\..\..\file.txt", @"file.txt" },
            { @"...\subfolder\..\file.txt",          @"...\file.txt",    @"...\file.txt",   @"...\file.txt",       @"...\file.txt" },
            { @"...\subfolder\..\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
            { @"...\..\subfolder\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
            { @"...\..\subfolder\..\..\file.txt",    @"file.txt",        @"..\file.txt",    @"...\..\..\file.txt", @"file.txt" },

            // Folder with 4 dots
            { @"....\..",        @"",    @"",       @"....\..",     @"" },
            { @"....\..\",       @"",    @"",       @"....\..\",    @"" },
            { @"....\..\..",     @"",    @"..",     @"....\..\..",  @"" },
            { @"....\..\..\",    @"",    @"..\",    @"....\..\..\", @"" },

            { @"....\subfolder\..",          @"",       @"",        @"....\",       @"...." },
            { @"....\subfolder\..\",         @"....\",  @"....\",   @"....\",       @"....\" },
            { @"....\subfolder\..\..",       @"",       @"",        @"....\..",     @"" },
            { @"....\subfolder\..\..\",      @"",       @"",        @"....\..\",    @"" },
            { @"....\..\subfolder\..\",      @"",       @"",        @"....\..\",    @"" },
            { @"....\..\subfolder\..\..",    @"",       @"..",      @"....\..\..",  @"" },
            { @"....\..\subfolder\..\..\",   @"",       @"..\",     @"....\..\..\", @"" },

            { @"....\..\file.txt",                   @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
            { @"....\..\..\file.txt",                @"file.txt",        @"..\file.txt",    @"....\..\..\file.txt", @"file.txt" },
            { @"....\subfolder\..\file.txt",         @"....\file.txt",   @"....\file.txt",  @"....\file.txt",       @"....\file.txt" },
            { @"....\subfolder\..\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
            { @"....\..\subfolder\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
            { @"....\..\subfolder\..\..\file.txt",   @"file.txt",        @"..\file.txt",    @"....\..\..\file.txt", @"file.txt" },
            
            // file/folder ending in dot
            { @"dot.\..",     @"",    @"",      @"dot.\..",      @"" },
            { @"dot.\..\",    @"",    @"",      @"dot.\..\",      @"" },
            { @"dot.\..\..",  @"",    @"..",    @"dot.\..\..",   @"" },
            { @"dot.\..\..\", @"",    @"..\",   @"dot.\..\..\",  @"" },

            { @"dot.\subfolder\..",          @"dot",     @"dot",    @"dot.\",       @"dot." },
            { @"dot.\subfolder\..\",         @"dot\",    @"dot\",   @"dot.\",       @"dot.\" },
            { @"dot.\subfolder\..\..",       @"",        @"",       @"dot.\..",     @"" },
            { @"dot.\subfolder\..\..\",      @"",        @"",       @"dot.\..\",    @"" },
            { @"dot.\..\subfolder\..\",      @"",        @"",       @"dot.\..\",    @"" },
            { @"dot.\..\subfolder\..\..",    @"",        @"..",     @"dot.\..\..",  @"" },
            { @"dot.\..\subfolder\..\..\",   @"",        @"..\",    @"dot.\..\..\", @"" },

            { @"dot.\..\file.txt",                   @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
            { @"dot.\..\..\file.txt",                @"file.txt",       @"..\file.txt",    @"dot.\..\..\file.txt",  @"file.txt" },
            { @"dot.\subfolder\..\file.txt",         @"dot\file.txt",   @"dot\file.txt",   @"dot.\file.txt",        @"dot.\file.txt" },
            { @"dot.\subfolder\..\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
            { @"dot.\..\subfolder\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
            { @"dot.\..\subfolder\..\..\file.txt",   @"file.txt",       @"..\file.txt",    @"dot.\..\..\file.txt",  @"file.txt" },
        };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item5 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { t.Item1, t.Item3 };
        public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
        public static IEnumerable<object[]> MemberData_ServerShare_Redundant_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
        public static IEnumerable<object[]> MemberData_UNC_Redundant_DoubleDot_EdgeCases =>
            from t in TestPaths_Redundant_DoubleDot_EdgeCases
            select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item5 };

        #endregion

        #endregion
    }
}
