// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.TypeSystem;
using Internal.NativeFormat;
using Internal.ReadyToRunConstants;
using System.Diagnostics;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class ImportReferenceProvider : INativeFormatTypeReferenceProvider
    {
        private ReadyToRunSymbolNodeFactory _symbolNodeFactory;

        public Import GetImportToType(TypeDesc type)
        {
            return _symbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.TypeHandle, type);
        }

        public Import GetImportToModule(ModuleDesc module)
        {
            Debug.Assert(module is IEcmaModule);
            return _symbolNodeFactory.ModuleLookup((IEcmaModule)module);
        }

        public void Initialize(ReadyToRunSymbolNodeFactory symbolNodeFactory)
        {
            _symbolNodeFactory = symbolNodeFactory;
        }

        internal Vertex EncodeReferenceToModule(NativeWriter writer, ModuleDesc module)
        {
            Import typeImport = GetImportToModule(module);
            return writer.GetTuple(writer.GetUnsignedConstant((uint)typeImport.Table.IndexFromBeginningOfArray), writer.GetUnsignedConstant((uint)typeImport.IndexFromBeginningOfArray));
        }

        internal Vertex EncodeReferenceToType(NativeWriter writer, TypeDesc type)
        {
            Import typeImport = GetImportToType(type);
            return writer.GetTuple(writer.GetUnsignedConstant((uint)typeImport.Table.IndexFromBeginningOfArray), writer.GetUnsignedConstant((uint)typeImport.IndexFromBeginningOfArray));
        }

        Vertex INativeFormatTypeReferenceProvider.EncodeReferenceToMethod(NativeWriter writer, MethodDesc method) => throw new NotImplementedException();
        Vertex INativeFormatTypeReferenceProvider.EncodeReferenceToType(NativeWriter writer, TypeDesc type) => EncodeReferenceToType(writer, type);
    }
}
