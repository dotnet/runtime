// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection
{
    public struct InterfaceMapping
    {
        internal InterfaceMapping(MethodInfo[] interfaceMethods, MethodInfo[] targetMethods, Type interfaceType, Type targetType)
        {
            this.InterfaceMethods = interfaceMethods;
            this.TargetMethods = targetMethods;
            this.InterfaceType = interfaceType;
            this.TargetType = targetType;
        }

        public MethodInfo[] InterfaceMethods;
        public MethodInfo[] TargetMethods;
        public Type InterfaceType;
        public Type TargetType;
    }
}


