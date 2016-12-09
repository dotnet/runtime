// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class IntegerValidator : ConfigurationValidatorBase
    {
        private readonly ValidationFlags _flags;
        private readonly int _maxValue;
        private readonly int _minValue;
        private readonly int _resolution;

        public IntegerValidator(int minValue, int maxValue) :
            this(minValue, maxValue, false, 1)
        { }

        public IntegerValidator(int minValue, int maxValue, bool rangeIsExclusive) :
            this(minValue, maxValue, rangeIsExclusive, 1)
        { }

        public IntegerValidator(int minValue, int maxValue, bool rangeIsExclusive, int resolution)
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
            return type == typeof(int);
        }

        public override void Validate(object value)
        {
            ValidatorUtils.HelperParamValidation(value, typeof(int));

            ValidatorUtils.ValidateScalar((int)value,
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