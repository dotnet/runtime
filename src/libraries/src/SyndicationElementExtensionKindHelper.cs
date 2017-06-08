//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.Globalization;
    using System.Collections.ObjectModel;

    static class SyndicationElementExtensionKindHelper
    {
        internal static bool IsDefined(SyndicationElementExtensionKind value)
        {
            return (value == SyndicationElementExtensionKind.DataContract ||
                value == SyndicationElementExtensionKind.XmlElement ||
                value == SyndicationElementExtensionKind.XmlSerializer);
        }

        internal enum SyndicationElementExtensionKind
        {
            DataContract,
            XmlElement,
            XmlSerializer
        }
    }
}
