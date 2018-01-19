// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyDefaultAliasAttribute : Attribute
    {
        public AssemblyDefaultAliasAttribute(string defaultAlias)
        {
            DefaultAlias = defaultAlias;
        }

        public string DefaultAlias { get; }
    }
}

