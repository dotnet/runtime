// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Internal.Runtime.Augments;

namespace Internal.Reflection
{
    internal class ReflectionExecutionDomainCallbacksImplementation : ReflectionExecutionDomainCallbacks
    {
        public override IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle runtimeTypeHandle) => throw new NotSupportedException(SR.Reflection_Disabled);
        public override MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress) => null;
        public override Assembly GetAssemblyForHandle(RuntimeTypeHandle typeHandle) => new RuntimeAssemblyInfo(typeHandle);
        public override MethodInfo GetDelegateMethod(Delegate del) => throw new NotSupportedException(SR.Reflection_Disabled);
    }
}
