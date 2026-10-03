// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

internal sealed partial class CompiledGlobStrategy
{
    // <summary>
    //  Mutable matcher state passed by reference into <see cref="Backtrack"/> instead of
    //  individual <c>ref int</c> parameters. Cuts <see cref="Backtrack"/>'s argument list
    //  from ten to three, which on net481 RyuJIT measurably reduces call-site overhead and
    //  improves inlining behavior. Holds the active program/input cursors plus both
    //  savepoint slots (AnyRun and GlobStar) and the per-GlobStar invariants.
    // </summary>
    private ref struct BacktrackState
    {
        public int ProgramIndex;
        public int InputIndex;
        public int AnyRunProgramIndex;
        public int AnyRunInputIndex;
        public int GlobStarProgramIndex;
        public int GlobStarInputIndex;
        public int GlobStarInitialInput;
        public int GlobStarFlags;
    }
}
