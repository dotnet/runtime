// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.GrammarBuilding
{

    internal sealed class GrammarBuilderRuleRef : GrammarBuilderBase
    {
        #region Constructors

        internal GrammarBuilderRuleRef(Uri uri, string rule)
        {
            _uri = uri.OriginalString + ((rule != null) ? "#" + rule : "");
        }

        private GrammarBuilderRuleRef(string sgrsUri)
        {
            _uri = sgrsUri;
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            GrammarBuilderRuleRef refObj = obj as GrammarBuilderRuleRef;
            if (refObj == null)
            {
                return false;
            }
            return _uri == refObj._uri;
        }
        public override int GetHashCode()
        {
            return _uri.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal override GrammarBuilderBase Clone()
        {
            return new GrammarBuilderRuleRef(_uri);
        }

        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            Uri ruleUri = new(_uri, UriKind.RelativeOrAbsolute);
            return elementFactory.CreateRuleRef(parent, ruleUri, null, null);
        }

        #endregion

        #region Internal Properties

        internal override string DebugSummary
        {
            get
            {
                return "#" + _uri;
            }
        }

        #endregion

        #region Private Fields

        private readonly string _uri;

        #endregion
    }
}
