// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public struct EcmaSignatureAnalyzer
    {
        private readonly EcmaModule _module;
        private BlobReader _blobReader;
        private readonly NodeFactory _factory;
        private readonly DependencySink<NodeFactory> _dependencies;

        private EcmaSignatureAnalyzer(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            _module = module;
            _blobReader = blobReader;
            _factory = factory;
            _dependencies = dependencies;
        }

        private void AnalyzeCustomModifier(SignatureTypeCode typeCode)
        {
            _dependencies.Add(_factory.GetNodeForTypeToken(_module, _blobReader.ReadTypeHandle()), "Custom modifier");
        }

        private void AnalyzeType()
        {
            AnalyzeType(_blobReader.ReadSignatureTypeCode());
        }

        private void AnalyzeType(SignatureTypeCode typeCode)
        {
        again:
            switch (typeCode)
            {
                case SignatureTypeCode.Void:
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.Single:
                case SignatureTypeCode.Double:
                case SignatureTypeCode.Char:
                case SignatureTypeCode.String:
                case SignatureTypeCode.IntPtr:
                case SignatureTypeCode.UIntPtr:
                case SignatureTypeCode.Object:
                case SignatureTypeCode.TypedReference:
                    break;
                case SignatureTypeCode.GenericTypeParameter:
                case SignatureTypeCode.GenericMethodParameter:
                    _blobReader.ReadCompressedInteger();
                    break;
                case SignatureTypeCode.TypeHandle:
                    _dependencies.Add(_factory.GetNodeForTypeToken(_module, _blobReader.ReadTypeHandle()), "Signature reference");
                    break;
                case SignatureTypeCode.SZArray:
                case SignatureTypeCode.Pointer:
                case SignatureTypeCode.ByReference:
                // Allthough multi-dimension arrays have additional rank and bounds information
                // We dont need it in the analyzer phase
                case SignatureTypeCode.Array: 
                    AnalyzeType();
                    break;
                case SignatureTypeCode.RequiredModifier:
                case SignatureTypeCode.OptionalModifier:
                    AnalyzeCustomModifier(typeCode);
                    typeCode = _blobReader.ReadSignatureTypeCode();
                    goto again;
                case SignatureTypeCode.GenericTypeInstance:
                    _blobReader.ReadCompressedInteger();
                    _dependencies.Add(_factory.GetNodeForTypeToken(_module, _blobReader.ReadTypeHandle()), "Signature reference");
                    int numGenericArgs = _blobReader.ReadCompressedInteger();
                    for (int i = 0; i < numGenericArgs; i++)
                    {
                        AnalyzeType();
                    }
                    break;
                case SignatureTypeCode.FunctionPointer:
                    AnalyzeMethodSignature();
                    break;
                default:
                    throw new BadImageFormatException();
            }
        }

        public static void AnalyzeStandaloneSignatureBlob(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeStandaloneSignatureBlob();
        }

        private void AnalyzeStandaloneSignatureBlob()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            switch (header.Kind)
            {
                case SignatureKind.Method:
                    AnalyzeMethodSignature(header);
                    break;
                case SignatureKind.LocalVariables:
                    AnalyzeLocalVariablesBlob(header);
                    break;
                default:
                    throw new BadImageFormatException();
            }

        }

        private void AnalyzeLocalVariablesBlob(SignatureHeader header)
        { 
            int varCount = _blobReader.ReadCompressedInteger();
            for (int i = 0; i < varCount; i++)
            {
            again:
                SignatureTypeCode typeCode = _blobReader.ReadSignatureTypeCode();
                if (typeCode == SignatureTypeCode.RequiredModifier || typeCode == SignatureTypeCode.OptionalModifier)
                {
                    AnalyzeCustomModifier(typeCode);
                    goto again;
                }
                if (typeCode == SignatureTypeCode.Pinned)
                {
                    goto again;
                }
                if (typeCode == SignatureTypeCode.ByReference)
                {
                    goto again;
                }
                AnalyzeType(typeCode);
            }

        }

        public static void AnalyzeMethodSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeMethodSignature();
        }

        private void AnalyzeMethodSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            AnalyzeMethodSignature(header);
        }

        private void AnalyzeMethodSignature(SignatureHeader header)
        {
            int arity = header.IsGeneric ? _blobReader.ReadCompressedInteger() : 0;
            int paramCount = _blobReader.ReadCompressedInteger();

            // Return type
            AnalyzeType();

            for (int i = 0; i < paramCount; i++)
            {
                AnalyzeType();
            }

        }

        public static void AnalyzeFieldSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeFieldSignature();
        }

        private void AnalyzeFieldSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            AnalyzeFieldSignature(header);
        }

        private void AnalyzeFieldSignature(SignatureHeader header)
        {
            // Return type
            AnalyzeType();

        }

        public static void AnalyzeMemberReferenceSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeMemberReferenceSignature();
        }

        private void AnalyzeMemberReferenceSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            if (header.Kind == SignatureKind.Method)
            {
                AnalyzeMethodSignature(header);
            }
            else
            {
                System.Diagnostics.Debug.Assert(header.Kind == SignatureKind.Field);
                AnalyzeFieldSignature(header);
            }
        }

        public static void AnalyzeTypeSpecSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeTypeSpecSignature();
        }

        private void AnalyzeTypeSpecSignature()
        {
            AnalyzeType();
        }

        public static void AnalyzeMethodSpecSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzeMethodSpecSignature();
        }

        private void AnalyzeMethodSpecSignature()
        {

            //II.23.2.15 MethodSpec GENRICINST GenArgCount Type Type*

            if (_blobReader.ReadSignatureHeader().Kind != SignatureKind.MethodSpecification)
                ThrowHelper.ThrowBadImageFormatException();

            int count = _blobReader.ReadCompressedInteger();

            if (count <= 0)
                ThrowHelper.ThrowBadImageFormatException();

            for (int i = 0; i < count; i++)
            {
                AnalyzeType();
            }

        }

        public static void AnalyzePropertySignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencySink<NodeFactory> dependencies)
        {
            EcmaSignatureAnalyzer analyzer = new(module, blobReader, factory, dependencies);
            analyzer.AnalyzePropertySignature();
        }

        private void AnalyzePropertySignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            System.Diagnostics.Debug.Assert(header.Kind == SignatureKind.Property);
            AnalyzeMethodSignature(header);
        }
    }
}
