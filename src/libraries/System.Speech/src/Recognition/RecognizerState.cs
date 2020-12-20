// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;


namespace System.Speech.Recognition
{
    // Current recognizer state.
    /// TODOC <_include file='doc\RecognizerState.uex' path='docs/doc[@for="RecognizerState"]/*' />
    public enum RecognizerState
    {
        // The recognizer is currently stopped and not listening.
        /// TODOC <_include file='doc\RecognizerState.uex' path='docs/doc[@for="RecognizerState.Stopped"]/*' />
        Stopped,

        // The recognizer is currently listening.
        /// TODOC <_include file='doc\RecognizerState.uex' path='docs/doc[@for="RecognizerState.Listening"]/*' />
        Listening
    }
}


