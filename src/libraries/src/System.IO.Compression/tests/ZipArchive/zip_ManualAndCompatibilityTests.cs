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
    }
}
