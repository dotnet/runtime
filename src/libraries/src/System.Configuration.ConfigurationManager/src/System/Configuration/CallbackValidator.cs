// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class CallbackValidator : ConfigurationValidatorBase
    {
        private readonly ValidatorCallback _callback;
        private readonly Type _type;

        public CallbackValidator(Type type, ValidatorCallback callback) : this(callback)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            _type = type;
        }

        // Do not check for null type here to handle the callback attribute case
        internal CallbackValidator(ValidatorCallback callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            _type = null;
            _callback = callback;
        }

        public override bool CanValidate(Type type)
        {
            return (type == _type) || (_type == null);
        }

        public override void Validate(object value)
        {
            _callback(value);
        }
    }
}