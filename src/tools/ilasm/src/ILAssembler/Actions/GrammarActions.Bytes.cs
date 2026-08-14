// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private ImmutableArray<byte>.Builder? _byteAccumulator;

    /// <summary>
    /// Starts accumulating the bytes of a <c>bytearray</c> literal.
    /// </summary>
    /// <remarks>
    /// Parse-tree construction is disabled for the entire parse, so the individual <c>hexbyte</c>
    /// contexts and terminals are collectable as soon as they are matched. The bytes themselves
    /// stream into this accumulator, which keeps a single byte array alive instead of a context and
    /// a terminal node per byte.
    /// </remarks>
    internal void BeginBytes() => _byteAccumulator = ImmutableArray.CreateBuilder<byte>();

    internal void AddByte(byte value) => _byteAccumulator?.Add(value);

    internal ImmutableArray<byte> EndBytes()
    {
        ImmutableArray<byte>.Builder? accumulator = _byteAccumulator;
        _byteAccumulator = null;
        return accumulator?.DrainToImmutable() ?? ImmutableArray<byte>.Empty;
    }

    /// <summary>
    /// Parses a single <c>hexbyte</c> token.
    /// </summary>
    internal static byte ParseHexbyte(IToken token)
    {
        // hexbyte can be HEXBYTE, INT32, or ID token (due to lexer ambiguity).
        // Validate the text is 1-2 hex characters to avoid FormatException
        // from non-hex ID tokens or values > 0xFF from longer INT32 tokens.
        string text = token.Text;
        if (text.Length <= 2 && byte.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
        {
            return value;
        }

        // For invalid hex values, mask to byte (matching native ilasm tolerance).
        return int.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int intValue)
            ? (byte)(intValue & 0xFF)
            : (byte)0;
    }

}
