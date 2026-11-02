// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
    internal ImmutableArray<byte>.Builder CreateByteAccumulator()
        => ImmutableArray.CreateBuilder<byte>();

    internal void AddByte(ImmutableArray<byte>.Builder accumulator, byte value)
        => accumulator.Add(value);

    internal ImmutableArray<byte> EndBytes(ImmutableArray<byte>.Builder accumulator)
        => accumulator.DrainToImmutable();
#pragma warning restore CA1822

    /// <summary>
    /// Parses a single <c>hexbyte</c> token.
    /// </summary>
    internal static byte ParseHexbyte(IToken token)
    {
        // hexbyte can be HEXBYTE, INT32, or ID token (due to lexer ambiguity).
        ReadOnlySpan<char> text = token.Text.AsSpan();
        bool isNegative = text.StartsWith("-");
        if (isNegative)
        {
            text = text.Slice(1);
        }

        if (text.StartsWith("0x"))
        {
            text = text.Slice(2);
        }

        if (!uint.TryParse(
                text,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out uint value))
        {
            return 0;
        }

        if (isNegative)
        {
            value = unchecked(0u - value);
        }

        return (byte)(value & byte.MaxValue);
    }

}
