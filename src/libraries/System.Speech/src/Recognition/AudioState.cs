// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // Current audio state.
    public enum AudioState
    {
        // The audio input is stopped.
        Stopped,

        // The audio input contains silence.
        Silence,

        // The audio input contains speech signal.
        Speech
    }
}
