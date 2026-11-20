// Licensed to the .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
    [SetupCompileBefore("TypeMapDefiningAssemblyLib.dll", new[] { "Dependencies/TypeMapDefiningAssemblyLib.cs" })]
    [SetupCompileBefore("TypeMapDefiningAssemblyTargetLib.dll", new[] { "Dependencies/TypeMapDefiningAssemblyTargetLib.cs" }, references: new[] { "TypeMapDefiningAssemblyLib.dll" })]
    [SetupLinkerAction("link", "TypeMapDefiningAssemblyLib")]
    [SetupLinkerAction("link", "TypeMapDefiningAssemblyTargetLib")]
    [SetupLinkerAction("link", "System.Private.CoreLib")]
    [SetupLinkerArgument("--ignore-link-attributes", "false")]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyGroup))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyTarget))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblySourceBefore))]
    [KeptMemberInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblySourceBefore), ".ctor()")]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblySourceAfter))]
    [KeptMemberInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblySourceAfter), ".ctor()")]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyProxyBefore))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyProxyAfter))]
    [RemovedTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyUnusedSource))]
    [RemovedTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyUnusedTarget))]
    [RemovedTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyUnusedGroup))]
    [RemovedAttributeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(TypeMapAssemblyTargetAttribute<DefiningAssemblyUnusedGroup>))]
    [KeptAttributeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(TypeMapAttribute<DefiningAssemblyGroup>))]
    [KeptAttributeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(TypeMapAssociationAttribute<DefiningAssemblyGroup>))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyTargetLib.dll", typeof(DefiningAssemblyTransitiveTarget))]
    [KeptAttributeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(TypeMapAssemblyTargetAttribute<DefiningAssemblyGroup>))]
    [KeptAttributeInAssembly("TypeMapDefiningAssemblyTargetLib.dll", typeof(TypeMapAssemblyTargetAttribute<DefiningAssemblyGroup>))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyExternalOnlyGroup))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyExternalOnlyTarget))]
    [RemovedTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyUnusedProxy))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyProxyOnlyGroup))]
    [KeptTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyProxyOnlyTarget))]
    [RemovedTypeInAssembly("TypeMapDefiningAssemblyLib.dll", typeof(DefiningAssemblyUnusedExternalTarget))]
    class TypeMapDefiningAssembly
    {
        public static void Main()
        {
            Console.WriteLine(new DefiningAssemblySourceBefore());
            var externalMap = TypeMapping.GetOrCreateExternalTypeMapping<DefiningAssemblyGroup>();
            var proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<DefiningAssemblyGroup>();
            Console.WriteLine(new DefiningAssemblySourceAfter());

            Console.WriteLine(externalMap["unconditional"]);
            Console.WriteLine(externalMap["before"]);
            Console.WriteLine(externalMap["after"]);
            Console.WriteLine(externalMap["transitive"]);
            Console.WriteLine(proxyMap[typeof(DefiningAssemblySourceBefore)]);
            Console.WriteLine(proxyMap[typeof(DefiningAssemblySourceAfter)]);
            Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<DefiningAssemblyExternalOnlyGroup>()["external"]);
            Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<DefiningAssemblyProxyOnlyGroup>()[typeof(DefiningAssemblySourceBefore)]);
        }
    }
}
