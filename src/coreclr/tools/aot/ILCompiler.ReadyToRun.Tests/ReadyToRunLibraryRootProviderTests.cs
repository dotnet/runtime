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
using crossgen2::ILCompiler.DependencyAnalysis.Wasm;
using crossgen2::ILCompiler.ObjectWriter;
using crossgen2::ILCompiler.ObjectWriter.WasmInstructions;
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
    [InlineData(false, 0, WasmValueType.I32)]
    [InlineData(true, 0, WasmValueType.I32)]
    [InlineData(false, 2, WasmValueType.I32)]
    [InlineData(true, 127, WasmValueType.I32)]
    [InlineData(false, 2, WasmValueType.F64)]
    [InlineData(true, 2, WasmValueType.F64)]
    public void DeferredWasmIndirectCallsRemapFramesAndDebugInfo(bool tailCall, int localCount, WasmValueType localType)
    {
        const byte DropOpcode = 0x1A;
        ReadyToRunCompilerContext context = CreateContext(targetArchitecture: TargetArchitecture.Wasm32);
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo target = new(method);
        MethodWithGCInfo caller = new(method);
        NodeFactory factory = (NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory));
        SetField(factory, typeof(NodeFactory), "<Target>k__BackingField", context.Target);
        ReadyToRunCodegenCompilation compilation = (ReadyToRunCodegenCompilation)RuntimeHelpers.GetUninitializedObject(
            typeof(ReadyToRunCodegenCompilation));
        SetField(compilation, typeof(ReadyToRunCodegenCompilation), "_nodeFactory", factory);
        DeferredWasmCallProcessor processor = (DeferredWasmCallProcessor)RuntimeHelpers.GetUninitializedObject(
            typeof(DeferredWasmCallProcessor));
        SetField(processor, typeof(DeferredWasmCallProcessor), "_compilation", compilation);
        SetField(processor, typeof(DeferredWasmCallProcessor), "_finalizedMethods",
            new Dictionary<MethodDesc, DeferredWasmMethodFinalizationInfo>());
        SetField(compilation, typeof(ReadyToRunCodegenCompilation), "<DeferredWasmCalls>k__BackingField", processor);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 4, emitPrecode: false, emitGCRefMap: false);
        DelayLoadMethodImport fallback = (DelayLoadMethodImport)RuntimeHelpers.GetUninitializedObject(typeof(DelayLoadMethodImport));
        SetField(fallback, typeof(Import), "<Table>k__BackingField", table);
        SetField(fallback, typeof(DelayLoadMethodImport), "_localMethod", target);
        WasmMethodFrameIdentityNode callerIdentity = new(factory, caller);
        WasmMethodFrameIdentityNode targetIdentity = new(factory, target);
        WasmTypeNode signature = new(WasmLowering.GetSignature(method).FuncType);
        DeferredWasmCallNode call = new(fallback, signature);
        FuncletKind[] funcletKinds = [FuncletKind.CatchOrFilterHandler, FuncletKind.Filter, FuncletKind.Finally, FuncletKind.Fault];
        int[] parameterCounts = [3, 3, 3, 2, 2];
        FrameInfo[] frames = new FrameInfo[parameterCounts.Length];
        OffsetMapping[] locations = new OffsetMapping[frames.Length];
        NativeVarInfo[] variables = new NativeVarInfo[1];
        List<byte> bytes = new();
        List<Relocation> relocations = new();

        for (int i = 0; i < frames.Length; i++)
        {
            int start = bytes.Count;
            bytes.AddRange(new byte[5]);
            bytes.Add((byte)(localCount == 0 ? 0 : 1));
            if (localCount != 0)
            {
                bytes.AddRange([(byte)localCount, (byte)localType]);
            }
            EmitRelocation(WasmExprKind.I32Const, RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, callerIdentity);
            bytes.Add(DropOpcode);
            if (i == 0)
            {
                variables[0].startOffset = (uint)bytes.Count;
            }
            bytes.AddRange([(byte)WasmExprKind.LocalGet, 0, (byte)WasmExprKind.LocalGet, 0]);
            EmitRelocation(WasmExprKind.I32Const, RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, targetIdentity);
            bool returnsValue = i < 3;
            EmitRelocation(tailCall && returnsValue ? WasmExprKind.ReturnCall : WasmExprKind.Call,
                RelocType.WASM_FUNCTION_INDEX_LEB, call);
            locations[i] = new OffsetMapping { nativeOffset = (uint)bytes.Count, ilOffset = (uint)i };
            if (i == 0)
            {
                variables[0].endOffset = (uint)bytes.Count;
            }
            if (!returnsValue)
            {
                bytes.Add(DropOpcode);
            }
            bytes.Add((byte)WasmExprKind.End);
            byte[] size = new byte[5];
            DwarfHelper.WritePaddedULEB128(size, (ulong)(bytes.Count - start - size.Length));
            for (int j = 0; j < size.Length; j++)
            {
                bytes[start + j] = size[j];
            }
            frames[i] = new FrameInfo(i == 0 ? FrameInfoFlags.HasEHInfo : FrameInfoFlags.Handler,
                start, bytes.Count, [(byte)i]);
        }
        caller.SetCode(new ObjectNode.ObjectData(bytes.ToArray(), relocations.ToArray(), 1, [caller]));
        caller.InitializeFrameInfos(frames);
        caller.InitializeFuncletKinds(funcletKinds);
        new DeferredWasmCallRewriter(compilation).Rewrite(caller, locations, variables);

        ObjectNode.ObjectData rewritten = caller.GetData(factory, relocsOnly: false);
        OffsetMapping[] expectedLocations = new OffsetMapping[frames.Length];
        NativeVarInfo[] expectedVariables = new NativeVarInfo[1];
        ISymbolNode imageBase = factory.GetWellKnownWasmGlobalSymbol(new(WasmWellKnownGlobalSymbolNode.ImageBaseName));
        int position = 0;
        for (int i = 0; i < frames.Length; i++)
        {
            FrameInfo frame = caller.FrameInfos[i];
            Assert.Equal(position, frame.StartOffset);
            Assert.Equal(frames[i].Flags, frame.Flags);
            Assert.Same(frames[i].BlobData, frame.BlobData);
            AssertImmediate((uint)(frame.EndOffset - frame.StartOffset - 5));
            Assert.Equal(frame.StartOffset + 5, position);
            bool extendGroup = localCount != 0 && localType == WasmValueType.I32;
            AssertImmediate((uint)(localCount == 0 || extendGroup ? 1 : 2));
            if (localCount != 0)
            {
                AssertImmediate((uint)(localCount + (extendGroup ? 1 : 0)));
                Assert.Equal((byte)localType, rewritten.Data[position++]);
            }
            if (!extendGroup)
            {
                AssertImmediate(1);
                Assert.Equal((byte)WasmValueType.I32, rewritten.Data[position++]);
            }
            AssertInstruction(WasmExprKind.I32Const);
            AssertImmediate(0, RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, callerIdentity);
            Assert.Equal(DropOpcode, rewritten.Data[position++]);
            if (i == 0)
            {
                expectedVariables[0].startOffset = (uint)position;
            }
            AssertInstruction(WasmExprKind.LocalGet);
            AssertImmediate(0);
            AssertInstruction(WasmExprKind.LocalGet);
            AssertImmediate(0);
            AssertInstruction(WasmExprKind.GlobalGet);
            AssertImmediate(0, RelocType.WASM_GLOBAL_INDEX_LEB, imageBase);
            AssertInstruction(WasmExprKind.I32Load);
            AssertImmediate(2);
            AssertImmediate(0, RelocType.WASM_MEMORY_ADDR_REL_LEB, fallback);
            AssertInstruction(WasmExprKind.LocalTee);
            AssertImmediate((uint)(parameterCounts[i] + localCount));
            AssertInstruction(WasmExprKind.LocalGet);
            AssertImmediate((uint)(parameterCounts[i] + localCount));
            AssertInstruction(WasmExprKind.I32Load);
            AssertImmediate(2);
            AssertImmediate(0);
            bool returnsValue = i < 3;
            AssertInstruction(tailCall && returnsValue ? WasmExprKind.ReturnCallIndirect : WasmExprKind.CallIndirect);
            AssertImmediate(0, RelocType.WASM_TYPE_INDEX_LEB, signature);
            AssertImmediate(0);
            expectedLocations[i] = new OffsetMapping { nativeOffset = (uint)position, ilOffset = (uint)i };
            if (i == 0)
            {
                expectedVariables[0].endOffset = (uint)position;
            }
            if (!returnsValue)
            {
                Assert.Equal(DropOpcode, rewritten.Data[position++]);
            }
            AssertInstruction(WasmExprKind.End);
            Assert.Equal(frame.EndOffset, position);
        }
        Assert.Equal(rewritten.Data.Length, position);
        Assert.Equal(frames.Length * 4, rewritten.Relocs.Length);
        for (int i = 0; i < rewritten.Relocs.Length; i++)
        {
            Assert.Null(caller.GetDirectCallTarget(i));
        }
        MethodWithGCInfo expectedDebugInfo = new(method);
        expectedDebugInfo.InitializeDebugLocInfos(expectedLocations);
        expectedDebugInfo.InitializeDebugVarInfos(expectedVariables);
        Assert.Equal(expectedDebugInfo.DebugLocInfos, caller.DebugLocInfos);
        Assert.Equal(expectedDebugInfo.DebugVarInfos, caller.DebugVarInfos);

        int EmitRelocation(WasmExprKind instruction, RelocType type, ISymbolNode symbol)
        {
            bytes.Add((byte)instruction);
            int offset = bytes.Count;
            relocations.Add(new Relocation(type, offset, symbol));
            bytes.AddRange([0x80, 0x80, 0x80, 0x80, 0]);
            return offset;
        }

        void AssertInstruction(WasmExprKind instruction) => Assert.Equal((byte)instruction, rewritten.Data[position++]);

        void AssertImmediate(uint expected, RelocType? type = null, ISymbolNode? symbol = null)
        {
            if (type is not null)
            {
                Relocation relocation = Assert.Single(rewritten.Relocs, relocation => relocation.Offset == position);
                Assert.Equal(type.Value, relocation.RelocType);
                Assert.Same(symbol, relocation.Target);
            }
            Assert.Equal(expected, DwarfHelper.ReadULEB128(rewritten.Data.AsSpan(position), out int count));
            position += count;
        }

        static void SetField(object instance, Type type, string name, object value) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    public void DeferredWasmDirectCallsPreserveBodyLayout(int callCount, bool eager, bool debugInfo)
    {
        ReadyToRunCompilerContext context = CreateContext(targetArchitecture: TargetArchitecture.Wasm32);
        EcmaMethod targetMethod = (EcmaMethod)context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo target = new(targetMethod);
        MethodWithGCInfo caller = new(targetMethod);
        NodeFactory factory = (NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory));
        SetField(factory, "<Target>k__BackingField", context.Target);
        ReadyToRunCodegenCompilation compilation = (ReadyToRunCodegenCompilation)RuntimeHelpers.GetUninitializedObject(
            typeof(ReadyToRunCodegenCompilation));
        SetField(compilation, "_nodeFactory", factory);
        DeferredWasmCallProcessor processor = (DeferredWasmCallProcessor)RuntimeHelpers.GetUninitializedObject(
            typeof(DeferredWasmCallProcessor));
        SetField(processor, "_compilation", compilation);
        SetField(processor, "_finalizedMethods", new Dictionary<MethodDesc, DeferredWasmMethodFinalizationInfo>
        {
            [targetMethod] = new(0, eager),
        });
        SetField(compilation, "<DeferredWasmCalls>k__BackingField", processor);

        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            eager ? ReadyToRunImportSectionFlags.Eager : ReadyToRunImportSectionFlags.PCode,
            8, emitPrecode: !eager, emitGCRefMap: false);
        Import import = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        ReadyToRunSymbolNodeFactory symbols = (ReadyToRunSymbolNodeFactory)RuntimeHelpers.GetUninitializedObject(
            typeof(ReadyToRunSymbolNodeFactory));
        List<MethodWithToken> selectedTokens = new();
        SetField(symbols, eager ? "_eagerReadyToRunMethodEntries" : "_precodeReadyToRunMethodEntries",
            new NodeCache<MethodWithToken, Import>(selectedToken =>
            {
                selectedTokens.Add(selectedToken);
                return import;
            }));
        SetField(compilation, "<SymbolNodeFactory>k__BackingField", symbols);
        DelayLoadMethodImport fallback = (DelayLoadMethodImport)RuntimeHelpers.GetUninitializedObject(typeof(DelayLoadMethodImport));
        SetField(fallback, "_localMethod", target);
        DeferredWasmCallNode call = new(fallback);
        WasmMethodFrameIdentityNode identity = new(factory, target);
        MethodWithToken token = new(targetMethod, new ModuleToken(targetMethod.Module, targetMethod.Handle),
            constrainedType: null, unboxing: false, genericContextObject: null);
        SetField(fallback, "<MethodWithToken>k__BackingField",
            new MethodWithToken(targetMethod, token.Token, constrainedType: targetMethod.OwningType,
                unboxing: false, genericContextObject: null));
        DelayLoadMethodImport activationImport = (DelayLoadMethodImport)RuntimeHelpers.GetUninitializedObject(typeof(DelayLoadMethodImport));
        SetField(activationImport, "<MethodWithToken>k__BackingField", token);
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports = new() { [target] = activationImport };
        Relocation[] relocations = new Relocation[1 + callCount * 2];
        relocations[0] = new(RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, 2, identity);
        for (int i = 0; i < callCount; i++)
        {
            relocations[1 + i * 2] = new(RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, 8 + i * 12, identity);
            relocations[2 + i * 2] = new(RelocType.WASM_FUNCTION_INDEX_LEB, 14 + i * 12, call);
        }
        byte[] code = new byte[64];
        for (int i = 0; i < callCount; i++)
        {
            code[7 + i * 12] = (byte)WasmExprKind.I32Const;
            code[13 + i * 12] = (byte)(i == 0 ? WasmExprKind.Call : WasmExprKind.ReturnCall);
        }
        ObjectNode.ObjectData original = new(code, relocations, 16, [caller]);
        caller.SetCode(original);
        FrameInfo[] frames = [new(FrameInfoFlags.HasEHInfo, 0, 32, [1]), new(FrameInfoFlags.Handler, 32, 64, [2])];
        caller.InitializeFrameInfos(frames);
        OffsetMapping[]? locations = debugInfo ? [new() { nativeOffset = 17, ilOffset = 2 }] : null;
        NativeVarInfo[]? variables = debugInfo ? [new() { startOffset = 3, endOffset = 29 }] : null;
        MethodWithGCInfo expectedDebugInfo = new(targetMethod);
        if (debugInfo)
        {
            expectedDebugInfo.InitializeDebugLocInfos(locations);
            expectedDebugInfo.InitializeDebugVarInfos(variables);
        }

        Relocation[] repeatedRelocations = (Relocation[])relocations.Clone();
        Import existingEagerImport = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        if (eager && callCount != 0)
        {
            caller.AddDeferredWasmDependency(existingEagerImport);
            caller.AddDeferredWasmDependency(import);
        }
        if (!eager && callCount != 0)
        {
            caller.Fixups.Add(import);
        }
        DeferredWasmCallRewriter rewriter = new(compilation);
        rewriter.Rewrite(caller, locations, variables, activationImports);

        Assert.Same(original, caller.GetData(factory, relocsOnly: false));
        Assert.Same(frames, caller.FrameInfos);
        Assert.Same(identity, relocations[0].Target);
        for (int i = 0; i < callCount; i++)
        {
            Assert.Same(identity, relocations[1 + i * 2].Target);
            Assert.Same(target, relocations[2 + i * 2].Target);
            Assert.Equal(14 + i * 12, relocations[2 + i * 2].Offset);
        }
        if (callCount != 0)
        {
            Assert.Null(Assert.Single(selectedTokens).ConstrainedType);
            target.FinalizePrecodeFixups();
            caller.FinalizePrecodeFixups();
            if (eager)
            {
                Import[] guarantees = caller.GetGuaranteedPrecodeFixups(new HashSet<Import>());
                Assert.Equal(2, guarantees.Length);
                Assert.Contains(import, guarantees);
                Assert.Contains(existingEagerImport, guarantees);
            }
            else
            {
                Assert.Same(import, Assert.Single(caller.Fixups));
            }
            Assert.Same(target, caller.GetDirectCallTarget(2));
        }
        Assert.Equal(expectedDebugInfo.DebugLocInfos, caller.DebugLocInfos);
        Assert.Equal(expectedDebugInfo.DebugVarInfos, caller.DebugVarInfos);

        MethodWithGCInfo nextCaller = new(targetMethod);
        nextCaller.SetCode(new ObjectNode.ObjectData(original.Data, repeatedRelocations, 16, [nextCaller]));
        nextCaller.InitializeFrameInfos(frames);
        rewriter.Rewrite(nextCaller, activationImports: activationImports);
        if (callCount != 0)
        {
            Assert.Same(target, nextCaller.GetDirectCallTarget(2));
        }
        nextCaller.FinalizePrecodeFixups();
        Assert.Equal(callCount == 0 ? 0 : 1, nextCaller.GetGuaranteedPrecodeFixups(new HashSet<Import>()).Length);
        Assert.Equal(!eager && callCount != 0 ? 1 : 0, nextCaller.Fixups.Count);

        static void SetField(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    }

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

        Assert.Equal(expected, DeferredWasmCallProcessor.IsDirectCallDepthExempt(type));
    }

    [Theory]
    [InlineData("NoInlining")]
    [InlineData("NoOptimization")]
    public void EagerFinalizationRemovesCalleeDepthFromNonEagerLevels(string coldMethodOption)
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
            NodeFactory factory = (NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory));
            DeferredWasmCallFinalizationPlan plan = DeferredWasmCallFinalizationPlan.Create(
                [
                    CreateMethodNode(eagerLeaf),
                    CreateMethodNode(eagerCold, eagerLeaf),
                    CreateMethodNode(eagerMiddle, eagerCold),
                    CreateMethodNode(eagerRoot, eagerMiddle),
                    CreateMethodNode(nonEagerLeaf, eagerRoot, eagerCold),
                    CreateMethodNode(nonEagerCaller, nonEagerLeaf),
                    CreateMethodNode(cycleLeft, cycleRight),
                    CreateMethodNode(cycleRight, cycleLeft, eagerRoot),
                ], factory);

            DeferredWasmCallFinalizationPlan eagerPlan = plan.CreateEagerPlan([eagerRoot]);
            Assert.Equal(4, eagerPlan.LevelCount);
            Assert.Same(eagerLeaf, Assert.Single(eagerPlan.GetMethodsAtLevel(0)));
            Assert.Same(eagerCold, Assert.Single(eagerPlan.GetMethodsAtLevel(1)));
            Assert.Same(eagerMiddle, Assert.Single(eagerPlan.GetMethodsAtLevel(2)));
            Assert.Same(eagerRoot, Assert.Single(eagerPlan.GetMethodsAtLevel(3)));

            HashSet<MethodDesc> finalizedEagerMethods = [eagerLeaf, eagerCold, eagerMiddle, eagerRoot];
            DeferredWasmCallFinalizationPlan nonEagerPlan = plan.CreateNonEagerPlan(finalizedEagerMethods);
            Assert.Equal(2, nonEagerPlan.LevelCount);
            Assert.True(new HashSet<MethodDesc>(nonEagerPlan.GetMethodsAtLevel(0))
                .SetEquals([nonEagerLeaf, cycleLeft, cycleRight]));
            Assert.Same(nonEagerCaller, Assert.Single(nonEagerPlan.GetMethodsAtLevel(1)));

            finalizedEagerMethods.Remove(eagerRoot);
            DeferredWasmCallFinalizationPlan failedRootPlan = plan.CreateNonEagerPlan(finalizedEagerMethods);
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
    public void FinalizationPlanUsesPublishedCallRelocations(bool reverseOrder)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc leaf = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodDesc middle = context.GetWellKnownType(WellKnownType.Boolean).GetMethod("GetHashCode"u8, null);
        MethodDesc caller = context.GetWellKnownType(WellKnownType.Char).GetMethod("GetHashCode"u8, null);
        MethodDesc ignored = context.GetWellKnownType(WellKnownType.Int64).GetMethod("GetHashCode"u8, null);
        MethodDesc external = context.GetWellKnownType(WellKnownType.Double).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo leafNode = CreateMethodNode(leaf);
        MethodWithGCInfo middleNode = CreateMethodNode(middle, leaf);
        MethodWithGCInfo callerNode = new(caller);
        MethodWithGCInfo ignoredNode = CreateMethodNode(ignored, caller);
        DelayLoadMethodImport fallback = (DelayLoadMethodImport)RuntimeHelpers.GetUninitializedObject(typeof(DelayLoadMethodImport));
        typeof(DelayLoadMethodImport).GetField("_localMethod", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(fallback, middleNode);
        DeferredWasmCallNode deferred = new(fallback);
        callerNode.SetCode(new ObjectNode.ObjectData(new byte[40],
            [
                new(RelocType.WASM_FUNCTION_INDEX_LEB, 1, deferred),
                new(RelocType.WASM_FUNCTION_INDEX_LEB, 7, deferred),
                new(RelocType.WASM_FUNCTION_INDEX_LEB, 13, leafNode),
                new(RelocType.IMAGE_REL_BASED_DIR64, 19, ignoredNode),
                new(RelocType.WASM_FUNCTION_INDEX_LEB, 28, new MethodWithGCInfo(external)),
            ], 1, [callerNode]));
        List<MethodWithGCInfo> nodes = [leafNode, middleNode, callerNode, ignoredNode];
        if (reverseOrder)
        {
            nodes.Reverse();
        }
        NodeFactory factory = (NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory));

        DeferredWasmCallFinalizationPlan plan = DeferredWasmCallFinalizationPlan.Create(nodes, factory);

        Assert.Equal(4, plan.LevelCount);
        Assert.Same(leaf, Assert.Single(plan.GetMethodsAtLevel(0)));
        Assert.Same(middle, Assert.Single(plan.GetMethodsAtLevel(1)));
        Assert.Same(caller, Assert.Single(plan.GetMethodsAtLevel(2)));
        Assert.Same(ignored, Assert.Single(plan.GetMethodsAtLevel(3)));
    }

    private static MethodWithGCInfo CreateMethodNode(MethodDesc method, params MethodDesc[] callees)
    {
        MethodWithGCInfo node = new(method);
        Relocation[] relocations = new Relocation[callees.Length];
        for (int i = 0; i < callees.Length; i++)
        {
            relocations[i] = new Relocation(RelocType.WASM_FUNCTION_INDEX_LEB, 1 + i * 6, new MethodWithGCInfo(callees[i]));
        }
        node.SetCode(new ObjectNode.ObjectData(new byte[1 + callees.Length * 6], relocations, 1, [node]));

        return node;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ModuleEagerGuaranteesIncludeRelocatedAndExplicitDependencies(int source)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo caller = new(method);
        ImportSectionNode table = new("EagerImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.Eager, 8, emitPrecode: false, emitGCRefMap: false);
        Import cell = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        Relocation importRelocation = new(RelocType.IMAGE_REL_BASED_HIGHLOW, 0, cell);
        Relocation[] codeRelocations = [];
        switch (source)
        {
            case 0:
                codeRelocations = [importRelocation];
                break;
            case 1:
                MethodColdCodeNode coldCode = new(method);
                coldCode.SetCode(new ObjectNode.ObjectData([], [importRelocation], 1, [coldCode]));
                caller.ColdCodeNode = coldCode;
                break;
            case 2:
                MethodReadOnlyDataNode readOnlyData = new(method);
                Relocation roReference = new(RelocType.IMAGE_REL_BASED_HIGHLOW, 4, readOnlyData);
                readOnlyData.InitializeData(new ObjectNode.ObjectData([], [importRelocation, roReference], 1, [readOnlyData]));
                codeRelocations = [roReference, roReference];
                break;
            case 3:
                MethodReadWriteDataNode readWriteData = new(method);
                Relocation rwReference = new(RelocType.IMAGE_REL_BASED_HIGHLOW, 4, readWriteData);
                readWriteData.InitializeData(new ObjectNode.ObjectData([], [importRelocation, rwReference], 1, [readWriteData]));
                codeRelocations = [rwReference, rwReference];
                break;
            case 4:
                caller.AddDeferredWasmDependency(cell);
                break;
        }
        caller.SetCode(new ObjectNode.ObjectData([], codeRelocations, 1, [caller]));
        caller.Fixups.Add(cell);

        caller.FinalizePrecodeFixups();

        Assert.Empty(caller.Fixups);
        Assert.Same(cell, Assert.Single(caller.GetGuaranteedPrecodeFixups(new HashSet<Import>())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HelperAndFuncletCallsDoNotInheritActivationGuarantees(bool funclet)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo caller = new(method);
        MethodWithGCInfo target = funclet ? caller : new(method) { IsJitHelper = true };
        ImportSectionNode table = new("PrecodeImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        Import cell = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        if (!funclet)
        {
            target.Fixups.Add(cell);
            target.FinalizePrecodeFixups();
        }
        SetDirectCallTargets(caller, target);
        if (!funclet)
        {
            Relocation[] relocations = caller.GetData(null, relocsOnly: false).Relocs;
            relocations[0] = new(RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, 0,
                new WasmMethodFrameIdentityNode((NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory)), caller));
        }
        caller.FinalizePrecodeFixups();

        Assert.Null(caller.GetDirectCallTarget(1));
        Assert.Empty(caller.GetGuaranteedPrecodeFixups(new HashSet<Import>()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrecodeGuaranteesShareUnchangedCalleeCoverage(bool moduleEager)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MethodDesc method = context.GetWellKnownType(WellKnownType.Int32).GetMethod("GetHashCode"u8, null);
        MethodWithGCInfo callee = new(method) { IsJitHelper = true };
        MethodWithGCInfo caller = new(method);
        ImportSectionNode table = new("TestImports", ReadyToRunImportSectionType.Unknown,
            ReadyToRunImportSectionFlags.PCode, 8, emitPrecode: true, emitGCRefMap: false);
        Import cell = new(table, new ReadyToRunHelperSignature(ReadyToRunHelper.Module));
        callee.Fixups.Add(cell);
        callee.FinalizePrecodeFixups();
        SetDirectCallTargets(caller, callee);
        caller.Fixups.Add(cell);
        caller.FinalizePrecodeFixups();
        Import[] callerGuarantees = caller.GetGuaranteedPrecodeFixups(new HashSet<Import>());

        if (moduleEager)
        {
            HashSet<Import> eagerFixups = [cell];
            callee.ApplyModuleEagerFixups(eagerFixups);
            caller.ApplyModuleEagerFixups(eagerFixups);
            Assert.Empty(callee.Fixups);
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
        SetDirectCallTargets(left, leaf);
        left.Fixups.Add(leftCell);
        left.FinalizePrecodeFixups(scratch: scratch);
        MethodWithGCInfo right = new(method);
        SetDirectCallTargets(right, leaf);
        right.Fixups.Add(rightCell);
        right.FinalizePrecodeFixups(scratch: scratch);
        MethodWithGCInfo outer = new(method);
        SetDirectCallTargets(outer, left, right);
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
        SetDirectCallTargets(caller, callee);
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

    private static void SetDirectCallTargets(MethodWithGCInfo caller, params MethodWithGCInfo[] targets)
    {
        NodeFactory factory = (NodeFactory)RuntimeHelpers.GetUninitializedObject(typeof(NodeFactory));
        Relocation[] relocations = new Relocation[targets.Length * 2];
        for (int i = 0; i < targets.Length; i++)
        {
            relocations[i * 2] = new(RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB, i * 12,
                new WasmMethodFrameIdentityNode(factory, targets[i]));
            relocations[i * 2 + 1] = new(RelocType.WASM_FUNCTION_INDEX_LEB, i * 12 + 6, targets[i]);
        }
        caller.SetCode(new ObjectNode.ObjectData([], relocations, 1, [caller]));
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

    private static ReadyToRunCompilerContext CreateContext(
        string? assemblyPath = null, TargetArchitecture targetArchitecture = TargetArchitecture.X64)
    {
        bool isWasm = targetArchitecture == TargetArchitecture.Wasm32;
        InstructionSetSupport instructionSetSupport = new(default, default, targetArchitecture);
        TargetDetails target = new(targetArchitecture, isWasm ? TargetOS.Browser : TargetOS.Linux, TargetAbi.NativeAot);
        Dictionary<string, string> inputFilePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath,
        };
        if (assemblyPath is not null)
            inputFilePaths.Add(Path.GetFileNameWithoutExtension(assemblyPath), assemblyPath);

        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: !isWasm, targetIsSingleThreaded: isWasm,
            instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = inputFilePaths,
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
        context.SetSystemModule((EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib"));

        return context;
    }
}
