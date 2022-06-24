// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.Pgo;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class InstrumentationDataTableNode : HeaderTableNode
    {
        private readonly NodeFactory _factory;
        private ReadyToRunSymbolNodeFactory _symbolNodeFactory;
        private readonly MethodDesc[] _instrumentationDataMethods;
        private readonly ProfileDataManager _profileDataManager;

        public InstrumentationDataTableNode(NodeFactory factory, MethodDesc[] instrumentationDataMethods, ProfileDataManager profileDataManager)
            : base(factory.Target)
        {
            _factory = factory;
            _instrumentationDataMethods = instrumentationDataMethods;
            _profileDataManager = profileDataManager;
        }

        public void Initialize(ReadyToRunSymbolNodeFactory symbolNodeFactory)
        {
            _symbolNodeFactory = symbolNodeFactory;
        }

        class PgoValueEmitter : IPgoEncodedValueEmitter<TypeSystemEntityOrUnknown, TypeSystemEntityOrUnknown>
        {
            public PgoValueEmitter(CompilationModuleGroup compilationGroup, ReadyToRunSymbolNodeFactory factory, bool actuallyCaptureOutput)
            {
                _compilationGroup = compilationGroup;
                _symbolFactory = factory;
                _actuallyCaptureOutput = actuallyCaptureOutput;
            }

            public void Clear()
            {
                _longs.Clear();
                _imports.Clear();
                _typeConversions.Clear();
                _methodConversions.Clear();
                _unknownTypesFound = 0;
                _unknownMethodsFound = 0;
            }

            public IReadOnlyList<Import> ReferencedImports => _imports;
            List<long> _longs = new List<long>();
            List<Import> _imports = new List<Import>();
            Dictionary<TypeSystemEntityOrUnknown, int> _typeConversions = new Dictionary<TypeSystemEntityOrUnknown, int>();
            Dictionary<TypeSystemEntityOrUnknown, int> _methodConversions = new Dictionary<TypeSystemEntityOrUnknown, int>();
            int _unknownTypesFound = 0;
            int _unknownMethodsFound = 0;
            CompilationModuleGroup _compilationGroup;
            ReadyToRunSymbolNodeFactory _symbolFactory;
            bool _actuallyCaptureOutput;

            public byte[] ToByteArray()
            {
                return PgoProcessor.PgoEncodedCompressedLongGenerator(_longs).ToArray();
            }

            public bool EmitDone()
            {
                // This writer wants the done emitted as a schema terminator
                return false;
            }
            public void EmitLong(long value, long previousValue)
            {
                if (_actuallyCaptureOutput)
                    _longs.Add(value - previousValue);
            }
            public void EmitType(TypeSystemEntityOrUnknown type, TypeSystemEntityOrUnknown previousValue)
            {
                EmitLong(TypeToInt(type), TypeToInt(previousValue));
            }

            public void EmitMethod(TypeSystemEntityOrUnknown method, TypeSystemEntityOrUnknown previousValue)
            {
                EmitLong(MethodToInt(method), MethodToInt(previousValue));
            }

            private int TypeToInt(TypeSystemEntityOrUnknown handle)
            {
                if (handle.IsNull || (handle.AsType == null && handle.AsUnknown == 0))
                    return 0;

                if (_typeConversions.TryGetValue(handle, out int computedInt))
                {
                    return computedInt;
                }
                if (handle.AsType != null && _compilationGroup.VersionsWithTypeReference(handle.AsType))
                {
                    Import typeHandleImport = (Import)_symbolFactory.CreateReadyToRunHelper(ReadyToRunHelperId.TypeHandle, handle.AsType);
                    _imports.Add(typeHandleImport);

                    if (_actuallyCaptureOutput)
                    {
                        if (typeHandleImport.Table.IndexFromBeginningOfArray >= 0xF)
                        {
                            // The current implementation of this table only allows for 15 different
                            // import tables to be used. This is probably enough for long term
                            // but this code will throw if we use more import tables and attempt
                            // to encode pgo data
                            throw new Exception("Unexpected high index for table import");
                        }

                        computedInt = (typeHandleImport.IndexFromBeginningOfArray << 4) | typeHandleImport.Table.IndexFromBeginningOfArray;
                    }
                    else
                    {
                        computedInt = _imports.Count << 1;
                    }
                }
                else
                {
                    computedInt = ((++_unknownTypesFound) << 4) | 0xF;
                }
                _typeConversions.Add(handle, computedInt);
                return computedInt;
            }

            private int MethodToInt(TypeSystemEntityOrUnknown handle)
            {
                if (handle.IsNull || (handle.AsMethod == null && handle.AsUnknown == 0))
                    return 0;

                if (_methodConversions.TryGetValue(handle, out int computedInt))
                {
                    return computedInt;
                }
                if (handle.AsMethod != null && _compilationGroup.VersionsWithMethodBody(handle.AsMethod))
                {
                    EcmaMethod typicalMethod = (EcmaMethod)handle.AsMethod.GetTypicalMethodDefinition();
                    ModuleToken moduleToken = new ModuleToken(typicalMethod.Module, typicalMethod.Handle);

                    MethodWithToken tok = new MethodWithToken(handle.AsMethod, moduleToken, constrainedType: null, unboxing: false, context: null);
                    Import methodHandleImport = (Import)_symbolFactory.CreateReadyToRunHelper(ReadyToRunHelperId.MethodHandle, tok);
                    _imports.Add(methodHandleImport);

                    if (_actuallyCaptureOutput)
                    {
                        if (methodHandleImport.Table.IndexFromBeginningOfArray >= 0xF)
                        {
                            // The current implementation of this table only allows for 15 different
                            // import tables to be used. This is probably enough for long term
                            // but this code will throw if we use more import tables and attempt
                            // to encode pgo data
                            throw new Exception("Unexpected high index for table import");
                        }

                        computedInt = (methodHandleImport.IndexFromBeginningOfArray << 4) | methodHandleImport.Table.IndexFromBeginningOfArray;
                    }
                    else
                    {
                        computedInt = _imports.Count << 1;
                    }
                }
                else
                {
                    computedInt = ((++_unknownMethodsFound) << 4) | 0xF;
                }
                _methodConversions.Add(handle, computedInt);
                return computedInt;
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunInstrumentationDataTable");
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            PgoValueEmitter pgoEmitter = new PgoValueEmitter(_factory.CompilationModuleGroup, _symbolNodeFactory, false);
            foreach (MethodDesc method in _instrumentationDataMethods)
            {
                PgoProcessor.EncodePgoData(_profileDataManager[method].SchemaData, pgoEmitter, false);
            }
            DependencyListEntry[] symbols = new DependencyListEntry[pgoEmitter.ReferencedImports.Count];
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i] = new DependencyListEntry(pgoEmitter.ReferencedImports[i], "Pgo Instrumentation Data");
            }

            return new DependencyList(symbols);
        }


        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());
            }

            PgoValueEmitter pgoEmitter = new PgoValueEmitter(_factory.CompilationModuleGroup, _symbolNodeFactory, true);
            NativeWriter hashtableWriter = new NativeWriter();

            Section hashtableSection = hashtableWriter.NewSection();
            VertexHashtable vertexHashtable = new VertexHashtable();
            hashtableSection.Place(vertexHashtable);

            Dictionary<byte[], BlobVertex> uniqueInstrumentationData = new Dictionary<byte[], BlobVertex>(ByteArrayComparer.Instance);

            foreach (MethodDesc method in _instrumentationDataMethods)
            {
                pgoEmitter.Clear();
                PgoProcessor.EncodePgoData(CorInfoImpl.ConvertTypeHandleHistogramsToCompactTypeHistogramFormat(_profileDataManager[method].SchemaData, factory.CompilationModuleGroup), pgoEmitter, false);

                // In composite R2R format, always enforce owning type to let us share generic instantiations among modules
                EcmaMethod typicalMethod = (EcmaMethod)method.GetTypicalMethodDefinition();
                ModuleToken moduleToken = new ModuleToken(typicalMethod.Module, typicalMethod.Handle);

                ArraySignatureBuilder signatureBuilder = new ArraySignatureBuilder();
                signatureBuilder.EmitMethodSignature(
                    new MethodWithToken(method, moduleToken, constrainedType: null, unboxing: false, context: null),
                    enforceDefEncoding: true,
                    enforceOwningType: _factory.CompilationModuleGroup.EnforceOwningType(moduleToken.Module),
                    factory.SignatureContext,
                    isInstantiatingStub: false);
                byte[] signature = signatureBuilder.ToArray();
                BlobVertex signatureBlob = new BlobVertex(signature);

                byte[] encodedInstrumentationData = pgoEmitter.ToByteArray();
                BlobVertex instrumentationDataBlob = null;
                if (!uniqueInstrumentationData.TryGetValue(encodedInstrumentationData, out instrumentationDataBlob))
                {
                    instrumentationDataBlob = new BlobVertex(encodedInstrumentationData);
                    hashtableSection.Place(instrumentationDataBlob);
                    uniqueInstrumentationData.Add(encodedInstrumentationData, instrumentationDataBlob);
                }

                PgoInstrumentedDataWithSignatureBlobVertex pgoDataVertex = new PgoInstrumentedDataWithSignatureBlobVertex(signatureBlob, 0, instrumentationDataBlob);
                hashtableSection.Place(pgoDataVertex);
                vertexHashtable.Append(unchecked((uint)ReadyToRunHashCode.MethodHashCode(method)), pgoDataVertex);
            }

            MemoryStream hashtableContent = new MemoryStream();
            hashtableWriter.Save(hashtableContent);
            return new ObjectData(
                data: hashtableContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        public override int ClassCode => 1887299452;
    }
}
