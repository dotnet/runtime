// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    internal static class TextSyndicationContentKindHelper
    {
        public static bool IsDefined(TextSyndicationContentKind kind)
        {
            return (kind == TextSyndicationContentKind.Plaintext
                || kind == TextSyndicationContentKind.Html
                || kind == TextSyndicationContentKind.XHtml);
        }
    }
}
