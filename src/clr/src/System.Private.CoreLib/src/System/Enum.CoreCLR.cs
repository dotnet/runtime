// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public override extern bool Equals(object? obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int InternalCompareTo(object thisRef, object? target);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool InternalHasFlag(Enum flags);

        private class EnumInfo
        {
            public readonly bool HasFlagsAttribute;
            public readonly ulong[] Values;
            public readonly string[] Names;

            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public EnumInfo(bool hasFlagsAttribute, ulong[] values, string[] names)
            {
                HasFlagsAttribute = hasFlagsAttribute;
                Values = values;
                Names = names;
            }
        }

        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            EnumInfo? entry = enumType.GenericCache as EnumInfo;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[]? values = null;
                string[]? names = null;
                RuntimeTypeHandle enumTypeHandle = enumType.GetTypeHandleInternal();
                GetEnumValuesAndNames(
                    JitHelpers.GetQCallTypeHandleOnStack(ref enumTypeHandle),
                    JitHelpers.GetObjectHandleOnStack(ref values),
                    JitHelpers.GetObjectHandleOnStack(ref names),
                    getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                entry = new EnumInfo(hasFlagsAttribute, values!, names!);
                enumType.GenericCache = entry;
            }

            return entry;
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            // Get all of the values
            return GetEnumInfo(enumType, false).Values;
        }

        internal static string[] InternalGetNames(RuntimeType enumType)
        {
            // Get all of the names
            return GetEnumInfo(enumType, true).Names;
        }

        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException(nameof(flag));

            if (!this.GetType().IsEquivalentTo(flag.GetType()))
            {
                throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), this.GetType()));
            }

            return InternalHasFlag(flag);
        }

        public static string? GetName(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumName(value);
        }

        public static string[] GetNames(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumNames();
        }

        public static Type GetUnderlyingType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumUnderlyingType();
        }

        public static Array GetValues(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.GetEnumValues();
        }

        public static bool IsDefined(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            return enumType.IsEnumDefined(value);
        }

        private static RuntimeType ValidateRuntimeType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType));
            if (!(enumType is RuntimeType rtType))
                throw new ArgumentException(SR.Arg_MustBeType, nameof(enumType));
            return rtType;
        }

        public int CompareTo(object? target)
        {
            const int retIncompatibleMethodTables = 2;  // indicates that the method tables did not match
            const int retInvalidEnumType = 3; // indicates that the enum was of an unknown/unsupported underlying type

            if (this == null)
                throw new NullReferenceException();

            int ret = InternalCompareTo(this, target);

            if (ret < retIncompatibleMethodTables)
            {
                // -1, 0 and 1 are the normal return codes
                return ret;
            }
            else if (ret == retIncompatibleMethodTables)
            {
                Type thisType = this.GetType();
                Type targetType = target!.GetType();

                throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, targetType, thisType));
            }
            else
            {
                // assert valid return code (3)
                Debug.Assert(ret == retInvalidEnumType, "Enum.InternalCompareTo return code was invalid");

                throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
            }
        }
    }
}
