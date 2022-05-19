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
    public static string BuildCookie(MethodInfo method)
    {
        static string TypeToCookie(Type t)
        {
            string? c = t.Name switch
            {
                nameof(String) => "I",
                nameof(Boolean) => "I",
                nameof(Char) => "I",
                nameof(Byte) => "I",
                nameof(Int16) => "I",
                nameof(UInt16) => "I",
                nameof(Int32) => "I",
                nameof(UInt32) => "I",
                nameof(IntPtr) => "I",
                nameof(UIntPtr) => "I",
                nameof(Int64) => "L",
                nameof(UInt64) => "L",
                nameof(Single) => "F",
                nameof(Double) => "D",
                "Void" => "V",
                _ => null
            };

            if (c == null)
            {
                if (t.IsArray)
                    c = "I";
                else if (t.IsClass)
                    c = "I";
                else if (t.IsInterface)
                    c = "I";
                else if (t.IsEnum)
                    c = TypeToCookie(t.GetEnumUnderlyingType());
                else if (t.IsValueType)
                    c = "I";
            }

            if (c == null)
            {
                //throw new NotSupportedException($"Type '{t.Name}' is not supported.")
                c = $"<{t.Name}>";
            }

            return c;
        }

        string result = TypeToCookie(method.ReturnType).ToString();
        foreach (var parameter in method.GetParameters())
        {
            result += TypeToCookie(parameter.ParameterType);
        }

        return result;
    }
}
