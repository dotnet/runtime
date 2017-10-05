// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Runtime.CompilerServices;

    public class ReferencedCategoriesDocument : CategoriesDocument
    {
        private Uri _link;

        public ReferencedCategoriesDocument()
        {
        }

        public ReferencedCategoriesDocument(Uri link)
            : base()
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            _link = link;
        }

        public Uri Link
        {
            get { return _link; }
            set { _link = value; }
        }

        internal override bool IsInline
        {
            get { return false; }
        }
    }
}
