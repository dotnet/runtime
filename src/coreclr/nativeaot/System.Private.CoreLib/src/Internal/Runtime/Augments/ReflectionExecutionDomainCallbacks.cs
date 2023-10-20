// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Internal.Runtime.Augments
//-------------------------------------------------
//  Why does this exist?:
//    Internal.Reflection.Execution cannot physically live in System.Private.CoreLib.dll
//    as it has a dependency on System.Reflection.Metadata. It's inherently
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
using System.Reflection;
using System.Reflection.Runtime.TypeInfos;

namespace Internal.Runtime.Augments
{
    [CLSCompliant(false)]
    public abstract class ReflectionExecutionDomainCallbacks
    {
        public abstract IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle runtimeTypeHandle);

        // Flotsam and jetsam.
        public abstract Exception CreateMissingMetadataException(Type typeWithMissingMetadata);

        public abstract MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress);
        public abstract Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle);

        public abstract MethodInfo GetDelegateMethod(Delegate del);

        public abstract Exception GetExceptionForHR(int hr);
    }
}
