// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.GrammarBuilding
{

    internal abstract class GrammarBuilderBase
    {
        #region Internal Methods

        internal abstract GrammarBuilderBase Clone();

        internal abstract IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds);

        internal virtual int CalcCount(BuilderElements parent)
        {
            Marked = false;
            Parent = parent;
            return Count;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Used by the GrammarBuilder optimizer to count the number of children and descendant for
        /// an element
        /// </summary>
        internal virtual int Count
        {
            get
            {
                return _count;
            }

            set
            {
                _count = value;
            }
        }

        /// <summary>
        /// Marker to know if an element has already been visited.
        /// </summary>
        internal virtual bool Marked
        {
            get
            {
                return _marker;
            }

            set
            {
                _marker = value;
            }
        }

        /// <summary>
        /// Marker to know if an element has already been visited.
        /// </summary>
        internal virtual BuilderElements Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
            }
        }

        internal abstract string DebugSummary { get; }

        #endregion

        #region Private Fields

        private int _count = 1;

        private bool _marker;

        private BuilderElements _parent;

        #endregion
    }
}
