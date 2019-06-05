// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    internal class EmptyCAHolder : ICustomAttributeProvider
    {
        internal EmptyCAHolder() { }

        object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();

        object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit) => Array.Empty<object>();

        bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit) => false;
    }
}
