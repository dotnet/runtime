// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static partial class CachedReflectionInfo
    {
        public static ConstructorInfo Nullable_Boolean_Ctor => field ??= typeof(bool?).GetConstructor(new[] { typeof(bool) })!;
        public static ConstructorInfo Decimal_Ctor_Int32 => field ??= typeof(decimal).GetConstructor(new[] { typeof(int) })!;
        public static ConstructorInfo Decimal_Ctor_UInt32 => field ??= typeof(decimal).GetConstructor(new[] { typeof(uint) })!;
        public static ConstructorInfo Decimal_Ctor_Int64 => field ??= typeof(decimal).GetConstructor(new[] { typeof(long) })!;
        public static ConstructorInfo Decimal_Ctor_UInt64 => field ??= typeof(decimal).GetConstructor(new[] { typeof(ulong) })!;
        public static ConstructorInfo Decimal_Ctor_Int32_Int32_Int32_Bool_Byte => field ??= typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!;
        public static FieldInfo Decimal_One => field ??= typeof(decimal).GetField(nameof(decimal.One))!;
        public static FieldInfo Decimal_MinusOne => field ??= typeof(decimal).GetField(nameof(decimal.MinusOne))!;
        public static FieldInfo Decimal_MinValue => field ??= typeof(decimal).GetField(nameof(decimal.MinValue))!;
        public static FieldInfo Decimal_MaxValue => field ??= typeof(decimal).GetField(nameof(decimal.MaxValue))!;
        public static FieldInfo Decimal_Zero => field ??= typeof(decimal).GetField(nameof(decimal.Zero))!;
        public static FieldInfo DateTime_MinValue => field ??= typeof(DateTime).GetField(nameof(DateTime.MinValue))!;
        public static MethodInfo MethodBase_GetMethodFromHandle_RuntimeMethodHandle => field ??= typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle) })!;
        public static MethodInfo MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle => field ??= typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) })!;
        public static MethodInfo MethodInfo_CreateDelegate_Type_Object => field ??= typeof(MethodInfo).GetMethod(nameof(MethodInfo.CreateDelegate), new[] { typeof(Type), typeof(object) })!;
        public static MethodInfo String_op_Equality_String_String => field ??= typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!;
        public static MethodInfo String_Equals_String_String => field ??= typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(string) })!;
        public static MethodInfo DictionaryOfStringInt32_Add_String_Int32 => field ??= typeof(Dictionary<string, int>).GetMethod(nameof(Dictionary<string, int>.Add), new[] { typeof(string), typeof(int) })!;
        public static ConstructorInfo DictionaryOfStringInt32_Ctor_Int32 => field ??= typeof(Dictionary<string, int>).GetConstructor(new[] { typeof(int) })!;
        public static MethodInfo Type_GetTypeFromHandle => field ??= typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;
        public static MethodInfo Object_GetType => field ??= typeof(object).GetMethod(nameof(object.GetType))!;
        public static MethodInfo Decimal_op_Implicit_Byte => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(byte) })!;
        public static MethodInfo Decimal_op_Implicit_SByte => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(sbyte) })!;
        public static MethodInfo Decimal_op_Implicit_Int16 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(short) })!;
        public static MethodInfo Decimal_op_Implicit_UInt16 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(ushort) })!;
        public static MethodInfo Decimal_op_Implicit_Int32 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(int) })!;
        public static MethodInfo Decimal_op_Implicit_UInt32 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(uint) })!;
        public static MethodInfo Decimal_op_Implicit_Int64 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(long) })!;
        public static MethodInfo Decimal_op_Implicit_UInt64 => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(ulong) })!;
        public static MethodInfo Decimal_op_Implicit_Char => field ??= typeof(decimal).GetMethod("op_Implicit", new[] { typeof(char) })!;
        public static MethodInfo Math_Pow_Double_Double => field ??= typeof(Math).GetMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) })!;

        // Closure and RuntimeOps helpers are used only in the compiler.
        public static ConstructorInfo Closure_ObjectArray_ObjectArray => field ??= typeof(Closure).GetConstructor(new[] { typeof(object[]), typeof(object[]) })!;
        public static FieldInfo Closure_Constants => field ??= typeof(Closure).GetField(nameof(Closure.Constants))!;
        public static FieldInfo Closure_Locals => field ??= typeof(Closure).GetField(nameof(Closure.Locals))!;
        public static MethodInfo RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array => field ??= typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.CreateRuntimeVariables), new[] { typeof(object[]), typeof(long[]) })!;
        public static MethodInfo RuntimeOps_CreateRuntimeVariables => field ??= typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.CreateRuntimeVariables), Type.EmptyTypes)!;
        public static MethodInfo RuntimeOps_MergeRuntimeVariables => field ??= typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.MergeRuntimeVariables))!;
        public static MethodInfo RuntimeOps_Quote => field ??= typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.Quote))!;
    }
}
