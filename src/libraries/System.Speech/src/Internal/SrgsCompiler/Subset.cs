// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System;
using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal class Subset : ParseElement, ISubset
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// Process the 'subset' element.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="backend"></param>
        /// <param name="text"></param>
        /// <param name="mode"></param>
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
                if (text.IndexOf(ch) >= 0)
                {
                    text = text.Replace(ch, ' ');
                }
            }

            // Add transition to the new state with normalized token.
            parent.AddArc(backend.SubsetTransition(text, mode));
        }

        #endregion

        //*******************************************************************
        //
        // Internal Method
        //
        //*******************************************************************

        #region Intenal Method

        /// <summary>
        /// </summary>
        /// <param name="parentElement"></param>
        void IElement.PostParse(IElement parentElement)
        {
        }

        #endregion
    }
}
