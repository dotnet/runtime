// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TimeSpanValidatorAttribute : ConfigurationValidatorAttribute
    {
        public const string TimeSpanMinValue = "-10675199.02:48:05.4775808";
        public const string TimeSpanMaxValue = "10675199.02:48:05.4775807";

        public override ConfigurationValidatorBase ValidatorInstance
            => new TimeSpanValidator(MinValue, MaxValue, ExcludeRange);

        public TimeSpan MinValue { get; private set; } = TimeSpan.MinValue;

        public TimeSpan MaxValue { get; private set; } = TimeSpan.MaxValue;

        public string MinValueString
        {
            get { return MinValue.ToString(); }
            set
            {
                TimeSpan timeValue = TimeSpan.Parse(value, CultureInfo.InvariantCulture);

                if (MaxValue < timeValue)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                MinValue = timeValue;
            }
        }

        public string MaxValueString
        {
            get { return MaxValue.ToString(); }
            set
            {
                TimeSpan timeValue = TimeSpan.Parse(value, CultureInfo.InvariantCulture);

                if (MinValue > timeValue)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                MaxValue = timeValue;
            }
        }

        public bool ExcludeRange { get; set; } = false;
    }
}