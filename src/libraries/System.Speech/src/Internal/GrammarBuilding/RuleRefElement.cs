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
    internal sealed class RuleRefElement : GrammarBuilderBase
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
        /// <param name="rule"></param>
        internal RuleRefElement(RuleElement rule)
        {
            _rule = rule;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="semanticKey"></param>
        internal RuleRefElement(RuleElement rule, string semanticKey)
        {
            _rule = rule;
            _semanticKey = semanticKey;
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
            RuleRefElement refObj = obj as RuleRefElement;
            if (refObj == null)
            {
                return false;
            }
            return _semanticKey == refObj._semanticKey && _rule.Equals(refObj._rule);
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
        /// <param name="item"></param>
        internal void Add(GrammarBuilderBase item)
        {
            _rule.Add(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override internal GrammarBuilderBase Clone()
        {
            return new RuleRefElement(_rule, _semanticKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <param name="builders"></param>
        internal void CloneItems(RuleRefElement builders)
        {
            _rule.CloneItems(builders._rule);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementFactory"></param>
        /// <param name="parent"></param>
        /// <param name="rule"></param>
        /// <param name="ruleIds"></param>
        /// <returns></returns>
        override internal IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create the new rule and add the reference to the item
            return elementFactory.CreateRuleRef(parent, new Uri("#" + Rule.RuleName, UriKind.Relative), _semanticKey, null);
        }

        #endregion


        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        internal RuleElement Rule
        {
            get
            {
                return _rule;
            }
        }

        override internal string DebugSummary
        {
            get
            {
                return "#" + Rule.Name + (_semanticKey != null ? ":" + _semanticKey : "");
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
        private readonly RuleElement _rule;
        private readonly string _semanticKey;

        #endregion
    }
}
