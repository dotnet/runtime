//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceModel
{
    using System;
    internal class SR
    {
        internal static object XmlBufferQuotaExceeded;

        public static string InvalidObjectTypePassed = "resource";

        public static string ErrorParsingDateTime = "resource";
        public static string ErrorParsingFeed = "resource";

        public static string UnknownFeedXml = "resource";

        public static string Atom10SpecRequiresTextConstruct = "resource";
        public static string FeedHasNonContiguousItems = "resource";
        public static string ErrorParsingItem = "resource";
        public static string FeedFormatterDoesNotHaveFeed = "resource";
        public static string DocumentFormatterDoesNotHaveDocument = "resource";
        public static string UnknownDocumentXml = "resource";
        public static string ErrorParsingDocument = "resource";
        public static string UnsupportedRssVersion = "resource";
        public static string FeedAuthorsIgnoredOnWrite = "resource";
        public static string FeedContributorsIgnoredOnWrite = "resource";
        public static string FeedIdIgnoredOnWrite = "resource";
        public static string FeedLinksIgnoredOnWrite = "resource";
        public static string ItemAuthorsIgnoredOnWrite = "resource";
        public static string ItemLinksIgnoredOnWrite = "resource";
        public static string ItemLastUpdatedTimeIgnoredOnWrite = "resource";
        public static string ItemCopyrightIgnoredOnWrite = "resource";
        public static string ItemContentIgnoredOnWrite = "resource";
        public static string ItemContributorsIgnoredOnWrite = "resource";
        public static string ValueMustBeNonNegative = "resource";
        public static string UriGeneratorSchemeMustNotBeEmpty = "resource";

        public static string XmlBufferInInvalidState = "resource";
        public static string UnknownItemXml = "resource";
        public static string ItemFormatterDoesNotHaveItem = "resource";
        public static string FeedCreatedNullCategory = "resource";
        public static string ItemCreatedNullCategory = "resource";
        public static string FeedCreatedNullItem = "resource";
        public static string FeedCreatedNullPerson = "resource";
        public static string ItemCreatedNullPerson = "resource";
        public static string TraceCodeSyndicationFeedReadBegin = "resource";
        public static string TraceCodeSyndicationFeedReadEnd = "resource";
        public static string TraceCodeSyndicationFeedWriteBegin = "resource";
        public static string TraceCodeSyndicationFeedWriteEnd = "resource";
        public static string TraceCodeSyndicationItemReadBegin = "resource";
        public static string TraceCodeSyndicationItemReadEnd = "resource";
        public static string TraceCodeSyndicationItemWriteBegin = "resource";
        public static string TraceCodeSyndicationItemWriteEnd = "resource";
        public static string TraceCodeSyndicationProtocolElementIgnoredOnRead = "resource";
        public static string XmlLineInfo = "resource";
        public static string XmlFoundEndOfFile = "resource";
        public static string XmlFoundElement = "resource";
        public static string XmlFoundEndElement = "resource";
        public static string XmlFoundText = "resource";
        public static string XmlFoundComment = "resource";
        public static string XmlFoundCData = "resource";
        public static string XmlFoundNodeType = "resource";
        public static string XmlStartElementExpected = "resource";

        public static string OuterNameOfElementExtensionEmpty= "resource";
        public static string ErrorInLine= "resource";

        public static string OuterElementNameNotSpecified= "resource";
        public static string ExtensionNameNotSpecified= "resource";
        public static string UnbufferedItemsCannotBeCloned= "resource";

        internal static string GetString(string nvalidObjectTypePassed, params object[] args)
        {
            //"resource"
            return nvalidObjectTypePassed;
        }
    }
}