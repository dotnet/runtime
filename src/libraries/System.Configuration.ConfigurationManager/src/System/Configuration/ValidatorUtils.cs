// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Configuration
{
    internal static class ValidatorUtils
    {
        public static void HelperParamValidation(object value, Type allowedType)
        {
            if (value == null) return;

            if (value.GetType() != allowedType)
                throw new ArgumentException(SR.Validator_value_type_invalid, string.Empty);
        }

        public static void ValidateScalar<T>(T value, T min, T max, T resolution, bool exclusiveRange)
            where T : IComparable<T>
        {
            ValidateRangeImpl(value, min, max, exclusiveRange);

            // Validate the resolution
            ValidateResolution(resolution.ToString(), Convert.ToInt64(value, CultureInfo.InvariantCulture),
                Convert.ToInt64(resolution, CultureInfo.InvariantCulture));
        }

        private static void ValidateRangeImpl<T>(T value, T min, T max, bool exclusiveRange) where T : IComparable<T>
        {
            IComparable<T> itfValue = value;

            bool valueIsInRange = itfValue.CompareTo(min) >= 0;

            if (valueIsInRange && (itfValue.CompareTo(max) > 0))
            {
                // TRUE: value > max
                valueIsInRange = false;
            }

            if (valueIsInRange ^ exclusiveRange) return;

            // Throw range validation error
            string error;

            if (min.Equals(max))
            {
                // First group of errors - the min and max range are the same. i.e. the valid value must be the same/equal to the min(max)
                error = exclusiveRange
                    ? SR.Validation_scalar_range_violation_not_different
                    : SR.Validation_scalar_range_violation_not_equal;
            }
            else
            {
                // Second group of errors: min != max. I.e. its a range
                error = exclusiveRange
                    ? SR.Validation_scalar_range_violation_not_outside_range
                    : SR.Validation_scalar_range_violation_not_in_range;
            }

            throw new ArgumentException(SR.Format(CultureInfo.InvariantCulture,
                error,
                min.ToString(),
                max.ToString()));
        }

        private static void ValidateResolution(string resolutionAsString, long value, long resolution)
        {
            Debug.Assert(resolution > 0, "resolution > 0");

            if (value % resolution != 0)
                throw new ArgumentException(SR.Format(SR.Validator_scalar_resolution_violation, resolutionAsString));
        }

        public static void ValidateScalar(TimeSpan value, TimeSpan min, TimeSpan max, long resolutionInSeconds,
            bool exclusiveRange)
        {
            ValidateRangeImpl(value, min, max, exclusiveRange);

            // Validate the resolution
            if (resolutionInSeconds > 0)
            {
                ValidateResolution(TimeSpan.FromSeconds(resolutionInSeconds).ToString(), value.Ticks,
                    resolutionInSeconds * TimeSpan.TicksPerSecond);
            }
        }
    }
}
