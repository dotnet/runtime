// Licensed to the .NET Foundation under one or more agreements.
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
        public static MethodInfo String_Format_String_ObjectArray =>
                                  s_String_Format_String_ObjectArray ??
                                 (s_String_Format_String_ObjectArray = typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object[]) }));

        private static ConstructorInfo s_InvalidCastException_Ctor_String;
        public static ConstructorInfo InvalidCastException_Ctor_String =>
                                       s_InvalidCastException_Ctor_String ??
                                      (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) }));

        private static MethodInfo s_CallSiteOps_SetNotMatched;
        public static MethodInfo CallSiteOps_SetNotMatched =>
                                  s_CallSiteOps_SetNotMatched ??
                                 (s_CallSiteOps_SetNotMatched = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.SetNotMatched)));

        private static MethodInfo s_CallSiteOps_CreateMatchmaker;
        public static MethodInfo CallSiteOps_CreateMatchmaker =>
                                  s_CallSiteOps_CreateMatchmaker ??
                                 (s_CallSiteOps_CreateMatchmaker = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.CreateMatchmaker)));

        private static MethodInfo s_CallSiteOps_GetMatch;
        public static MethodInfo CallSiteOps_GetMatch =>
                                  s_CallSiteOps_GetMatch ??
                                 (s_CallSiteOps_GetMatch = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetMatch)));

        private static MethodInfo s_CallSiteOps_ClearMatch;
        public static MethodInfo CallSiteOps_ClearMatch =>
                                  s_CallSiteOps_ClearMatch ??
                                 (s_CallSiteOps_ClearMatch = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.ClearMatch)));

        private static MethodInfo s_CallSiteOps_UpdateRules;
        public static MethodInfo CallSiteOps_UpdateRules =>
                                  s_CallSiteOps_UpdateRules ??
                                 (s_CallSiteOps_UpdateRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.UpdateRules)));

        private static MethodInfo s_CallSiteOps_GetRules;
        public static MethodInfo CallSiteOps_GetRules =>
                                  s_CallSiteOps_GetRules ??
                                 (s_CallSiteOps_GetRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetRules)));

        private static MethodInfo s_CallSiteOps_GetRuleCache;
        public static MethodInfo CallSiteOps_GetRuleCache =>
                                  s_CallSiteOps_GetRuleCache ??
                                 (s_CallSiteOps_GetRuleCache = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetRuleCache)));

        private static MethodInfo s_CallSiteOps_GetCachedRules;
        public static MethodInfo CallSiteOps_GetCachedRules =>
                                  s_CallSiteOps_GetCachedRules ??
                                 (s_CallSiteOps_GetCachedRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetCachedRules)));

        private static MethodInfo s_CallSiteOps_AddRule;
        public static MethodInfo CallSiteOps_AddRule =>
                                  s_CallSiteOps_AddRule ??
                                 (s_CallSiteOps_AddRule = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.AddRule)));

        private static MethodInfo s_CallSiteOps_MoveRule;
        public static MethodInfo CallSiteOps_MoveRule =>
                                  s_CallSiteOps_MoveRule ??
                                 (s_CallSiteOps_MoveRule = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.MoveRule)));

        private static MethodInfo s_CallSiteOps_Bind;
        public static MethodInfo CallSiteOps_Bind =>
                                  s_CallSiteOps_Bind ??
                                 (s_CallSiteOps_Bind = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.Bind)));

        private static MethodInfo s_DynamicObject_TryGetMember;
        public static MethodInfo DynamicObject_TryGetMember =>
                                  s_DynamicObject_TryGetMember ??
                                 (s_DynamicObject_TryGetMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryGetMember)));

        private static MethodInfo s_DynamicObject_TrySetMember;
        public static MethodInfo DynamicObject_TrySetMember =>
                                  s_DynamicObject_TrySetMember ??
                                 (s_DynamicObject_TrySetMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TrySetMember)));

        private static MethodInfo s_DynamicObject_TryDeleteMember;
        public static MethodInfo DynamicObject_TryDeleteMember =>
                                  s_DynamicObject_TryDeleteMember ??
                                 (s_DynamicObject_TryDeleteMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryDeleteMember)));

        private static MethodInfo s_DynamicObject_TryGetIndex;
        public static MethodInfo DynamicObject_TryGetIndex =>
                                  s_DynamicObject_TryGetIndex ??
                                 (s_DynamicObject_TryGetIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryGetIndex)));

        private static MethodInfo s_DynamicObject_TrySetIndex;
        public static MethodInfo DynamicObject_TrySetIndex =>
                                  s_DynamicObject_TrySetIndex ??
                                 (s_DynamicObject_TrySetIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TrySetIndex)));

        private static MethodInfo s_DynamicObject_TryDeleteIndex;
        public static MethodInfo DynamicObject_TryDeleteIndex =>
                                  s_DynamicObject_TryDeleteIndex ??
                                 (s_DynamicObject_TryDeleteIndex = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryDeleteIndex)));

        private static MethodInfo s_DynamicObject_TryConvert;
        public static MethodInfo DynamicObject_TryConvert =>
                                  s_DynamicObject_TryConvert ??
                                 (s_DynamicObject_TryConvert = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryConvert)));

        private static MethodInfo s_DynamicObject_TryInvoke;
        public static MethodInfo DynamicObject_TryInvoke =>
                                  s_DynamicObject_TryInvoke ??
                                 (s_DynamicObject_TryInvoke = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryInvoke)));

        private static MethodInfo s_DynamicObject_TryInvokeMember;
        public static MethodInfo DynamicObject_TryInvokeMember =>
                                  s_DynamicObject_TryInvokeMember ??
                                 (s_DynamicObject_TryInvokeMember = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryInvokeMember)));

        private static MethodInfo s_DynamicObject_TryBinaryOperation;
        public static MethodInfo DynamicObject_TryBinaryOperation =>
                                  s_DynamicObject_TryBinaryOperation ??
                                 (s_DynamicObject_TryBinaryOperation = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryBinaryOperation)));

        private static MethodInfo s_DynamicObject_TryUnaryOperation;
        public static MethodInfo DynamicObject_TryUnaryOperation =>
                                  s_DynamicObject_TryUnaryOperation ??
                                 (s_DynamicObject_TryUnaryOperation = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryUnaryOperation)));

        private static MethodInfo s_DynamicObject_TryCreateInstance;
        public static MethodInfo DynamicObject_TryCreateInstance =>
                                  s_DynamicObject_TryCreateInstance ??
                                 (s_DynamicObject_TryCreateInstance = typeof(DynamicObject).GetMethod(nameof(DynamicObject.TryCreateInstance)));
    }
}
