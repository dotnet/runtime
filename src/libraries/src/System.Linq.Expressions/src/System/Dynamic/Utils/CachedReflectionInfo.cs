﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static partial class CachedReflectionInfo
    {
        private static MethodInfo s_String_Format_String_ObjectArray;
        public  static MethodInfo   String_Format_String_ObjectArray =>
                                  s_String_Format_String_ObjectArray ??
                                 (s_String_Format_String_ObjectArray = typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) }));

        private static ConstructorInfo s_InvalidCastException_Ctor_String;
        public  static ConstructorInfo   InvalidCastException_Ctor_String =>
                                       s_InvalidCastException_Ctor_String ??
                                      (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) }));

        private static MethodInfo s_CallSiteOps_SetNotMatched;
        public  static MethodInfo   CallSiteOps_SetNotMatched =>
                                  s_CallSiteOps_SetNotMatched ??
                                 (s_CallSiteOps_SetNotMatched = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.SetNotMatched)));

        private static MethodInfo s_DynamicObject_TryGetMember;
        public  static MethodInfo   DynamicObject_TryGetMember =>
                                  s_DynamicObject_TryGetMember ??
                                 (s_DynamicObject_TryGetMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryGetMember)));

        private static MethodInfo s_DynamicObject_TrySetMember;
        public  static MethodInfo   DynamicObject_TrySetMember =>
                                  s_DynamicObject_TrySetMember ??
                                 (s_DynamicObject_TrySetMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TrySetMember)));

        private static MethodInfo s_DynamicObject_TryDeleteMember;
        public  static MethodInfo   DynamicObject_TryDeleteMember =>
                                  s_DynamicObject_TryDeleteMember ??
                                 (s_DynamicObject_TryDeleteMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryDeleteMember)));

        private static MethodInfo s_DynamicObject_TryGetIndex;
        public  static MethodInfo   DynamicObject_TryGetIndex =>
                                  s_DynamicObject_TryGetIndex ??
                                 (s_DynamicObject_TryGetIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryGetIndex)));

        private static MethodInfo s_DynamicObject_TrySetIndex;
        public  static MethodInfo   DynamicObject_TrySetIndex =>
                                  s_DynamicObject_TrySetIndex ??
                                 (s_DynamicObject_TrySetIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TrySetIndex)));

        private static MethodInfo s_DynamicObject_TryDeleteIndex;
        public  static MethodInfo   DynamicObject_TryDeleteIndex =>
                                  s_DynamicObject_TryDeleteIndex ??
                                 (s_DynamicObject_TryDeleteIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryDeleteIndex)));

        private static MethodInfo s_DynamicObject_TryConvert;
        public  static MethodInfo   DynamicObject_TryConvert =>
                                  s_DynamicObject_TryConvert ??
                                 (s_DynamicObject_TryConvert = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryConvert)));

        private static MethodInfo s_DynamicObject_TryInvoke;
        public  static MethodInfo   DynamicObject_TryInvoke =>
                                  s_DynamicObject_TryInvoke ??
                                 (s_DynamicObject_TryInvoke = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryInvoke)));

        private static MethodInfo s_DynamicObject_TryInvokeMember;
        public  static MethodInfo   DynamicObject_TryInvokeMember =>
                                  s_DynamicObject_TryInvokeMember ??
                                 (s_DynamicObject_TryInvokeMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryInvokeMember)));

        private static MethodInfo s_DynamicObject_TryBinaryOperation;
        public  static MethodInfo   DynamicObject_TryBinaryOperation =>
                                  s_DynamicObject_TryBinaryOperation ??
                                 (s_DynamicObject_TryBinaryOperation = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryBinaryOperation)));

        private static MethodInfo s_DynamicObject_TryUnaryOperation;
        public  static MethodInfo   DynamicObject_TryUnaryOperation =>
                                  s_DynamicObject_TryUnaryOperation ??
                                 (s_DynamicObject_TryUnaryOperation = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryUnaryOperation)));

        private static MethodInfo s_DynamicObject_TryCreateInstance;
        public  static MethodInfo   DynamicObject_TryCreateInstance =>
                                  s_DynamicObject_TryCreateInstance ??
                                 (s_DynamicObject_TryCreateInstance = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryCreateInstance)));
    }
}
