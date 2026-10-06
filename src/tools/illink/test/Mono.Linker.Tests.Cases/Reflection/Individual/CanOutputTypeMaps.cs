// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;
using Mono.Linker.Tests.Cases.Reflection.Individual;

[assembly: TypeMap<CanOutputTypeMaps.Group>("keep<&\"\t\r\n", typeof(CanOutputTypeMaps.Nested.Target))]
[assembly: TypeMap<CanOutputTypeMaps.Group>("conditional", typeof(CanOutputTypeMaps.Nested.Target), typeof(CanOutputTypeMaps.Source))]
[assembly: TypeMap<CanOutputTypeMaps.Group>("trimmed", typeof(CanOutputTypeMaps.Unused), typeof(CanOutputTypeMaps.Unused))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.Group>(typeof(CanOutputTypeMaps.Source), typeof(CanOutputTypeMaps.Nested.Generic<int[]>))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.Group>(typeof(CanOutputTypeMaps.Unused), typeof(CanOutputTypeMaps.Unused))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("array", typeof(int[,]))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("byref", typeof(int))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("example/Outer$Inner[0]", typeof(CanOutputTypeMaps.Nested.Target))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("[Lexample/Outer$Inner;", typeof(CanOutputTypeMaps.Nested.Target[]))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("", typeof(CanOutputTypeMaps.Nested.Generic<int[]>))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("pointer", typeof(int*))]
[assembly: TypeMap<CanOutputTypeMaps.ExternalOnly>("unicode/\u0130\u4e2d\U0001f600", typeof(CanOutputTypeMaps.Nested.Target))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.ExternalOnly>(typeof(CanOutputTypeMaps.Source), typeof(object))]
[assembly: TypeMap<CanOutputTypeMaps.ProxyOnly>("unrequested", typeof(object))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.ProxyOnly>(typeof(CanOutputTypeMaps.Source), typeof(object))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.ProxyOnly>(typeof(CanOutputTypeMaps.Nested.Generic<int[]>), typeof(CanOutputTypeMaps.Nested.Target))]
[assembly: TypeMap<CanOutputTypeMaps.GenericUniverse<int[]>>("generic-universe", typeof(CanOutputTypeMaps.Nested.Generic<string[,]>))]
[assembly: TypeMap<CanOutputTypeMaps.Empty>("trimmed", typeof(CanOutputTypeMaps.Unused), typeof(CanOutputTypeMaps.Unused))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.Empty>(typeof(CanOutputTypeMaps.Unused), typeof(object))]
[assembly: TypeMap<CanOutputTypeMaps.Unrequested>("unrequested", typeof(object))]
[assembly: TypeMap<CanOutputTypeMaps.Invalid>("duplicate", typeof(object))]
[assembly: TypeMap<CanOutputTypeMaps.Invalid>("duplicate", typeof(string))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.Invalid>(typeof(CanOutputTypeMaps.Source), typeof(object))]
[assembly: TypeMapAssociation<CanOutputTypeMaps.Invalid>(typeof(CanOutputTypeMaps.Source), typeof(string))]
[assembly: TypeMapAssemblyTarget<TypeMapOutputGroup>("Maps+Library")]

namespace Mono.Linker.Tests.Cases.Reflection.Individual;

[SetupCompileArgument("/unsafe")]
[SetupLinkerAction("link", "System.Private.CoreLib")]
[SetupLinkerArgument("--ignore-link-attributes", "false")]
[SetupCompileBefore("Maps+Library.dll", new[] { "../Dependencies/TypeMapOutputLibrary.cs" })]
[SetupLinkerAction("copy", "Maps+Library")]
public class CanOutputTypeMaps
{
    public static void Main()
    {
        Console.WriteLine(new Source());
        Console.WriteLine(new Nested.Generic<int[]>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<Group>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<Group>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<ExternalOnly>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<ProxyOnly>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<Empty>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<Empty>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<Invalid>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<Invalid>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<TypeMapOutputGroup>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<GenericUniverse<int[]>>());
    }

    public class Group;
    public class ExternalOnly;
    public class ProxyOnly;
    public class Empty;
    public class Unrequested;
    public class Invalid;
    public class GenericUniverse<T>;
    public class Source;
    public class Unused;

    public class Nested
    {
        public class Target;
        public class Generic<T>;
    }
}
