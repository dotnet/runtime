// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

internal static partial class GlobCompiler
{
    public static bool TryCompile(
        string pattern,
        bool ignoreCase,
        [NotNullWhen(true)] out GlobStrategy? result) =>
        Factory.TryCreate(pattern, ignoreCase, out result);
}
