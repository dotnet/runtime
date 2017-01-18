// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class TimeSpanValidator : ConfigurationValidatorBase
    {
        private readonly ValidationFlags _flags;
        private readonly TimeSpan _maxValue;
        private readonly TimeSpan _minValue;
        private readonly long _resolution;

        public TimeSpanValidator(TimeSpan minValue, TimeSpan maxValue)
            : this(minValue, maxValue, false, 0)
        { }

        public TimeSpanValidator(TimeSpan minValue, TimeSpan maxValue, bool rangeIsExclusive)
            : this(minValue, maxValue, rangeIsExclusive, 0)
        { }

        public TimeSpanValidator(TimeSpan minValue, TimeSpan maxValue, bool rangeIsExclusive, long resolutionInSeconds)
        {
            if (resolutionInSeconds < 0) throw new ArgumentOutOfRangeException(nameof(resolutionInSeconds));

            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), SR.Validator_min_greater_than_max);

            _minValue = minValue;
            _maxValue = maxValue;
            _resolution = resolutionInSeconds;

            _flags = rangeIsExclusive ? ValidationFlags.ExclusiveRange : ValidationFlags.None;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(TimeSpan);
        }

        public override void Validate(object value)
        {
            ValidatorUtils.HelperParamValidation(value, typeof(TimeSpan));

            ValidatorUtils.ValidateScalar((TimeSpan)value,
                _minValue,
                _maxValue,
                _resolution,
                _flags == ValidationFlags.ExclusiveRange);
        }

        private enum ValidationFlags
        {
            None = 0x0000,
            ExclusiveRange = 0x0001, // If set the value must be outside of the range instead of inside
        }
    }
}