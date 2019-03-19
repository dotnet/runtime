// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEnumValuesAndNames(RuntimeTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, bool getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public override extern bool Equals(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int InternalCompareTo(object o1, object o2);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool InternalHasFlag(Enum flags);

        private static TypeValuesAndNames GetCachedValuesAndNames(RuntimeType enumType, bool getNames)
        {
            TypeValuesAndNames entry = enumType.GenericCache as TypeValuesAndNames;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[] values = null;
                string[] names = null;
                GetEnumValuesAndNames(
                    enumType.GetTypeHandleInternal(),
                    JitHelpers.GetObjectHandleOnStack(ref values),
                    JitHelpers.GetObjectHandleOnStack(ref names),
                    getNames);
                bool isFlags = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                entry = new TypeValuesAndNames(isFlags, values, names);
                enumType.GenericCache = entry;
            }

            return entry;
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

        public static string GetName(Type enumType, object value)
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
    }
}
