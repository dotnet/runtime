// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AccessedThroughPropertyAttribute : Attribute
    {
        public AccessedThroughPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
