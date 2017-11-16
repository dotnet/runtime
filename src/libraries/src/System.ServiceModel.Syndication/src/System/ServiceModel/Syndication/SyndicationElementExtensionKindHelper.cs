// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Collections.ObjectModel;

namespace System.ServiceModel.Syndication
{
    internal static class SyndicationElementExtensionKindHelper
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
