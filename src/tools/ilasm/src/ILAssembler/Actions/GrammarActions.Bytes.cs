// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
