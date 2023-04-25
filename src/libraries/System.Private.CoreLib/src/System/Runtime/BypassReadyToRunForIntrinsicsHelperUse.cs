// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    // Use this attribute to indicate that a function should only be compiled into a Ready2Run
    // binary if the associated type will always have a well defined value for its IsSupported property
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    internal sealed class BypassReadyToRunForIntrinsicsHelperUseAttribute : Attribute
    {
        public BypassReadyToRunForIntrinsicsHelperUseAttribute(Type intrinsicsTypeUsedInHelperFunction)
        {
            IntrinsicsTypeUsedInHelperFunction = intrinsicsTypeUsedInHelperFunction;
        }

        public Type IntrinsicsTypeUsedInHelperFunction { get; }
    }
}
