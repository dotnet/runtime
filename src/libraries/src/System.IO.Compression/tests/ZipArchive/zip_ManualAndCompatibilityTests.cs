// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_ManualAndCompatabilityTests
    {
        [Fact]
        public static async Task CompatibilityTests()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("7zip.zip")), ZipTest.zfolder("normal"), ZipArchiveMode.Update, false, false);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("windows.zip")), ZipTest.zfolder("normalWithoutEmptyDir"), ZipArchiveMode.Update, true, false);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("dotnetzipstreaming.zip")), ZipTest.zfolder("normal"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("sharpziplib.zip")), ZipTest.zfolder("normalWithoutEmptyDir"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("xceedstreaming.zip")), ZipTest.zfolder("normal"), ZipArchiveMode.Update, true, true);
        }

        [Fact]
        public static async Task CompatibilityTestsMsFiles()
        {
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("excel.xlsx")), ZipTest.compat("excel"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("powerpoint.pptx")), ZipTest.compat("powerpoint"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("word.docx")), ZipTest.compat("word"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("silverlight.xap")), ZipTest.compat("silverlight"), ZipArchiveMode.Update, true, true);
            ZipTest.IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(
                ZipTest.compat("packaging.package")), ZipTest.compat("packaging"), ZipArchiveMode.Update, true, true);
        }
    }
}

