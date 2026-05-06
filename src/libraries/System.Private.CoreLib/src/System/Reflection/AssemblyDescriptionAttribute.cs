// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyDescriptionAttribute : Attribute
    {
        public AssemblyDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }
    }
}
