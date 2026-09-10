// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using ILCompiler.DependencyAnalysis.Wasm;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.PortableCallHelpers
{
    /// <summary>
    /// Thrown when a signature token has no representation in the generated C.
    /// </summary>
    internal sealed class InvalidSignatureCharException(char c)
        : Exception($"Can't handle signature '{c}'")
    {
        public char Char { get; } = c;
    }

    /// <summary>
    /// Maps between the wasm signature strings produced by <see cref="WasmLowering"/> and the C
    /// declarations the generated interop files are built from.
    /// </summary>
    /// <remarks>
    /// The signature string format is documented in docs/design/coreclr/botr/readytorun-format.md
    /// (section "Wasm Signature String Encoding"). <see cref="WasmLowering"/> is the single
    /// implementation of the encoding; everything here either produces a string through it or
    /// consumes one it produced.
    /// </remarks>
    internal static class InteropSignature
    {
        /// <summary>
        /// Returns the wasm signature string for a method.
        /// </summary>
        /// <param name="method">The method to encode.</param>
        /// <param name="flags">
        /// The default lowers as a managed method, taking the leading 'T' for an instance method
        /// and the trailing 'p' for the portable entry point argument;
        /// <see cref="WasmLowering.LoweringFlags.IsUnmanagedCallersOnly"/> lowers as a native function.
        /// </param>
        public static string GetMethodSignature(MethodDesc method, WasmLowering.LoweringFlags flags = WasmLowering.LoweringFlags.None)
            => WasmLowering.GetSignature(method.Signature, flags).SignatureString;

        /// <summary>
        /// Gets the signature encoding for a type in parameter position: a primitive character
        /// (<c>i</c>, <c>l</c>, <c>f</c>, <c>d</c>, <c>V</c>), a multi-slot token, or
        /// <c>S&lt;size&gt;</c>/<c>A&lt;size&gt;</c> for a struct that is passed by reference.
        /// </summary>
        public static string GetAbiToken(TypeDesc type)
        {
            if (type.IsVoid)
                return "v";

            TypeDesc loweredType = WasmLowering.LowerToAbiType(type);
            if (loweredType is null)
            {
                // WasmLowering.GetSignature splits this case in two, and both have to be mirrored
                // here or a type gets one token in a method signature and a different one at the
                // interop boundary. Multi-segment types come first: they travel by value across
                // several wasm parameters, so calling them by-reference structs would both hide
                // them from the multi-slot rejection and mis-declare them in C.
                if (WasmLowering.TryGetMultiSegmentLayout(type, out WasmValueType slotType, out int slotCount))
                    return string.Create(CultureInfo.InvariantCulture, $"{WasmLowering.WasmValueTypeToSigChar(slotType)}{slotCount}");

                // Passed by reference; the size is what the callee needs to know. 'A' marks a struct
                // whose alignment exceeds a stack slot, matching what WasmLowering.GetSignature emits
                // so a type gets the same token here as it does inside a method signature.
                Debug.Assert(type is DefType, "LowerToAbiType only returns null for aggregates");
                char kind = CompilerTypeSystemContext.GetClassAlignmentRequirementStatic((DefType)type) > 8 ? 'A' : 'S';
                return string.Create(CultureInfo.InvariantCulture, $"{kind}{type.GetElementSize().AsInt}");
            }

            return WasmLowering.WasmValueTypeToSigChar(WasmLowering.LowerType(loweredType)).ToString();
        }

        /// <summary>
        /// Parses a signature string into individual tokens. Single-char types produce one-char
        /// tokens; struct encodings produce multi-char tokens like "S8" or "A32", and a multi-slot
        /// parameter produces a two-char token like "l2" or "V4". The 'a' and 'p' suffixes are
        /// included as their own tokens.
        /// </summary>
        public static List<string> ParseSignatureTokens(string signature)
        {
            List<string> tokens = [];
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
        public static bool IsMultiSlotToken(string token)
            => token.Length == 2 && token[0] is 'l' or 'V' && char.IsDigit(token[1]);

        private static void RejectMultiSlotToken(string token)
        {
            if (IsMultiSlotToken(token))
                throw new LogAsErrorException($"Multi-slot signature token '{token}' is not supported in interop thunks");
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
                'S' or 'A' or 'T' => "int32_t",
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
                'i' or 'T' => "ARG_I32",
                'l' => "ARG_I64",
                'f' => "ARG_F32",
                'd' => "ARG_F64",
                'S' or 'A' => "ARG_IND",
                _ => throw new InvalidSignatureCharException(token[0])
            };
        }

        /// <summary>
        /// Returns the number of INTERP_STACK_SLOT_SIZE slots consumed by a token. Struct tokens
        /// consume max((size + 7) / 8, 1) slots; all others consume 1.
        /// </summary>
        public static int TokenToSlotCount(string token)
        {
            if (token[0] is not ('S' or 'A') || token.Length < 2)
                return 1;

            return Math.Max((GetStructSize(token) + 7) / 8, 1);
        }

        public static int GetStructSize(string token)
            => int.Parse(token.AsSpan(1), CultureInfo.InvariantCulture);
    }
}
