// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    // Base class for runtime implemented Type
    public abstract class RuntimeType : TypeInfo
    {
        public sealed override string? GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            ulong rawValue;
            if (!Enum.TryGetUnboxedValueOfEnumOrInteger(value, out rawValue))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            // For desktop compatibility, do not bounce an incoming integer that's the wrong size.
            // Do a value-preserving cast of both it and the enum values and do a 64-bit compare.

            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum);

            return Enum.GetEnumName(this, rawValue);
        }

        public sealed override string[] GetEnumNames()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            string[] ret = Enum.InternalGetNames(this);

            // Make a copy since we can't hand out the same array since users can modify them
            return new ReadOnlySpan<string>(ret).ToArray();
        }

        public sealed override Type GetEnumUnderlyingType()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return Enum.InternalGetUnderlyingType(this);
        }

        public sealed override bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            if (value is string valueAsString)
            {
                EnumInfo enumInfo = Enum.GetEnumInfo(this);
                foreach (string name in enumInfo.Names)
                {
                    if (valueAsString == name)
                        return true;
                }
                return false;
            }
            else
            {
                ulong rawValue;
                if (!Enum.TryGetUnboxedValueOfEnumOrInteger(value, out rawValue))
                {
                    if (Type.IsIntegerType(value.GetType()))
                        throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, value.GetType(), Enum.InternalGetUnderlyingType(this)));
                    else
                        throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
                }

                if (value is Enum)
                {
                    if (!Enum.ValueTypeMatchesEnumType(this, value))
                        throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, value.GetType(), this));
                }
                else
                {
                    Type underlyingType = Enum.InternalGetUnderlyingType(this);
                    if (!(underlyingType.TypeHandle.ToEETypePtr() == value.GetEETypePtr()))
                        throw new ArgumentException(SR.Format(SR.Arg_EnumUnderlyingTypeAndObjectMustBeSameType, value.GetType(), underlyingType));
                }

                return Enum.GetEnumName(this, rawValue) != null;
            }
        }

        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload instead.")]
        public sealed override Array GetEnumValues()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            Array values = Enum.GetEnumInfo(this).ValuesAsUnderlyingType;
            int count = values.Length;
            // Without universal shared generics, chances are slim that we'll have the appropriate
            // array type available. Offer an escape hatch that avoids a MissingMetadataException
            // at the cost of a small appcompat risk.
            Array result;
            if (AppContext.TryGetSwitch("Switch.System.Enum.RelaxedGetValues", out bool isRelaxed) && isRelaxed)
                result = Array.CreateInstance(Enum.InternalGetUnderlyingType(this), count);
            else
                result = Array.CreateInstance(this, count);
            Array.Copy(values, result, values.Length);
            return result;
        }

        public sealed override Array GetEnumValuesAsUnderlyingType()
        {
            if (!IsActualEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");

            return (Array)Enum.GetEnumInfo(this).ValuesAsUnderlyingType.Clone();
        }

        internal bool IsActualEnum
            => TryGetEEType(out EETypePtr eeType) && eeType.IsEnum;
    }
}
