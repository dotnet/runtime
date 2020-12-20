// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Internal.GrammarBuilding;
using System.Speech.Internal;

namespace System.Speech.Recognition
{
    /// <summary>
    /// 
    /// </summary>

    [DebuggerDisplay("{_tag.DebugSummary}")]
    public class SemanticResultValue
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="value"></param>
        public SemanticResultValue(object value)
        {
            Helpers.ThrowIfNull(value, "value");

            _tag = new TagElement(value);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="value"></param>
        public SemanticResultValue(string phrase, object value)
        {
            Helpers.ThrowIfEmptyOrNull(phrase, "phrase");
            Helpers.ThrowIfNull(value, "value");

            _tag = new TagElement(new GrammarBuilderPhrase((string)phrase.Clone()), value);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="value"></param>
        public SemanticResultValue(GrammarBuilder builder, object value)
        {
            Helpers.ThrowIfNull(builder, "builder");
            Helpers.ThrowIfNull(value, "value");

            _tag = new TagElement(builder.Clone(), value);
        }

        #endregion


        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        /// <summary>
        /// TODOC
        /// </summary>
        /// <returns></returns>
        public GrammarBuilder ToGrammarBuilder()
        {
            return new GrammarBuilder(this);
        }

        #endregion

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        /// <summary>
        /// 
        /// </summary>
        internal TagElement Tag
        {
            get
            {
                return _tag;
            }
        }

        #endregion


        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private TagElement _tag;

        #endregion
    }
}
