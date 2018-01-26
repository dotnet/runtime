// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    public partial struct XmlDateTimeData
    {
        public XmlDateTimeData(string dateTimeString, System.Xml.XmlQualifiedName elementQualifiedName) { throw null; }
        public string DateTimeString { get; }
        public System.Xml.XmlQualifiedName ElementQualifiedName { get; }
    }
    public partial struct XmlUriData
    {
        public XmlUriData(string uriString, UriKind uriKind, System.Xml.XmlQualifiedName elementQualifiedName) { throw null; }
        public System.Xml.XmlQualifiedName ElementQualifiedName { get; }
        public System.UriKind UriKind { get; }
        public string UriString { get; }
    }
    public partial class SyndicationFeed
    {
        public System.ServiceModel.Syndication.SyndicationLink Documentation { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<string> SkipDays { get { throw null; } }
        public System.Collections.ObjectModel.Collection<int> SkipHours { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationTextInput TextInput { get { throw null; } set { } }
        public System.TimeSpan? TimeToLive { get { throw null; } set { } }
    }
    public abstract partial class SyndicationFeedFormatter
    {
        public System.ServiceModel.Syndication.TryParseDateTimeCallback DateTimeParser { get; set; }
        public System.ServiceModel.Syndication.TryParseUriCallback UriParser { get; set; }
    }
    public delegate bool TryParseDateTimeCallback(System.ServiceModel.Syndication.XmlDateTimeData data, out System.DateTimeOffset dateTimeOffset);
    public delegate bool TryParseUriCallback(System.ServiceModel.Syndication.XmlUriData data, out System.Uri uri);
    public partial class SyndicationTextInput
    {
        public string Description { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationLink Link { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Title { get { throw null; } set { } }
        public SyndicationTextInput() { }
    }
}
