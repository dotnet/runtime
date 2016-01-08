// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_ManualAndCompatabilityTests
    {
        [Theory]
        [InlineData("7zip.zip", "normal", false, false)]
        [InlineData("windows.zip", "normalWithoutEmptyDir", true, false)]
        [InlineData("dotnetzipstreaming.zip", "normal", true, true)]
        [InlineData("sharpziplib.zip", "normalWithoutEmptyDir", true, true)]
        [InlineData("xceedstreaming.zip", "normal", true, true)]
        public static async Task CompatibilityTests(string zipFile, string zipFolder, bool dontRequireExplicit, bool dontCheckTimes)
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(ZipTest.compat(zipFile)), ZipTest.zfolder(zipFolder), ZipArchiveMode.Update, dontRequireExplicit, dontCheckTimes);}

        [Theory]
        [InlineData("excel.xlsx", "excel", true, true)]
        [InlineData("powerpoint.pptx", "powerpoint", true, true)]
        [InlineData("word.docx", "word", true, true)]
        [InlineData("silverlight.xap", "silverlight", true, true)]
        [InlineData("packaging.package", "packaging", true, true)]
        public static async Task CompatibilityTestsMsFiles(string withTrailing, string withoutTrailing, bool dontRequireExplicit, bool dontCheckTimes)
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(ZipTest.compat(withTrailing)), ZipTest.compat(withoutTrailing), ZipArchiveMode.Update, dontRequireExplicit, dontCheckTimes);
        }

        /// <summary>
        /// This test ensures that a zipfile created on one platform with a file containing potentially invalid characters elsewhere
        /// will be interpreted based on the source OS path name validation rules. 
        /// 
        /// For example, the file "aa\bb\cc\dd" in a zip created on Unix should be one file "aa\bb\cc\dd" whereas the same file
        /// in a zip created on Windows should be interpreted as the file "dd" underneath three subdirectories.
        /// </summary>
        [Theory]
        [InlineData("backslashes_FromUnix.zip", "aa\\bb\\cc\\dd")]
        [InlineData("backslashes_FromWindows.zip", "dd")]
        [InlineData("WindowsInvalid_FromUnix.zip", "aa<b>d")]
        [InlineData("WindowsInvalid_FromWindows.zip", "aa<b>d")]
        [InlineData("NullCharFileName_FromWindows.zip", "a\06b6d")]
        [InlineData("NullCharFileName_FromUnix.zip", "a\06b6d")]
        public static async Task ZipWithInvalidFileNames_ParsedBasedOnSourceOS(string zipName, string fileName)
        {
            using (Stream stream = await StreamHelpers.CreateTempCopyStream(ZipTest.compat(zipName)))
            using (ZipArchive archive = new ZipArchive(stream))
            {
                Assert.Equal(1, archive.Entries.Count);
                ZipArchiveEntry entry = archive.Entries[0];
                Assert.Equal(fileName, entry.Name);
            }
        }
    }
}
