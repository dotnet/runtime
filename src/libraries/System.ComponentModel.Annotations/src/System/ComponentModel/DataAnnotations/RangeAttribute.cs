// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Used for specifying a range constraint
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class RangeAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="minimum">The minimum value, inclusive</param>
        /// <param name="maximum">The maximum value, inclusive</param>
        public RangeAttribute(int minimum, int maximum)
            : base(() => SR.RangeAttribute_ValidationError)
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(int);
        }

        /// <summary>
        ///     Constructor that takes double minimum and maximum values
        /// </summary>
        /// <param name="minimum">The minimum value, inclusive</param>
        /// <param name="maximum">The maximum value, inclusive</param>
        public RangeAttribute(double minimum, double maximum)
            : base(() => SR.RangeAttribute_ValidationError)
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(double);
        }

        /// <summary>
        ///     Allows for specifying range for arbitrary types. The minimum and maximum strings
        ///     will be converted to the target type.
        /// </summary>
        /// <param name="type">The type of the range parameters. Must implement IComparable.</param>
        /// <param name="minimum">The minimum allowable value.</param>
        /// <param name="maximum">The maximum allowable value.</param>
        [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public RangeAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
            string minimum,
            string maximum)
            : base(() => SR.RangeAttribute_ValidationError)
        {
            OperandType = type;
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        ///     Gets the minimum value for the range
        /// </summary>
        public object Minimum { get; private set; }

        /// <summary>
        ///     Gets the maximum value for the range
        /// </summary>
        public object Maximum { get; private set; }

        /// <summary>
        ///     Gets the type of the <see cref="Minimum" /> and <see cref="Maximum" /> values (e.g. Int32, Double, or some custom
        ///     type)
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type OperandType { get; }

        /// <summary>
        /// Determines whether string values for <see cref="Minimum"/> and <see cref="Maximum"/> are parsed in the invariant
        /// culture rather than the current culture in effect at the time of the validation.
        /// </summary>
        public bool ParseLimitsInInvariantCulture { get; set; }

        /// <summary>
        /// Determines whether any conversions necessary from the value being validated to <see cref="OperandType"/> as set
        /// by the <c>type</c> parameter of the <see cref="RangeAttribute(Type, string, string)"/> constructor are carried
        /// out in the invariant culture rather than the current culture in effect at the time of the validation.
        /// </summary>
        /// <remarks>This property has no effects with the constructors with <see cref="int"/> or <see cref="double"/>
        /// parameters, for which the invariant culture is always used for any conversions of the validated value.</remarks>
        public bool ConvertValueInInvariantCulture { get; set; }

        private Func<object, object?>? Conversion { get; set; }

        private void Initialize(IComparable minimum, IComparable maximum, Func<object, object?> conversion)
        {
            if (minimum.CompareTo(maximum) > 0)
            {
                throw new InvalidOperationException(SR.Format(SR.RangeAttribute_MinGreaterThanMax, maximum, minimum));
            }

            Minimum = minimum;
            Maximum = maximum;
            Conversion = conversion;
        }

        /// <summary>
        ///     Returns true if the value falls between min and max, inclusive.
        /// </summary>
        /// <param name="value">The value to test for validity.</param>
        /// <returns><c>true</c> means the <paramref name="value" /> is valid</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public override bool IsValid(object? value)
        {
            // Validate our properties and create the conversion function
            SetupConversion();

            // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
            if (value == null || (value as string)?.Length == 0)
            {
                return true;
            }

            object? convertedValue;

            try
            {
                convertedValue = Conversion!(value);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }

            var min = (IComparable)Minimum;
            var max = (IComparable)Maximum;
            return min.CompareTo(convertedValue) <= 0 && max.CompareTo(convertedValue) >= 0;
        }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
        /// </summary>
        /// <remarks>This override exists to provide a formatted message describing the minimum and maximum values</remarks>
        /// <param name="name">The user-visible name to include in the formatted message.</param>
        /// <returns>A localized string describing the minimum and maximum values</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public override string FormatErrorMessage(string name)
        {
            SetupConversion();

            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Minimum, Maximum);
        }

        /// <summary>
        ///     Validates the properties of this attribute and sets up the conversion function.
        ///     This method throws exceptions if the attribute is not configured properly.
        ///     If it has once determined it is properly configured, it is a NOP.
        /// </summary>
        private void SetupConversion()
        {
            if (Conversion == null)
            {
                object minimum = Minimum;
                object maximum = Maximum;

                if (minimum == null || maximum == null)
                {
                    throw new InvalidOperationException(SR.RangeAttribute_Must_Set_Min_And_Max);
                }

                // Careful here -- OperandType could be int or double if they used the long form of the ctor.
                // But the min and max would still be strings.  Do use the type of the min/max operands to condition
                // the following code.
                Type operandType = minimum.GetType();

                if (operandType == typeof(int))
                {
                    Initialize((int)minimum, (int)maximum, v => Convert.ToInt32(v, CultureInfo.InvariantCulture));
                }
                else if (operandType == typeof(double))
                {
                    Initialize((double)minimum, (double)maximum,
                        v => Convert.ToDouble(v, CultureInfo.InvariantCulture));
                }
                else
                {
                    Type type = OperandType;
                    if (type == null)
                    {
                        throw new InvalidOperationException(SR.RangeAttribute_Must_Set_Operand_Type);
                    }
                    Type comparableType = typeof(IComparable);
                    if (!comparableType.IsAssignableFrom(type))
                    {
                        throw new InvalidOperationException(SR.Format(SR.RangeAttribute_ArbitraryTypeNotIComparable,
                                                            type.FullName,
                                                            comparableType.FullName));
                    }

                    TypeConverter converter = GetOperandTypeConverter();
                    IComparable min = (IComparable)(ParseLimitsInInvariantCulture
                        ? converter.ConvertFromInvariantString((string)minimum)!
                        : converter.ConvertFromString((string)minimum))!;
                    IComparable max = (IComparable)(ParseLimitsInInvariantCulture
                        ? converter.ConvertFromInvariantString((string)maximum)!
                        : converter.ConvertFromString((string)maximum))!;

                    Func<object, object?> conversion;
                    if (ConvertValueInInvariantCulture)
                    {
                        conversion = value => value.GetType() == type
                            ? value
                            : converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                    }
                    else
                    {
                        conversion = value => value.GetType() == type ? value : converter.ConvertFrom(value);
                    }

                    Initialize(min, max, conversion);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor that allows this code to be called is marked with RequiresUnreferencedCode.")]
        private TypeConverter GetOperandTypeConverter() =>
            TypeDescriptor.GetConverter(OperandType);
    }
}
