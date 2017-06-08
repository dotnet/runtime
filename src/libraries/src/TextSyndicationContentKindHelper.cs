//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    static class TextSyndicationContentKindHelper
    {
        public static bool IsDefined(TextSyndicationContentKind kind)
        {
            return (kind == TextSyndicationContentKind.Plaintext
                || kind == TextSyndicationContentKind.Html
                || kind == TextSyndicationContentKind.XHtml);
        }
    }
}
