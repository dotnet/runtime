// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(string conditionString)
        {
            _conditionString = conditionString;
        }

        public string ConditionString
        {
            get
            {
                return _conditionString;
            }
        }

        private string _conditionString;
    }
}
