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
        public string DispositionType { get { return default(string); } set { } }
        public System.Collections.Specialized.StringDictionary Parameters { get { return default(System.Collections.Specialized.StringDictionary); } }
        public string FileName { get { return default(string); } set { } }
        public DateTime CreationDate { get { return default(DateTime); } set { } }
        public DateTime ModificationDate { get { return default(DateTime); } set { } }
        public bool Inline { get { return default(bool); } set { } }
        public DateTime ReadDate { get { return default(DateTime); } set { } }
        public long Size { get { return default(long); } set { } }
        public override string ToString() { return default(string); }
        public override bool Equals(object rparam) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public class ContentType
    {
        public ContentType() { }
        public ContentType(string contentType) { }
        public string Boundary { get { return default(string); } set { } }
        public string CharSet { get { return default(string); } set { } }
        public string MediaType { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public System.Collections.Specialized.StringDictionary Parameters { get { return default(System.Collections.Specialized.StringDictionary); } }
        public override string ToString() { return default(string); }
        public override bool Equals(object rparam) { return default(bool); }
        public override int GetHashCode() { return default(int); }
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
