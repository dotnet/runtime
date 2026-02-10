// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class ImportReferenceProvider : INativeFormatTypeReferenceProvider
    {
        private ReadyToRunSymbolNodeFactory _symbolNodeFactory;

        public Import GetImportToType(TypeDesc type)
        {
            return _symbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.TypeHandle, type);
        }

        public void Initialize(ReadyToRunSymbolNodeFactory symbolNodeFactory)
        {
            _symbolNodeFactory = symbolNodeFactory;
        }

        Vertex INativeFormatTypeReferenceProvider.EncodeReferenceToMethod(NativeWriter writer, MethodDesc method) => throw new NotImplementedException();
        Vertex INativeFormatTypeReferenceProvider.EncodeReferenceToType(NativeWriter writer, TypeDesc type)
        {
            Import typeImport = GetImportToType(type);
            return writer.GetTuple(writer.GetUnsignedConstant((uint)typeImport.Table.IndexFromBeginningOfArray), writer.GetUnsignedConstant((uint)typeImport.IndexFromBeginningOfArray));
        }
    }
}
