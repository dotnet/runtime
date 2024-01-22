// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            get => _categories ??= new NullNotAllowedCollection<SyndicationCategory>();
        }

        public bool IsFixed { get; set; }

        public string Scheme { get; set; }

        internal override bool IsInline => true;

        protected internal virtual SyndicationCategory CreateCategory() => new SyndicationCategory();
    }
}
