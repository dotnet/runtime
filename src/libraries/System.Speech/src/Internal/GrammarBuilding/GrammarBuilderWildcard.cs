// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.GrammarBuilding
{

    internal sealed class GrammarBuilderWildcard : GrammarBuilderBase
    {
        #region Constructors

        internal GrammarBuilderWildcard()
        {
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            GrammarBuilderWildcard refObj = obj as GrammarBuilderWildcard;
            return refObj != null;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal override GrammarBuilderBase Clone()
        {
            return new GrammarBuilderWildcard();
        }

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
