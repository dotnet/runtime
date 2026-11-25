// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  Opcode markers used by <see cref="CompiledGlobStrategy"/>'s bytecode-in-a-string
//  encoding. Values are Unicode noncharacters, which are
//  reserved by the Unicode standard for application-internal use and never appear
//  in conforming text.
// </summary>
internal static class GlobOpCodes
{
    // <summary>Match zero or more characters (<c>*</c>). No payload.</summary>
    public const char AnyRun = '\uFDD1';

    // <summary>
    //  Literal run. Followed by one length character and that many literal characters.
    // </summary>
    public const char Literal = '\uFDD2';

    // <summary>
    //  Globstar (<c>**</c>). Matches zero or more path segments (including the separators
    //  between them). Followed by one
    //  payload char whose low two bits encode which surrounding separators the scanner
    //  absorbed into this opcode: bit 0 (<see cref="GlobStarFlagLead"/>) means the
    //  pattern had a separator immediately before the <c>**</c> token; bit 1
    //  (<see cref="GlobStarFlagTrail"/>) means a separator followed it. The two bits
    //  together describe four shapes-whole-pattern <c>**</c> (neither),
    //  leading <c>**/</c> (trail only), trailing <c>/**</c> (lead only), and middle
    //  <c>/**/</c> (both)-each with their own validity constraints on the
    //  absorbed input slice.
    // </summary>
    public const char GlobStar = '\uFDD5';

    // <summary>
    //  GlobStar payload bit: a path separator preceded the <c>**</c> in the source
    //  pattern and was absorbed by this opcode. Equivalent to "the matched slice must
    //  start with the separator (or be empty when paired with a non-set
    //  <see cref="GlobStarFlagTrail"/>)".
    // </summary>
    public const int GlobStarFlagLead = 1;

    // <summary>
    //  GlobStar payload bit: a path separator followed the <c>**</c> in the source
    //  pattern and was absorbed by this opcode. Equivalent to "the matched slice must
    //  end with the separator (or be empty when paired with a non-set
    //  <see cref="GlobStarFlagLead"/>)".
    // </summary>
    public const int GlobStarFlagTrail = 2;
}
