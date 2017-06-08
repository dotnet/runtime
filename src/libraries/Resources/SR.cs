//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceModel
{
    using System;
    internal class SR
    {
        internal static object XmlBufferQuotaExceeded;

        public static string InvalidObjectTypePassed { get { throw new NotImplementedException();  } internal set { } }

        public static string ErrorParsingDateTime { get { throw new NotImplementedException();  } internal set { } }
        public static string ErrorParsingFeed { get { throw new NotImplementedException();  } internal set { } }

        public static string UnknownFeedXml { get { throw new NotImplementedException(); } internal set { } }

        public static string Atom10SpecRequiresTextConstruct { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedHasNonContiguousItems { get { throw new NotImplementedException(); } internal set { } }
        public static string ErrorParsingItem { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedFormatterDoesNotHaveFeed { get { throw new NotImplementedException(); } internal set { } }
        public static string DocumentFormatterDoesNotHaveDocument { get { throw new NotImplementedException(); } internal set { } }
        public static string UnknownDocumentXml { get { throw new NotImplementedException(); } internal set { } }
        public static string ErrorParsingDocument { get { throw new NotImplementedException(); } internal set { } }
        public static string UnsupportedRssVersion { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedAuthorsIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedContributorsIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedIdIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedLinksIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemAuthorsIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemLinksIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemLastUpdatedTimeIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemCopyrightIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemContentIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemContributorsIgnoredOnWrite { get { throw new NotImplementedException(); } internal set { } }
        public static string ValueMustBeNonNegative { get { throw new NotImplementedException(); } internal set { } }
        public static string UriGeneratorSchemeMustNotBeEmpty { get { throw new NotImplementedException(); } internal set { } }

        public static string XmlBufferInInvalidState { get { throw new NotImplementedException(); } internal set { } }
        public static string UnknownItemXml { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemFormatterDoesNotHaveItem { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedCreatedNullCategory { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemCreatedNullCategory { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedCreatedNullItem { get { throw new NotImplementedException(); } internal set { } }
        public static string FeedCreatedNullPerson { get { throw new NotImplementedException(); } internal set { } }
        public static string ItemCreatedNullPerson { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationFeedReadBegin { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationFeedReadEnd { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationFeedWriteBegin { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationFeedWriteEnd { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationItemReadBegin { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationItemReadEnd { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationItemWriteBegin { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationItemWriteEnd { get { throw new NotImplementedException(); } internal set { } }
        public static string TraceCodeSyndicationProtocolElementIgnoredOnRead { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlLineInfo { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundEndOfFile { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundElement { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundEndElement { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundText { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundComment { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundCData { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlFoundNodeType { get { throw new NotImplementedException(); } internal set { } }
        public static string XmlStartElementExpected { get { throw new NotImplementedException(); } internal set { } }

        public static string OuterNameOfElementExtensionEmpty { get; internal set; }
        public static string ErrorInLine { get; internal set; }

        public static string OuterElementNameNotSpecified { get; internal set; }
        public static string ExtensionNameNotSpecified { get; internal set; }
        public static string UnbufferedItemsCannotBeCloned { get; internal set; }

        internal static string GetString(string nvalidObjectTypePassed, params object[] args)
        {
            //throw new NotImplementedException();
            return nvalidObjectTypePassed;
        }
    }
}