// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class LongValidator : ConfigurationValidatorBase
    {
        private readonly ValidationFlags _flags;
        private readonly long _maxValue;
        private readonly long _minValue;
        private readonly long _resolution;

        public LongValidator(long minValue, long maxValue)
            : this(minValue, maxValue, false, 1)
        { }

        public LongValidator(long minValue, long maxValue, bool rangeIsExclusive)
            : this(minValue, maxValue, rangeIsExclusive, 1)
        { }

        public LongValidator(long minValue, long maxValue, bool rangeIsExclusive, long resolution)
        {
            if (resolution <= 0) throw new ArgumentOutOfRangeException(nameof(resolution));

            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), SR.Validator_min_greater_than_max);

            _minValue = minValue;
            _maxValue = maxValue;
            _resolution = resolution;

            _flags = rangeIsExclusive ? ValidationFlags.ExclusiveRange : ValidationFlags.None;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(long);
        }

        public override void Validate(object value)
        {
            ValidatorUtils.HelperParamValidation(value, typeof(long));

            ValidatorUtils.ValidateScalar((long)value,
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