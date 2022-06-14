// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                ArgumentOutOfRangeException.ThrowIfLessThan(_maxLength, value);

                _minLength = value;
            }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(_minLength, value);

                _maxLength = value;
            }
        }

        public string InvalidCharacters { get; set; }
    }
}
