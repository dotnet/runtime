// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyInformationalVersionAttribute : Attribute
    {
        public AssemblyInformationalVersionAttribute(string informationalVersion)
        {
            InformationalVersion = informationalVersion;
        }

        public string InformationalVersion { get; }
    }
}
