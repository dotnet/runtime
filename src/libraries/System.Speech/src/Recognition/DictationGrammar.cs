// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Speech.Recognition
{
    // Class for grammars based on a statistical language model for doing dictation.
    /// TODOC <_include file='doc\DictationGrammar.uex' path='docs/doc[@for="DictationGrammar"]/*' />
    
    public class DictationGrammar: Grammar
    {
        // The implementation of DictationGrammar stores a Uri in the Grammar.Uri field.
        // Then when LoadGrammar is called the Uri handling part of LoadGrammar is modified to check
        // if the grammar object is a DictationGrammar, in which case the SAPI dictation methods are called.
        // The Uri is "grammar:dictation" for regular dictation and "grammar:dictation#spelling" for a spelling. 

        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        // Load the generic dictation language model.
        /// TODOC <_include file='doc\DictationGrammar.uex' path='docs/doc[@for="DictationGrammar.DictationGrammar1"]/*' />
        public DictationGrammar() : base(_defaultDictationUri, null, null)
        {
        }

        // Load a specific topic. The topic is of the form "grammar:dictation#topic"
        /// TODOC <_include file='doc\DictationGrammar.uex' path='docs/doc[@for="DictationGrammar.DictationGrammar2"]/*' />
        public DictationGrammar(string topic) : base (new Uri(topic, UriKind.RelativeOrAbsolute), null, null)
        {
        }

        #endregion



        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        /// TODOC <_include file='doc\DictationGrammar.uex' path='docs/doc[@for="DictationGrammar.SetDictationContext"]/*' />
        public void SetDictationContext(string precedingText, string subsequentText)
        {
            if (State != GrammarState.Loaded)
            {
                throw new InvalidOperationException(SR.Get(SRID.GrammarNotLoaded));
            }
            // Note: You can only call this method after the Grammar is Loaded.
            // In theory we could support this more generally but there doesn't seem to be a lot of point.
            Debug.Assert(Recognizer != null);

            Recognizer.SetDictationContext(this, precedingText, subsequentText);
        }

        #endregion



        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods


        #endregion



        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private static Uri _defaultDictationUri = new Uri("grammar:dictation");

        #endregion

    }
}

