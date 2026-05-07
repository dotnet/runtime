// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// Computes Wasm signature strings from reflection metadata.
// The signature string format is documented in docs/design/coreclr/botr/readytorun-format.md
// (section "Wasm Signature String Encoding").
internal static class SignatureMapper
{
    // Hardcoded struct sizes for types that crossgen2 encodes as S<N>.
    // The fully general case is handled by crossgen2's type system; these
    // cover the small set of multi-field structs that appear in InternalCall
    // and PInvoke signatures.
    private static readonly Dictionary<string, int> s_knownStructSizes = new()
    {
        ["System.Runtime.CompilerServices.QCallModule"] = 8,
        ["System.Runtime.CompilerServices.QCallAssembly"] = 8,
        ["System.Runtime.CompilerServices.QCallTypeHandle"] = 8,
        ["System.GC+GCHeapHardLimitInfo"] = 64,
    };

    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct, out int structSize, int depth = 0)
    {
        isByRefStruct = false;
        structSize = 0;

        if (depth > 5) {
            log.Warning("WASM0064", $"Unbounded recursion detected through parameter type '{t.Name}'");
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
            c = TypeToChar(underlyingType, log, out _, out structSize, ++depth);
        }
        else if (t.IsPointer)
            c = 'i';
        else if (PInvokeTableGenerator.IsFunctionPointer(t))
            c = 'i';
        else if (t.IsValueType)
        {
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields.Length == 1)
            {
                Type fieldType = fields[0].FieldType;
                return TypeToChar(fieldType, log, out isByRefStruct, out structSize, ++depth);
            }
            else
            {
                string fullName = t.FullName ?? t.Name;
                if (s_knownStructSizes.TryGetValue(fullName, out int size))
                {
                    structSize = size;
                }
                else
                {
                    log.Error("WASM0067",
                        $"SignatureMapper: unknown multi-field struct '{fullName}' (fields: {fields.Length}) — add its size to s_knownStructSizes in SignatureMapper.cs");
                    return null;
                }

                c = 'S';
            }

            isByRefStruct = true;
        }
        else
            log.Warning("WASM0065", $"Unsupported parameter type '{t.Name}'");

        return c;
    }

    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct, int depth = 0)
        => TypeToChar(t, log, out isByRefStruct, out _, depth);

    /// <summary>
    /// Builds the multi-char token for a type in the signature string.
    /// For most types this is a single character; for multi-field structs it is "S&lt;N&gt;".
    /// </summary>
    private static string? TypeToSignatureToken(Type t, LogAdapter log, out bool isByRefStruct)
    {
        char? c = TypeToChar(t, log, out isByRefStruct, out int structSize);
        if (c is null)
            return null;

        if (c == 'S' && structSize > 0)
            return $"S{structSize}";

        return c.Value.ToString();
    }

    public static string? MethodToSignature(MethodInfo method, LogAdapter log, bool includeThis = false)
    {
        string? returnToken = TypeToSignatureToken(method.ReturnType, log, out bool resultIsByRef);
        if (returnToken is null)
            return null;

        var sb = new StringBuilder();

        if (resultIsByRef)
        {
            // Struct return — encode as S<N> (the return type token already has the size)
            sb.Append(returnToken);
        }
        else
        {
            sb.Append(returnToken);
        }

        if (includeThis && !method.IsStatic)
        {
            sb.Append('T');
        }

        foreach (var parameter in method.GetParameters())
        {
            string? paramToken = TypeToSignatureToken(parameter.ParameterType, log, out _);
            if (paramToken is null)
                return null;

            sb.Append(paramToken);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a signature string into individual tokens.
    /// Single-char types produce one-char tokens; S&lt;N&gt; produces a multi-char token like "S8" or "S64".
    /// The 'p' suffix is included as its own token.
    /// </summary>
    public static List<string> ParseSignatureTokens(string signature)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < signature.Length)
        {
            if (signature[i] == 'S')
            {
                int start = i;
                i++; // skip 'S'
                while (i < signature.Length && char.IsDigit(signature[i]))
                    i++;
                tokens.Add(signature.Substring(start, i - start));
            }
            else
            {
                tokens.Add(signature[i].ToString());
                i++;
            }
        }

        return tokens;
    }

    public static string TokenToNativeType(string token) => token[0] switch
    {
        'v' => "void",
        'i' => "int32_t",
        'l' => "int64_t",
        'f' => "float",
        'd' => "double",
        'S' => "int32_t",
        'T' => "int32_t",
        'p' => "PCODE",
        _ => throw new InvalidSignatureCharException(token[0])
    };

    public static string TokenToNameType(string token) => token[0] switch
    {
        'v' => "Void",
        'i' => "I32",
        'l' => "I64",
        'f' => "F32",
        'd' => "F64",
        'S' => token, // e.g. "S8", "S64" — encodes size in the name
        'T' => "This",
        'p' => "PE",
        _ => throw new InvalidSignatureCharException(token[0])
    };

    public static string TokenToArgType(string token) => token[0] switch
    {
        'i' => "ARG_I32",
        'l' => "ARG_I64",
        'f' => "ARG_F32",
        'd' => "ARG_F64",
        'S' => "ARG_IND",
        'T' => "ARG_I32",
        _ => throw new InvalidSignatureCharException(token[0])
    };

    /// <summary>
    /// Returns the number of INTERP_STACK_SLOT_SIZE slots consumed by a token.
    /// Struct tokens (S&lt;N&gt;) consume max((size + 7) / 8, 1) slots; all others consume 1.
    /// </summary>
    public static int TokenToSlotCount(string token)
    {
        if (token[0] != 'S' || token.Length < 2)
            return 1;

        int size = int.Parse(token.Substring(1));
        return Math.Max((size + 7) / 8, 1);
    }

    // Legacy single-char overloads — still used by consumers that don't encounter S<N> tokens.
    public static string CharToNativeType(char c) => TokenToNativeType(c.ToString());
    public static string CharToNameType(char c) => TokenToNameType(c.ToString());
    public static string CharToArgType(char c) => TokenToArgType(c.ToString());

    public static string TypeToNameType(Type t, LogAdapter log)
    {
        char? c = TypeToChar(t, log, out _);
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
