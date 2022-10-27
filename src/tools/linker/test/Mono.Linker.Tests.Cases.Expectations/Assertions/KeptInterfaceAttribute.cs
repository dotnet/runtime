// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class KeptInterfaceAttribute : KeptAttribute
    {

        public KeptInterfaceAttribute(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
        }

        public KeptInterfaceAttribute(Type interfaceType, params object[] typeArguments)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (typeArguments == null)
                throw new ArgumentNullException(nameof(typeArguments));
        }
    }
}
