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
    internal static char? TypeToChar(Type t, LogAdapter log, out bool isByRefStruct)
    {
        isByRefStruct = false;

        char? c = null;
        if (t.Namespace == "System") {
            c = t.Name switch
            {
                nameof(String) => 'I',
                nameof(Boolean) => 'I',
                nameof(Char) => 'I',
                nameof(Byte) => 'I',
                nameof(Int16) => 'I',
                nameof(UInt16) => 'I',
                nameof(Int32) => 'I',
                nameof(UInt32) => 'I',
                nameof(Int64) => 'L',
                nameof(UInt64) => 'L',
                nameof(Single) => 'F',
                nameof(Double) => 'D',
                // FIXME: These will need to be L for wasm64
                nameof(IntPtr) => 'I',
                nameof(UIntPtr) => 'I',
                "Void" => 'V',
                _ => null
            };
        }

        if (c == null)
        {
            // FIXME: Most of these need to be L for wasm64
            if (t.IsArray)
                c = 'I';
            else if (t.IsByRef)
                c = 'I';
            else if (typeof(Delegate).IsAssignableFrom(t))
                // FIXME: Should we narrow this to only certain types of delegates?
                c = 'I';
            else if (t.IsClass)
                c = 'I';
            else if (t.IsInterface)
                c = 'I';
            else if (t.IsEnum)
                c = TypeToChar(t.GetEnumUnderlyingType(), log, out _);
            else if (t.IsPointer)
                c = 'I';
            else if (PInvokeTableGenerator.IsFunctionPointer(t))
                c = 'I';
            else if (t.IsValueType)
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fields.Length == 1)
                    return TypeToChar(fields[0].FieldType, log, out isByRefStruct);
                else if (PInvokeTableGenerator.IsBlittable(t, log))
                    c = 'I';

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
            result = "VI";
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
