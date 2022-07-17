// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // We might want to make this inherited someday.  But I suspect it shouldn't
    // be necessary.
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    internal sealed class TypeDependencyAttribute : Attribute
    {
        private readonly string typeName;

        public TypeDependencyAttribute(string typeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            this.typeName = typeName;
        }
    }
}
