// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// The half of SignatureMapper that maps signature tokens to native types. It is kept free of
// MSBuild dependencies so that tests can compile it directly; the reflection-based half needs a
// LogAdapter and cannot be linked on its own.
internal static partial class SignatureMapper
{
    /// <summary>
    /// Parses a signature string into individual tokens.
    /// Single-char types produce one-char tokens; struct encodings produce multi-char tokens like
    /// "S8" or "A32", and a multi-slot parameter produces a two-char token like "l2" or "V4".
    /// The 'a' and 'p' suffixes are included as their own tokens.
    /// </summary>
    public static List<string> ParseSignatureTokens(string signature)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < signature.Length)
        {
            if (signature[i] is 'S' or 'A')
            {
                int start = i;
                i++; // skip 'S'/'A'
                while (i < signature.Length && char.IsDigit(signature[i]))
                    i++;
                tokens.Add(signature.Substring(start, i - start));
            }
            else if (signature[i] is 'l' or 'V' && i + 1 < signature.Length && char.IsDigit(signature[i + 1]))
            {
                tokens.Add(signature.Substring(i, 2));
                i += 2;
            }
            else
            {
                tokens.Add(signature[i].ToString());
                i++;
            }
        }

        return tokens;
    }

    /// <summary>
    /// True for a token describing a type passed by value across several wasm parameters
    /// ("l2", "V2", "V4"). Interop signatures do not use these today.
    /// </summary>
    private static bool IsMultiSlotToken(string token)
        => token.Length == 2 && token[0] is 'l' or 'V' && char.IsDigit(token[1]);

    private static void RejectMultiSlotToken(string token)
    {
        if (IsMultiSlotToken(token))
            throw new NotSupportedException($"Multi-slot signature token '{token}' is not supported in interop thunks");
    }

    public static string TokenToNativeType(string token)
    {
        RejectMultiSlotToken(token);
        return token[0] switch
        {
            'v' => "void",
            'i' => "int32_t",
            'l' => "int64_t",
            'f' => "float",
            'd' => "double",
            'S' or 'A' => "int32_t",
            'T' => "int32_t",
            'p' => "PCODE",
            _ => throw new InvalidSignatureCharException(token[0])
        };
    }

    public static string TokenToNameType(string token)
    {
        RejectMultiSlotToken(token);
        return token[0] switch
        {
            'v' => "Void",
            'i' => "I32",
            'l' => "I64",
            'f' => "F32",
            'd' => "F64",
            'S' or 'A' => token,
            'T' => "This",
            'p' => "PE",
            _ => throw new InvalidSignatureCharException(token[0])
        };
    }

    public static string TokenToArgType(string token)
    {
        RejectMultiSlotToken(token);
        return token[0] switch
        {
            'i' => "ARG_I32",
            'l' => "ARG_I64",
            'f' => "ARG_F32",
            'd' => "ARG_F64",
            'S' or 'A' => "ARG_IND",
            'T' => "ARG_I32",
            _ => throw new InvalidSignatureCharException(token[0])
        };
    }

    /// <summary>
    /// Returns the number of INTERP_STACK_SLOT_SIZE slots consumed by a token.
    /// Struct tokens consume max((size + 7) / 8, 1) slots; all others consume 1.
    /// </summary>
    public static int TokenToSlotCount(string token)
    {
        if (token[0] is not ('S' or 'A') || token.Length < 2)
            return 1;

        int size = GetStructSize(token);
        return Math.Max((size + 7) / 8, 1);
    }

    internal static int GetStructSize(string token)
    {
        return int.Parse(token.Substring(1));
    }

    // Legacy single-char overloads — still used by consumers that don't encounter S<N> tokens.
    public static string CharToNativeType(char c) => TokenToNativeType(c.ToString());
    public static string CharToNameType(char c) => TokenToNameType(c.ToString());
    public static string CharToArgType(char c) => TokenToArgType(c.ToString());

    public static bool IsVoidSignature(string signature) => signature[0] == 'v';
}

internal sealed class InvalidSignatureCharException : Exception
{
    public char Char { get; private set; }

    public InvalidSignatureCharException(char c) : base($"Can't handle signature '{c}'") => Char = c;
}
