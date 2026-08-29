// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// Computes Wasm signature strings from reflection metadata.
// The signature string format is documented in docs/design/coreclr/botr/readytorun-format.md
// (section "Wasm Signature String Encoding").
internal static partial class SignatureMapper
{
    // Hardcoded struct layouts for types that crossgen2 encodes as struct tokens.
    // The fully general case is handled by crossgen2's type system; these
    // cover the small set of multi-field structs that appear in InternalCall
    // and PInvoke signatures.
    private static readonly Dictionary<string, (int Size, int Alignment)> s_knownStructLayouts = new()
    {
        ["System.Runtime.CompilerServices.QCallModule"] = (8, 8),
        ["System.Runtime.CompilerServices.QCallAssembly"] = (8, 8),
        ["System.Runtime.CompilerServices.QCallTypeHandle"] = (8, 8),
        ["System.GC+GCHeapHardLimitInfo"] = (64, 8),
        // Used by WBT tests
        ["WasmAppBuilderTestsPairStruct"] = (8, 8),
        ["WasmAppBuilderTests.S"] = (8, 8),
        ["WasmAppBuilderTests.Test+S"] = (8, 8),
    };

    private static char? TypeToChar(
        Type t,
        LogAdapter log,
        out bool isByRefStruct,
        out int structSize,
        out int structAlignment,
        int depth = 0)
    {
        isByRefStruct = false;
        structSize = 0;
        structAlignment = 0;

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
            c = TypeToChar(underlyingType, log, out _, out structSize, out structAlignment, ++depth);
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
                return TypeToChar(fieldType, log, out isByRefStruct, out structSize, out structAlignment, ++depth);
            }
            else
            {
                string fullName = t.FullName ?? t.Name;
                if (s_knownStructLayouts.TryGetValue(fullName, out (int Size, int Alignment) layout))
                {
                    structSize = layout.Size;
                    structAlignment = layout.Alignment;
                }
                else
                {
                    log.Error("WASM0067",
                        $"SignatureMapper: unknown multi-field struct '{fullName}' (fields: {fields.Length}) — add its layout to s_knownStructLayouts in SignatureMapper.cs");
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

    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct, out int structSize, int depth = 0)
        => TypeToChar(t, log, out isByRefStruct, out structSize, out _, depth);

    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct, int depth = 0)
        => TypeToChar(t, log, out isByRefStruct, out _, depth);

    /// <summary>
    /// Builds the multi-char token for a type in the signature string.
    /// For most types this is a single character; for multi-field structs it is a struct token.
    /// </summary>
    private static string? TypeToSignatureToken(Type t, LogAdapter log, out bool isByRefStruct, bool isReturn = false)
    {
        // Types the wasm ABI splits across several by-value slots are rejected in interop rather
        // than encoded. Exposing them here means teaching the thunk generator that one signature
        // token can map to several native parameters, which is a larger design question; today no
        // InternalCall or PInvoke signature uses one.
        if (IsMultiSlotType(t))
        {
            log.Error("WASM0068",
                $"SignatureMapper: '{t.FullName ?? t.Name}' is passed across multiple wasm slots, which interop signatures do not support");
            isByRefStruct = false;
            return null;
        }

        char? c = TypeToChar(t, log, out isByRefStruct, out int structSize, out int structAlignment);
        if (c is null)
            return null;

        if (c == 'S' && structSize > 0)
            return $"{(!isReturn && structAlignment > 8 ? 'A' : 'S')}{structSize}";

        return c.Value.ToString();
    }

    /// <summary>
    /// True for a type the wasm ABI passes across several by-value slots: Int128/UInt128 and
    /// Decimal128 as i64 slots, a 256- or 512-bit vector as v128 slots. A single-field wrapper is
    /// passed as the type it wraps, so unwrap first, exactly as crossgen2 and the runtime do.
    /// </summary>
    private static bool IsMultiSlotType(Type t)
    {
        for (int depth = 0; depth <= 5; depth++)
        {
            switch (t.Namespace, t.Name)
            {
                case ("System", "Int128"):
                case ("System", "UInt128"):
                case ("System.Numerics", "Decimal128"):
                    return true;
                case ("System.Runtime.Intrinsics", "Vector256`1") when HasNumericElementType(t):
                case ("System.Runtime.Intrinsics", "Vector512`1") when HasNumericElementType(t):
                    return true;
            }

            if (!t.IsValueType || t.IsPrimitive || t.IsEnum)
            {
                return false;
            }

            // Only unwrap a wrapper its field fills exactly. Sizes cannot be measured here: these
            // types come from a MetadataLoadContext, where Marshal.SizeOf always throws. A single
            // field fills its struct unless the struct sets a size of its own or places the field
            // at an offset, so treat either as padded and keep the struct ABI.
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields.Length != 1 ||
                t.StructLayoutAttribute is not { Size: 0, Value: not LayoutKind.Explicit })
            {
                return false;
            }

            t = fields[0].FieldType;
        }

        return false;
    }

    /// <summary>
    /// The other two encoders only treat a vector as a sequence of wasm values when its element type
    /// is a primitive numeric, so Vector256&lt;bool&gt; and Vector256&lt;char&gt; use the struct ABI.
    /// </summary>
    private static bool HasNumericElementType(Type t)
    {
        Type[] arguments = t.GetGenericArguments();
        if (arguments.Length != 1)
        {
            return false;
        }

        // An enum reports its underlying numeric type code, but crossgen2 and the runtime both
        // reject enums as a vector element type.
        if (arguments[0].IsEnum)
        {
            return false;
        }

        return Type.GetTypeCode(arguments[0]) is >= TypeCode.SByte and <= TypeCode.Double
            || arguments[0] == typeof(IntPtr) || arguments[0] == typeof(UIntPtr);
    }

    public static string? MethodToSignature(MethodInfo method, LogAdapter log, bool includeThis = false)
    {
        string? returnToken = TypeToSignatureToken(method.ReturnType, log, out bool resultIsByRef, isReturn: true);
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

    public static string TypeToNameType(Type t, LogAdapter log)
    {
        char? c = TypeToChar(t, log, out _);
        if (c is null)
            throw new InvalidSignatureCharException('?');

        return CharToNameType(c.Value);
    }
}
