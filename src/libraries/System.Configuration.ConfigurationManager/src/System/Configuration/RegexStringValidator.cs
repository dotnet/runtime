// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace System.Configuration
{
    public class RegexStringValidator : ConfigurationValidatorBase
    {
        private readonly string _expression;
        private readonly Regex _regex;

        public RegexStringValidator(string regex)
        {
            if (string.IsNullOrEmpty(regex)) throw ExceptionUtil.ParameterNullOrEmpty(nameof(regex));

            _expression = regex;
            _regex = new Regex(regex, RegexOptions.Compiled);
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(string);
        }

        public override void Validate(object value)
        {
            ValidatorUtils.HelperParamValidation(value, typeof(string));

            if (value is string s && !_regex.IsMatch(s))
            {
                throw new ArgumentException(SR.Format(SR.Regex_validator_error, _expression));
            }
        }
    }
}
