// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions.Compiler
{
    internal static class ILGen
    {
        internal static void Emit(this ILGenerator il, OpCode opcode, MethodBase methodBase)
        {
            Debug.Assert(methodBase is MethodInfo || methodBase is ConstructorInfo);

            var ctor = methodBase as ConstructorInfo;
            if ((object)ctor != null)
            {
                il.Emit(opcode, ctor);
            }
            else
            {
                il.Emit(opcode, (MethodInfo)methodBase);
            }
        }

        #region Instruction helpers

        internal static void EmitLoadArg(this ILGenerator il, int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < ushort.MaxValue);

            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= byte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldarg_S, (byte)index);
                    }
                    else
                    {
                        // cast to short, result is correct ushort.
                        il.Emit(OpCodes.Ldarg, (short)index);
                    }
                    break;
            }
        }

        internal static void EmitLoadArgAddress(this ILGenerator il, int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < ushort.MaxValue);

            if (index <= byte.MaxValue)
            {
                il.Emit(OpCodes.Ldarga_S, (byte)index);
            }
            else
            {
                // cast to short, result is correct ushort.
                il.Emit(OpCodes.Ldarga, (short)index);
            }
        }

        internal static void EmitStoreArg(this ILGenerator il, int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < ushort.MaxValue);

            if (index <= byte.MaxValue)
            {
                il.Emit(OpCodes.Starg_S, (byte)index);
            }
            else
            {
                // cast to short, result is correct ushort.
                il.Emit(OpCodes.Starg, (short)index);
            }
        }

        /// <summary>
        /// Emits a Ldind* instruction for the appropriate type
        /// </summary>
        internal static void EmitLoadValueIndirect(this ILGenerator il, Type type)
        {
            Debug.Assert(type != null);

            switch (type.GetTypeCode())
            {
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldind_I1);
                    break;
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldind_U1);
                    break;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldind_I2);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldind_U2);
                    break;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldind_I4);
                    break;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldind_U4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldind_I8);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldind_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldind_R8);
                    break;
                default:
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Ldobj, type);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }
                    break;
            }
        }


        /// <summary>
        /// Emits a Stind* instruction for the appropriate type.
        /// </summary>
        internal static void EmitStoreValueIndirect(this ILGenerator il, Type type)
        {
            Debug.Assert(type != null);

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    il.Emit(OpCodes.Stind_I1);
                    break;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Stind_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Stind_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Stind_I8);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Stind_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Stind_R8);
                    break;
                default:
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Stobj, type);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stind_Ref);
                    }
                    break;
            }
        }

        // Emits the Ldelem* instruction for the appropriate type

        internal static void EmitLoadElement(this ILGenerator il, Type type)
        {
            Debug.Assert(type != null);

            if (!type.IsValueType)
            {
                il.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                        il.Emit(OpCodes.Ldelem_I1);
                        break;
                    case TypeCode.Byte:
                        il.Emit(OpCodes.Ldelem_U1);
                        break;
                    case TypeCode.Int16:
                        il.Emit(OpCodes.Ldelem_I2);
                        break;
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                        il.Emit(OpCodes.Ldelem_U2);
                        break;
                    case TypeCode.Int32:
                        il.Emit(OpCodes.Ldelem_I4);
                        break;
                    case TypeCode.UInt32:
                        il.Emit(OpCodes.Ldelem_U4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        il.Emit(OpCodes.Ldelem_I8);
                        break;
                    case TypeCode.Single:
                        il.Emit(OpCodes.Ldelem_R4);
                        break;
                    case TypeCode.Double:
                        il.Emit(OpCodes.Ldelem_R8);
                        break;
                    default:
                        il.Emit(OpCodes.Ldelem, type);
                        break;
                }
            }
        }

        /// <summary>
        /// Emits a Stelem* instruction for the appropriate type.
        /// </summary>
        internal static void EmitStoreElement(this ILGenerator il, Type type)
        {
            Debug.Assert(type != null);

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Stelem, type);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    break;
            }
        }

        internal static void EmitType(this ILGenerator il, Type type)
        {
            Debug.Assert(type != null);

            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, Type_GetTypeFromHandle);
        }

        #endregion

        #region Fields, properties and methods

        internal static void EmitFieldAddress(this ILGenerator il, FieldInfo fi)
        {
            Debug.Assert(fi != null);

            il.Emit(fi.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, fi);
        }

        internal static void EmitFieldGet(this ILGenerator il, FieldInfo fi)
        {
            Debug.Assert(fi != null);

            il.Emit(fi.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fi);
        }

        internal static void EmitFieldSet(this ILGenerator il, FieldInfo fi)
        {
            Debug.Assert(fi != null);

            il.Emit(fi.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fi);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        internal static void EmitNew(this ILGenerator il, ConstructorInfo ci)
        {
            Debug.Assert(ci != null);
            Debug.Assert(!ci.DeclaringType.ContainsGenericParameters);

            il.Emit(OpCodes.Newobj, ci);
        }

        #endregion

        #region Constants

        internal static void EmitNull(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldnull);
        }

        internal static void EmitString(this ILGenerator il, string value)
        {
            Debug.Assert(value != null);

            il.Emit(OpCodes.Ldstr, value);
        }

        internal static void EmitPrimitive(this ILGenerator il, bool value)
        {
            il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }

        internal static void EmitPrimitive(this ILGenerator il, int value)
        {
            OpCode c;
            switch (value)
            {
                case -1:
                    c = OpCodes.Ldc_I4_M1;
                    break;
                case 0:
                    c = OpCodes.Ldc_I4_0;
                    break;
                case 1:
                    c = OpCodes.Ldc_I4_1;
                    break;
                case 2:
                    c = OpCodes.Ldc_I4_2;
                    break;
                case 3:
                    c = OpCodes.Ldc_I4_3;
                    break;
                case 4:
                    c = OpCodes.Ldc_I4_4;
                    break;
                case 5:
                    c = OpCodes.Ldc_I4_5;
                    break;
                case 6:
                    c = OpCodes.Ldc_I4_6;
                    break;
                case 7:
                    c = OpCodes.Ldc_I4_7;
                    break;
                case 8:
                    c = OpCodes.Ldc_I4_8;
                    break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    return;
            }
            il.Emit(c);
        }

        private static void EmitPrimitive(this ILGenerator il, uint value)
        {
            il.EmitPrimitive(unchecked((int)value));
        }

        private static void EmitPrimitive(this ILGenerator il, long value)
        {
            if (int.MinValue <= value & value <= uint.MaxValue)
            {
                il.EmitPrimitive(unchecked((int)value));
                // While often not of consequence depending on what follows, there are cases where this
                // casting matters. Values [0, int.MaxValue] can use either safely, but negative values
                // must use conv.i8 and those (int.MaxValue, uint.MaxValue] must use conv.u8, or else
                // the higher bits will be wrong.
                il.Emit(value > 0 ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I8, value);
            }
        }

        private static void EmitPrimitive(this ILGenerator il, ulong value)
        {
            il.EmitPrimitive(unchecked((long)value));
        }

        private static void EmitPrimitive(this ILGenerator il, double value)
        {
            il.Emit(OpCodes.Ldc_R8, value);
        }

        private static void EmitPrimitive(this ILGenerator il, float value)
        {
            il.Emit(OpCodes.Ldc_R4, value);
        }

        // matches TryEmitConstant
        internal static bool CanEmitConstant(object value, Type type)
        {
            if (value == null || CanEmitILConstant(type))
            {
                return true;
            }

            Type t = value as Type;
            if (t != null)
            {
                return ShouldLdtoken(t);
            }

            MethodBase mb = value as MethodBase;
            return mb != null && ShouldLdtoken(mb);
        }

        // matches TryEmitILConstant
        private static bool CanEmitILConstant(Type type)
        {
            switch (type.GetNonNullableType().GetTypeCode())
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.String:
                    return true;
            }

            return false;
        }

        //
        // Note: we support emitting more things as IL constants than
        // Linq does
        internal static bool TryEmitConstant(this ILGenerator il, object value, Type type, ILocalCache locals)
        {
            if (value == null)
            {
                // Smarter than the Linq implementation which uses the initobj
                // pattern for all value types (works, but requires a local and
                // more IL)
                il.EmitDefault(type, locals);
                return true;
            }

            // Handle the easy cases
            if (il.TryEmitILConstant(value, type))
            {
                return true;
            }

            // Check for a few more types that we support emitting as constants
            Type t = value as Type;
            if (t != null)
            {
                if (ShouldLdtoken(t))
                {
                    il.EmitType(t);
                    if (type != typeof(Type))
                    {
                        il.Emit(OpCodes.Castclass, type);
                    }

                    return true;
                }

                return false;
            }

            MethodBase mb = value as MethodBase;
            if (mb != null && ShouldLdtoken(mb))
            {
                il.Emit(OpCodes.Ldtoken, mb);
                Type dt = mb.DeclaringType;
                if (dt != null && dt.IsGenericType)
                {
                    il.Emit(OpCodes.Ldtoken, dt);
                    il.Emit(OpCodes.Call, MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle);
                }
                else
                {
                    il.Emit(OpCodes.Call, MethodBase_GetMethodFromHandle_RuntimeMethodHandle);
                }

                if (type != typeof(MethodBase))
                {
                    il.Emit(OpCodes.Castclass, type);
                }

                return true;
            }

            return false;
        }

        private static bool ShouldLdtoken(Type t)
        {
            // If CompileToMethod is re-enabled, t is TypeBuilder should also return
            // true when not compiling to a DynamicMethod
            return t.IsGenericParameter || t.IsVisible;
        }

        internal static bool ShouldLdtoken(MethodBase mb)
        {
            // Can't ldtoken on a DynamicMethod
            if (mb is DynamicMethod)
            {
                return false;
            }

            Type dt = mb.DeclaringType;
            return dt == null || ShouldLdtoken(dt);
        }

        private static bool TryEmitILConstant(this ILGenerator il, object value, Type type)
        {
            Debug.Assert(value != null);

            if (type.IsNullableType())
            {
                Type nonNullType = type.GetNonNullableType();

                if (TryEmitILConstant(il, value, nonNullType))
                {
                    il.Emit(OpCodes.Newobj, type.GetConstructor(new[] { nonNullType }));
                    return true;
                }

                return false;
            }

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    il.EmitPrimitive((bool)value);
                    return true;
                case TypeCode.SByte:
                    il.EmitPrimitive((sbyte)value);
                    return true;
                case TypeCode.Int16:
                    il.EmitPrimitive((short)value);
                    return true;
                case TypeCode.Int32:
                    il.EmitPrimitive((int)value);
                    return true;
                case TypeCode.Int64:
                    il.EmitPrimitive((long)value);
                    return true;
                case TypeCode.Single:
                    il.EmitPrimitive((float)value);
                    return true;
                case TypeCode.Double:
                    il.EmitPrimitive((double)value);
                    return true;
                case TypeCode.Char:
                    il.EmitPrimitive((char)value);
                    return true;
                case TypeCode.Byte:
                    il.EmitPrimitive((byte)value);
                    return true;
                case TypeCode.UInt16:
                    il.EmitPrimitive((ushort)value);
                    return true;
                case TypeCode.UInt32:
                    il.EmitPrimitive((uint)value);
                    return true;
                case TypeCode.UInt64:
                    il.EmitPrimitive((ulong)value);
                    return true;
                case TypeCode.Decimal:
                    il.EmitDecimal((decimal)value);
                    return true;
                case TypeCode.String:
                    il.EmitString((string)value);
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Linq Conversions

        internal static void EmitConvertToType(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            if (TypeUtils.AreEquivalent(typeFrom, typeTo))
            {
                return;
            }

            Debug.Assert(typeFrom != typeof(void) && typeTo != typeof(void));

            bool isTypeFromNullable = typeFrom.IsNullableType();
            bool isTypeToNullable = typeTo.IsNullableType();

            Type nnExprType = typeFrom.GetNonNullableType();
            Type nnType = typeTo.GetNonNullableType();

            if (typeFrom.IsInterface || // interface cast
               typeTo.IsInterface ||
               typeFrom == typeof(object) || // boxing cast
               typeTo == typeof(object) ||
               typeFrom == typeof(System.Enum) ||
               typeFrom == typeof(System.ValueType) ||
               TypeUtils.IsLegalExplicitVariantDelegateConversion(typeFrom, typeTo))
            {
                il.EmitCastToType(typeFrom, typeTo);
            }
            else if (isTypeFromNullable || isTypeToNullable)
            {
                il.EmitNullableConversion(typeFrom, typeTo, isChecked, locals);
            }
            else if (!(typeFrom.IsConvertible() && typeTo.IsConvertible()) // primitive runtime conversion
                     &&
                     (nnExprType.IsAssignableFrom(nnType) || // down cast
                     nnType.IsAssignableFrom(nnExprType))) // up cast
            {
                il.EmitCastToType(typeFrom, typeTo);
            }
            else if (typeFrom.IsArray && typeTo.IsArray) // reference conversion from one array type to another via castclass
            {
                il.EmitCastToType(typeFrom, typeTo);
            }
            else
            {
                il.EmitNumericConversion(typeFrom, typeTo, isChecked);
            }
        }


        private static void EmitCastToType(this ILGenerator il, Type typeFrom, Type typeTo)
        {
            if (typeFrom.IsValueType)
            {
                Debug.Assert(!typeTo.IsValueType);
                il.Emit(OpCodes.Box, typeFrom);
                if (typeTo != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, typeTo);
                }
            }
            else
            {
                il.Emit(typeTo.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, typeTo);
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static void EmitNumericConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            TypeCode tc = typeTo.GetTypeCode();
            TypeCode tf = typeFrom.GetTypeCode();

            if (tc == tf)
            {
                // Between enums of same underlying type, or between such an enum and the underlying type itself.
                // Includes bool-backed enums, which is the only valid conversion to or from bool.
                // Just leave the value on the stack, and treat it as the wanted type.
                return;
            }

            bool isFromUnsigned = tf.IsUnsigned();
            OpCode convCode;
            switch (tc)
            {
                case TypeCode.Single:
                    if (isFromUnsigned)
                        il.Emit(OpCodes.Conv_R_Un);
                    convCode = OpCodes.Conv_R4;
                    break;
                case TypeCode.Double:
                    if (isFromUnsigned)
                        il.Emit(OpCodes.Conv_R_Un);
                    convCode = OpCodes.Conv_R8;
                    break;
                case TypeCode.Decimal:

                    // NB: TypeUtils.IsImplicitNumericConversion makes the promise that implicit conversions
                    //     from various integral types and char to decimal are possible. Coalesce allows the
                    //     conversion lambda to be omitted in these cases, so we have to handle this case in
                    //     here as well, by using the op_Implicit operator implementation on System.Decimal
                    //     because there are no opcodes for System.Decimal.

                    Debug.Assert(typeFrom != typeTo);

                    MethodInfo method;

                    switch (tf)
                    {
                        case TypeCode.Byte: method = Decimal_op_Implicit_Byte; break;
                        case TypeCode.SByte: method = Decimal_op_Implicit_SByte; break;
                        case TypeCode.Int16: method = Decimal_op_Implicit_Int16; break;
                        case TypeCode.UInt16: method = Decimal_op_Implicit_UInt16; break;
                        case TypeCode.Int32: method = Decimal_op_Implicit_Int32; break;
                        case TypeCode.UInt32: method = Decimal_op_Implicit_UInt32; break;
                        case TypeCode.Int64: method = Decimal_op_Implicit_Int64; break;
                        case TypeCode.UInt64: method = Decimal_op_Implicit_UInt64; break;
                        case TypeCode.Char: method = Decimal_op_Implicit_Char; break;
                        default:
                            throw ContractUtils.Unreachable;
                    }

                    il.Emit(OpCodes.Call, method);
                    return;
                case TypeCode.SByte:
                    if (isChecked)
                    {
                        convCode = isFromUnsigned ? OpCodes.Conv_Ovf_I1_Un : OpCodes.Conv_Ovf_I1;
                    }
                    else
                    {
                        if (tf == TypeCode.Byte)
                        {
                            return;
                        }

                        convCode = OpCodes.Conv_I1;
                    }

                    break;
                case TypeCode.Byte:
                    if (isChecked)
                    {
                        convCode = isFromUnsigned ? OpCodes.Conv_Ovf_U1_Un : OpCodes.Conv_Ovf_U1;
                    }
                    else
                    {
                        if (tf == TypeCode.SByte)
                        {
                            return;
                        }

                        convCode = OpCodes.Conv_U1;
                    }

                    break;
                case TypeCode.Int16:
                    switch (tf)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                            return;
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            if (!isChecked)
                            {
                                return;
                            }

                            break;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned ? OpCodes.Conv_Ovf_I2_Un : OpCodes.Conv_Ovf_I2)
                        : OpCodes.Conv_I2;
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    switch (tf)
                    {
                        case TypeCode.Byte:
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            return;
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                            if (!isChecked)
                            {
                                return;
                            }

                            break;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned ? OpCodes.Conv_Ovf_U2_Un : OpCodes.Conv_Ovf_U2)
                        : OpCodes.Conv_U2;
                    break;
                case TypeCode.Int32:
                    switch (tf)
                    {
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            return;
                        case TypeCode.UInt32:
                            if (!isChecked)
                            {
                                return;
                            }

                            break;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_I4)
                        : OpCodes.Conv_I4;
                    break;
                case TypeCode.UInt32:
                    switch (tf)
                    {
                        case TypeCode.Byte:
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            return;
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                            if (!isChecked)
                            {
                                return;
                            }

                            break;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned ? OpCodes.Conv_Ovf_U4_Un : OpCodes.Conv_Ovf_U4)
                        : OpCodes.Conv_U4;
                    break;
                case TypeCode.Int64:
                    if (!isChecked && tf == TypeCode.UInt64)
                    {
                        return;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned ? OpCodes.Conv_Ovf_I8_Un : OpCodes.Conv_Ovf_I8)
                        : (isFromUnsigned ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
                    break;
                case TypeCode.UInt64:
                    if (!isChecked && tf == TypeCode.Int64)
                    {
                        return;
                    }

                    convCode = isChecked
                        ? (isFromUnsigned || tf.IsFloatingPoint() ? OpCodes.Conv_Ovf_U8_Un : OpCodes.Conv_Ovf_U8)
                        : (isFromUnsigned || tf.IsFloatingPoint() ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
                    break;
                default:
                    throw ContractUtils.Unreachable;
            }

            il.Emit(convCode);
        }

        private static void EmitNullableToNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            Debug.Assert(typeFrom.IsNullableType());
            Debug.Assert(typeTo.IsNullableType());
            Label labIfNull;
            Label labEnd;
            LocalBuilder locFrom = locals.GetLocal(typeFrom);
            il.Emit(OpCodes.Stloc, locFrom);
            // test for null
            il.Emit(OpCodes.Ldloca, locFrom);
            il.EmitHasValue(typeFrom);
            labIfNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, labIfNull);
            il.Emit(OpCodes.Ldloca, locFrom);
            locals.FreeLocal(locFrom);
            il.EmitGetValueOrDefault(typeFrom);
            Type nnTypeFrom = typeFrom.GetNonNullableType();
            Type nnTypeTo = typeTo.GetNonNullableType();
            il.EmitConvertToType(nnTypeFrom, nnTypeTo, isChecked, locals);
            // construct result type
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            il.Emit(OpCodes.Newobj, ci);
            labEnd = il.DefineLabel();
            il.Emit(OpCodes.Br_S, labEnd);
            // if null then create a default one
            il.MarkLabel(labIfNull);
            LocalBuilder locTo = locals.GetLocal(typeTo);
            il.Emit(OpCodes.Ldloca, locTo);
            il.Emit(OpCodes.Initobj, typeTo);
            il.Emit(OpCodes.Ldloc, locTo);
            locals.FreeLocal(locTo);
            il.MarkLabel(labEnd);
        }


        private static void EmitNonNullableToNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            Debug.Assert(!typeFrom.IsNullableType());
            Debug.Assert(typeTo.IsNullableType());
            Type nnTypeTo = typeTo.GetNonNullableType();
            il.EmitConvertToType(typeFrom, nnTypeTo, isChecked, locals);
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            il.Emit(OpCodes.Newobj, ci);
        }


        private static void EmitNullableToNonNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            Debug.Assert(typeFrom.IsNullableType());
            Debug.Assert(!typeTo.IsNullableType());
            if (typeTo.IsValueType)
                il.EmitNullableToNonNullableStructConversion(typeFrom, typeTo, isChecked, locals);
            else
                il.EmitNullableToReferenceConversion(typeFrom);
        }


        private static void EmitNullableToNonNullableStructConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            Debug.Assert(typeFrom.IsNullableType());
            Debug.Assert(!typeTo.IsNullableType());
            Debug.Assert(typeTo.IsValueType);
            LocalBuilder locFrom = locals.GetLocal(typeFrom);
            il.Emit(OpCodes.Stloc, locFrom);
            il.Emit(OpCodes.Ldloca, locFrom);
            locals.FreeLocal(locFrom);
            il.EmitGetValue(typeFrom);
            Type nnTypeFrom = typeFrom.GetNonNullableType();
            il.EmitConvertToType(nnTypeFrom, typeTo, isChecked, locals);
        }


        private static void EmitNullableToReferenceConversion(this ILGenerator il, Type typeFrom)
        {
            Debug.Assert(typeFrom.IsNullableType());
            // We've got a conversion from nullable to Object, ValueType, Enum, etc.  Just box it so that
            // we get the nullable semantics.
            il.Emit(OpCodes.Box, typeFrom);
        }


        private static void EmitNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
        {
            bool isTypeFromNullable = typeFrom.IsNullableType();
            bool isTypeToNullable = typeTo.IsNullableType();
            Debug.Assert(isTypeFromNullable || isTypeToNullable);
            if (isTypeFromNullable && isTypeToNullable)
                il.EmitNullableToNullableConversion(typeFrom, typeTo, isChecked, locals);
            else if (isTypeFromNullable)
                il.EmitNullableToNonNullableConversion(typeFrom, typeTo, isChecked, locals);
            else
                il.EmitNonNullableToNullableConversion(typeFrom, typeTo, isChecked, locals);
        }


        internal static void EmitHasValue(this ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }


        internal static void EmitGetValue(this ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }


        internal static void EmitGetValueOrDefault(this ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("GetValueOrDefault", System.Type.EmptyTypes);
            Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }

        #endregion

        #region Arrays

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// Emits an array of constant values provided in the given array.
        /// The array is strongly typed.
        /// </summary>
        internal static void EmitArray<T>(this ILGenerator il, T[] items, ILocalCache locals)
        {
            Debug.Assert(items != null);

            il.EmitPrimitive(items.Length);
            il.Emit(OpCodes.Newarr, typeof(T));
            for (int i = 0; i < items.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.EmitPrimitive(i);
                il.TryEmitConstant(items[i], typeof(T), locals);
                il.EmitStoreElement(typeof(T));
            }
        }
#endif

        /// <summary>
        /// Emits an array of values of count size.
        /// </summary>
        internal static void EmitArray(this ILGenerator il, Type elementType, int count)
        {
            Debug.Assert(elementType != null);
            Debug.Assert(count >= 0);

            il.EmitPrimitive(count);
            il.Emit(OpCodes.Newarr, elementType);
        }

        /// <summary>
        /// Emits an array construction code.
        /// The code assumes that bounds for all dimensions
        /// are already emitted.
        /// </summary>
        internal static void EmitArray(this ILGenerator il, Type arrayType)
        {
            Debug.Assert(arrayType != null);
            Debug.Assert(arrayType.IsArray);

            if (arrayType.IsSZArray)
            {
                il.Emit(OpCodes.Newarr, arrayType.GetElementType());
            }
            else
            {
                Type[] types = new Type[arrayType.GetArrayRank()];
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = typeof(int);
                }
                ConstructorInfo ci = arrayType.GetConstructor(types);
                Debug.Assert(ci != null);
                il.EmitNew(ci);
            }
        }

        #endregion

        #region Support for emitting constants

        private static void EmitDecimal(this ILGenerator il, decimal value)
        {
            int[] bits = decimal.GetBits(value);
            int scale = (bits[3] & int.MaxValue) >> 16;
            if (scale == 0)
            {
                if (int.MinValue <= value)
                {
                    if (value <= int.MaxValue)
                    {
                        int intValue = decimal.ToInt32(value);
                        switch (intValue)
                        {
                            case -1:
                                il.Emit(OpCodes.Ldsfld, Decimal_MinusOne);
                                return;
                            case 0:
                                il.EmitDefault(typeof(decimal), locals: null); // locals won't be used.
                                return;
                            case 1:
                                il.Emit(OpCodes.Ldsfld, Decimal_One);
                                return;
                            default:
                                il.EmitPrimitive(intValue);
                                il.EmitNew(Decimal_Ctor_Int32);
                                return;
                        }
                    }

                    if (value <= uint.MaxValue)
                    {
                        il.EmitPrimitive(decimal.ToUInt32(value));
                        il.EmitNew(Decimal_Ctor_UInt32);
                        return;
                    }
                }

                if (long.MinValue <= value)
                {
                    if (value <= long.MaxValue)
                    {
                        il.EmitPrimitive(decimal.ToInt64(value));
                        il.EmitNew(Decimal_Ctor_Int64);
                        return;
                    }

                    if (value <= ulong.MaxValue)
                    {
                        il.EmitPrimitive(decimal.ToUInt64(value));
                        il.EmitNew(Decimal_Ctor_UInt64);
                        return;
                    }

                    if (value == decimal.MaxValue)
                    {
                        il.Emit(OpCodes.Ldsfld, Decimal_MaxValue);
                        return;
                    }
                }
                else if (value == decimal.MinValue)
                {
                    il.Emit(OpCodes.Ldsfld, Decimal_MinValue);
                    return;
                }
            }

            il.EmitPrimitive(bits[0]);
            il.EmitPrimitive(bits[1]);
            il.EmitPrimitive(bits[2]);
            il.EmitPrimitive((bits[3] & 0x80000000) != 0);
            il.EmitPrimitive(unchecked((byte)scale));
            il.EmitNew(Decimal_Ctor_Int32_Int32_Int32_Bool_Byte);
        }

        /// <summary>
        /// Emits default(T)
        /// Semantics match C# compiler behavior
        /// </summary>
        internal static void EmitDefault(this ILGenerator il, Type type, ILocalCache locals)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.DateTime:
                    il.Emit(OpCodes.Ldsfld, DateTime_MinValue);
                    break;

                case TypeCode.Object:
                    if (type.IsValueType)
                    {
                        // Type.GetTypeCode on an enum returns the underlying
                        // integer TypeCode, so we won't get here.
                        Debug.Assert(!type.IsEnum);

                        // This is the IL for default(T) if T is a generic type
                        // parameter, so it should work for any type. It's also
                        // the standard pattern for structs.
                        LocalBuilder lb = locals.GetLocal(type);
                        il.Emit(OpCodes.Ldloca, lb);
                        il.Emit(OpCodes.Initobj, type);
                        il.Emit(OpCodes.Ldloc, lb);
                        locals.FreeLocal(lb);
                        break;
                    }

                    goto case TypeCode.Empty;

                case TypeCode.Empty:
                case TypeCode.String:
                case TypeCode.DBNull:
                    il.Emit(OpCodes.Ldnull);
                    break;

                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I8);
                    break;

                case TypeCode.Single:
                    il.Emit(OpCodes.Ldc_R4, default(float));
                    break;

                case TypeCode.Double:
                    il.Emit(OpCodes.Ldc_R8, default(double));
                    break;

                case TypeCode.Decimal:
                    il.Emit(OpCodes.Ldsfld, Decimal_Zero);
                    break;

                default:
                    throw ContractUtils.Unreachable;
            }
        }

        #endregion
    }
}
