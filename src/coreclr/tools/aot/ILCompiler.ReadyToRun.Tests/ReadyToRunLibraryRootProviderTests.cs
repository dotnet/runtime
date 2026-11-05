// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using crossgen2::ILCompiler;
using crossgen2::ILCompiler.DependencyAnalysis;
using crossgen2::ILCompiler.DependencyAnalysis.ReadyToRun;
using crossgen2::Internal.ReadyToRunConstants;
using crossgen2::Internal.JitInterface;
using crossgen2::Internal.Text;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunLibraryRootProviderTests
{
    [Theory]
    [InlineData("System", "Boolean", true)]
    [InlineData("System", "Char", true)]
    [InlineData("System", "SByte", true)]
    [InlineData("System", "Byte", true)]
    [InlineData("System", "Int16", true)]
    [InlineData("System", "UInt16", true)]
    [InlineData("System", "Int32", true)]
    [InlineData("System", "UInt32", true)]
    [InlineData("System", "Int64", true)]
    [InlineData("System", "UInt64", true)]
    [InlineData("System", "IntPtr", true)]
    [InlineData("System", "UIntPtr", true)]
    [InlineData("System", "Single", true)]
    [InlineData("System", "Double", true)]
    [InlineData("System", "Object", true)]
    [InlineData("System", "String", true)]
    [InlineData("System", "Decimal", false)]
    [InlineData("System", "Half", false)]
    [InlineData("System", "Enum", false)]
    [InlineData("System.Collections", "ArrayList", true)]
    [InlineData("System.Collections.Generic", "List`1", true)]
    [InlineData("System.Collections.Generic", "Dictionary`2", true)]
    [InlineData("System.Collections.Concurrent", "ConcurrentDictionary`2", true)]
    [InlineData("System.Collections.Generic", "List`1+Enumerator", true)]
    [InlineData("System.Collections.Generic", "Dictionary`2+Enumerator", true)]
    [InlineData("System.Reflection", "Assembly", false)]
    public void DirectCallDepthExemptionsFollowOwningType(string namespaceName, string typeName, bool expected)
    {
        ReadyToRunCompilerContext context = CreateContext();
        string[] nestedNames = typeName.Split('+');
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(nestedNames[0]));
        for (int i = 1; i < nestedNames.Length; i++)
        {
            type = type.GetNestedType(Encoding.UTF8.GetBytes(nestedNames[i]));
        }

        Assert.Equal(expected, ReadyToRunCodegenCompilation.IsDirectCallDepthExempt(type));
    }

    [Theory]
    [InlineData("NoInlining")]
    [InlineData("NoOptimization")]
    public void EagerCompilationRemovesCalleeDepthFromNonEagerLevels(string coldMethodOption)
    {
        string source = $$"""
            using System.Runtime.CompilerServices;
            namespace Tests;
            public static class EagerCalls
            {
                public static void EagerLeaf() { }
                [MethodImpl(MethodImplOptions.{{coldMethodOption}})]
                public static void EagerCold() { }
                public static void EagerMiddle() { }
                public static void EagerRoot() { }
                public static void NonEagerLeaf() { }
                public static void NonEagerCaller() { }
                public static void CycleLeft() { }
                public static void CycleRight() { }
            }
            """;

        string workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        try
        {
            string assemblyPath = Path.Combine(workingDirectory, "EagerCalls.dll");
            R2RTestCaseCompiler compiler = new([TestPaths.SystemPrivateCoreLibPath]);
            compiler.CompileAssembly("EagerCalls", [source], assemblyPath);
            ReadyToRunCompilerContext context = CreateContext(assemblyPath);
            MetadataType type = context.GetModuleForSimpleName("EagerCalls").GetType("Tests"u8, "EagerCalls"u8);
            MethodDesc eagerLeaf = type.GetMethod("EagerLeaf"u8, null);
            MethodDesc eagerCold = type.GetMethod("EagerCold"u8, null);
            MethodDesc eagerMiddle = type.GetMethod("EagerMiddle"u8, null);
            MethodDesc eagerRoot = type.GetMethod("EagerRoot"u8, null);
            MethodDesc nonEagerLeaf = type.GetMethod("NonEagerLeaf"u8, null);
            MethodDesc nonEagerCaller = type.GetMethod("NonEagerCaller"u8, null);
            MethodDesc cycleLeft = type.GetMethod("CycleLeft"u8, null);
            MethodDesc cycleRight = type.GetMethod("CycleRight"u8, null);
            ReadyToRunCallGraphBuilder graph = new();
            graph.AddCall(eagerRoot, eagerMiddle);
            graph.AddCall(eagerMiddle, eagerCold);
            graph.AddCall(eagerCold, eagerLeaf);
            graph.AddCall(nonEagerLeaf, eagerRoot);
            graph.AddCall(nonEagerLeaf, eagerCold);
            graph.AddCall(nonEagerCaller, nonEagerLeaf);
            graph.AddCall(cycleLeft, cycleRight);
            graph.AddCall(cycleRight, cycleLeft);
            graph.AddCall(cycleRight, eagerRoot);
            ReadyToRunCompilationPlan plan = graph.CreatePlan(
                [eagerLeaf, eagerCold, eagerMiddle, eagerRoot, nonEagerLeaf, nonEagerCaller, cycleLeft, cycleRight]);

            ReadyToRunCompilationPlan eagerPlan = plan.CreateEagerPlan([eagerRoot]);
            Assert.Equal(4, eagerPlan.LevelCount);
            Assert.Same(eagerLeaf, Assert.Single(eagerPlan.GetMethodsAtLevel(0)));
            Assert.Same(eagerCold, Assert.Single(eagerPlan.GetMethodsAtLevel(1)));
            Assert.Same(eagerMiddle, Assert.Single(eagerPlan.GetMethodsAtLevel(2)));
            Assert.Same(eagerRoot, Assert.Single(eagerPlan.GetMethodsAtLevel(3)));

            HashSet<MethodDesc> compiledEagerMethods = [eagerLeaf, eagerCold, eagerMiddle, eagerRoot];
            ReadyToRunCompilationPlan nonEagerPlan = plan.CreateNonEagerPlan(compiledEagerMethods);
            Assert.Equal(2, nonEagerPlan.LevelCount);
            Assert.True(new HashSet<MethodDesc>(nonEagerPlan.GetMethodsAtLevel(0))
                .SetEquals([nonEagerLeaf, cycleLeft, cycleRight]));
            Assert.Same(nonEagerCaller, Assert.Single(nonEagerPlan.GetMethodsAtLevel(1)));

            compiledEagerMethods.Remove(eagerRoot);
            ReadyToRunCompilationPlan failedRootPlan = plan.CreateNonEagerPlan(compiledEagerMethods);
            Assert.Equal(3, failedRootPlan.LevelCount);
            Assert.Same(eagerRoot, Assert.Single(failedRootPlan.GetMethodsAtLevel(0)));
            Assert.Contains(nonEagerLeaf, failedRootPlan.GetMethodsAtLevel(1));
            Assert.Same(nonEagerCaller, Assert.Single(failedRootPlan.GetMethodsAtLevel(2)));
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolverReleasesCompilationMetadata(bool mutableModule)
    {
        ReadyToRunCompilerContext context = CreateContext();
        EcmaModule coreLib = (EcmaModule)context.SystemModule;
        ReadyToRunSingleAssemblyCompilationModuleGroup group = new(new ReadyToRunCompilationModuleGroupConfig
        {
            Context = context,
            CompilationModuleSet = [coreLib],
            VersionBubbleModuleSet = [coreLib],
            CrossModuleInlineable = [],
        });
        WeakReference metadata = AttachResolverMetadata(group, context, mutableModule);

        group.Resolver.ClearCompilationState();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(metadata.IsAlive);
        GC.KeepAlive(group);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecodeGuaranteesShareUnchangedCalleeCoverage(bool moduleEager)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo callee = new(method);
        MethodWithGCInfo caller = new(method);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        Import cell = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        callee.Fixups.Add(cell);
        callee.FinalizePrecodeFixups();
        caller.AddDirectCallTarget(callee);
        caller.Fixups.Add(cell);
        caller.FinalizePrecodeFixups();
        Import[] callerGuarantees = caller.GetGuaranteedPrecodeFixups(new HashSet<Import>());

        if (moduleEager)
        {
            Import eagerCell = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
            HashSet<Import> eagerFixups = [eagerCell];
            callee.ApplyModuleEagerFixups(eagerFixups);
            caller.ApplyModuleEagerFixups(eagerFixups);
            Assert.DoesNotContain(eagerCell, callee.GuaranteedPrecodeFixups);
            Assert.DoesNotContain(eagerCell, callerGuarantees);
        }

        Assert.Empty(caller.Fixups);
        Assert.Same(callee.GuaranteedPrecodeFixups, callerGuarantees);
        Assert.Same(cell, Assert.Single(callerGuarantees));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransitivePrecodeGuaranteesDoNotMutateSharedCallees(bool reuseScratch)
    {
        HashSet<Import>? scratch = reuseScratch ? new HashSet<Import>() : null;
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        ReadyToRunHelperSignature signature = new(ReadyToRunHelper.Module);
        Import leafCell = new(table, signature);
        Import leftCell = new(table, signature);
        Import rightCell = new(table, signature);
        Import outerCell = new(table, signature);
        MethodWithGCInfo leaf = new(method);
        leaf.Fixups.Add(leafCell);
        leaf.FinalizePrecodeFixups(scratch: scratch);
        Assert.Null(leaf.GuaranteedPrecodeFixups);
        MethodWithGCInfo left = new(method);
        left.AddDirectCallTarget(leaf);
        left.Fixups.Add(leftCell);
        left.FinalizePrecodeFixups(scratch: scratch);
        MethodWithGCInfo right = new(method);
        right.AddDirectCallTarget(leaf);
        right.Fixups.Add(rightCell);
        right.FinalizePrecodeFixups(scratch: scratch);
        MethodWithGCInfo outer = new(method);
        outer.AddDirectCallTarget(left);
        outer.AddDirectCallTarget(right);
        outer.Fixups.AddRange([leafCell, leftCell, rightCell, outerCell]);
        outer.FinalizePrecodeFixups(scratch: scratch);
        Assert.Null(outer.GuaranteedPrecodeFixups);
        HashSet<Import> consumerScratch = scratch ?? new HashSet<Import>();
        Import[] outerGuarantees = outer.GetGuaranteedPrecodeFixups(consumerScratch);

        Assert.NotNull(leaf.GuaranteedPrecodeFixups);
        Assert.Same(leafCell, Assert.Single(leaf.GuaranteedPrecodeFixups));
        Assert.True(new HashSet<Import>(left.GuaranteedPrecodeFixups).SetEquals([leafCell, leftCell]));
        Assert.True(new HashSet<Import>(right.GuaranteedPrecodeFixups).SetEquals([leafCell, rightCell]));
        Assert.True(new HashSet<Import>(outerGuarantees).SetEquals([leafCell, leftCell, rightCell, outerCell]));
        Assert.Same(outerCell, Assert.Single(outer.Fixups));
        if (scratch is not null)
        {
            Assert.Empty(scratch);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void PrecodeGuaranteesAreSharedByConcurrentConsumers(int fixupCount)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo methodNode = new(method);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        Import[] cells = new Import[fixupCount];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new Import(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
            methodNode.Fixups.Add(cells[i]);
        }
        methodNode.FinalizePrecodeFixups();
        Assert.Null(methodNode.GuaranteedPrecodeFixups);

        Import[][] guarantees = new Import[8][];
        Parallel.For(0, guarantees.Length, i =>
        {
            HashSet<Import> scratch = new();
            guarantees[i] = methodNode.GetGuaranteedPrecodeFixups(scratch);
            Assert.Empty(scratch);
        });

        Assert.All(guarantees, snapshot =>
        {
            Assert.Same(guarantees[0], snapshot);
            Assert.Equal(cells.Length, snapshot.Length);
            Assert.All(cells, cell => Assert.Contains(cell, snapshot));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EagerPromotionPreservesDistinctImportSymbols(bool reverseOrder)
    {
        ImportSectionNode precodeTable = new("PrecodeImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        ImportSectionNode eagerTable = new("EagerImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.Eager, 8, emitPrecode: false, emitGCRefMap: false);
        ReadyToRunHelperSignature signature = new(ReadyToRunHelper.Module);
        Import promotedCell = new PrecodeHelperImport(precodeTable, signature);
        Import eagerCell = new(eagerTable, signature, sortLast: true);
        NativeAotNameMangler mangler = new() { CompilationUnitPrefix = new Utf8String("Test") };
        Utf8StringBuilder originalName = new();
        promotedCell.AppendMangledName(mangler, originalName);

        promotedCell.PromoteToEager(eagerTable);

        Utf8StringBuilder promotedName = new();
        Utf8StringBuilder eagerName = new();
        promotedCell.AppendMangledName(mangler, promotedName);
        eagerCell.AppendMangledName(mangler, eagerName);
        Assert.Same(eagerCell.Table, promotedCell.Table);
        Assert.Equal(originalName.ToString(), promotedName.ToString());
        Assert.NotEqual(eagerName.ToString(), promotedName.ToString());

        Import[] imports = reverseOrder ? [eagerCell, promotedCell] : [promotedCell, eagerCell];
        Array.Sort(imports, CompilerComparer.Instance);
        Assert.Same(promotedCell, imports[0]);
        Assert.Same(eagerCell, imports[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecodeCoverageRequiresTheSameImportCell(bool sharedCell)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo callee = new(method);
        MethodWithGCInfo caller = new(method);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        ReadyToRunHelperSignature signature = new(ReadyToRunHelper.Module);
        Import calleeCell = new(table, signature);
        Import callerCell = sharedCell ? calleeCell : new(table, signature);
        callee.Fixups.Add(calleeCell);
        callee.FinalizePrecodeFixups();
        caller.AddDirectCallTarget(callee);
        caller.Fixups.Add(callerCell);
        caller.FinalizePrecodeFixups();

        if (sharedCell)
        {
            Assert.Empty(caller.Fixups);
        }
        else
        {
            Assert.Same(callerCell, Assert.Single(caller.Fixups));
        }
    }

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AttachResolverMetadata(
        ReadyToRunCompilationModuleGroupBase group, ReadyToRunCompilerContext context, bool mutableModule)
    {
        if (mutableModule)
        {
            crossgen2::Internal.TypeSystem.Ecma.MutableModule module = new(context, "TestMetadata",
                default(AssemblyFlags), null, new Version(0, 0), AssemblyHashAlgorithm.None, static _ => 0, group);
            group.Resolver.InitManifestMutableModule(module);
            return new WeakReference(module);
        }

        object metadata = new();
        group.Resolver.SetModuleIndexLookup(_ =>
        {
            GC.KeepAlive(metadata);
            return 0;
        });
        return new WeakReference(metadata);
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
