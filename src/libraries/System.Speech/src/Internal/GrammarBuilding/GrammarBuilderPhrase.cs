// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;

namespace System.Speech.Internal.GrammarBuilding
{
    [DebuggerDisplay("{DebugSummary}")]
    internal sealed class GrammarBuilderPhrase : GrammarBuilderBase
    {
        #region Constructors

        internal GrammarBuilderPhrase(string phrase)
            : this(phrase, false, SubsetMatchingMode.OrderedSubset)
        {
        }

        internal GrammarBuilderPhrase(string phrase, SubsetMatchingMode subsetMatchingCriteria)
            : this(phrase, true, subsetMatchingCriteria)
        {
        }

        private GrammarBuilderPhrase(string phrase, bool subsetMatching, SubsetMatchingMode subsetMatchingCriteria)
        {
            _phrase = phrase;
            _subsetMatching = subsetMatching;
            switch (subsetMatchingCriteria)
            {
                case SubsetMatchingMode.OrderedSubset:
                    _matchMode = MatchMode.OrderedSubset;
                    break;
                case SubsetMatchingMode.OrderedSubsetContentRequired:
                    _matchMode = MatchMode.OrderedSubsetContentRequired;
                    break;
                case SubsetMatchingMode.Subsequence:
                    _matchMode = MatchMode.Subsequence;
                    break;
                case SubsetMatchingMode.SubsequenceContentRequired:
                    _matchMode = MatchMode.SubsequenceContentRequired;
                    break;
            }
        }

        private GrammarBuilderPhrase(string phrase, bool subsetMatching, MatchMode matchMode)
        {
            _phrase = phrase;
            _subsetMatching = subsetMatching;
            _matchMode = matchMode;
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            GrammarBuilderPhrase refObj = obj as GrammarBuilderPhrase;
            if (refObj == null)
            {
                return false;
            }
            return _phrase == refObj._phrase && _matchMode == refObj._matchMode && _subsetMatching == refObj._subsetMatching;
        }
        public override int GetHashCode()
        {
            return _phrase.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal override GrammarBuilderBase Clone()
        {
            return new GrammarBuilderPhrase(_phrase, _subsetMatching, _matchMode);
        }

        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            return CreatePhraseElement(elementFactory, parent);
        }

        #endregion

        #region Internal Properties

        internal override string DebugSummary
        {
            get
            {
                return "'" + _phrase + "'";
            }
        }

        #endregion

        #region Private Methods

        private IElement CreatePhraseElement(IElementFactory elementFactory, IElement parent)
        {
            if (_subsetMatching)
            {
                // Create and return the ISubset representing the current phrase
                return elementFactory.CreateSubset(parent, _phrase, _matchMode);
            }
            else
            {
                if (elementFactory is SrgsElementCompilerFactory)
                {
                    XmlParser.ParseText(parent, _phrase, null, null, -1f, new CreateTokenCallback(elementFactory.CreateToken));
                }
                else
                {
                    // Create and return the IElementText representing the current phrase
                    return elementFactory.CreateText(parent, _phrase);
                }
            }
            return null;
        }

        #endregion

        #region Private Fields

        private readonly string _phrase;
        private readonly bool _subsetMatching;
        private readonly MatchMode _matchMode;

        #endregion
    }
}
