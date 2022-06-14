// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LongValidatorAttribute : ConfigurationValidatorAttribute
    {
        private long _max = long.MaxValue;
        private long _min = long.MinValue;

        public override ConfigurationValidatorBase ValidatorInstance => new LongValidator(_min, _max, ExcludeRange);

        public long MinValue
        {
            get { return _min; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(_max, value);
                _min = value;
            }
        }

        public long MaxValue
        {
            get { return _max; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(_min, value);
                _max = value;
            }
        }

        public bool ExcludeRange { get; set; }
    }
}
