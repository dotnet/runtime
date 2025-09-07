// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// An <see cref="ILProvider"/> that modifies the IL provider from the underlying IL provider
    /// and adds a ilc.i4.0/stsfld instruction at the beginning of each method.
    /// Also acts as a <see cref="ICompilationRootProvider"/> that can root the underlying data
    /// that the static field refers to.
    /// </summary>
    public class ReachabilityInstrumentationProvider : ILProvider, ICompilationRootProvider
    {
        private const string TokenUseBeginSymbol = "__tokenuse_begin";

        private readonly ILProvider _nestedProvider;
        private readonly ConcurrentDictionary<EcmaModule, ExternSymbolMappedField[]> _isTokenUsedStates = new();

        public ReachabilityInstrumentationProvider(ILProvider nestedProvider)
            => (_nestedProvider) = (nestedProvider);

        public static MethodDesc CreateInitializerMethod(CompilerTypeSystemContext context) => new InitializeMethod(context);

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.AddCompilationRoot(new ReachabilityDataBlobNode(this), "Reachability data");
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            MethodIL nestedIL = _nestedProvider.GetMethodIL(method);

            // Non-ecma methods or methods without a body don't have a matching RVA field.
            // Note: we purposefuly skip methods with genericness for now.
            if (method is not EcmaMethod ecmaMethod || nestedIL is null)
            {
                return nestedIL;
            }

            // Find the associated RVA static field
            ExternSymbolMappedField[] fields = _isTokenUsedStates.GetOrAdd(ecmaMethod.Module, m => new ExternSymbolMappedField[m.MetadataReader.MethodDefinitions.Count + 1]);
            ref ExternSymbolMappedField field = ref fields[MetadataTokens.GetRowNumber(ecmaMethod.Handle)];
            if (field == null)
            {
                string name = $"__tokenuse_{ecmaMethod.MetadataReader.GetGuid(ecmaMethod.MetadataReader.GetModuleDefinition().Mvid):N}_{MetadataTokens.GetToken(ecmaMethod.Handle):X8}";
                Interlocked.CompareExchange(ref field, new ExternSymbolMappedField(method.Context.GetWellKnownType(WellKnownType.Byte), name), null);
            }

            // Return the updated methodIL
            return new InstrumentedMethodIL(nestedIL, field);
        }

        private sealed class InstrumentedMethodIL : MethodIL
        {
            private readonly MethodIL _originalMethodIL;
            private readonly FieldDesc _accountingField;

            private const int InstrumentationPrefixByteCount = 6;

            public InstrumentedMethodIL(MethodIL originalMethodIL, FieldDesc accountingField)
                => (_originalMethodIL, _accountingField) = (originalMethodIL, accountingField);

            public override int MaxStack => Math.Max(_originalMethodIL.MaxStack, 1);

            public override bool IsInitLocals => _originalMethodIL.IsInitLocals;

            public override MethodDesc OwningMethod => _originalMethodIL.OwningMethod;

            public override ILExceptionRegion[] GetExceptionRegions()
            {
                // Exception regions are shifted due to the injected instructions
                ILExceptionRegion[] regions = _originalMethodIL.GetExceptionRegions();
                if (regions.Length > 0)
                {
                    regions = (ILExceptionRegion[])regions.Clone();
                    for (int i = 0; i < regions.Length; i++)
                    {
                        ref ILExceptionRegion region = ref regions[i];
                        region = new ILExceptionRegion(region.Kind,
                            region.TryOffset + InstrumentationPrefixByteCount, region.TryLength,
                            region.HandlerOffset + InstrumentationPrefixByteCount, region.HandlerLength,
                            region.ClassToken,
                            region.Kind == ILExceptionRegionKind.Filter ? region.FilterOffset + InstrumentationPrefixByteCount : 0);
                    }
                }
                return regions;
            }

            public override byte[] GetILBytes()
            {
                // Prefix the IL bytes with the ldc/stsfld instructions
                byte[] originalBytes = _originalMethodIL.GetILBytes();
                byte[] newBytes = new byte[originalBytes.Length + InstrumentationPrefixByteCount];

                newBytes[0] = (byte)ILOpcode.ldc_i4_1;
                newBytes[1] = (byte)ILOpcode.stsfld;
                BinaryPrimitives.WriteInt32LittleEndian(newBytes.AsSpan()[2..6], MetadataTokens.GetToken(MetadataTokens.FieldDefinitionHandle(0)));
                Array.Copy(originalBytes, 0, newBytes, InstrumentationPrefixByteCount, originalBytes.Length);

                return newBytes;
            }

            public override LocalVariableDefinition[] GetLocals() => _originalMethodIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw)
            {
                // Handle the special synthetic token injected by GetILBytes above
                if (token == MetadataTokens.GetToken(MetadataTokens.FieldDefinitionHandle(0)))
                {
                    return _accountingField;
                }

                return _originalMethodIL.GetObject(token, notFoundBehavior);
            }

            // We don't bother updating debug info, so skip for now.
            public override MethodDebugInformation GetDebugInfo() => MethodDebugInformation.None;
        }

        private sealed class ReachabilityDataBlobNode : ObjectNode, ISymbolDefinitionNode
        {
            private readonly ReachabilityInstrumentationProvider _parent;

            public ReachabilityDataBlobNode(ReachabilityInstrumentationProvider parent) => _parent = parent;

            public override bool IsShareable => false;
            public override int ClassCode => 0x66bf16;
            public override bool StaticDependenciesAreComputed => true;
            public int Offset => 0;
            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(TokenUseBeginSymbol);
            public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.DataSection;
            protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
            {
                // Shape of data:
                // __tokenuse_begin:
                //     I4: total length of blob
                //     GUID: MVID1
                //     I4: number of tokens
                // __tokenuse_MVID1_0A000001:
                //     bool: is token used?
                // ............
                // __tokenuse_MVID1_N:
                //     bool: is token used?
                // ............
                //     GUID: MVID_N
                // ............
                if (relocsOnly)
                    return new ObjectData([], [], 1, []);

                KeyValuePair<EcmaModule, ExternSymbolMappedField[]>[] useInfos = new KeyValuePair<EcmaModule, ExternSymbolMappedField[]>[_parent._isTokenUsedStates.Count];
                int i = 0;
                foreach (KeyValuePair<EcmaModule, ExternSymbolMappedField[]> item in _parent._isTokenUsedStates)
                {
                    useInfos[i++] = item;
                }

                Array.Sort(useInfos, (a, b) => TypeSystemComparer.Instance.Compare(a.Key, b.Key));

                ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
                builder.AddSymbol(this);

                ObjectDataBuilder.Reservation lengthReservation = builder.ReserveInt();

                foreach (KeyValuePair<EcmaModule, ExternSymbolMappedField[]> item in useInfos)
                {
                    Guid mvid = item.Key.MetadataReader.GetGuid(item.Key.MetadataReader.GetModuleDefinition().Mvid);
                    builder.EmitBytes(mvid.ToByteArray());
                    builder.EmitInt(item.Value.Length);

                    foreach (ExternSymbolMappedField field in item.Value)
                    {
                        if (field != null)
                        {
                            builder.AddSymbol(new UntrackedSymbol(builder.CountBytes, field.SymbolName));
                        }
                        builder.EmitByte(0);
                    }
                }

                builder.EmitInt(lengthReservation, builder.CountBytes);

                return builder.ToObjectData();
            }

            private sealed class UntrackedSymbol : ISymbolDefinitionNode
            {
                private readonly int _offset;
                private readonly string _name;

                public UntrackedSymbol(int offset, string name)
                    => (_offset, _name) = (offset, name);

                int ISymbolDefinitionNode.Offset => _offset;
                int ISymbolNode.Offset => 0;

                bool ISymbolNode.RepresentsIndirectionCell => false;
                bool IDependencyNode<NodeFactory>.InterestingForDynamicDependencyAnalysis => false;
                bool IDependencyNode<NodeFactory>.HasDynamicDependencies => false;
                bool IDependencyNode<NodeFactory>.HasConditionalStaticDependencies => false;
                bool IDependencyNode<NodeFactory>.StaticDependenciesAreComputed => true;
                bool IDependencyNode.Marked => true;

                string IDependencyNode<NodeFactory>.GetName(NodeFactory context) => _name;
                void ISymbolNode.AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(_name);
                IEnumerable<CombinedDependencyListEntry> IDependencyNode<NodeFactory>.GetConditionalStaticDependencies(NodeFactory context) => throw new NotImplementedException();
                IEnumerable<DependencyListEntry> IDependencyNode<NodeFactory>.GetStaticDependencies(NodeFactory context) => null;
                IEnumerable<CombinedDependencyListEntry> IDependencyNode<NodeFactory>.SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => throw new NotImplementedException();
            }
        }

        private sealed partial class InitializeMethod : ILStubMethod
        {
            private readonly CompilerTypeSystemContext _context;
            private readonly ExternSymbolMappedField _dataField;
            private MethodSignature _signature;

            public InitializeMethod(CompilerTypeSystemContext context)
            {
                _context = context;
                _dataField = new ExternSymbolMappedField(context.GetWellKnownType(WellKnownType.Int32), TokenUseBeginSymbol);
            }

            public override TypeSystemContext Context => _context;
            public override TypeDesc OwningType => _context.GeneratedAssembly.GetGlobalModuleType();
            public override string Name => "InitializeReachabilityInfo";
            public override ReadOnlySpan<byte> U8Name => "InitializeReachabilityInfo"u8;
            public override string DiagnosticName => "InitializeReachabilityInfo";

            public override MethodIL EmitIL()
            {
                ILEmitter emitter = new ILEmitter();
                ILCodeStream codeStream = emitter.NewCodeStream();

                ILToken dataFieldToken = emitter.NewToken(_dataField);
                codeStream.Emit(ILOpcode.ldsflda, dataFieldToken);
                codeStream.Emit(ILOpcode.ldsflda, dataFieldToken);
                codeStream.Emit(ILOpcode.ldind_i4);

                MethodDesc registerMethod = Context.GetHelperEntryPoint("ReachabilityInstrumentationSupport"u8, "Register"u8);
                codeStream.Emit(ILOpcode.call, emitter.NewToken(registerMethod));

                codeStream.Emit(ILOpcode.ret);
                return emitter.Link(this);
            }

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                Debug.Assert(this == other);
                return 0;
            }

            public override MethodSignature Signature
            {
                get
                {
                    _signature ??= new MethodSignature(MethodSignatureFlags.Static, 0,
                                Context.GetWellKnownType(WellKnownType.Void),
                                System.Array.Empty<TypeDesc>());

                    return _signature;
                }
            }

            protected override int ClassCode => 0x77da82;
        }
    }
}
