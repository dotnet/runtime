// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    public abstract partial class SyndicationFeedFormatter
    {
        public System.Func<string, string, string, System.DateTimeOffset> DateTimeParser { get { throw null; } set { } }
        public System.Func<string, System.UriKind, string, string, System.Uri> UriParser { get { throw null; } set { } }
    }
}
