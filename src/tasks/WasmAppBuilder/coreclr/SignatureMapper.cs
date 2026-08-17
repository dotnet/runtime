// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// Computes Wasm signature strings from reflection metadata.
// The signature string format is documented in docs/design/coreclr/botr/readytorun-format.md
// (section "Wasm Signature String Encoding").
internal sealed class SignatureMapper
{
    private readonly LogAdapter _log;
    private readonly IWasmAbiTypeResolver _resolver;

    public SignatureMapper(LogAdapter log, IWasmAbiTypeResolver resolver)
    {
        _log = log;
        _resolver = resolver;
    }

    internal char? TypeToChar(
        Type t,
        out bool isByRefStruct,
        int depth = 0)
    {
        isByRefStruct = false;

        if (depth > 5) {
            _log.Warning("WASM0064", $"Unbounded recursion detected through parameter type '{t.Name}'");
            return null;
        }

        // See https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md
        char? c = null;
        if (t.Namespace == "System")
        {
            c = t.Name switch
            {
                nameof(String) => 'i',
                nameof(Boolean) => 'i',
                nameof(Char) => 'i',
                nameof(SByte) => 'i',
                nameof(Byte) => 'i',
                nameof(Int16) => 'i',
                nameof(UInt16) => 'i',
                nameof(Int32) => 'i',
                nameof(UInt32) => 'i',
                nameof(Int64) => 'l',
                nameof(UInt64) => 'l',
                nameof(Single) => 'f',
                nameof(Double) => 'd',
                // FIXME: These will need to be L for wasm64
                nameof(IntPtr) => 'i',
                nameof(UIntPtr) => 'i',
                "Void" => 'v',
                _ => null
            };
        }

        if (c != null)
            return c;

        // FIXME: Most of these need to be L for wasm64
        if (t.IsByRef)
            c = 'i';
        else if (t.IsClass)
            c = 'i';
        else if (t.IsInterface)
            c = 'i';
        else if (t.IsEnum)
        {
            Type underlyingType = t.GetEnumUnderlyingType();
            c = TypeToChar(underlyingType, out _, ++depth);
        }
        else if (t.IsPointer)
            c = 'i';
        else if (PInvokeTableGenerator.IsFunctionPointer(t))
            c = 'i';
        else if (t.IsValueType)
        {
            // Reflection has no field layout engine, so the ABI encoding of a struct - its size and
            // alignment, and whether it collapses to a single primitive or spreads across several
            // wasm parameters - comes from the compiler's own type system.
            string token = _resolver.GetAbiToken(t);
            if (IsMultiSlotToken(token))
            {
                // A type the wasm ABI splits across several by-value slots is rejected in interop
                // rather than encoded. Supporting it means teaching the thunk generator that one
                // signature token can map to several native parameters, which is a larger design
                // question; today no InternalCall or PInvoke signature uses one.
                _log.Error("WASM0068",
                    $"SignatureMapper: '{t.FullName ?? t.Name}' is passed across multiple wasm slots, which interop signatures do not support");
                return null;
            }

            if (token[0] is 'S' or 'A')
            {
                isByRefStruct = true;
            }

            c = token[0];
        }
        else
            _log.Warning("WASM0065", $"Unsupported parameter type '{t.Name}'");

        return c;
    }

    /// <summary>
    /// Returns the wasm signature string for a method.
    /// </summary>
    /// <remarks>
    /// Delegates to the compiler's own lowering rather than building the string from
    /// <see cref="TypeToChar"/>. That resolves each parameter from the method's signature blob, so
    /// generic instantiations work, and it keeps one implementation of the encoding instead of a
    /// second one here that has to be kept in agreement with compiled code.
    /// </remarks>
    public string? MethodToSignature(MethodInfo method, bool includeThis = false)
    {
        // A managed signature is what picks up the 'T' for an instance method and the trailing 'p';
        // everything else describes a native function.
        WasmLoweringFlags flags = includeThis ? WasmLoweringFlags.None : WasmLoweringFlags.IsUnmanagedCallersOnly;

        return _resolver.GetMethodSignature(method, flags);
    }

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

    public string TypeToNameType(Type t)
    {
        char? c = TypeToChar(t, out _);
        if (c is null)
            throw new InvalidSignatureCharException('?');

        return CharToNameType(c.Value);
    }

    public static bool IsVoidSignature(string signature) => signature[0] == 'v';
}

internal sealed class InvalidSignatureCharException : Exception
{
    public char Char { get; private set; }

    public InvalidSignatureCharException(char c) : base($"Can't handle signature '{c}'") => Char = c;
}
