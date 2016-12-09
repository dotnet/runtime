// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StringValidatorAttribute : ConfigurationValidatorAttribute
    {
        private int _maxLength = int.MaxValue;
        private int _minLength;

        public override ConfigurationValidatorBase ValidatorInstance
            => new StringValidator(_minLength, _maxLength, InvalidCharacters);

        public int MinLength
        {
            get { return _minLength; }
            set
            {
                if (_maxLength < value)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                _minLength = value;
            }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set
            {
                if (_minLength > value)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Validator_min_greater_than_max);

                _maxLength = value;
            }
        }

        public string InvalidCharacters { get; set; }
    }
}