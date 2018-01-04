// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ParseDateTimeArgs
    {
        public string DateTimeString { get { throw null; } set { } }
        public System.Xml.XmlQualifiedName ElemntQualifiedName { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ParseUriArgs
    {
        public System.Xml.XmlQualifiedName ElemntQualifiedName { get { throw null; } set { } }
        public System.UriKind UriKind { get { throw null; } set { } }
        public string UriString { get { throw null; } set { } }
    }
    public abstract partial class SyndicationFeedFormatter
    {
        public System.ServiceModel.Syndication.TryParseDateTime DateTimeParser { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TryParseUri UriParser { get { throw null; } set { } }
    }
    public delegate bool TryParseDateTime(System.ServiceModel.Syndication.ParseDateTimeArgs parseDateTimeArgs, out System.DateTimeOffset dateTimeOffset);
    public delegate bool TryParseUri(System.ServiceModel.Syndication.ParseUriArgs parseUriArgs, out System.Uri uri);
}
