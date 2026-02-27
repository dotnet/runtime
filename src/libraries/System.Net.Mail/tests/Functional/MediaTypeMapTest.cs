// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Mime.Tests
{
    public class MediaTypeMapTest
    {
        [Fact]
        public void GetMediaType_WithNullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pathOrExtension", () => MediaTypeMap.GetMediaType(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(".")]
        [InlineData("README")]
        [InlineData(".unknown")]
        [InlineData("unknown")]
        [InlineData("file.unknown")]
        [InlineData(".xyz123")]
        public void GetMediaType_UnknownExtension_ReturnsNull(string input)
        {
            Assert.Null(MediaTypeMap.GetMediaType(input));
            Assert.Null(MediaTypeMap.GetMediaType((ReadOnlySpan<char>)input));
        }

        [Theory]
        [InlineData("file.tar.gz", "application/gzip")]
        [InlineData("script.min.js", "text/javascript")]
        [InlineData("data.backup.json", "application/json")]
        public void GetMediaType_WithMultipleDots_UsesLastExtension(string path, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType(path));
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType((ReadOnlySpan<char>)path));
        }

        [Theory]
        [InlineData(".pdf", "application/pdf")]
        [InlineData("pdf", "application/pdf")]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData("jpg", "image/jpeg")]
        [InlineData(".jpeg", "image/jpeg")]
        [InlineData(".png", "image/png")]
        [InlineData(".gif", "image/gif")]
        [InlineData(".html", "text/html")]
        [InlineData(".htm", "text/html")]
        [InlineData(".css", "text/css")]
        [InlineData(".js", "text/javascript")]
        [InlineData(".mjs", "text/javascript")]
        [InlineData(".jsx", "text/jsx")]
        [InlineData(".jfif", "image/jpeg")]
        [InlineData(".rtf", "application/rtf")]
        [InlineData(".ods", "application/vnd.oasis.opendocument.spreadsheet")]
        [InlineData(".json", "application/json")]
        [InlineData(".xml", "application/xml")]
        [InlineData(".zip", "application/zip")]
        [InlineData(".txt", "text/plain")]
        [InlineData(".cgm", "image/cgm")]
        [InlineData(".mp4", "video/mp4")]
        [InlineData(".mp3", "audio/mpeg")]
        [InlineData(".wasm", "application/wasm")]
        [InlineData(".ttf", "font/ttf")]
        [InlineData(".woff2", "font/woff2")]
        [InlineData(".woff", "font/woff")]
        [InlineData(".rar", "application/vnd.rar")]
        public void GetMediaType_WithExtension_ReturnsCorrectMediaType(string extension, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType(extension));
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType((ReadOnlySpan<char>)extension));
        }

        [Theory]
        [InlineData("file.pdf", "application/pdf")]
        [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("image.png", "image/png")]
        [InlineData(@"C:\path\to\file.jpg", "image/jpeg")]
        [InlineData("/path/to/file.html", "text/html")]
        [InlineData("archive.tar.gz", "application/gzip")]
        [InlineData("style.min.css", "text/css")]
        public void GetMediaType_WithFilePath_ReturnsCorrectMediaType(string path, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType(path));
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType((ReadOnlySpan<char>)path));
        }

        [Theory]
        [InlineData(".PDF", "application/pdf")]
        [InlineData(".Pdf", "application/pdf")]
        [InlineData("PDF", "application/pdf")]
        [InlineData(".JPG", "image/jpeg")]
        [InlineData("file.PNG", "image/png")]
        public void GetMediaType_CaseInsensitive(string input, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType(input));
            Assert.Equal(expectedMediaType, MediaTypeMap.GetMediaType((ReadOnlySpan<char>)input));
        }

        [Fact]
        public void GetExtension_WithNullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("mediaType", () => MediaTypeMap.GetExtension(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(".")]
        [InlineData("abc")]
        [InlineData("unknown/type")]
        [InlineData("application/unknown")]
        [InlineData("custom/x-custom")]
        public void GetExtension_UnknownMediaType_ReturnsNull(string mediaType)
        {
            Assert.Null(MediaTypeMap.GetExtension(mediaType));
            Assert.Null(MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType));
        }

        [Theory]
        [InlineData("application/pdf", ".pdf")]
        [InlineData("image/cgm", ".cgm")]
        [InlineData("image/jpeg", ".jpg")]
        [InlineData("image/png", ".png")]
        [InlineData("text/html", ".html")]
        [InlineData("text/plain", ".txt")]
        [InlineData("application/json", ".json")]
        [InlineData("video/mp4", ".mp4")]
        [InlineData("audio/mpeg", ".mp3")]
        [InlineData("font/woff2", ".woff2")]
        public void GetExtension_WithMediaType_ReturnsCorrectExtension(string mediaType, string expectedExtension)
        {
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension(mediaType));
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType));
        }

        [Theory]
        [InlineData("APPLICATION/PDF", ".pdf")]
        [InlineData("Image/Jpeg", ".jpg")]
        [InlineData("TEXT/HTML", ".html")]
        public void GetExtension_CaseInsensitive(string mediaType, string expectedExtension)
        {
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension(mediaType));
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType));
        }

        [Theory]
        [InlineData("text/html; charset=utf-8", ".html")]
        [InlineData("application/json; charset=utf-8", ".json")]
        [InlineData("text/plain;charset=ISO-8859-1", ".txt")]
        [InlineData("image/png; name=image.png", ".png")]
        public void GetExtension_WithParameters_IgnoresParametersAndReturnsExtension(string mediaType, string expectedExtension)
        {
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension(mediaType));
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType));
        }

        [Fact]
        public void GetMediaType_RoundTrip_CommonExtensions()
        {
            ReadOnlySpan<string> extensions = [".pdf", ".jpg", ".png", ".html", ".json", ".zip", ".txt", ".mp4"];

            foreach (string ext in extensions)
            {
                Assert.Equal(ext, MediaTypeMap.GetExtension(MediaTypeMap.GetMediaType(ext)));
            }
        }

        [Fact]
        public void GetExtension_RoundTrip_CommonMediaTypes()
        {
            ReadOnlySpan<string> mediaTypes = 
            [
                "application/pdf", 
                "image/png", 
                "text/html", 
                "application/json",
                "video/mp4"
            ];

            foreach (string mediaType in mediaTypes)
            {
                Assert.Equal(mediaType, MediaTypeMap.GetMediaType(MediaTypeMap.GetExtension(mediaType)));
                Assert.Equal(mediaType, MediaTypeMap.GetMediaType(MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType)));
            }
        }

        [Theory]
        [InlineData("   application/pdf   ", ".pdf")]
        [InlineData("text/html  ", ".html")]
        [InlineData("  image/png", ".png")]
        public void GetExtension_WithWhitespace_TrimsAndReturnsCorrectExtension(string mediaType, string expectedExtension)
        {
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension(mediaType));
            Assert.Equal(expectedExtension, MediaTypeMap.GetExtension((ReadOnlySpan<char>)mediaType));
        }
    }
}
