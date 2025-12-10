// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            c = TypeToChar(underlyingType, log, out _, ++depth);
        }
        else if (t.IsPointer)
            c = 'i';
        else if (t.IsFunctionPointer)
            c = 'i';
        else if (t.IsValueType)
        {
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields.Length == 1)
            {
                Type fieldType = fields[0].FieldType;
                return TypeToChar(fieldType, log, out isByRefStruct, ++depth);
            }
            else if (PInvokeTableGenerator.IsBlittable(t, log))
                c = 'n';

            isByRefStruct = true;
        }
        else
            log.Warning("WASM0065", $"Unsupported parameter type '{t.Name}'");

        return c;
    }

    public static string? MethodToSignature(MethodInfo method, LogAdapter log)
    {
        string? result = TypeToChar(method.ReturnType, log, out bool resultIsByRef)?.ToString();
        if (result == null)
        {
            return null;
        }

        if (resultIsByRef)
        {
            result = "n";
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
        'v' => "void",
        'i' => "int32_t",
        'l' => "int64_t",
        'f' => "float",
        'd' => "double",
        'n' => "int32_t",
        _ => throw new InvalidSignatureCharException(c)
    };

    public static string CharToNameType(char c) => c switch
    {
        'v' => "Void",
        'i' => "I32",
        'l' => "I64",
        'f' => "F32",
        'd' => "F64",
        'n' => "IND",
        _ => throw new InvalidSignatureCharException(c)
    };

    public static string CharToArgType(char c) => c switch
    {
        'i' => "ARG_I32",
        'l' => "ARG_I64",
        'f' => "ARG_F32",
        'd' => "ARG_F64",
        'n' => "ARG_IND",
        _ => throw new InvalidSignatureCharException(c)
    };

    public static string TypeToNameType(Type t, LogAdapter log)
    {
        char? c = TypeToChar(t, log, out _);
        if (c == null)
        {
            throw new InvalidSignatureCharException('?');
        }

        return CharToNameType(c.Value);
    }

    public static bool IsVoidSignature(string signature) => signature[0] == 'v';
}

internal sealed class InvalidSignatureCharException : Exception
{
    public char Char { get; private set; }

    public InvalidSignatureCharException(char c) : base($"Can't handle signature '{c}'") => Char = c;
}
