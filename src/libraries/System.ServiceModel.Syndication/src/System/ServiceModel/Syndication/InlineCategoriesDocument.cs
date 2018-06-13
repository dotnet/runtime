// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace System.ServiceModel.Syndication
{
    public class InlineCategoriesDocument : CategoriesDocument
    {
        private Collection<SyndicationCategory> _categories;

        public InlineCategoriesDocument()
        {
        }

        public InlineCategoriesDocument(IEnumerable<SyndicationCategory> categories) : this(categories, false, null)
        {
        }

        public InlineCategoriesDocument(IEnumerable<SyndicationCategory> categories, bool isFixed, string scheme)
        {
            if (categories != null)
            {
                _categories = new NullNotAllowedCollection<SyndicationCategory>();
                foreach (SyndicationCategory category in categories)
                {
                    _categories.Add(category);
                }
            }

            IsFixed = isFixed;
            Scheme = scheme;
        }

        public Collection<SyndicationCategory> Categories
        {
            get => _categories ?? (_categories = new NullNotAllowedCollection<SyndicationCategory>());
        }

        public bool IsFixed { get; set; }

        public string Scheme { get; set; }

        internal override bool IsInline => true;

        internal protected virtual SyndicationCategory CreateCategory() => new SyndicationCategory();
    }
}
