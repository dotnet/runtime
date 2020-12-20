// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem"]/*' />
    public enum AudioSignalProblem
    {
        // No signal problem.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.None"]/*' />
        None = 0,

        // The audio input is too noisy for accurate recognition of the input phrase.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.TooNoisy"]/*' />
        TooNoisy,

        // The audio input does not contain any audio signal (flat line).
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.NoSignal"]/*' />
        NoSignal,

        // The audio input is too loud, resulting in clipping of the signal.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.TooLoud"]/*' />
        TooLoud,

        // The audio input is too soft, resulting in sub-optimal recognition of the input phrase.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.TooSoft"]/*' />
        TooSoft,

        // The audio input is too fast for optimal recognition.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.TooFast"]/*' />
        TooFast,

        // The audio input is too slow for optimal recognition.
        /// TODOC <_include file='doc\AudioSignalProblem.uex' path='docs/doc[@for="AudioSignalProblem.TooSlow"]/*' />
        TooSlow
    }
}
