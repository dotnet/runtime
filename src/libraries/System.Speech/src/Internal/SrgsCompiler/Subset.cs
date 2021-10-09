// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal class Subset : ParseElement, ISubset
    {
        #region Constructors

        /// <summary>
        /// Process the 'subset' element.
        /// </summary>
        public Subset(ParseElementCollection parent, Backend backend, string text, MatchMode mode)
            : base(parent._rule)
        {
            // replace tab, cr, lf with spaces
            foreach (char ch in Helpers._achTrimChars)
            {
                if (ch == ' ')
                {
                    continue;
                }
                if (text.Contains(ch))
                {
                    text = text.Replace(ch, ' ');
                }
            }

            // Add transition to the new state with normalized token.
            parent.AddArc(backend.SubsetTransition(text, mode));
        }

        #endregion

        #region Internal Method
        void IElement.PostParse(IElement parentElement)
        {
        }

        #endregion
    }
}
