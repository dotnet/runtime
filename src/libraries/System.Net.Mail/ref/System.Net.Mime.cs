// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Mime
{
    public class ContentDisposition
    {
        public ContentDisposition() { }
        public ContentDisposition(string disposition) { }
        public string DispositionType { get { throw null; } set { } }
        public System.Collections.Specialized.StringDictionary Parameters { get { throw null; } }
        public string FileName { get { throw null; } set { } }
        public DateTime CreationDate { get { throw null; } set { } }
        public DateTime ModificationDate { get { throw null; } set { } }
        public bool Inline { get { throw null; } set { } }
        public DateTime ReadDate { get { throw null; } set { } }
        public long Size { get { throw null; } set { } }
        public override string ToString() { throw null; }
        public override bool Equals(object rparam) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public class ContentType
    {
        public ContentType() { }
        public ContentType(string contentType) { }
        public string Boundary { get { throw null; } set { } }
        public string CharSet { get { throw null; } set { } }
        public string MediaType { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public System.Collections.Specialized.StringDictionary Parameters { get { throw null; } }
        public override string ToString() { throw null; }
        public override bool Equals(object rparam) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public static class DispositionTypeNames
    {
        public const string Inline = "inline";
        public const string Attachment = "attachment";
    }
    public static class MediaTypeNames
    {
        public static class Text
        {
            public const string Plain = "text/plain";
            public const string Html = "text/html";
            public const string Xml = "text/xml";
            public const string RichText = "text/richtext";
        }

        public static class Application
        {
            public const string Soap = "application/soap+xml";
            public const string Octet = "application/octet-stream";
            public const string Rtf = "application/rtf";
            public const string Pdf = "application/pdf";
            public const string Zip = "application/zip";
        }

        public static class Image
        {
            public const string Gif = "image/gif";
            public const string Tiff = "image/tiff";
            public const string Jpeg = "image/jpeg";
        }
    }
    public enum TransferEncoding
    {
        Unknown = -1,
        QuotedPrintable = 0,
        Base64 = 1,
        SevenBit = 2,
        EightBit = 3,
    }
}
