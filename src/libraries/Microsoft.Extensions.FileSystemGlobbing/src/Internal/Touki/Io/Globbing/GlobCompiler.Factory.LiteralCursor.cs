// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

internal static partial class GlobCompiler
{
    private static partial class Factory
    {
        // <summary>
        //  Records the in-progress position and length of the most recently emitted
        //  <see cref="GlobOpCodes.Literal"/> opcode within the encoder's
        //  <see cref="ValueStringBuilder"/>. <see cref="TryEncodeProgram"/> uses this so the
        //  <see cref="GlobOpCodes.GlobStar"/> emitter can retroactively strip the trailing
        //  separator from the prior Literal when a segment-bounded <c>**</c> absorbs it.
        //  <see cref="None"/> represents "no Literal currently at the tail of the buffer"
        //  (the most recent opcode is something else, or the buffer is empty).
        // </summary>
        private struct LiteralCursor
        {
            public int Start;
            public int Length;

            public static LiteralCursor None => new() { Start = -1, Length = 0 };
        }
    }
}
