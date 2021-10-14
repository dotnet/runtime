// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILTrim.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILTrim.DependencyAnalysis
{
    public struct EcmaSignatureAnalyzer
    {
        private readonly EcmaModule _module;
        private BlobReader _blobReader;
        private readonly NodeFactory _factory;
        private DependencyList _dependenciesOrNull;

        private DependencyList Dependencies
        {
            get
            {
                return _dependenciesOrNull ??= new DependencyList();
            }
        }

        private EcmaSignatureAnalyzer(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies)
        {
            _module = module;
            _blobReader = blobReader;
            _factory = factory;
            _dependenciesOrNull = dependencies;
        }

        private void AnalyzeCustomModifier(SignatureTypeCode typeCode)
        {
            Dependencies.Add(_factory.GetNodeForToken(_module, _blobReader.ReadTypeHandle()), "Custom modifier");
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
                    Dependencies.Add(_factory.GetNodeForToken(_module, _blobReader.ReadTypeHandle()), "Signature reference");
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
                    Dependencies.Add(_factory.GetNodeForToken(_module, _blobReader.ReadTypeHandle()), "Signature reference");
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

        public static DependencyList AnalyzeStandaloneSignatureBlob(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies = null)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeStandaloneSignatureBlob();
        }

        private DependencyList AnalyzeStandaloneSignatureBlob()
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

            return _dependenciesOrNull;
        }

        private DependencyList AnalyzeLocalVariablesBlob(SignatureHeader header)
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

            return _dependenciesOrNull;
        }

        public static DependencyList AnalyzeMethodSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies = null)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeMethodSignature();
        }

        private DependencyList AnalyzeMethodSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            return AnalyzeMethodSignature(header);
        }

        private DependencyList AnalyzeMethodSignature(SignatureHeader header)
        {
            int arity = header.IsGeneric ? _blobReader.ReadCompressedInteger() : 0;
            int paramCount = _blobReader.ReadCompressedInteger();

            // Return type
            AnalyzeType();

            for (int i = 0; i < paramCount; i++)
            {
                AnalyzeType();
            }

            return _dependenciesOrNull;
        }

        public static DependencyList AnalyzeFieldSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies = null)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeFieldSignature();
        }

        private DependencyList AnalyzeFieldSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            return AnalyzeFieldSignature(header);
        }

        private DependencyList AnalyzeFieldSignature(SignatureHeader header)
        {
            // Return type
            AnalyzeType();

            return _dependenciesOrNull;
        }

        public static DependencyList AnalyzeMemberReferenceSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies = null)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeMemberReferenceSignature();
        }

        private DependencyList AnalyzeMemberReferenceSignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            if (header.Kind == SignatureKind.Method)
            {
                return AnalyzeMethodSignature(header);
            }
            else
            {
                System.Diagnostics.Debug.Assert(header.Kind == SignatureKind.Field);
                return AnalyzeFieldSignature(header);
            }
        }

        public static DependencyList AnalyzeTypeSpecSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeTypeSpecSignature();
        }

        private DependencyList AnalyzeTypeSpecSignature()
        {
            AnalyzeType();
            return _dependenciesOrNull;
        }

        public static DependencyList AnalyzeMethodSpecSignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzeMethodSpecSignature();
        }

        private DependencyList AnalyzeMethodSpecSignature()
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

            return _dependenciesOrNull;
        }

        public static DependencyList AnalyzePropertySignature(EcmaModule module, BlobReader blobReader, NodeFactory factory, DependencyList dependencies = null)
        {
            return new EcmaSignatureAnalyzer(module, blobReader, factory, dependencies).AnalyzePropertySignature();
        }

        private DependencyList AnalyzePropertySignature()
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            System.Diagnostics.Debug.Assert(header.Kind == SignatureKind.Property);
            return AnalyzeMethodSignature(header);
        }
    }
}
