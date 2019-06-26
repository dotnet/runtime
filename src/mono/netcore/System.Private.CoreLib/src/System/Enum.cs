// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
	partial class Enum
	{
		internal sealed class EnumInfo
		{
			public readonly bool HasFlagsAttribute;
			public readonly ulong[] Values;
			public readonly string[] Names;

			// Each entry contains a list of sorted pair of enum field names and values, sorted by values
			public EnumInfo (bool hasFlagsAttribute, ulong[] values, string[] names)
			{
				HasFlagsAttribute = hasFlagsAttribute;
				Values = values;
				Names = names;
			}
		}

		public int CompareTo (object? target)
		{
			const int retIncompatibleMethodTables = 2;  // indicates that the method tables did not match

			int ret = InternalCompareTo (this, target);

			if (ret < retIncompatibleMethodTables)
				// -1, 0 and 1 are the normal return codes
				return ret;

			if (ret == retIncompatibleMethodTables)
				throw new ArgumentException (SR.Format (SR.Arg_EnumAndObjectMustBeSameType, target!.GetType (), GetType ()));

			throw new InvalidOperationException (SR.InvalidOperation_UnknownEnumType);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern bool InternalHasFlag (Enum flags);

		[Intrinsic]
		public bool HasFlag (Enum flag)
		{
			if (flag is null)
				throw new ArgumentNullException (nameof (flag));
			if (!this.GetType ().IsEquivalentTo (flag.GetType ()))
				throw new ArgumentException (SR.Format (SR.Argument_EnumTypeDoesNotMatch, flag.GetType (), this.GetType ()));

			return InternalHasFlag (flag);
		}

		public static string? GetName (Type enumType, object value)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof(enumType));

			return enumType.GetEnumName (value);
		}

		public static string[] GetNames (Type enumType)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof (enumType));

			return enumType.GetEnumNames ();
		}

		public static Type GetUnderlyingType (Type enumType)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof (enumType));

			return enumType.GetEnumUnderlyingType ();
		}

		public static Array GetValues (Type enumType)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof (enumType));

			return enumType.GetEnumValues ();
		}

		public static bool IsDefined (Type enumType, object value)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof (enumType));

			return enumType.IsEnumDefined (value);
		}

		internal static ulong[] InternalGetValues (RuntimeType enumType)
		{
			// Get all of the values
			return GetEnumInfo (enumType, false).Values;
		}

		internal static string[] InternalGetNames (RuntimeType enumType)
		{
			// Get all of the names
			return GetEnumInfo (enumType, true).Names;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool GetEnumValuesAndNames (RuntimeType enumType, out ulong[] values, out string[] names);

		static EnumInfo GetEnumInfo (RuntimeType enumType, bool getNames = true)
		{
			var entry = enumType.Cache.EnumInfo;

			if (entry == null || (getNames && entry.Names == null)) {
				if (!GetEnumValuesAndNames (enumType, out var values, out var names))
					Array.Sort (values, names, System.Collections.Generic.Comparer<ulong>.Default);

				bool hasFlagsAttribute = enumType.IsDefined (typeof (FlagsAttribute), inherit: false);
				entry = new EnumInfo (hasFlagsAttribute, values, names);
				enumType.Cache.EnumInfo = entry;
			}

			return entry;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern object InternalBoxEnum (RuntimeType enumType, long value);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int InternalCompareTo (object o1, object? o2);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern CorElementType InternalGetCorElementType ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern RuntimeType InternalGetUnderlyingType (RuntimeType enumType);

		static RuntimeType ValidateRuntimeType (Type enumType)
		{
			if (enumType is null)
				throw new ArgumentNullException (nameof (enumType));
			if (!enumType.IsEnum)
				throw new ArgumentException (SR.Arg_MustBeEnum, nameof (enumType));
			if (!(enumType is RuntimeType rtType))
				throw new ArgumentException (SR.Arg_MustBeType, nameof (enumType));
			return rtType;
		}
	}
}
