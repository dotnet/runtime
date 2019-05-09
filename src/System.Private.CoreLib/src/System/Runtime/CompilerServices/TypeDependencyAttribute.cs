// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // We might want to make this inherited someday.  But I suspect it shouldn't
    // be necessary.
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    internal sealed class TypeDependencyAttribute : Attribute
    {
        private string typeName;

        public TypeDependencyAttribute(string typeName)
        {
            if (typeName == null) throw new ArgumentNullException(nameof(typeName));
            this.typeName = typeName;
        }
    }
}



