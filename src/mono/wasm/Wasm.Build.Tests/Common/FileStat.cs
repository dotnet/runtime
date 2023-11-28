// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Wasm.Build.Tests;
public record FileStat(bool Exists, DateTime LastWriteTimeUtc, long Length, string FullPath);
