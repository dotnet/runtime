// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IntegerValidatorAttribute : ConfigurationValidatorAttribute
    {
        private int _max = int.MaxValue;
        private int _min = int.MinValue;

        public override ConfigurationValidatorBase ValidatorInstance => new IntegerValidator(_min, _max, ExcludeRange);

        public int MinValue
        {
            get { return _min; }
            set
            {
                if (_max < value)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                _min = value;
            }
        }

        public int MaxValue
        {
            get { return _max; }
            set
            {
                if (_min > value)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                _max = value;
            }
        }

        public bool ExcludeRange { get; set; } = false;
    }
}