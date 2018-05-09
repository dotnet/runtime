// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class ObfuscateAssemblyAttribute : Attribute
    {
        public ObfuscateAssemblyAttribute(bool assemblyIsPrivate)
        {
            AssemblyIsPrivate = assemblyIsPrivate;
        }

        public bool AssemblyIsPrivate { get; }
        public bool StripAfterObfuscation { get; set; } = true;
    }
}

