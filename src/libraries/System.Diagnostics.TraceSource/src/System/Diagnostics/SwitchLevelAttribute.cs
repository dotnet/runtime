// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

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
            [MemberNotNull(nameof(_type))]
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _type = value;
            }
        }
    }
}
