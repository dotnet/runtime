// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    public class ReferencedCategoriesDocument : CategoriesDocument
    {
        public ReferencedCategoriesDocument()
        {
        }

        public ReferencedCategoriesDocument(Uri link) : base()
        {
            Link = link ?? throw new ArgumentNullException(nameof(link));
        }

        public Uri Link { get; set; }

        internal override bool IsInline => false;
    }
}
