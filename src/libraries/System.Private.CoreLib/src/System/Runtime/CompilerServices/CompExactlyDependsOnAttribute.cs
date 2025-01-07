// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.CompilerServices
{
    // Use this attribute to indicate that a function should only be compiled into a Ready2Run
    // binary if the associated type will always have a well defined value for its IsSupported property
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
#if MONO
    [Conditional("unnecessary")] // Mono doesn't use Ready2Run so we can remove this attribute to reduce size
#endif
    internal sealed class CompExactlyDependsOnAttribute : Attribute
    {
        public CompExactlyDependsOnAttribute(Type intrinsicsTypeUsedInHelperFunction)
        {
            IntrinsicsTypeUsedInHelperFunction = intrinsicsTypeUsedInHelperFunction;
        }

        public Type IntrinsicsTypeUsedInHelperFunction { get; }
    }
}
