// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

/// <summary>
/// Answers what a type looks like in a wasm ABI signature.
/// </summary>
internal interface IWasmAbiTypeResolver
{
    /// <summary>
    /// Returns the signature encoding for <paramref name="type"/> in parameter position: a single
    /// character for a type passed by value, or "S&lt;size&gt;" for a struct passed by reference.
    /// </summary>
    /// <exception cref="LogAsErrorException">The type has no wasm ABI encoding, or could not be resolved.</exception>
    string GetAbiToken(Type type);
}
