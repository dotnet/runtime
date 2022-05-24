// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

internal static class CookieHelper
{
    private static char? TypeToChar(Type t)
    {
        char? c = t.Name switch
        {
            nameof(String) => 'I',
            nameof(Boolean) => 'I',
            nameof(Char) => 'I',
            nameof(Byte) => 'I',
            nameof(Int16) => 'I',
            nameof(UInt16) => 'I',
            nameof(Int32) => 'I',
            nameof(UInt32) => 'I',
            nameof(IntPtr) => 'I',
            nameof(UIntPtr) => 'I',
            nameof(Int64) => 'L',
            nameof(UInt64) => 'L',
            nameof(Single) => 'F',
            nameof(Double) => 'D',
            "Void" => 'V',
            _ => null
        };

        if (c == null)
        {
            if (t.IsArray)
                c = 'I';
            else if (t.IsClass)
                c = 'I';
            else if (t.IsInterface)
                c = 'I';
            else if (t.IsEnum)
                c = TypeToChar(t.GetEnumUnderlyingType());
            else if (t.IsValueType)
                c = 'I';
        }

        return c;
    }

    public static string? BuildCookie(MethodInfo method)
    {
        string? result = TypeToChar(method.ReturnType)?.ToString();
        if (result == null)
        {
            return null;
        }

        foreach (var parameter in method.GetParameters())
        {
            char? parameterChar = TypeToChar(parameter.ParameterType);
            if (parameterChar == null)
            {
                return null;
            }

            result += parameterChar;
        }

        return result;
    }
}
