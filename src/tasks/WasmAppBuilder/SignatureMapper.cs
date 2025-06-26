// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WasmAppBuilder;

internal static class SignatureMapper
{
    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct, int depth = 0)
    {
        isByRefStruct = false;

        if (depth > 5) {
            log.Warning("WASM0064", $"Unbounded recursion detected through parameter type '{t.Name}'");
            return null;
        }

        char? c = null;
#if SignatureMappingWasm64
        const char ptrChar = 'L'; // Pointer types are L for wasm64
        const string refVoid = "VL"; // ByRef structs are passed as a pointer to the struct in slot 0
#else
        const char ptrChar = 'I'; // Pointer types are I for wasm32
        const string refVoid = "VI"; // ByRef structs are passed as a pointer to the struct in slot 0
#endif
        if (t.Namespace == "System") {
            c = t.Name switch
            {
                nameof(String) => ptrChar,
                nameof(Boolean) => 'I',
                nameof(Char) => 'I',
                nameof(SByte) => 'I',
                nameof(Byte) => 'I',
                nameof(Int16) => 'I',
                nameof(UInt16) => 'I',
                nameof(Int32) => 'I',
                nameof(UInt32) => 'I',
                nameof(Int64) => 'L',
                nameof(UInt64) => 'L',
                nameof(Single) => 'F',
                nameof(Double) => 'D',
                nameof(IntPtr) => ptrChar,
                nameof(UIntPtr) => ptrChar,
                "Void" => 'V',
                _ => null
            };
        }

        if (c == null)
        {            
            if (t.IsArray)
                c = ptrChar;
            else if (t.IsByRef)
                c = ptrChar;
            else if (typeof(Delegate).IsAssignableFrom(t))
                // FIXME: Should we narrow this to only certain types of delegates?
                c = ptrChar;
            else if (t.IsClass)
                c = ptrChar;
            else if (t.IsInterface)
                c = ptrChar;
            else if (t.IsEnum) {
                Type underlyingType = t.GetEnumUnderlyingType();
                c = TypeToChar(underlyingType, log, out _, ++depth);
            } else if (t.IsPointer)
                c = ptrChar;
            else if (PInvokeTableGenerator.IsFunctionPointer(t))
                c = ptrChar;
            else if (t.IsValueType)
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fields.Length == 1) {
                    Type fieldType = fields[0].FieldType;
                    return TypeToChar(fieldType, log, out isByRefStruct, ++depth);
                } else if (PInvokeTableGenerator.IsBlittable(t, log))
                    c =ptrChar;

                isByRefStruct = true;
            }
            else
                log.Warning("WASM0064", $"Unsupported parameter type '{t.Name}'");
        }

        return c;
    }

    public static string? MethodToSignature(MethodInfo method, LogAdapter log)
    {        
        string? result = TypeToChar(method.ReturnType, log, out bool resultIsByRef)?.ToString();

        if (result == null)
        {
            return null;
        }

        if (resultIsByRef) {
            // WASM abi passes a result-pointer in slot 0 instead of returning struct results
            result = refVoid;
        }

        foreach (var parameter in method.GetParameters())
        {
            char? parameterChar = TypeToChar(parameter.ParameterType, log, out _);
            if (parameterChar == null)
            {
                return null;
            }

            result += parameterChar;
        }

        return result;
    }

    public static string CharToNativeType(char c) => c switch
    {
        'V' => "void",
        'I' => "int",
        'L' => "int64_t",
        'F' => "float",
        'D' => "double",
        _ => throw new InvalidSignatureCharException(c)
    };

    public static bool IsVoidSignature(string signature) => signature[0] == 'V';
}

internal sealed class InvalidSignatureCharException : Exception
{
    public char Char { get; private set; }

    public InvalidSignatureCharException(char c) : base($"Can't handle signature '{c}'") => Char = c;
}
