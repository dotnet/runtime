// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyCultureAttribute : Attribute
    {
        public AssemblyCultureAttribute(string culture)
        {
            Culture = culture;
        }

        public string Culture { get; }
    }
}
