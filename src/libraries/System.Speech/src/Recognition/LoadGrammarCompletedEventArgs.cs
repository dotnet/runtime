// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;


namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="LoadGrammarCompletedEventArgs"]/*' />
    // Event args used in the LoadGrammarCompleted event.

    public class LoadGrammarCompletedEventArgs : AsyncCompletedEventArgs
    {

        #region Constructors

        internal LoadGrammarCompletedEventArgs(Grammar grammar, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _grammar = grammar;
        }

        #endregion



        #region Public Properties

        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="LoadGrammarCompletedEventArgs.Grammar"]/*' />
        public Grammar Grammar
        {
            get { return _grammar; }
        }

        #endregion



        #region Private Fields

#pragma warning disable 6524
        private Grammar _grammar;
#pragma warning restore 6524

        #endregion

    }
}
