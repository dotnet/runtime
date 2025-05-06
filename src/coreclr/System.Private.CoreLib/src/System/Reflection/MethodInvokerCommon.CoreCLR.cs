// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static unsafe partial class MethodInvokerCommon
    {
        // Zero parameter methods such as property getters:
        private static InvokeFunc_Obj0Args InvokeFuncObj0_bool => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, bool>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_byte => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, byte>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_char => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, char>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_DateTime => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, DateTime>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_DateTimeOffset => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, DateTimeOffset>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_decimal => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, decimal>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_double => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, double>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_float => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, float>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_Guid => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, Guid>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_short => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, short>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_int => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, int>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_long => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, long>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_nint => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, nint>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_nuint => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, nuint>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_object => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, object?>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_sbyte => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, sbyte>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_ushort => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, ushort>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_uint => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, uint>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_ulong => field ??= new InvokeFunc_Obj0Args((fn, o) => InstanceCalliHelper.Call((delegate*<object, ulong>)fn, o!));
        private static InvokeFunc_Obj0Args InvokeFuncObj0_void => field ??= new InvokeFunc_Obj0Args((fn, o) => { InstanceCalliHelper.Call((delegate*<object, void>)fn, o!); return null; });

        // Zero parameter methods but for Enums; these require a return transform and are not cached.
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_byte_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, byte>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_short_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, short>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_int_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, int>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_long_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, long>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_sbyte_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, sbyte>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_ushort_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, ushort>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_uint_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, uint>)fn, o!)));
        private static InvokeFunc_Obj0Args CreateInvokeFuncObj0_ulong_enum(Type enumType) => new InvokeFunc_Obj0Args((fn, o) => Enum.ToObject(enumType, InstanceCalliHelper.Call((delegate*<object, ulong>)fn, o!)));

        // One parameter methods such as property setters:
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_bool => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, bool, void>)fn, o!, (bool)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_byte => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, byte, void>)fn, o!, (byte)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_char => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, char, void>)fn, o!, (char)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_DateTime => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, DateTime, void>)fn, o!, (DateTime)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_DateTimeOffset => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, DateTimeOffset, void>)fn, o!, (DateTimeOffset)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_decimal => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, decimal, void>)fn, o!, (decimal)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_double => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, double, void>)fn, o!, (double)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_float => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, float, void>)fn, o!, (float)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_Guid => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, Guid, void>)fn, o!, (Guid)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_short => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, short, void>)fn, o!, (short)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_int => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, int, void>)fn, o!, (int)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_long => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, long, void>)fn, o!, (long)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_nint => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, nint, void>)fn, o!, (nint)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_nuint => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, nuint, void>)fn, o!, (nuint)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_object => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, object?, void>)fn, o!, (object?)arg1); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_sbyte => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, sbyte, void>)fn, o!, (sbyte)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_ushort => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, ushort, void>)fn, o!, (ushort)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_uint => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, uint, void>)fn, o!, (uint)arg1!); return null; });
        private static InvokeFunc_Obj1Arg InvokeFuncObj1_ulong => field ??= new InvokeFunc_Obj1Arg((fn, o, arg1) => { InstanceCalliHelper.Call((delegate*<object, ulong, void>)fn, o!, (ulong)arg1!); return null; });

        // Two or more parameter methods (object-based):
        private static InvokeFunc_Obj4Args InvokeFunc_Obj4Args_2 => field ?? new InvokeFunc_Obj4Args((fn, o, arg1, arg2, _, _) => { InstanceCalliHelper.Call((delegate*<object, object?, object?, void>)fn, o!, arg1, arg2); return null; });
        private static InvokeFunc_Obj4Args InvokeFunc_Obj4Args_3 => field ?? new InvokeFunc_Obj4Args((fn, o, arg1, arg2, arg3, _) => { InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, void>)fn, o!, arg1, arg2, arg3); return null; });
        private static InvokeFunc_Obj4Args InvokeFunc_Obj4Args_4 => field ?? new InvokeFunc_Obj4Args((fn, o, arg1, arg2, arg3, arg4) => { InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, void>)fn, o!, arg1, arg2, arg3, arg4); return null; });
        private static InvokeFunc_ObjSpanArgs InvokeFunc_ObjSpanArgs_5 => field ?? new InvokeFunc_ObjSpanArgs((fn, o, args) => { InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, void>)fn, o!, args[0], args[1], args[2], args[3], args[4]); return null; });
        private static InvokeFunc_ObjSpanArgs InvokeFunc_ObjSpanArgs_6 => field ?? new InvokeFunc_ObjSpanArgs((fn, o, args) => { InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, object?, void>)fn, o!, args[0], args[1], args[2], args[3], args[4], args[5]); return null; });

        // For CoreClr, this will eventually return 'false' as we plan on removing the interpreted path since it is
        // only used for startup perf and that is now addressed by using calli.
        internal static bool UseInterpretedPath => LocalAppContextSwitches.ForceInterpretedInvoke || !RuntimeFeature.IsDynamicCodeSupported;

        /// <summary>
        /// Returns a delegate that can be used to invoke a method without having to JIT.
        /// </summary>
        private static unsafe bool TryGetCalliFunc(MethodBase method, RuntimeType[] parameterTypes, RuntimeType returnType, InvokerStrategy strategy, out Delegate? invokeFunc)
        {
            if (strategy == InvokerStrategy.Ref4 || strategy == InvokerStrategy.RefMany)
            {
                invokeFunc = null;
                return false;
            }

            Debug.Assert(
                strategy == InvokerStrategy.Obj0 ||
                strategy == InvokerStrategy.Obj1 ||
                strategy == InvokerStrategy.Obj4 ||
                strategy == InvokerStrategy.ObjSpan);


            if (method.DeclaringType is not Type declaringType ||
                // Instance methods on a value type are not supported.
                declaringType.IsValueType ||
                // Currently we don't need to support statics for startup perf.
                method.IsStatic ||
                !SupportsCalli(method))
            {
                invokeFunc = null;
                return false;
            }

            if (strategy == InvokerStrategy.Obj0)
            {
                invokeFunc = GetWellKnownSignatureFor0Args(returnType);
                if (invokeFunc is not null)
                {
                    return true;
                }
            }

            if (strategy == InvokerStrategy.Obj1 && returnType == typeof(void))
            {
                invokeFunc = GetWellKnownSignatureFor1Arg(parameterTypes[0]);
                if (invokeFunc is not null)
                {
                    return true;
                }
            }

            // We only support void return types and reference type parameters from here primarily to support common constructor patterns.
            if (returnType != typeof(void) || !AreAllParametersReferenceTypes(parameterTypes, returnType))
            {

                invokeFunc = null;
                return false;
            }

            switch (parameterTypes.Length)
            {
                case 2:
                    invokeFunc = InvokeFunc_Obj4Args_2;
                    break;
                case 3:
                    invokeFunc = InvokeFunc_Obj4Args_3;
                    break;
                case 4:
                    invokeFunc = InvokeFunc_Obj4Args_4;
                    break;
                case 5:
                    invokeFunc = InvokeFunc_ObjSpanArgs_5;
                    break;
                case 6:
                    invokeFunc = InvokeFunc_ObjSpanArgs_6;
                    break;
                default:
                    invokeFunc  = null;
                    return false;
            }

            return true;

            static bool AreAllParametersReferenceTypes(RuntimeType[] parameterTypes, RuntimeType returnType)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    RuntimeType type = NormalizeType(parameterTypes[i]);
                    if (type != typeof(object))
                    {
                        return false;
                    }
                }

                return returnType == typeof(object);
            }
        }

        /// <summary>
        /// Returns a delegate that can be used to invoke a method with no arguments which are typically property getters.
        /// </summary>
        public static unsafe Delegate? GetWellKnownSignatureFor0Args(RuntimeType returnType)
        {
            //In the checks below, the more common types are first to improve perf.

            // Enums require a return transform to convert from the underlying type to the enum type.
            if (returnType.IsEnum)
            {
                Type underlyingType = (RuntimeType)returnType.GetEnumUnderlyingType()!;
                if (underlyingType == typeof(int)) return CreateInvokeFuncObj0_int_enum(returnType);
                if (underlyingType == typeof(byte)) return CreateInvokeFuncObj0_byte_enum(returnType);
                if (underlyingType == typeof(short)) return CreateInvokeFuncObj0_short_enum(returnType);
                if (underlyingType == typeof(long)) return CreateInvokeFuncObj0_long_enum(returnType);
                if (underlyingType == typeof(uint)) return CreateInvokeFuncObj0_uint_enum(returnType);
                if (underlyingType == typeof(sbyte)) return CreateInvokeFuncObj0_sbyte_enum(returnType);
                if (underlyingType == typeof(ushort)) return CreateInvokeFuncObj0_ushort_enum(returnType);
                Debug.Assert(underlyingType == typeof(ulong));
                return CreateInvokeFuncObj0_ulong_enum(underlyingType);
            }

            returnType = NormalizeType(returnType);

            if (returnType.Assembly != typeof(object).Assembly)
            {
                // We can only hard-code types in this assembly.
                return null;
            }

            if (returnType == typeof(object)) return InvokeFuncObj0_object;
            if (returnType == typeof(void)) return InvokeFuncObj0_void;
            if (returnType == typeof(int)) return InvokeFuncObj0_int;
            if (returnType == typeof(bool)) return InvokeFuncObj0_bool;
            if (returnType == typeof(char)) return InvokeFuncObj0_char;
            if (returnType == typeof(byte)) return InvokeFuncObj0_byte;
            if (returnType == typeof(short)) return InvokeFuncObj0_short;
            if (returnType == typeof(long)) return InvokeFuncObj0_long;
            if (returnType == typeof(decimal)) return InvokeFuncObj0_decimal;
            if (returnType == typeof(double)) return InvokeFuncObj0_double;
            if (returnType == typeof(float)) return InvokeFuncObj0_float;
            if (returnType == typeof(DateTime)) return InvokeFuncObj0_DateTime;
            if (returnType == typeof(DateTimeOffset)) return InvokeFuncObj0_DateTimeOffset;
            if (returnType == typeof(Guid)) return InvokeFuncObj0_Guid;
            if (returnType == typeof(nint)) return InvokeFuncObj0_nint;
            if (returnType == typeof(nuint)) return InvokeFuncObj0_nuint;
            if (returnType == typeof(uint)) return InvokeFuncObj0_uint;
            if (returnType == typeof(sbyte)) return InvokeFuncObj0_sbyte;
            if (returnType == typeof(ushort)) return InvokeFuncObj0_ushort;
            if (returnType == typeof(ulong)) return InvokeFuncObj0_ulong;

            return null;
        }

        /// <summary>
        /// Returns a delegate that can be used to invoke a method with a single argument and no return which are typically property setters.
        /// </summary>
        public static unsafe Delegate? GetWellKnownSignatureFor1Arg(RuntimeType argType)
        {
            // Enums require a return transform to convert to the underlying type.
            if (argType.IsEnum)
            {
                argType = (RuntimeType)argType.GetEnumUnderlyingType();
            }
            else
            {
                argType = NormalizeType(argType);

                if (argType.Assembly != typeof(object).Assembly)
                {
                    // We can only hard-code types in this assembly.
                    return null;
                }
            }

            if (argType == typeof(object)) return InvokeFuncObj1_object;
            if (argType == typeof(int)) return InvokeFuncObj1_int;
            if (argType == typeof(bool)) return InvokeFuncObj1_bool;
            if (argType == typeof(char)) return InvokeFuncObj1_char;
            if (argType == typeof(byte)) return InvokeFuncObj1_byte;
            if (argType == typeof(short)) return InvokeFuncObj1_short;
            if (argType == typeof(long)) return InvokeFuncObj1_long;
            if (argType == typeof(decimal)) return InvokeFuncObj1_decimal;
            if (argType == typeof(double)) return InvokeFuncObj1_double;
            if (argType == typeof(float)) return InvokeFuncObj1_float;
            if (argType == typeof(DateTime)) return InvokeFuncObj1_DateTime;
            if (argType == typeof(DateTimeOffset)) return InvokeFuncObj1_DateTimeOffset;
            if (argType == typeof(Guid)) return InvokeFuncObj1_Guid;
            if (argType == typeof(nint)) return InvokeFuncObj1_nint;
            if (argType == typeof(nuint)) return InvokeFuncObj1_nuint;
            if (argType == typeof(sbyte)) return InvokeFuncObj1_sbyte;
            if (argType == typeof(ushort)) return InvokeFuncObj1_ushort;
            if (argType == typeof(uint)) return InvokeFuncObj1_uint;
            if (argType == typeof(ulong)) return InvokeFuncObj1_ulong;

            return null;
        }

        private static RuntimeType NormalizeType(RuntimeType type)
        {
            if (type.IsClass || type.IsInterface)
            {
                type = (RuntimeType)typeof(object);
            }

            return type;
        }

        private static bool SupportsCalli(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                return false;
            }

            RuntimeType declaringType = (RuntimeType)method.DeclaringType!;

            // Generic types require newobj\call\callvirt.
            if (declaringType.IsGenericType || method.IsGenericMethod)
            {
                return false;
            }

            // Arrays have element types that are not supported by calli plus the constructor is special.
            if (declaringType.IsArray)
            {
                return false;
            }

            if (method is RuntimeConstructorInfo)
            {
                // Strings require initialization through newobj.
                if (ReferenceEquals(declaringType, typeof(string)))
                {
                    return false;
                }
            }
            else
            {
                // Check if polymorphic.
                // For value types, calli is not supported for boxed Object-based virtual methods such as ToString().
                if (method.IsVirtual && (declaringType.IsValueType || (!declaringType.IsSealed && !method.IsFinal)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SupportsParameterTypes(RuntimeType[] parameterTypes, RuntimeType returnType)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                // We already checked the strategy that would tell us if the method has ref parameters.
                Debug.Assert(!parameterTypes[i].IsByRef);

                if (parameterTypes[i].IsPointer)
                {
                    return false;
                }
            }

            return !returnType.IsPointer && !returnType.IsByRef;
        }
    }
}
