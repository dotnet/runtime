// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define CODE_ANALYSIS

using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Diagnostics;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class GrammarBuilderDictation : GrammarBuilderBase
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
        internal GrammarBuilderDictation()
            : this(null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        internal GrammarBuilderDictation(string category)
        {
            _category = category;
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
            GrammarBuilderDictation refObj = obj as GrammarBuilderDictation;
            if (refObj == null)
            {
                return false;
            }
            return _category == refObj._category;
        }

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return _category == null ? 0 : _category.GetHashCode();
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
            return new GrammarBuilderDictation(_category);
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
            // Return the IRuleRef to the dictation grammar
            return CreateRuleRefToDictation(elementFactory, parent);
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
                string category = _category != null ? ":" + _category : string.Empty;
                return "dictation" + category;
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
        private IRuleRef CreateRuleRefToDictation(IElementFactory elementFactory, IElement parent)
        {
            Uri ruleUri;
            if (!string.IsNullOrEmpty(_category) && _category == "spelling")
            {
                ruleUri = new Uri("grammar:dictation#spelling", UriKind.RelativeOrAbsolute);
            }
            else
            {
                ruleUri = new Uri("grammar:dictation", UriKind.RelativeOrAbsolute);
            }

            return elementFactory.CreateRuleRef(parent, ruleUri, null, null);
        }

        #endregion


        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        /// <summary>
        /// 
        /// </summary>
        private readonly string _category;

        #endregion
    }
}
