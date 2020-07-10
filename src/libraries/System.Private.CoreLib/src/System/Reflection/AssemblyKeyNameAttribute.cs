// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyKeyNameAttribute : Attribute
    {
        public AssemblyKeyNameAttribute(string keyName)
        {
            KeyName = keyName;
        }

        public string KeyName { get; }
    }
}
