// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class StringValidator : ConfigurationValidatorBase
    {
        private readonly string _invalidChars;
        private readonly int _maxLength;
        private readonly int _minLength;

        public StringValidator(int minLength)
            : this(minLength, int.MaxValue, null)
        { }

        public StringValidator(int minLength, int maxLength)
            : this(minLength, maxLength, null)
        { }

        public StringValidator(int minLength, int maxLength, string invalidCharacters)
        {
            _minLength = minLength;
            _maxLength = maxLength;
            _invalidChars = invalidCharacters;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(string);
        }

        public override void Validate(object value)
        {
            ValidatorUtils.HelperParamValidation(value, typeof(string));

            string data = value as string;
            int len = data?.Length ?? 0;

            if (len < _minLength)
                throw new ArgumentException(string.Format(SR.Validator_string_min_length, _minLength));
            if (len > _maxLength)
                throw new ArgumentException(string.Format(SR.Validator_string_max_length, _maxLength));

            // Check if the string contains any invalid characters
            if ((len > 0) && !string.IsNullOrEmpty(_invalidChars))
            {
                char[] array = new char[_invalidChars.Length];

                _invalidChars.CopyTo(0, array, 0, _invalidChars.Length);

                if (data.IndexOfAny(array) != -1)
                    throw new ArgumentException(string.Format(SR.Validator_string_invalid_chars, _invalidChars));
            }
        }
    }
}