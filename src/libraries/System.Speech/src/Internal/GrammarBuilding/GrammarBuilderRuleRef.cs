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
    internal sealed class GrammarBuilderRuleRef : GrammarBuilderBase
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
        /// <param name="uri"></param>
        /// <param name="rule"></param>
        internal GrammarBuilderRuleRef(Uri uri, string rule)
        {
            _uri = uri.OriginalString + ((rule != null) ? "#" + rule : "");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sgrsUri"></param>
        private GrammarBuilderRuleRef(string sgrsUri)
        {
            _uri = sgrsUri;
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
            GrammarBuilderRuleRef refObj = obj as GrammarBuilderRuleRef;
            if (refObj == null)
            {
                return false;
            }
            return _uri == refObj._uri;
        }

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return _uri.GetHashCode();
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
            return new GrammarBuilderRuleRef(_uri);
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
            Uri ruleUri = new Uri(_uri, UriKind.RelativeOrAbsolute);
            return elementFactory.CreateRuleRef(parent, ruleUri, null, null);
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
                return "#" + _uri;
            }
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
        private readonly string _uri;

        #endregion
    }
}
