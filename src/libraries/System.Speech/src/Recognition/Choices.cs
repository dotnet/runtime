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

    [DebuggerDisplay("{_oneOf.DebugSummary}")]
    public class Choices
    {

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public Choices()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrases"></param>
        public Choices(params string[] phrases)
        {
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            Add(phrases);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="alternateChoices"></param>
        public Choices(params GrammarBuilder[] alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            Add(alternateChoices);
        }

        #endregion



        #region Public Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="phrases"></param>
        public void Add(params string[] phrases)
        {
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            foreach (string phrase in phrases)
            {
                Helpers.ThrowIfEmptyOrNull(phrase, "phrase");

                _oneOf.Add(phrase);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="alternateChoices"></param>
        public void Add(params GrammarBuilder[] alternateChoices)
        {
            Helpers.ThrowIfNull(alternateChoices, nameof(alternateChoices));

            foreach (GrammarBuilder alternateChoice in alternateChoices)
            {
                Helpers.ThrowIfNull(alternateChoice, "alternateChoice");

                _oneOf.Items.Add(new ItemElement(alternateChoice));
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <returns></returns>
        public GrammarBuilder ToGrammarBuilder()
        {
            return new GrammarBuilder(this);
        }

        #endregion

        #region Internal Properties

        internal OneOfElement OneOf
        {
            get
            {
                return _oneOf;
            }
        }

        #endregion



        #region Private Fields

        private OneOfElement _oneOf = new();

        #endregion
    }
}
