// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;
using Mono.Linker.Tests.Cases.Reflection.Dependencies.Library;

[assembly: TypeMap<UsedTypeMapUniverse>("UnimportantString", typeof(TargetTypeUnconditional1))]
[assembly: TypeMap<UsedTypeMapUniverse>("UnimportantString", typeof(TargetTypeConditional1), typeof(TrimTarget1))]
[assembly: TypeMapAssociation<UsedTypeMapUniverse>(typeof(ProxySource1), typeof(ProxyTarget1))]

[assembly: TypeMap<UnusedTypeMapUniverse>("UnimportantString", typeof(TargetTypeUnconditional2))]
[assembly: TypeMap<UnusedTypeMapUniverse>("UnimportantString", typeof(TargetTypeConditional2), typeof(TrimTarget2))]
[assembly: TypeMapAssociation<UnusedTypeMapUniverse>(typeof(ProxySource2), typeof(ProxyTarget2))]
[assembly: TypeMapAssemblyTarget<UsedTypeMapUniverse>("library2")]

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
    public class TypeMapReferencedAssembly
    {
        public static void Main()
        {
            // Mark expected trim targets
            _ = new TrimTarget1();
            _ = new ProxySource1();
            _ = new TrimTarget2();
            _ = new ProxySource2();

            // Mark expected type map universe
            _ = TypeMapping.GetOrCreateExternalTypeMapping<UsedTypeMapUniverse>();
            _ = TypeMapping.GetOrCreateProxyTypeMapping<UsedTypeMapUniverse>();
        }
    }

    public class UsedTypeMapUniverse;
    public class UnusedTypeMapUniverse;
}

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies.Library
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
