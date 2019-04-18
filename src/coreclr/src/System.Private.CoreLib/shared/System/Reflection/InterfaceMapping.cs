// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable // TODO-NULLABLE: Re-review
namespace System.Reflection
{
    public struct InterfaceMapping
    {
        public Type TargetType;               // The type implementing the interface
        public Type InterfaceType;            // The type representing the interface
        public MethodInfo[] TargetMethods;    // The methods implementing the interface
        public MethodInfo[] InterfaceMethods; // The methods defined on the interface
    }
}
