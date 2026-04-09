// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal sealed class SemanticTag : ParseElement, ISemanticTag
    {
        #region Constructors

        internal SemanticTag(ParseElement parent, Backend backend)
            : base(parent._rule)
        {
        }

        #endregion

        #region Internal Methods
        // The probability that this item will be repeated.
        void ISemanticTag.Content(IElement parentElement, string sTag, int iLine)
        {
            //Return if the Tag content is empty
            sTag = sTag.Trim(Helpers._achTrimChars);

            if (string.IsNullOrEmpty(sTag))
            {
                return;
            }

            // Build semantic properties to attach to epsilon transition.
            // <tag>script</tag>
            _propInfo._ulId = (uint)iLine;
            _propInfo._comValue = sTag;

            ParseElementCollection parent = (ParseElementCollection)parentElement;

            // Attach the semantic properties on the parent element.
            parent.AddSemanticInterpretationTag(_propInfo);
        }

        #endregion

        #region Private Fields

        private CfgGrammar.CfgProperty _propInfo = new();

        #endregion
    }
}
