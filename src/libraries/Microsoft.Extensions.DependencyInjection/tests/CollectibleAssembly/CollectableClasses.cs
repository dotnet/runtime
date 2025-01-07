// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CollectibleAssembly
{
    public class ClassToCreate
    {
        public object ClassAsCtorArgument { get; set; }

        public ClassToCreate(ClassAsCtorArgument obj) { ClassAsCtorArgument = obj; }

        public static object Create(ServiceProvider provider)
        {
            // Both the type to create (ClassToCreate) and the ctor's arg type (ClassAsCtorArgument) are
            // located in this assembly, so both types need to be GC'd for this assembly to be collected.
            return ActivatorUtilities.CreateInstance<ClassToCreate>(provider, new ClassAsCtorArgument());
        }
    }

    public class ClassAsCtorArgument
    {
    }
}
