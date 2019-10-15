// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SwitchLevelAttribute : Attribute
    {
        private Type _type;

        public SwitchLevelAttribute(Type switchLevelType)
        {
            SwitchLevelType = switchLevelType;
        }

        public Type SwitchLevelType
        {
            get { return _type; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _type = value;
            }
        }
    }
}
