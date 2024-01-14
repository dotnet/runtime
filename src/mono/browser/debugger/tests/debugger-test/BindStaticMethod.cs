// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace DebuggerTests
{
    // this is fake implementation of legacy `bind_static_method`
    // so that we don't have to rewrite all the tests which use it via `invoke_static_method`
    public sealed partial class BindStaticMethod
    {

        [JSExport]
        [return: JSMarshalAs<JSType.Any>()]
        public static object GetMethodInfo(string monoMethodName)
        {
            return GetMethodInfoImpl(monoMethodName);
        }

        [JSExport]
        public static unsafe IntPtr GetMonoMethodPtr(string monoMethodName)
        {
            var methodInfo = GetMethodInfoImpl(monoMethodName);
            var temp = new IntPtrAndHandle { methodHandle = methodInfo.MethodHandle };
            return temp.ptr;
        }

        public static MethodInfo GetMethodInfoImpl(string monoMethodName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(monoMethodName, nameof(monoMethodName));
            // [debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers
            var partsA = monoMethodName.Split(' ');
            var assemblyName = partsA[0].Substring(1, partsA[0].Length - 2);
            var partsN = partsA[1].Split(':');
            var className = partsN[0];
            var methodName = partsN[1];

            var typeName = $"{className}, {assemblyName}";
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type not found {typeName}");
            }

            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new ArgumentException($"Method not found {className}.{methodName}");
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
                if (typeof(Task).IsAssignableFrom(p.ParameterType))
                {
                    sb.Append("Task");
                }
                else if (p.ParameterType.GenericTypeArguments.Length > 0)
                {
                    throw new NotImplementedException($"Parameter {p.Name} type {p.ParameterType.FullName}");
                }
                else
                {
                    sb.Append(p.ParameterType.Name);
                }
            }

            sb.Append("_");
            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                sb.Append("Task");
            }
            else if (method.ReturnType.GenericTypeArguments.Length > 0)
            {
                throw new NotImplementedException($"Method return type {method.ReturnType.FullName}");
            }
            else
            {
                sb.Append(method.ReturnType.Name);
            }

            return sb.ToString();
        }

        [JSExport]
        public static void Invoke_Void([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, null);
        }

        [JSExport]
        public static Task Invoke_Task([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            return (Task)method.Invoke(null, null);
        }

        [JSExport]
        public static string Invoke_String([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            return (string)method.Invoke(null, null);
        }

        [JSExport]
        public static void Invoke_Boolean_Void([JSMarshalAs<JSType.Any>()] object methodInfo, bool p1)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1 });
        }

        [JSExport]
        public static Task Invoke_Boolean_Task([JSMarshalAs<JSType.Any>()] object methodInfo, bool p1)
        {
            var method = (MethodInfo)methodInfo;
            return (Task)method.Invoke(null, new object[] { p1 });
        }

        [JSExport]
        public static void Invoke_Int32_Void([JSMarshalAs<JSType.Any>()] object methodInfo, int p1)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1 });
        }

        [JSExport]
        public static void Invoke_Int32_Int32_Void([JSMarshalAs<JSType.Any>()] object methodInfo, int p1, int p2)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2 });
        }

        [JSExport]
        public static void Invoke_Int32_Int32_Int32_Void([JSMarshalAs<JSType.Any>()] object methodInfo, int p1, int p2, int p3)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2, p3 });
        }

        [JSExport]
        public static int Invoke_Int32([JSMarshalAs<JSType.Any>()] object methodInfo)
        {
            var method = (MethodInfo)methodInfo;
            return (int)method.Invoke(null, null);
        }

        [JSExport]
        public static int Invoke_Int32_Int32([JSMarshalAs<JSType.Any>()] object methodInfo, int p1)
        {
            var method = (MethodInfo)methodInfo;
            return (int)method.Invoke(null, new object[] { p1 });
        }

        [JSExport]
        public static int Invoke_Int32_Int32_Int32([JSMarshalAs<JSType.Any>()] object methodInfo, int p1, int p2)
        {
            var method = (MethodInfo)methodInfo;
            return (int)method.Invoke(null, new object[] { p1, p2 });
        }

        [JSExport]
        public static void Invoke_String_Void([JSMarshalAs<JSType.Any>()] object methodInfo, string p1)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1 });
        }

        [JSExport]
        public static void Invoke_String_String_Void([JSMarshalAs<JSType.Any>()] object methodInfo, string p1, string p2)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2 });
        }

        [JSExport]
        public static void Invoke_String_String_String_String_Void([JSMarshalAs<JSType.Any>()] object methodInfo, string p1, string p2, string p3, string p4)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2, p3, p4 });
        }

        [JSExport]
        public static string Invoke_String_String_String([JSMarshalAs<JSType.Any>()] object methodInfo, string p1, string p2)
        {
            var method = (MethodInfo)methodInfo;
            return (string)method.Invoke(null, new object[] { p1, p2 });
        }

        [JSExport]
        public static void Invoke_String_String_String_String_String_String_String_String_Void([JSMarshalAs<JSType.Any>()] object methodInfo, string p1, string p2, string p3, string p4, string p5, string p6, string p7, string p8)
        {
            var method = (MethodInfo)methodInfo;
            method.Invoke(null, new object[] { p1, p2, p3, p4, p5, p6, p7, p8 });
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle methodHandle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }
    }
}
