// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Text;

namespace System.IO.Compression.Tests
{
    public partial class zip_CreateTests : ZipFileTestBase
    {
        [Theory]
        // General purpose bit flag must get the appropriate bit set if a file comment or an entry name is unicode
        [InlineData("ascii", "ascii!!!", "utf-8", "UÄäÖöÕõÜü")]
        [InlineData("utf-8", "UÄäÖöÕõÜü", "ascii", "ascii!!!")]
        [InlineData("ascii", "ascii!!!", "latin1", "LÄäÖöÕõÜü")]
        [InlineData("latin1", "LÄäÖöÕõÜü", "ascii", "ascii!!!")]
        [InlineData("utf-8", "UÄäÖöÕõÜü", "latin1", "LÄäÖöÕõÜü")]
        [InlineData("latin1", "LÄäÖöÕõÜü", "utf-8", "UÄäÖöÕõÜü")]
        public static void Create_ZipArchiveEntry_DifferentEncodings_FullName_And_Comment(string en1, string s1, string en2, string s2)
        {
            Encoding e1 = Encoding.GetEncoding(en1);
            Encoding e2 = Encoding.GetEncoding(en2);
            string entryName = e1.GetString(e1.GetBytes(s1));
            string comment = e2.GetString(e2.GetBytes(s2));

            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create with no encoding to autoselect it if one of the two strings is unicode
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                entry.Comment = comment;

                Assert.Equal(entryName, entry.FullName);
                Assert.Equal(comment, entry.Comment);
            }

            // Open with no encoding
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    Assert.Equal(entryName, entry.FullName);
                    Assert.Equal(comment, entry.Comment);
                }
            }
        }
    }
}