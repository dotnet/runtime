// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Diagnostics;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class GrammarBuilderWildcard : GrammarBuilderBase
    {

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        internal GrammarBuilderWildcard()
        {
        }

        #endregion


        #region Public Methods

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.Equals"]/*' />
        public override bool Equals(object obj)
        {
            GrammarBuilderWildcard refObj = obj as GrammarBuilderWildcard;
            return refObj != null;
        }

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion


        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        internal override GrammarBuilderBase Clone()
        {
            return new GrammarBuilderWildcard();
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
            // Return a ruleref to Garbage
            IRuleRef ruleRef = elementFactory.Garbage;

            elementFactory.InitSpecialRuleRef(parent, ruleRef);

            return ruleRef;
        }

        #endregion

        #region Internal Properties

        internal override string DebugSummary
        {
            get
            {
                return "*";
            }
        }

        #endregion
    }
}
