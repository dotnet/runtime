
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;
using Mono.Linker.Tests.Cases.Reflection.Dependencies.Library2;

// Nothing in this assembly should be referenced or rooted directly
// This should validate the if only the type map attributes are kept, the assembly is still preserved
[assembly: TypeMap<string>("UnimportantString", typeof(long))]
[assembly: TypeMap<string>("UnimportantString", typeof(uint), typeof(string))]
[assembly: TypeMapAssociation<string>(typeof(int), typeof(uint))]

[assembly: TypeMap<UnusedTypeMapUniverse2>("UnimportantString", typeof(TargetTypeUnconditional2))]
[assembly: TypeMap<UnusedTypeMapUniverse2>("UnimportantString", typeof(TargetTypeConditional2), typeof(TrimTarget2))]
[assembly: TypeMapAssociation<UnusedTypeMapUniverse2>(typeof(ProxySource2), typeof(ProxyTarget2))]

[assembly: TypeMapAssemblyTarget<string>("library")] // Circular reference

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
    public class TypeMapReferencedAssembly2
    {
    }

    public class UsedTypeMapUniverse2;
    public class UnusedTypeMapUniverse2;
}

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies.Library2
{
    public class TargetTypeUnconditional1;
    public class TargetTypeConditional1;
    public class ProxySource1;
    public class ProxyTarget1;
    public class TargetTypeUnconditional2;
    public class TargetTypeConditional2;
    public class ProxySource2;
    public class ProxyTarget2;
    public class TrimTarget1;
    public class TrimTarget2;
}
