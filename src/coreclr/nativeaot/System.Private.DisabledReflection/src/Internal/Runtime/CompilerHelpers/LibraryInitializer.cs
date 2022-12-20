// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Reflection;
using Internal.Reflection.Augments;
using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerHelpers
{
    public class LibraryInitializer
    {
        public static void InitializeLibrary()
        {
            RuntimeAugments.Initialize(new ReflectionExecutionDomainCallbacksImplementation());
            ReflectionAugments.Initialize(new ReflectionCoreCallbacksImplementation());
        }
    }
}
