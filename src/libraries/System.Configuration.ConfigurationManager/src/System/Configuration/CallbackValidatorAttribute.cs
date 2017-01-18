// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CallbackValidatorAttribute : ConfigurationValidatorAttribute
    {
        private ValidatorCallback _callbackMethod;
        private string _callbackMethodName = string.Empty;
        private Type _type;

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get
            {
                if (_callbackMethod == null)
                {
                    if (_type == null) throw new ArgumentNullException(nameof(Type));

                    if (!string.IsNullOrEmpty(_callbackMethodName))
                    {
                        MethodInfo methodInfo = _type.GetMethod(_callbackMethodName, BindingFlags.Public | BindingFlags.Static);

                        if (methodInfo != null)
                        {
                            ParameterInfo[] parameters = methodInfo.GetParameters();
                            if ((parameters.Length == 1) && (parameters[0].ParameterType == typeof(object)))
                            {
                                _callbackMethod = (ValidatorCallback)Delegate.CreateDelegate(typeof(ValidatorCallback), methodInfo);
                            }
                        }
                    }
                }

                if (_callbackMethod == null)
                    throw new ArgumentException(string.Format(SR.Validator_method_not_found, _callbackMethodName));

                return new CallbackValidator(_callbackMethod);
            }
        }

        public Type Type
        {
            get { return _type; }
            set
            {
                _type = value;
                _callbackMethod = null;
            }
        }

        public string CallbackMethodName
        {
            get { return _callbackMethodName; }
            set
            {
                _callbackMethodName = value;
                _callbackMethod = null;
            }
        }
    }
}