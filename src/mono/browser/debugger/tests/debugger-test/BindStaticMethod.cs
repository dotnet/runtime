// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System;
using System.Reflection;
using System.Text;

namespace DebuggerTests
{
    // this is fake implementation of legacy `bind_static_method`
    // so that we don't have to rewrite all the tests which use it via `invoke_static_method`
    public sealed partial class BindStaticMethod
    {

        [JSExport]
        [return: JSMarshalAs<JSType.Any>()]
        public static object Find(string monoMethodName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(monoMethodName, nameof(monoMethodName));
            // [debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers
            var partsA = monoMethodName.Split(' ');
            var assemblyName = partsA[0].Substring(1, partsA[0].Length - 2);
            var partsN = partsA[1].Split(':');
            var clazzName = partsN[0];
            var methodName = partsN[1];

            var typeName = $"{clazzName}, {assemblyName}";
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type not found {typeName}");
            }

            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw new ArgumentException($"Method not found {typeName}.{methodName}");
            }

            return method;
        }

        [JSExport]
        public static string GetSignature([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            var sb = new StringBuilder("Invoke");
            foreach (var p in method.GetParameters())
            {
                sb.Append("_");
                sb.Append(p.ParameterType.Name);
            }
            sb.Append("_");
            sb.Append(method.ReturnType.Name);

            return sb.ToString();
        }

        [JSExport]
        public static void Invoke_Void([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, null);
        }

        [JSExport]
        public static void Invoke_String_String_Void([JSMarshalAs<JSType.Any>()] object methodInfo, string p1, string p2)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2 });
        }
    }
}
