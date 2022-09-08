// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_File_Base : TarTestsBase
    {
        // TestTarFormat, string testCaseName, CompressionMethod, bool copyData
        public static IEnumerable<object[]> WriteEntry_CopyArchive_Data()
        {
            foreach (CompressionMethod compressionMethod in Enum.GetValues<CompressionMethod>())
            {
                foreach (bool copyData in new object[] { false, true })
                {
                    foreach (object[] testCaseName in GetV7TestCaseNames())
                    {
                        if ((string)testCaseName[0] == "folder_file_utf8") // The legacy name field is stored in ASCII in the V7 format
                        {
                            continue;
                        }

                        yield return new object[] { TestTarFormat.v7, testCaseName[0], compressionMethod, copyData };
                    }

                    foreach (object[] testCaseNameObj in GetUstarTestCaseNames())
                    {
                        string testcaseName = (string)testCaseNameObj[0];
                        if (testcaseName is "folder_file_utf8" // The legacy name field is stored in ASCII in the Ustar format
                                         or "longpath_splitable_under255" // Folder stored under 'unarchived' runtime-assets due to NuGet not allowing very long paths
                                         or "specialfiles") // Same but for fifos/chardevices/blockdevices
                        {
                            continue;
                        }

                        yield return new object[] { TestTarFormat.ustar, testcaseName, compressionMethod, copyData };
                    }

                    foreach (object[] testCaseNameObj in GetPaxAndGnuTestCaseNames())
                    {
                        string testCaseName = (string)testCaseNameObj[0];
                        if (testCaseName is "longpath_splitable_under255"
                                         or "specialfiles"
                                         or "longpath_over255" // Folder stored under 'unarchived' runtime-assets due to NuGet not allowing very long paths
                                         or "longfilename_over100_under255" // Same
                                         or "file_longsymlink") // Same
                        {
                            continue;
                        }

                        if (testCaseName is not "folder_file_utf8") // The legacy name field is stored in ASCII in the GNU format
                        {
                            yield return new object[] { TestTarFormat.oldgnu, testCaseName, compressionMethod, copyData };
                            yield return new object[] { TestTarFormat.gnu, testCaseName, compressionMethod, copyData };
                        }

                        // folder_file_utf8 case name is ok to use in PAX because it allows storing the name in UTF8 in the extended attributes
                        yield return new object[] { TestTarFormat.pax_gea, testCaseName, compressionMethod, copyData };
                        yield return new object[] { TestTarFormat.pax, testCaseName, compressionMethod, copyData };

                    }
                }
            }
        }
    }
}
