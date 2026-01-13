// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static class Backoff
    {
        // We will use exponential backoff in rare cases when we need to change state atomically and cannot
        // make progress due to concurrent state changes by other threads.
        // While we cannot know the ideal amount of wait needed before making a successful attempt,
        // the exponential backoff will generally be not more than 2X worse than the perfect guess and
        // will do a lot less attempts than a simple retry. On multiprocessor machine fruitless attempts
        // will cause unnecessary sharing of the contended state which may make modifying the state more expensive.
        // To protect against degenerate cases we will cap the per-iteration wait to a few thousand spinwaits.
        private const uint MaxExponentialBackoffBits = 14;

        internal static unsafe void Exponential(uint attempt)
        {
            attempt = Math.Min(attempt, MaxExponentialBackoffBits);
            // We will backoff for some random number of spins that roughly grows as attempt^2
            // No need for much randomness here, randomness is "good to have", we could do without it,
            // so we will just cheaply hash in the stack location.
            uint rand = (uint)&attempt * 2654435769u;
            // Set the highmost bit to ensure minimum number of spins is exponentially increasing.
            // It basically guarantees that we spin at least 0, 1, 2, 4, 8, 16, times, and so on
            rand |= (1u << 31);
            uint spins = rand >> (byte)(32 - attempt);
            Thread.SpinWait((int)spins);
        }
    }
}
