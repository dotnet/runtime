// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Mime
{
    public static class MediaTypeNames
    {
        /// <summary>Specifies the kind of application data in an email message attachment.</summary>
        public static class Application
        {
            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data consists of url-encoded key-value pairs.</summary>
            public const string FormUrlEncoded = "application/x-www-form-urlencoded";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in JSON format.</summary>
            public const string Json = "application/json";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in JSON patch format.</summary>
            public const string JsonPatch = "application/json-patch+json";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in JSON text sequence format.</summary>
            public const string JsonSequence = "application/json-seq";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in Web Application Manifest.</summary>
            public const string Manifest = "application/manifest+json";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is not interpreted.</summary>
            public const string Octet = "application/octet-stream";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in Portable Document Format (PDF).</summary>
            public const string Pdf = "application/pdf";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in JSON problem detail format.</summary>
            public const string ProblemJson = "application/problem+json";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in XML problem detail format.</summary>
            public const string ProblemXml = "application/problem+xml";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in Rich Text Format (RTF).</summary>
            public const string Rtf = "application/rtf";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is a SOAP document.</summary>
            public const string Soap = "application/soap+xml";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in WASM format.</summary>
            public const string Wasm = "application/wasm";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in XML format.</summary>
            public const string Xml = "application/xml";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in XML Document Type Definition format.</summary>
            public const string XmlDtd = "application/xml-dtd";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is in XML patch format.</summary>
            public const string XmlPatch = "application/xml-patch+xml";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Application"/> data is compressed.</summary>
            public const string Zip = "application/zip";
        }

        /// <summary>Specifies the kind of font data in an email message attachment.</summary>
        public static class Font
        {
            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in font type collection format.</summary>
            public const string Collection = "font/collection";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in OpenType Layout (OTF) format.</summary>
            public const string Otf = "font/otf";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in SFNT format.</summary>
            public const string Sfnt = "font/sfnt";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in TrueType font (TTF) format.</summary>
            public const string Ttf = "font/ttf";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in WOFF format.</summary>
            public const string Woff = "font/woff";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Font"/> data is in WOFF2 format.</summary>
            public const string Woff2 = "font/woff2";
        }

        /// <summary>Specifies the kind of image data in an email message attachment.</summary>
        public static class Image
        {
            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in AVIF format.</summary>
            public const string Avif = "image/avif";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in BMP format.</summary>
            public const string Bmp = "image/bmp";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in GIF format.</summary>
            public const string Gif = "image/gif";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in ICO format.</summary>
            public const string Icon = "image/x-icon";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in JPEG format.</summary>
            public const string Jpeg = "image/jpeg";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in PNG format.</summary>
            public const string Png = "image/png";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in SVG format.</summary>
            public const string Svg = "image/svg+xml";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in TIFF format.</summary>
            public const string Tiff = "image/tiff";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Image"/> data is in WEBP format.</summary>
            public const string Webp = "image/webp";
        }

        /// <summary>Specifies the kind of multipart data in an email message attachment.</summary>
        public static class Multipart
        {
            /// <summary>Specifies that the <see cref="MediaTypeNames.Multipart"/> data consists of multiple byte ranges.</summary>
            public const string ByteRanges = "multipart/byteranges";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Multipart"/> data is in form data format.</summary>
            public const string FormData = "multipart/form-data";
        }

        /// <summary>Specifies the kind of text data in an email message attachment.</summary>
        public static class Text
        {
            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in CSS format.</summary>
            public const string Css = "text/css";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in CSV format.</summary>
            public const string Csv = "text/csv";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in HTML format.</summary>
            public const string Html = "text/html";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in JavaScript format.</summary>
            public const string JavaScript = "text/javascript";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in Markdown format.</summary>
            public const string Markdown = "text/markdown";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in plain text format.</summary>
            public const string Plain = "text/plain";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in Rich Text Format (RTF).</summary>
            public const string RichText = "text/richtext";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in Rich Text Format (RTF).</summary>
            public const string Rtf = "text/rtf";

            /// <summary>Specifies that the <see cref="MediaTypeNames.Text"/> data is in XML format.</summary>
            public const string Xml = "text/xml";
        }
    }
}
