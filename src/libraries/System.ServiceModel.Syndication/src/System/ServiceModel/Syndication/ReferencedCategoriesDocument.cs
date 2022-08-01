// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ServiceModel.Syndication
{
    public class ReferencedCategoriesDocument : CategoriesDocument
    {
        public ReferencedCategoriesDocument()
        {
        }

        public ReferencedCategoriesDocument(Uri link) : base()
        {
            if (link is null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            Link = link;
        }

        public Uri Link { get; set; }

        internal override bool IsInline => false;
    }
}
