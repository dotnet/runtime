// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static partial class CachedReflectionInfo
    {
        private static Type[] s_ArrayOfType_Bool;
        public  static Type[]   ArrayOfType_Bool =>
                              s_ArrayOfType_Bool ??
                             (s_ArrayOfType_Bool = new[] { typeof(bool) });

        private static ConstructorInfo s_Decimal_Ctor_Int32;
        public  static ConstructorInfo   Decimal_Ctor_Int32 =>
                                       s_Decimal_Ctor_Int32 ??
                                      (s_Decimal_Ctor_Int32 = typeof(decimal).GetConstructor(new[] { typeof(int) }));

        private static ConstructorInfo s_Decimal_Ctor_Int64;
        public  static ConstructorInfo   Decimal_Ctor_Int64 =>
                                       s_Decimal_Ctor_Int64 ??
                                      (s_Decimal_Ctor_Int64 = typeof(decimal).GetConstructor(new[] { typeof(long) }));

        private static ConstructorInfo s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte;
        public  static ConstructorInfo   Decimal_Ctor_Int32_Int32_Int32_Bool_Byte =>
                                       s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte ??
                                      (s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte = typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }));

        private static ConstructorInfo s_Closure_ObjectArray_ObjectArray;
        public  static ConstructorInfo   Closure_ObjectArray_ObjectArray =>
                                       s_Closure_ObjectArray_ObjectArray ??
                                      (s_Closure_ObjectArray_ObjectArray = typeof(Closure).GetConstructor(new[] { typeof(object[]), typeof(object[]) }));

        private static FieldInfo s_Closure_Constants;
        public  static FieldInfo   Closure_Constants =>
                                 s_Closure_Constants ??
                                (s_Closure_Constants = typeof(Closure).GetField(nameof(Closure.Constants)));

        private static FieldInfo s_Closure_Locals;
        public  static FieldInfo   Closure_Locals =>
                                 s_Closure_Locals ??
                                (s_Closure_Locals = typeof(Closure).GetField(nameof(Closure.Locals)));

        private static MethodInfo s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle;
        public  static MethodInfo   MethodBase_GetMethodFromHandle_RuntimeMethodHandle =>
                                  s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle ??
                                 (s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle) }));

        private static MethodInfo s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle;
        public  static MethodInfo   MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle =>
                                  s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle ??
                                 (s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));

        private static MethodInfo s_MethodInfo_CreateDelegate_Type_Object;
        public  static MethodInfo   MethodInfo_CreateDelegate_Type_Object =>
                                  s_MethodInfo_CreateDelegate_Type_Object ??
                                 (s_MethodInfo_CreateDelegate_Type_Object = typeof(MethodInfo).GetMethod(nameof(MethodInfo.CreateDelegate), new[] { typeof(Type), typeof(object) }));

        private static MethodInfo s_String_op_Equality_String_String;
        public  static MethodInfo   String_op_Equality_String_String =>
                                  s_String_op_Equality_String_String ??
                                 (s_String_op_Equality_String_String = typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) }));

        private static MethodInfo s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array;
        public  static MethodInfo   RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array =>
                                  s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array ??
                                 (s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array = typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.CreateRuntimeVariables), new[] { typeof(object[]), typeof(long[]) }));

        private static MethodInfo s_RuntimeOps_CreateRuntimeVariables;
        public  static MethodInfo   RuntimeOps_CreateRuntimeVariables =>
                                  s_RuntimeOps_CreateRuntimeVariables ??
                                 (s_RuntimeOps_CreateRuntimeVariables = typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.CreateRuntimeVariables), Type.EmptyTypes));

        private static MethodInfo s_RuntimeOps_MergeRuntimeVariables;
        public  static MethodInfo   RuntimeOps_MergeRuntimeVariables =>
                                  s_RuntimeOps_MergeRuntimeVariables ??
                                 (s_RuntimeOps_MergeRuntimeVariables = typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.MergeRuntimeVariables)));

        private static MethodInfo s_RuntimeOps_Quote;
        public  static MethodInfo   RuntimeOps_Quote =>
                                  s_RuntimeOps_Quote ??
                                 (s_RuntimeOps_Quote = typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.Quote)));

        private static MethodInfo s_DictionaryOfStringInt32_Add_String_Int32;
        public  static MethodInfo   DictionaryOfStringInt32_Add_String_Int32 =>
                                  s_DictionaryOfStringInt32_Add_String_Int32 ??
                                 (s_DictionaryOfStringInt32_Add_String_Int32 = typeof(Dictionary<string, int>).GetMethod(nameof(Dictionary<string, int>.Add), new[] { typeof(string), typeof(int) }));

        private static ConstructorInfo s_DictionaryOfStringInt32_Ctor_Int32;
        public  static ConstructorInfo   DictionaryOfStringInt32_Ctor_Int32 =>
                                       s_DictionaryOfStringInt32_Ctor_Int32 ??
                                      (s_DictionaryOfStringInt32_Ctor_Int32 = typeof(Dictionary<string, int>).GetConstructor(new[] { typeof(int) }));

        private static MethodInfo s_Type_GetTypeFromHandle;
        public  static MethodInfo   Type_GetTypeFromHandle =>
                                  s_Type_GetTypeFromHandle ??
                                 (s_Type_GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

        private static MethodInfo s_Object_GetType;
        public  static MethodInfo   Object_GetType =>
                                  s_Object_GetType ??
                                 (s_Object_GetType = typeof(object).GetMethod(nameof(object.GetType)));

        private static MethodInfo s_Decimal_op_Implicit_Byte;
        public  static MethodInfo   Decimal_op_Implicit_Byte =>
                                  s_Decimal_op_Implicit_Byte ??
                                 (s_Decimal_op_Implicit_Byte = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(byte) }));

        private static MethodInfo s_Decimal_op_Implicit_SByte;
        public  static MethodInfo   Decimal_op_Implicit_SByte =>
                                  s_Decimal_op_Implicit_SByte ??
                                 (s_Decimal_op_Implicit_SByte = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(sbyte) }));

        private static MethodInfo s_Decimal_op_Implicit_Int16;
        public  static MethodInfo   Decimal_op_Implicit_Int16 =>
                                  s_Decimal_op_Implicit_Int16 ??
                                 (s_Decimal_op_Implicit_Int16 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(short) }));

        private static MethodInfo s_Decimal_op_Implicit_UInt16;
        public  static MethodInfo   Decimal_op_Implicit_UInt16 =>
                                  s_Decimal_op_Implicit_UInt16 ??
                                 (s_Decimal_op_Implicit_UInt16 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(ushort) }));

        private static MethodInfo s_Decimal_op_Implicit_Int32;
        public  static MethodInfo   Decimal_op_Implicit_Int32 =>
                                  s_Decimal_op_Implicit_Int32 ??
                                 (s_Decimal_op_Implicit_Int32 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(int) }));

        private static MethodInfo s_Decimal_op_Implicit_UInt32;
        public  static MethodInfo   Decimal_op_Implicit_UInt32 =>
                                  s_Decimal_op_Implicit_UInt32 ??
                                 (s_Decimal_op_Implicit_UInt32 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(uint) }));

        private static MethodInfo s_Decimal_op_Implicit_Int64;
        public  static MethodInfo   Decimal_op_Implicit_Int64 =>
                                  s_Decimal_op_Implicit_Int64 ??
                                 (s_Decimal_op_Implicit_Int64 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(long) }));

        private static MethodInfo s_Decimal_op_Implicit_UInt64;
        public  static MethodInfo   Decimal_op_Implicit_UInt64 =>
                                  s_Decimal_op_Implicit_UInt64 ??
                                 (s_Decimal_op_Implicit_UInt64 = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(ulong) }));

        private static MethodInfo s_Decimal_op_Implicit_Char;
        public  static MethodInfo   Decimal_op_Implicit_Char =>
                                  s_Decimal_op_Implicit_Char ??
                                 (s_Decimal_op_Implicit_Char = typeof(decimal).GetMethod("op_Implicit", new[] { typeof(char) }));

        private static MethodInfo s_Math_Pow_Double_Double;
        public  static MethodInfo   Math_Pow_Double_Double =>
                                  s_Math_Pow_Double_Double ??
                                 (s_Math_Pow_Double_Double = typeof(Math).GetMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) }));
    }
}
