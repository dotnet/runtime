// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    public enum AudioSignalProblem
    {
        // No signal problem.
        None = 0,

        // The audio input is too noisy for accurate recognition of the input phrase.
        TooNoisy,

        // The audio input does not contain any audio signal (flat line).
        NoSignal,

        // The audio input is too loud, resulting in clipping of the signal.
        TooLoud,

        // The audio input is too soft, resulting in sub-optimal recognition of the input phrase.
        TooSoft,

        // The audio input is too fast for optimal recognition.
        TooFast,

        // The audio input is too slow for optimal recognition.
        TooSlow
    }
}
