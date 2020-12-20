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
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase"></param>
        internal GrammarBuilderPhrase(string phrase)
            : this(phrase, false, SubsetMatchingMode.OrderedSubset)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="subsetMatchingCriteria"></param>
        internal GrammarBuilderPhrase(string phrase, SubsetMatchingMode subsetMatchingCriteria)
            : this(phrase, true, subsetMatchingCriteria)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="subsetMatching"></param>
        /// <param name="subsetMatchingCriteria"></param>
        private GrammarBuilderPhrase(string phrase, bool subsetMatching, SubsetMatchingMode subsetMatchingCriteria)
        {
            _phrase = string.Copy(phrase);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="subsetMatching"></param>
        /// <param name="matchMode"></param>
        private GrammarBuilderPhrase(string phrase, bool subsetMatching, MatchMode matchMode)
        {
            _phrase = string.Copy(phrase);
            _subsetMatching = subsetMatching;
            _matchMode = matchMode;
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
            GrammarBuilderPhrase refObj = obj as GrammarBuilderPhrase;
            if (refObj == null)
            {
                return false;
            }
            return _phrase == refObj._phrase && _matchMode == refObj._matchMode && _subsetMatching == refObj._subsetMatching;
        }

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return _phrase.GetHashCode();
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
            return new GrammarBuilderPhrase(_phrase, _subsetMatching, _matchMode);
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
            return CreatePhraseElement(elementFactory, parent);
        }

        #endregion


        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        override internal string DebugSummary
        {
            get
            {
                return "'" + _phrase + "'";
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementFactory"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
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


        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private readonly string _phrase;
        private readonly bool _subsetMatching;
        private readonly MatchMode _matchMode;

        #endregion
    }
}
