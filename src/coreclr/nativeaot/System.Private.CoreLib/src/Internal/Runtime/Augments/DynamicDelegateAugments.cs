// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Internal.Runtime.Augments
//-------------------------------------------------
//  Why does this exist?:
//    Reflection.Execution cannot physically live in System.Private.CoreLib.dll
//    as it has a dependency on System.Reflection.Metadata. Its inherently
//    low-level nature means, however, it is closely tied to System.Private.CoreLib.dll.
//    This contract provides the two-communication between those two .dll's.
//
//
//  Implemented by:
//    System.Private.CoreLib.dll
//
//  Consumed by:
//    Reflection.Execution.dll

using System;

namespace Internal.Runtime.Augments
{
    internal static class DynamicDelegateAugments
    {
        //
        // Helper to create a interpreted delegate for LINQ and DLR expression trees
        //
        public static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object?[], object?> invoker)
        {
            return Delegate.CreateObjectArrayDelegate(delegateType, invoker);
        }
    }
}
