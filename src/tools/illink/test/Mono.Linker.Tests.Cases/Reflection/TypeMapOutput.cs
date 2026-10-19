// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

[assembly: TypeMap<TypeMapOutput.UsedUniverse>("example/Target", typeof(TypeMapOutput.Target))]
[assembly: TypeMap<TypeMapOutput.UsedUniverse>("example/\u0130<&\"Target", typeof(TypeMapOutput.Target[]))]
[assembly: TypeMap<TypeMapOutput.UsedUniverse>("trimmed", typeof(TypeMapOutput.UnusedTarget), typeof(TypeMapOutput.UnusedSource))]
[assembly: TypeMapAssociation<TypeMapOutput.UsedUniverse>(typeof(TypeMapOutput.Source), typeof(TypeMapOutput.Proxy<int>))]
[assembly: TypeMapAssociation<TypeMapOutput.UsedUniverse>(typeof(TypeMapOutput.UnusedSource), typeof(TypeMapOutput.UnusedTarget))]
[assembly: TypeMap<TypeMapOutput.EmptyUniverse>("trimmed", typeof(TypeMapOutput.UnusedTarget), typeof(TypeMapOutput.UnusedSource))]
[assembly: TypeMap<TypeMapOutput.ExternalOnlyUniverse>("external", typeof(TypeMapOutput.Target))]
[assembly: TypeMapAssociation<TypeMapOutput.ExternalOnlyUniverse>(typeof(TypeMapOutput.Source), typeof(TypeMapOutput.Proxy<int>))]
[assembly: TypeMap<TypeMapOutput.ProxyOnlyUniverse>("external", typeof(TypeMapOutput.Target))]
[assembly: TypeMapAssociation<TypeMapOutput.ProxyOnlyUniverse>(typeof(TypeMapOutput.Source), typeof(TypeMapOutput.Proxy<int>))]
[assembly: TypeMap<TypeMapOutput.UnusedUniverse>("unused", typeof(TypeMapOutput.UnusedTarget))]
[assembly: TypeMapAssemblyTarget<TypeMapEntryAssemblyGroup>("TypeMapEntryAssemblyLib")]

namespace Mono.Linker.Tests.Cases.Reflection;

[SetupCompileBefore("TypeMapEntryAssemblyLib.dll", new[] { "Dependencies/TypeMapEntryAssemblyLib.cs" })]
[SetupLinkerAction("link", "System.Private.CoreLib")]
[SetupLinkerArgument("--ignore-link-attributes", "false")]
[SetupLinkerArgument("--output-typemaps", FileName)]
#if NATIVEAOT
[SetupLinkerArgument("--warnaserror")]
[SetupLinkerArgument("--feature", "System.StartupHookProvider.IsSupported", "false")]
#endif
[SkipKeptItemsValidation]
[ExpectedNoWarnings]
[ExpectedXmlElement(FileName, "/typemaps")]
[ExpectedXmlElement(FileName, "/typemaps[@*]", 0)]
[ExpectedXmlElement(FileName, "/typemaps/group", 5)]
[ExpectedXmlElement(FileName, "/typemaps/group[count(@*) != 1 or not(@type)]", 0)]
[ExpectedXmlElement(FileName, "/typemaps/group[count(external) > 1 or count(proxy) > 1]", 0)]
[ExpectedXmlElement(FileName, "/typemaps/group/*[not(self::external or self::proxy) or @*]", 0)]
[ExpectedXmlElement(FileName, "/typemaps/group/*/*[not(self::entry)]", 0)]
[ExpectedXmlElement(FileName, "/typemaps/group/*/entry", 9)]
[ExpectedXmlElement(FileName, "/typemaps/group/*/entry[count(@*) != 2 or not(@key) or not(@value)]", 0)]
// The producer formatters differ in whitespace after assembly-name separators.
[ExpectedXmlElement(FileName, UsedGroup + "/external/entry", 2)]
[ExpectedXmlElement(FileName, UsedGroup + "/external/entry[@key='example/Target' and translate(@value, ' ', '')='" + TypePrefix + "Target,test']")]
[ExpectedXmlElement(FileName, UsedGroup + "/external/entry[@key='example/\u0130<&\"Target' and translate(@value, ' ', '')='" + TypePrefix + "Target[],test']")]
[ExpectedXmlElement(FileName, UsedGroup + "/proxy/" + ProxyEntry)]
[ExpectedXmlElement(FileName, EmptyGroup + "/external[not(entry)]")]
[ExpectedXmlElement(FileName, EmptyGroup + "/proxy", 0)]
[ExpectedXmlElement(FileName, ExternalOnlyGroup + "/external/entry[@key='external' and translate(@value, ' ', '')='" + TypePrefix + "Target,test']")]
[ExpectedXmlElement(FileName, ExternalOnlyGroup + "/proxy", 0)]
[ExpectedXmlElement(FileName, ProxyOnlyGroup + "/external", 0)]
[ExpectedXmlElement(FileName, ProxyOnlyGroup + "/proxy/" + ProxyEntry)]
[ExpectedXmlElement(FileName, DependencyGroup + "/external/entry[@key='entry_type1' and translate(@value, ' ', '')='" + DependencyPrefix + "EntryType1,TypeMapEntryAssemblyLib']")]
[ExpectedXmlElement(FileName, DependencyGroup + "/external/entry[@key='entry_type2' and translate(@value, ' ', '')='" + DependencyPrefix + "EntryType2,TypeMapEntryAssemblyLib']")]
[ExpectedXmlElement(FileName, DependencyGroup + "/proxy/entry[translate(@key, ' ', '')='" + DependencyPrefix + "EntrySource1,TypeMapEntryAssemblyLib' and translate(@value, ' ', '')='" + DependencyPrefix + "EntryProxy1,TypeMapEntryAssemblyLib']")]
[ExpectedXmlElement(FileName, DependencyGroup + "/proxy/entry[translate(@key, ' ', '')='" + DependencyPrefix + "EntrySource2,TypeMapEntryAssemblyLib' and translate(@value, ' ', '')='" + DependencyPrefix + "EntryProxy2,TypeMapEntryAssemblyLib']")]
class TypeMapOutput
{
    private const string FileName = "type maps.xml";
    private const string TypePrefix = "Mono.Linker.Tests.Cases.Reflection.TypeMapOutput+";
    private const string DependencyPrefix = "Mono.Linker.Tests.Cases.Reflection.Dependencies.";
    private const string UsedGroup = "/typemaps/group[translate(@type, ' ', '')='" + TypePrefix + "UsedUniverse,test']";
    private const string EmptyGroup = "/typemaps/group[translate(@type, ' ', '')='" + TypePrefix + "EmptyUniverse,test']";
    private const string ExternalOnlyGroup = "/typemaps/group[translate(@type, ' ', '')='" + TypePrefix + "ExternalOnlyUniverse,test']";
    private const string ProxyOnlyGroup = "/typemaps/group[translate(@type, ' ', '')='" + TypePrefix + "ProxyOnlyUniverse,test']";
    private const string DependencyGroup = "/typemaps/group[translate(@type, ' ', '')='" + DependencyPrefix + "TypeMapEntryAssemblyGroup,TypeMapEntryAssemblyLib']";
    private const string ProxyEntry = "entry[translate(@key, ' ', '')='" + TypePrefix + "Source,test' and translate(@value, ' ', '')='" + TypePrefix + "Proxy`1[[System.Int32,System.Private.CoreLib]],test']";

    public static void Main()
    {
        Console.WriteLine(new Source());
        Console.WriteLine(new EntrySource1());
        Console.WriteLine(new EntrySource2());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<UsedUniverse>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<UsedUniverse>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<EmptyUniverse>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<ExternalOnlyUniverse>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<ProxyOnlyUniverse>());
        Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<TypeMapEntryAssemblyGroup>());
        Console.WriteLine(TypeMapping.GetOrCreateProxyTypeMapping<TypeMapEntryAssemblyGroup>());
    }

    public class UsedUniverse;
    public class EmptyUniverse;
    public class ExternalOnlyUniverse;
    public class ProxyOnlyUniverse;
    public class UnusedUniverse;
    public class Target;
    public class Source;
    public class Proxy<T>;
    public class UnusedSource;
    public class UnusedTarget;
}
