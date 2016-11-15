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
                                 (s_String_Format_String_ObjectArray = typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) }));

        private static ConstructorInfo s_InvalidCastException_Ctor_String;
        public  static ConstructorInfo   InvalidCastException_Ctor_String =>
                                       s_InvalidCastException_Ctor_String ??
                                      (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) }));

        private static MethodInfo s_CallSiteOps_SetNotMatched;
        public  static MethodInfo   CallSiteOps_SetNotMatched =>
                                  s_CallSiteOps_SetNotMatched ??
                                 (s_CallSiteOps_SetNotMatched = typeof(CallSiteOps).GetMethod(nameof(CallSiteOps.SetNotMatched)));
    }
}
