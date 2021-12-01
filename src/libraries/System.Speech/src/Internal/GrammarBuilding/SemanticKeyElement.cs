// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;

namespace System.Speech.Internal.GrammarBuilding
{

    internal sealed class SemanticKeyElement : BuilderElements
    {
        #region Constructors

        internal SemanticKeyElement(string semanticKey)
        {
            _semanticKey = semanticKey;
            RuleElement rule = new(semanticKey);
            _ruleRef = new RuleRefElement(rule, _semanticKey);
            Items.Add(rule);
            Items.Add(_ruleRef);
        }

        #endregion

        #region Public Methods
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

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal new void Add(string phrase)
        {
            _ruleRef.Add(new GrammarBuilderPhrase(phrase));
        }

        internal new void Add(GrammarBuilder builder)
        {
            foreach (GrammarBuilderBase item in builder.InternalBuilder.Items)
            {
                _ruleRef.Add(item);
            }
        }

        internal override GrammarBuilderBase Clone()
        {
            SemanticKeyElement semanticKeyElement = new(_semanticKey);
            semanticKeyElement._ruleRef.CloneItems(_ruleRef);
            return semanticKeyElement;
        }

        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create the rule associated with this key
            _ruleRef.Rule.CreateElement(elementFactory, parent, rule, ruleIds);

            // Create the ruleRef
            IElement ruleRef = _ruleRef.CreateElement(elementFactory, parent, rule, ruleIds);

            return ruleRef;
        }

        #endregion

        #region Internal Properties

        internal override string DebugSummary
        {
            get
            {
                return _ruleRef.Rule.DebugSummary;
            }
        }

        #endregion

        #region Private Fields

        private readonly string _semanticKey;
        private readonly RuleRefElement _ruleRef;

        #endregion
    }
}
