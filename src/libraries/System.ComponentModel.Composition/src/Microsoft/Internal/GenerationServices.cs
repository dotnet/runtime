// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Internal
{
    internal static class GenerationServices
    {
        // Type.GetTypeFromHandle
        private static readonly MethodInfo s_typeGetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle")!;

        // typeofs are pretty expensive, so we cache them statically
        private static readonly Type s_typeType = typeof(System.Type);
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_charType = typeof(char);
        private static readonly Type s_booleanType = typeof(bool);
        private static readonly Type s_byteType = typeof(byte);
        private static readonly Type s_sByteType = typeof(sbyte);
        private static readonly Type s_int16Type = typeof(short);
        private static readonly Type s_uInt16Type = typeof(ushort);
        private static readonly Type s_int32Type = typeof(int);
        private static readonly Type s_uInt32Type = typeof(uint);
        private static readonly Type s_int64Type = typeof(long);
        private static readonly Type s_uInt64Type = typeof(ulong);
        private static readonly Type s_doubleType = typeof(double);
        private static readonly Type s_singleType = typeof(float);
        private static readonly Type s_iEnumerableTypeofT = typeof(System.Collections.Generic.IEnumerable<>);
        private static readonly Type s_iEnumerableType = typeof(System.Collections.IEnumerable);

        private static readonly MethodInfo ExceptionGetData = typeof(Exception).GetProperty("Data")!.GetGetMethod()!;
        private static readonly MethodInfo DictionaryAdd = typeof(IDictionary).GetMethod("Add")!;
        private static readonly ConstructorInfo ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes)!;

        public static ILGenerator CreateGeneratorForPublicConstructor(this TypeBuilder typeBuilder, Type[] ctrArgumentTypes)
        {
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                ctrArgumentTypes);

            ILGenerator ctorIL = ctorBuilder.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, ObjectCtor);

            return ctorIL;
        }

        /// Generates the code that loads the supplied value on the stack
        /// This is not as simple as it seems, as different instructions need to be generated depending
        /// on its type.
        /// We support:
        /// 1. All primitive types
        /// 2. Strings
        /// 3. Enums
        /// 4. typeofs
        /// 5. nulls
        /// 6. Enumerables
        /// 7. Delegates on static functions or any of the above
        /// Everything else cannot be represented as literals
        /// <param name="ilGenerator"></param>
        /// <param name="value"></param>
        public static void LoadValue(this ILGenerator ilGenerator, object? value)
        {
            Debug.Assert(ilGenerator != null);

            //
            // Get nulls out of the way - they are basically typeless, so we just load null
            //
            if (value == null)
            {
                ilGenerator.LoadNull();
                return;
            }

            //
            // Prepare for literal loading - decide whether we should box, and handle enums properly
            //
            Type valueType = value.GetType();
            object rawValue = value;
            if (valueType.IsEnum)
            {
                // enums are special - we need to load the underlying constant on the stack
                rawValue = Convert.ChangeType(value, Enum.GetUnderlyingType(valueType), null);
                valueType = rawValue.GetType();
            }

            //
            // Generate IL depending on the valueType - this is messier than it should ever be, but sadly necessary
            //
            if (valueType == GenerationServices.s_stringType)
            {
                // we need to check for strings before enumerables, because strings are IEnumerable<char>
                ilGenerator.LoadString((string)rawValue);
            }
            else if (GenerationServices.s_typeType.IsAssignableFrom(valueType))
            {
                ilGenerator.LoadTypeOf((Type)rawValue);
            }
            else if (GenerationServices.s_iEnumerableType.IsAssignableFrom(valueType))
            {
                // NOTE : strings and dictionaries are also enumerables, but we have already handled those
                ilGenerator.LoadEnumerable((IEnumerable)rawValue);
            }
            else if (
                (valueType == GenerationServices.s_charType) ||
                (valueType == GenerationServices.s_booleanType) ||
                (valueType == GenerationServices.s_byteType) ||
                (valueType == GenerationServices.s_sByteType) ||
                (valueType == GenerationServices.s_int16Type) ||
                (valueType == GenerationServices.s_uInt16Type) ||
                (valueType == GenerationServices.s_int32Type)
                )
            {
                // NOTE : Everything that is 32 bit or less uses ldc.i4. We need to pass int32, even if the actual types is shorter - this is IL memory model
                // direct casting to (int) won't work, because the value is boxed, thus we need to use Convert.
                // Sadly, this will not work for all cases - namely large uint32 - because they can't semantically fit into 32 signed bits
                // We have a special case for that next
                ilGenerator.LoadInt((int)Convert.ChangeType(rawValue, typeof(int), CultureInfo.InvariantCulture));
            }
            else if (valueType == GenerationServices.s_uInt32Type)
            {
                // NOTE : This one is a bit tricky. Ldc.I4 takes an Int32 as an argument, although it really treats it as a 32bit number
                // That said, some UInt32 values are larger that Int32.MaxValue, so the Convert call above will fail, which is why
                // we need to treat this case individually and cast to uint, and then - unchecked - to int.
                ilGenerator.LoadInt(unchecked((int)((uint)rawValue)));
            }
            else if (valueType == GenerationServices.s_int64Type)
            {
                ilGenerator.LoadLong((long)rawValue);
            }
            else if (valueType == GenerationServices.s_uInt64Type)
            {
                // NOTE : This one is a bit tricky. Ldc.I8 takes an Int64 as an argument, although it really treats it as a 64bit number
                // That said, some UInt64 values are larger that Int64.MaxValue, so the direct case we use above (or Convert, for that matter)will fail, which is why
                // we need to treat this case individually and cast to ulong, and then - unchecked - to long.
                ilGenerator.LoadLong(unchecked((long)((ulong)rawValue)));
            }
            else if (valueType == GenerationServices.s_singleType)
            {
                ilGenerator.LoadFloat((float)rawValue);
            }
            else if (valueType == GenerationServices.s_doubleType)
            {
                ilGenerator.LoadDouble((double)rawValue);
            }
            else
            {
                throw new InvalidOperationException(
                    SR.Format(SR.InvalidMetadataValue, value.GetType().FullName));
            }
        }

        /// Generates the code that adds an object to a dictionary stored in a local variable
        /// <param name="ilGenerator"></param>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddItemToLocalDictionary(this ILGenerator ilGenerator, LocalBuilder dictionary, object key, object value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldloc, dictionary);
            ilGenerator.LoadValue(key);
            ilGenerator.LoadValue(value);
            ilGenerator.Emit(OpCodes.Callvirt, DictionaryAdd);
        }

        /// Generates the code that adds an object from a local variable to a dictionary also stored in a local
        /// <param name="ilGenerator"></param>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddLocalToLocalDictionary(this ILGenerator ilGenerator, LocalBuilder dictionary, object key, LocalBuilder value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldloc, dictionary);
            ilGenerator.LoadValue(key);
            ilGenerator.Emit(OpCodes.Ldloc, value);
            ilGenerator.Emit(OpCodes.Callvirt, DictionaryAdd);
        }

        /// Generates the code to get the type of an object and store it in a local
        /// <param name="ilGenerator"></param>
        /// <param name="exception"></param>
        /// <param name="dataStore"></param>
        public static void GetExceptionDataAndStoreInLocal(this ILGenerator ilGenerator, LocalBuilder exception, LocalBuilder dataStore)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(dataStore);

            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldloc, exception);
            ilGenerator.Emit(OpCodes.Callvirt, ExceptionGetData);
            ilGenerator.Emit(OpCodes.Stloc, dataStore);
        }

        private static void LoadEnumerable(this ILGenerator ilGenerator, IEnumerable enumerable)
        {
            ArgumentNullException.ThrowIfNull(enumerable);

            Debug.Assert(ilGenerator != null);

            // We load enumerable as an array - this is the most compact and efficient way of representing it
            Type elementType;
            if (ReflectionServices.TryGetGenericInterfaceType(enumerable.GetType(), GenerationServices.s_iEnumerableTypeofT, out Type? closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }
            else
            {
                elementType = typeof(object);
            }

            //
            // elem[] array = new elem[<enumerable.Count()>]
            //
            Type generatedArrayType = elementType.MakeArrayType();
            LocalBuilder generatedArrayLocal = ilGenerator.DeclareLocal(generatedArrayType);

            ilGenerator.LoadInt(enumerable.Cast<object>().Count());
            ilGenerator.Emit(OpCodes.Newarr, elementType);
            ilGenerator.Emit(OpCodes.Stloc, generatedArrayLocal);

            int index = 0;
            foreach (object? value in enumerable)
            {
                //
                //array[<index>] = value;
                //
                ilGenerator.Emit(OpCodes.Ldloc, generatedArrayLocal);
                ilGenerator.LoadInt(index);
                ilGenerator.LoadValue(value);
                if (GenerationServices.IsBoxingRequiredForValue(value) && !elementType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, value!.GetType());
                }
                ilGenerator.Emit(OpCodes.Stelem, elementType);
                index++;
            }

            ilGenerator.Emit(OpCodes.Ldloc, generatedArrayLocal);
        }

        private static bool IsBoxingRequiredForValue(object? value)
        {
            if (value == null)
            {
                return false;
            }
            else
            {
                return value.GetType().IsValueType;
            }
        }

        private static void LoadNull(this ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldnull);
        }

        private static void LoadString(this ILGenerator ilGenerator, string s)
        {
            Debug.Assert(ilGenerator != null);

            if (s == null)
            {
                ilGenerator.LoadNull();
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldstr, s);
            }
        }

        private static void LoadInt(this ILGenerator ilGenerator, int value)
        {
            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldc_I4, value);
        }

        private static void LoadLong(this ILGenerator ilGenerator, long value)
        {
            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldc_I8, value);
        }

        private static void LoadFloat(this ILGenerator ilGenerator, float value)
        {
            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldc_R4, value);
        }

        private static void LoadDouble(this ILGenerator ilGenerator, double value)
        {
            Debug.Assert(ilGenerator != null);

            ilGenerator.Emit(OpCodes.Ldc_R8, value);
        }

        private static void LoadTypeOf(this ILGenerator ilGenerator, Type type)
        {
            Debug.Assert(ilGenerator != null);

            //typeofs() translate into ldtoken and Type::GetTypeFromHandle call
            ilGenerator.Emit(OpCodes.Ldtoken, type);
            ilGenerator.EmitCall(OpCodes.Call, GenerationServices.s_typeGetTypeFromHandleMethod, null);
        }
    }
}
