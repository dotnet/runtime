// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace System
{
    //
    // This file collects a set of Enum-related apis that run when the Type is subclassed by an application.
    // None of it runs on normal Type objects supplied by the runtime (as those types override these methods.)
    //
    // Since app-subclassed Types are "untrusted classes" that may or may not implement the complete surface area correctly,
    // this code should be considered brittle and not changed lightly.
    //
    public abstract partial class Type
    {
        public virtual bool IsEnumDefined(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(value));

            // Check if both of them are of the same type
            Type valueType = value.GetType();

            // If the value is an Enum then we need to extract the underlying value from it
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, this));

                valueType = valueType.GetEnumUnderlyingType();
            }

            // If a string is passed in
            if (valueType == typeof(string))
            {
                string[] names = GetEnumNames();
                if (Array.IndexOf(names, value) >= 0)
                    return true;
                else
                    return false;
            }

            // If an enum or integer value is passed in
            if (Type.IsIntegerType(valueType))
            {
                Type underlyingType = GetEnumUnderlyingType();
                // We cannot compare the types directly because valueType is always a runtime type but underlyingType might not be.
                if (underlyingType.GetTypeCodeImpl() != valueType.GetTypeCodeImpl())
                    throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, valueType, underlyingType));

                Array values = GetEnumRawConstantValues();
                return BinarySearch(values, value) >= 0;
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }

        public virtual string? GetEnumName(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(value));

            Type valueType = value.GetType();

            if (!(valueType.IsEnum || Type.IsIntegerType(valueType)))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            Array values = GetEnumRawConstantValues();
            int index = BinarySearch(values, value);

            if (index >= 0)
            {
                string[] names = GetEnumNames();
                return names[index];
            }

            return null;
        }

        public virtual string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            GetEnumData(out string[] names, out _);
            return names;
        }

        // Returns the enum values as an object array.
        private Array GetEnumRawConstantValues()
        {
            GetEnumData(out _, out Array values);
            return values;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern",
            Justification = "Literal fields on enums can never be trimmed")]
        // This will return enumValues and enumNames sorted by the values.
        private void GetEnumData(out string[] enumNames, out Array enumValues)
        {
            FieldInfo[] flds = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            object[] values = new object[flds.Length];
            string[] names = new string[flds.Length];

            for (int i = 0; i < flds.Length; i++)
            {
                names[i] = flds[i].Name;
                values[i] = flds[i].GetRawConstantValue()!;
            }

            // Insertion Sort these values in ascending order.
            // We use this O(n^2) algorithm, but it turns out that most of the time the elements are already in sorted order and
            // the common case performance will be faster than quick sorting this.
            Comparer comparer = Comparer.Default;
            for (int i = 1; i < values.Length; i++)
            {
                int j = i;
                string tempStr = names[i];
                object val = values[i];
                bool exchanged = false;

                // Since the elements are sorted we only need to do one comparision, we keep the check for j inside the loop.
                while (comparer.Compare(values[j - 1], val) > 0)
                {
                    names[j] = names[j - 1];
                    values[j] = values[j - 1];
                    j--;
                    exchanged = true;
                    if (j == 0)
                        break;
                }

                if (exchanged)
                {
                    names[j] = tempStr;
                    values[j] = val;
                }
            }

            enumNames = names;
            enumValues = values;
        }

        // Convert everything to ulong then perform a binary search.
        private static int BinarySearch(Array array, object value)
        {
            ulong[] ulArray = new ulong[array.Length];
            for (int i = 0; i < array.Length; ++i)
                ulArray[i] = Enum.ToUInt64(array.GetValue(i)!);

            ulong ulValue = Enum.ToUInt64(value);

            return Array.BinarySearch(ulArray, ulValue);
        }

        internal static bool IsIntegerType(Type t)
        {
            return t == typeof(int) ||
                    t == typeof(short) ||
                    t == typeof(ushort) ||
                    t == typeof(byte) ||
                    t == typeof(sbyte) ||
                    t == typeof(uint) ||
                    t == typeof(long) ||
                    t == typeof(ulong) ||
                    t == typeof(char) ||
                    t == typeof(bool);
        }
    }
}
