// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    /// <summary>
    /// Summary description for Rule.
    /// </summary>
    internal sealed class SemanticTag : ParseElement, ISemanticTag
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal SemanticTag(ParseElement parent, Backend backend)
            : base(parent._rule)
        {
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// TODOC <_include file='doc\Tag.uex' path='docs/doc[@for="Tag.RepeatProbability"]/*' />
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

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private CfgGrammar.CfgProperty _propInfo = new();

        #endregion
    }
}
