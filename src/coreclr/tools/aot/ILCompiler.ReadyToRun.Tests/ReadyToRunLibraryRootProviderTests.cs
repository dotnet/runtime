// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using crossgen2::ILCompiler;
using crossgen2::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunLibraryRootProviderTests
{
    [Theory]
    [InlineData("System.Numerics", "Vector`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector64`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector128`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector256`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector512`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Scalar`1", false)]
    [InlineData("System.Runtime.Intrinsics", "ISimdVector`2", false)]
    [InlineData("System.Numerics", "IAdditionOperators`3", false)]
    [InlineData("System.Numerics", "IBinaryInteger`1", false)]
    [InlineData("System.Numerics", "IBinaryNumber`1", false)]
    [InlineData("System.Numerics", "IDecrementOperators`1", false)]
    [InlineData("System.Numerics", "IDivisionOperators`3", false)]
    [InlineData("System.Numerics", "IExponentialFunctions`1", false)]
    [InlineData("System.Numerics", "IFloatingPoint`1", false)]
    [InlineData("System.Numerics", "IFloatingPointIeee754`1", false)]
    [InlineData("System.Numerics", "IIncrementOperators`1", false)]
    [InlineData("System.Numerics", "ILogarithmicFunctions`1", false)]
    [InlineData("System.Numerics", "IMultiplyOperators`3", false)]
    [InlineData("System.Numerics", "INumber`1", false)]
    [InlineData("System.Numerics", "INumberBase`1", false)]
    [InlineData("System.Numerics", "ISubtractionOperators`3", false)]
    [InlineData("System.Numerics", "ITrigonometricFunctions`1", false)]
    [InlineData("System.Numerics", "IUnaryNegationOperators`2", false)]
    [InlineData("System.Numerics", "TotalOrderIeee754Comparer`1", false)]
    [InlineData("System.Collections.Generic", "List`1", true)]
    [InlineData("System", "Span`1", true)]
    public void AutomaticTypeInstantiation(string namespaceName, string typeName, bool expected)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(typeName));

        InstantiatedType instantiated = ReadyToRunLibraryRootProvider.InstantiateIfPossible(type);
        Assert.Equal(expected, instantiated is not null);
        if (instantiated is not null)
        {
            Assert.Same(context.CanonType, instantiated.Instantiation[0]);
        }

        TypeDesc[] concreteArguments = new TypeDesc[type.Instantiation.Length];
        Array.Fill(concreteArguments, context.GetWellKnownType(WellKnownType.Int32));
        InstantiatedType concrete = type.MakeInstantiatedType(concreteArguments);
        Assert.Same(context.GetWellKnownType(WellKnownType.Int32), concrete.Instantiation[0]);
    }

    [Theory]
    [InlineData("System.Numerics", "Vector", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector64", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector128", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector256", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector512", false)]
    [InlineData("System", "Array", true)]
    public void AutomaticMethodInstantiation(string namespaceName, string typeName, bool expected)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(typeName));
        int methodsChecked = 0;
        int methodsInstantiated = 0;
        foreach (MethodDesc method in type.GetMethods())
        {
            if (!method.HasInstantiation)
                continue;

            MethodDesc instantiated = ReadyToRunLibraryRootProvider.InstantiateIfPossible(method);
            if (!expected)
            {
                Assert.Null(instantiated);
            }
            else if (instantiated is not null)
            {
                methodsInstantiated++;
                foreach (TypeDesc argument in instantiated.Instantiation)
                    Assert.Same(context.CanonType, argument);
            }

            methodsChecked++;
        }

        Assert.True(methodsChecked > 0);
        Assert.Equal(expected, methodsInstantiated > 0);
        if (!expected)
        {
            Assert.True(CheckNestedTypes(type) > 0);
        }
    }

    [Theory]
    [InlineData("System", "Byte")]
    [InlineData("System", "Char")]
    [InlineData("System", "Decimal")]
    [InlineData("System", "Double")]
    [InlineData("System", "Half")]
    [InlineData("System", "Int16")]
    [InlineData("System", "Int32")]
    [InlineData("System", "Int64")]
    [InlineData("System", "Int128")]
    [InlineData("System", "IntPtr")]
    [InlineData("System", "SByte")]
    [InlineData("System", "Single")]
    [InlineData("System", "UInt16")]
    [InlineData("System", "UInt32")]
    [InlineData("System", "UInt64")]
    [InlineData("System", "UInt128")]
    [InlineData("System", "UIntPtr")]
    [InlineData("System.Numerics", "BFloat16")]
    [InlineData("System.Numerics", "Decimal32")]
    [InlineData("System.Numerics", "Decimal64")]
    [InlineData("System.Numerics", "Decimal128")]
    [InlineData("System.Runtime.InteropServices", "NFloat")]
    public void ScalarConversionsAreNotAutomaticallyInstantiated(string namespaceName, string typeName)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(typeName));
        int methodsChecked = 0;
        foreach (MethodDesc method in type.GetMethods())
        {
            if (!method.HasInstantiation)
                continue;

            string methodName = method.Name.ToString();
            methodName = methodName[(methodName.LastIndexOf('.') + 1)..];
            if (methodName is not ("CreateChecked" or "CreateSaturating" or "CreateTruncating"
                or "ConvertToInteger" or "ConvertToIntegerNative")
                && !methodName.StartsWith("TryConvert", StringComparison.Ordinal))
                continue;

            Assert.Null(ReadyToRunLibraryRootProvider.InstantiateIfPossible(method));
            methodsChecked++;
        }

        Assert.True(methodsChecked > 0);
    }

    private static int CheckNestedTypes(MetadataType type)
    {
        int typesChecked = 0;
        foreach (MetadataType nested in type.GetNestedTypes())
        {
            if (nested.HasInstantiation)
            {
                Assert.Null(ReadyToRunLibraryRootProvider.InstantiateIfPossible(nested));
                typesChecked++;
            }

            typesChecked += CheckNestedTypes(nested);
        }

        return typesChecked;
    }

    [Theory]
    [InlineData("none", false)]
    [InlineData("none", true)]
    [InlineData("outer", false)]
    [InlineData("outer", true)]
    [InlineData("middle", false)]
    [InlineData("middle", true)]
    [InlineData("type", false)]
    [InlineData("type", true)]
    [InlineData("method", false)]
    [InlineData("method", true)]
    [InlineData("base", false)]
    [InlineData("base", true)]
    [InlineData("interface", false)]
    [InlineData("interface", true)]
    public void SuppressCanonicalRooting(string attributeLocation, bool genericType)
    {
        string source = $$"""
            namespace Tests
            {
                {{AttributeFor("base")}}
                public class Base { }

                {{AttributeFor("interface")}}
                public interface IOperations<T>
                {
                    static virtual void Ordinary() { }
                    static virtual void Generic<TMethod>() { }
                }

                {{AttributeFor("outer")}}
                public class Outer
                {
                    public static void Ordinary() { }

                    {{AttributeFor("middle")}}
                    public class Middle
                    {
                        {{AttributeFor("type")}}
                        public class Leaf{{(genericType ? "<T>" : "")}} : Base
                        {
                            {{AttributeFor("method")}}
                            public Leaf() { }

                            {{AttributeFor("method")}}
                            public static void Generic<TMethod>() { }

                            {{AttributeFor("method")}}
                            public static void Ordinary() { }

                            public static void Control() { }
                        }

                        public class Sibling<T>
                        {
                            public static void Generic<TMethod>() { }
                        }
                    }
                }
            }
            """;

        string workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        try
        {
            string assemblyPath = Path.Combine(workingDirectory, "Rooting.dll");
            R2RTestCaseCompiler compiler = new([TestPaths.SystemPrivateCoreLibPath]);
            compiler.CompileAssembly("Rooting",
                [source, R2RTestCaseCompiler.ReadEmbeddedSource("SuppressReadyToRunCanonicalRootingAttribute.cs")],
                assemblyPath);

            ReadyToRunCompilerContext context = CreateContext(assemblyPath);
            EcmaModule module = (EcmaModule)context.GetModuleForSimpleName("Rooting");
            context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
            {
                Context = context,
                IsInputBubble = true,
                CompilationModuleSet = [module],
                VersionBubbleModuleSet = [context.SystemModule, module],
                CrossModuleInlineable = [],
                InstructionSetSupport = context.InstructionSetSupport,
            }));

            MetadataType outer = module.GetType("Tests"u8, "Outer"u8);
            MetadataType middle = outer.GetNestedType("Middle"u8);
            MetadataType leaf = middle.GetNestedType(genericType ? "Leaf`1"u8 : "Leaf"u8);
            MetadataType sibling = middle.GetNestedType("Sibling`1"u8);
            MetadataType operations = module.GetType("Tests"u8, "IOperations`1"u8);
            bool typeSuppressed = attributeLocation is "outer" or "middle" or "type";
            using MemoryStream descriptor = new(Encoding.UTF8.GetBytes(
                """<linker><assembly fullname="Rooting" preserve="all" /></linker>"""));
            using MemoryStream methodDescriptor = new(Encoding.UTF8.GetBytes($$"""
                <linker>
                  <assembly fullname="Rooting">
                    <type fullname="Tests.Outer">
                      <method name="Ordinary" />
                    </type>
                    <type fullname="Tests.Outer/Middle/{{(genericType ? "Leaf`1" : "Leaf")}}">
                      <method name=".ctor" />
                      <method name="Generic" />
                      <method name="Ordinary" />
                      <method name="Control" />
                    </type>
                    <type fullname="Tests.Outer/Middle/Sibling`1">
                      <method name="Generic" />
                    </type>
                    <type fullname="Tests.IOperations`1">
                      <method name="Ordinary" />
                      <method name="Generic" />
                    </type>
                  </assembly>
                </linker>
                """));
            ICompilationRootProvider[] providers =
            [
                new ReadyToRunLibraryRootProvider(module),
                new ReadyToRunVisibilityRootProvider(module),
                new ReadyToRunXmlRootProvider(descriptor, default, module, "Rooting.xml"),
                new ReadyToRunXmlRootProvider(methodDescriptor, default, module, "RootingMethods.xml"),
            ];
            foreach (ICompilationRootProvider provider in providers)
            {
                RecordingRootingServiceProvider roots = new();
                provider.AddCompilationRoots(roots);

                Assert.Equal(!typeSuppressed && attributeLocation != "method",
                    roots.Methods.Contains(leaf.GetMethod("Generic"u8, null)));
                Assert.Equal(!genericType || (!typeSuppressed && attributeLocation != "method"),
                    roots.Methods.Contains(leaf.GetMethod("Ordinary"u8, null)));
                Assert.Equal(!genericType || (!typeSuppressed && attributeLocation != "method"),
                    roots.Methods.Contains(leaf.GetMethod(".ctor"u8, null)));
                Assert.Equal(!genericType || !typeSuppressed,
                    roots.Methods.Contains(leaf.GetMethod("Control"u8, null)));
                Assert.Equal(attributeLocation is not ("outer" or "middle"),
                    roots.Methods.Contains(sibling.GetMethod("Generic"u8, null)));
                Assert.Contains(outer.GetMethod("Ordinary"u8, null), roots.Methods);
                Assert.Equal(attributeLocation != "interface",
                    roots.Methods.Contains(operations.GetMethod("Ordinary"u8, null)));
                Assert.Equal(attributeLocation != "interface",
                    roots.Methods.Contains(operations.GetMethod("Generic"u8, null)));
            }

            MetadataType concrete = genericType
                ? leaf.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int32))
                : leaf;
            MethodDesc concreteMethod = concrete.GetMethod("Generic"u8, null)
                .MakeInstantiatedMethod(context.GetWellKnownType(WellKnownType.Int32));
            Assert.Same(context.GetWellKnownType(WellKnownType.Int32), concreteMethod.Instantiation[0]);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }

        string AttributeFor(string location) => attributeLocation == location
            ? "[System.Runtime.SuppressReadyToRunCanonicalRooting]"
            : "";
    }

    private sealed class RecordingRootingServiceProvider : IRootingServiceProvider
    {
        public HashSet<MethodDesc> Methods { get; } = new();

        public void AddCompilationRoot(MethodDesc method, bool rootMinimalDependencies, string reason, bool isJitHelper = false)
        {
            Methods.Add(method.GetTypicalMethodDefinition());
        }
    }

    private static ReadyToRunCompilerContext CreateContext(string? assemblyPath = null)
    {
        InstructionSetSupport instructionSetSupport = new(default, default, TargetArchitecture.X64);
        TargetDetails target = new(TargetArchitecture.X64, TargetOS.Linux, TargetAbi.NativeAot);
        Dictionary<string, string> inputFilePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath,
        };
        if (assemblyPath is not null)
            inputFilePaths.Add(Path.GetFileNameWithoutExtension(assemblyPath), assemblyPath);

        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: true, targetIsSingleThreaded: false,
            instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = inputFilePaths,
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
        context.SetSystemModule((EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib"));

        return context;
    }
}
