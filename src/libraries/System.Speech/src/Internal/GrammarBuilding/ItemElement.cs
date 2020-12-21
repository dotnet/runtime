// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;

namespace System.Speech.Internal.GrammarBuilding
{
    [DebuggerDisplay("{DebugSummary}")]
    internal sealed class ItemElement : BuilderElements
    {
        #region Constructors

        internal ItemElement(GrammarBuilderBase builder)
            : this(builder, 1, 1)
        {
        }

        internal ItemElement(int minRepeat, int maxRepeat)
            : this((GrammarBuilderBase)null, minRepeat, maxRepeat)
        {
        }

        internal ItemElement(GrammarBuilderBase builder, int minRepeat, int maxRepeat)
        {
            if (builder != null)
            {
                Add(builder);
            }
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
        }

        internal ItemElement(List<GrammarBuilderBase> builders, int minRepeat, int maxRepeat)
        {
            foreach (GrammarBuilderBase builder in builders)
            {
                Items.Add(builder);
            }
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
        }

        internal ItemElement(GrammarBuilder builders)
        {
            foreach (GrammarBuilderBase builder in builders.InternalBuilder.Items)
            {
                Items.Add(builder);
            }
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            ItemElement refObj = obj as ItemElement;
            if (refObj == null)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            return _minRepeat == refObj._minRepeat && _maxRepeat == refObj._maxRepeat;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal override GrammarBuilderBase Clone()
        {
            ItemElement item = new(_minRepeat, _maxRepeat);
            item.CloneItems(this);
            return item;
        }

        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create and return the real item (the item including the grammar)
            // for the current grammar
            IItem item = elementFactory.CreateItem(parent, rule, _minRepeat, _maxRepeat, 0.5f, 1f);

            // Create the children elements
            CreateChildrenElements(elementFactory, item, rule, ruleIds);

            return item;
        }

        #endregion

        #region Private Fields

        private readonly int _minRepeat = 1;
        private readonly int _maxRepeat = 1;

        #endregion
    }
}
