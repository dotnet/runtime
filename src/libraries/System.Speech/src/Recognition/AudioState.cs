// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{
    // Current audio state.
    /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="AudioState"]/*' />
    public enum AudioState
    {
        // The audio input is stopped.
        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="AudioState.Stopped"]/*' />
        Stopped,

        // The audio input contains silence.
        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="AudioState.Silence"]/*' />
        Silence,

        // The audio input contains speech signal.
        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="AudioState.Speech"]/*' />
        Speech
    }
}
