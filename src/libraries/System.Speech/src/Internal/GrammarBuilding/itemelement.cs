// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    ///
    /// </summary>
    [DebuggerDisplay("{DebugSummary}")]
    internal sealed class ItemElement : BuilderElements
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        internal ItemElement(GrammarBuilderBase builder)
            : this(builder, 1, 1)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        internal ItemElement(int minRepeat, int maxRepeat)
            : this((GrammarBuilderBase)null, minRepeat, maxRepeat)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        internal ItemElement(GrammarBuilderBase builder, int minRepeat, int maxRepeat)
        {
            if (builder != null)
            {
                Add(builder);
            }
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builders"></param>
        /// <param name="minRepeat"></param>
        /// <param name="maxRepeat"></param>
        internal ItemElement(List<GrammarBuilderBase> builders, int minRepeat, int maxRepeat)
        {
            foreach (GrammarBuilderBase builder in builders)
            {
                Items.Add(builder);
            }
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="builders"></param>
        internal ItemElement(GrammarBuilder builders)
        {
            foreach (GrammarBuilderBase builder in builders.InternalBuilder.Items)
            {
                Items.Add(builder);
            }
        }

        #endregion

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.Equals"]/*' />
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

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        internal override GrammarBuilderBase Clone()
        {
            ItemElement item = new ItemElement(_minRepeat, _maxRepeat);
            item.CloneItems(this);
            return item;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elementFactory"></param>
        /// <param name="parent"></param>
        /// <param name="rule"></param>
        /// <param name="ruleIds"></param>
        /// <returns></returns>
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

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private readonly int _minRepeat = 1;
        private readonly int _maxRepeat = 1;

        #endregion
    }
}
