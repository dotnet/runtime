// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Drawing.Tests
{
    public class ImageTests
    {
        public static IEnumerable<object[]> InvalidBytes_TestData()
        {
            // IconTests.Ctor_InvalidBytesInStream_TestData an array of 2 objects, but this test only uses the
            // 1st object.
            foreach (object[] data in IconTests.Ctor_InvalidBytesInStream_TestData())
            {
                yield return new object[] { data[0] };
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromFile_InvalidBytes_ThrowsOutOfMemoryException(byte[] bytes)
        {
            using (var file = TempFile.Create(bytes))
            {
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path));
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path, useEmbeddedColorManagement: true));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void FromFile_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null, useEmbeddedColorManagement: true));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void FromFile_EmptyFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty, useEmbeddedColorManagement: true));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void FromFile_LongSegment_ThrowsException()
        {
            // Throws PathTooLongException on Desktop and FileNotFoundException elsewhere.
            if (PlatformDetection.IsFullFramework)
            {
                string fileName = new string('a', 261);

                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName));
                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
            else
            {
                string fileName = new string('a', 261);

                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName));
                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
        }

        [Fact]
        public void FromFile_NoSuchFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile"));
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile", useEmbeddedColorManagement: true));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromStream_InvalidBytes_ThrowsArgumentException(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream));
                Assert.Equal(0, stream.Position);

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true));
                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true));
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void FromStream_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true, validateImageData: true));
        }
    }
}
