// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SemanticKeyElement : BuilderElements
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
        /// <param name="semanticKey"></param>
        internal SemanticKeyElement(string semanticKey)
        {
            _semanticKey = semanticKey;
            RuleElement rule = new RuleElement(semanticKey);
            _ruleRef = new RuleRefElement(rule, _semanticKey);
            Items.Add(rule);
            Items.Add(_ruleRef);
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
            SemanticKeyElement refObj = obj as SemanticKeyElement;
            if (refObj == null)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            // No need to check for the equality on _ruleRef. The children are in the Items, not the underlying rule
            return _semanticKey == refObj._semanticKey;
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
        /// <param name="phrase"></param>
        new internal void Add(string phrase)
        {
            _ruleRef.Add(new GrammarBuilderPhrase(phrase));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        new internal void Add(GrammarBuilder builder)
        {
            foreach (GrammarBuilderBase item in builder.InternalBuilder.Items)
            {
                _ruleRef.Add(item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override GrammarBuilderBase Clone()
        {
            SemanticKeyElement semanticKeyElement = new SemanticKeyElement(_semanticKey);
            semanticKeyElement._ruleRef.CloneItems(_ruleRef);
            return semanticKeyElement;
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
            // Create the rule associated with this key
            _ruleRef.Rule.CreateElement(elementFactory, parent, rule, ruleIds);

            // Create the ruleRef
            IElement ruleRef = _ruleRef.CreateElement(elementFactory, parent, rule, ruleIds);

            return ruleRef;
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
                return _ruleRef.Rule.DebugSummary;
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
        private readonly string _semanticKey;
        private readonly RuleRefElement _ruleRef;

        #endregion
    }
}
