// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static partial class CachedReflectionInfo
    {
        private static MethodInfo s_String_Format_String_ObjectArray;
        public  static MethodInfo   String_Format_String_ObjectArray =>
                                  s_String_Format_String_ObjectArray ??
                                 (s_String_Format_String_ObjectArray = typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object[]) }));

        private static ConstructorInfo s_InvalidCastException_Ctor_String;
        public  static ConstructorInfo   InvalidCastException_Ctor_String =>
                                       s_InvalidCastException_Ctor_String ??
                                      (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) }));

        private static MethodInfo s_CallSiteOps_SetNotMatched;
        public  static MethodInfo   CallSiteOps_SetNotMatched =>
                                  s_CallSiteOps_SetNotMatched ??
                                 (s_CallSiteOps_SetNotMatched = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.SetNotMatched)));

        private static MethodInfo s_CallSiteOps_CreateMatchmaker;
        public  static MethodInfo   CallSiteOps_CreateMatchmaker =>
                                  s_CallSiteOps_CreateMatchmaker ??
                                 (s_CallSiteOps_CreateMatchmaker = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.CreateMatchmaker)));

        private static MethodInfo s_CallSiteOps_GetMatch;
        public  static MethodInfo   CallSiteOps_GetMatch =>
                                  s_CallSiteOps_GetMatch ??
                                 (s_CallSiteOps_GetMatch = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetMatch)));

        private static MethodInfo s_CallSiteOps_ClearMatch;
        public  static MethodInfo   CallSiteOps_ClearMatch =>
                                  s_CallSiteOps_ClearMatch ??
                                 (s_CallSiteOps_ClearMatch = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.ClearMatch)));

        private static MethodInfo s_CallSiteOps_UpdateRules;
        public  static MethodInfo   CallSiteOps_UpdateRules =>
                                  s_CallSiteOps_UpdateRules ??
                                 (s_CallSiteOps_UpdateRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.UpdateRules)));

        private static MethodInfo s_CallSiteOps_GetRules;
        public  static MethodInfo   CallSiteOps_GetRules =>
                                  s_CallSiteOps_GetRules ??
                                 (s_CallSiteOps_GetRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetRules)));

        private static MethodInfo s_CallSiteOps_GetRuleCache;
        public  static MethodInfo   CallSiteOps_GetRuleCache =>
                                  s_CallSiteOps_GetRuleCache ??
                                 (s_CallSiteOps_GetRuleCache = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetRuleCache)));

        private static MethodInfo s_CallSiteOps_GetCachedRules;
        public  static MethodInfo   CallSiteOps_GetCachedRules =>
                                  s_CallSiteOps_GetCachedRules ??
                                 (s_CallSiteOps_GetCachedRules = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.GetCachedRules)));

        private static MethodInfo s_CallSiteOps_AddRule;
        public  static MethodInfo   CallSiteOps_AddRule =>
                                  s_CallSiteOps_AddRule ??
                                 (s_CallSiteOps_AddRule = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.AddRule)));

        private static MethodInfo s_CallSiteOps_MoveRule;
        public  static MethodInfo   CallSiteOps_MoveRule =>
                                  s_CallSiteOps_MoveRule ??
                                 (s_CallSiteOps_MoveRule = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.MoveRule)));

        private static MethodInfo s_CallSiteOps_Bind;
        public  static MethodInfo   CallSiteOps_Bind =>
                                  s_CallSiteOps_Bind ??
                                 (s_CallSiteOps_Bind = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.Bind)));
    }
}
