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

    [DebuggerDisplay("{_semanticKey.DebugSummary}")]
    public class SemanticResultKey
    {

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="semanticResultKey"></param>
        private SemanticResultKey(string semanticResultKey)
            : base()
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));

            _semanticKey = new SemanticKeyElement(semanticResultKey);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="semanticResultKey"></param>
        /// <param name="phrases"></param>
        public SemanticResultKey(string semanticResultKey, params string[] phrases)
            : this(semanticResultKey)
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));
            Helpers.ThrowIfNull(phrases, nameof(phrases));

            // Build a grammar builder with all the phrases
            foreach (string phrase in phrases)
            {
                _semanticKey.Add((string)phrase.Clone());
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="semanticResultKey"></param>
        /// <param name="builders"></param>
        public SemanticResultKey(string semanticResultKey, params GrammarBuilder[] builders)
            : this(semanticResultKey)
        {
            Helpers.ThrowIfEmptyOrNull(semanticResultKey, nameof(semanticResultKey));
            Helpers.ThrowIfNull(builders, "phrases");

            // Build a grammar builder with all the grammar builders
            foreach (GrammarBuilder builder in builders)
            {
                _semanticKey.Add(builder.Clone());
            }
        }

        #endregion


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

        #region Internal Properties

        internal SemanticKeyElement SemanticKeyElement
        {
            get
            {
                return _semanticKey;
            }
        }

        #endregion


        #region Private Fields

        private readonly SemanticKeyElement _semanticKey;

        #endregion
    }
}
