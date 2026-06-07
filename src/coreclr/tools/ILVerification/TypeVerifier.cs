// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeVerifier
{
    internal class TypeVerifier
    {
        private readonly EcmaModule _module;
        private readonly TypeDefinitionHandle _typeDefinitionHandle;
        private readonly ILVerifyTypeSystemContext _typeSystemContext;
        private readonly VerifierOptions _verifierOptions;

        public Action<VerifierError, object[]> ReportVerificationError
        {
            set;
            private get;
        }

        private void VerificationError(VerifierError error, params object[] args)
        {
            ReportVerificationError(error, args);
        }

        public TypeVerifier(EcmaModule module, TypeDefinitionHandle typeDefinitionHandle, ILVerifyTypeSystemContext typeSystemContext, VerifierOptions verifierOptions)
        {
            _module = module;
            _typeDefinitionHandle = typeDefinitionHandle;
            _typeSystemContext = typeSystemContext;
            _verifierOptions = verifierOptions;
        }

        public void Verify()
        {
            VerifyBaseType();
            VerifyInterfaces();
        }

        private void VerifyBaseType()
        {
            TypeDefinition typeDefinition = _module.MetadataReader.GetTypeDefinition(_typeDefinitionHandle);
            EcmaType type = _module.GetType(_typeDefinitionHandle);
            EntityHandle baseType = typeDefinition.BaseType;
            if (baseType.IsNil)
            {
                if (!type.IsObject && !type.IsModuleType && !type.IsInterface)
                {
                    VerificationError(VerifierError.InvalidBaseType, Format(type), Format(baseType));
                }
            }
            else if (baseType.Kind == HandleKind.TypeSpecification)
            {
                if (!IsValidBaseTypeSpecification((TypeSpecificationHandle)baseType))
                {
                    VerificationError(VerifierError.InvalidBaseType, Format(type), Format(baseType));
                }

            }
            else if (IsValueType(baseType))
            {
                VerificationError(VerifierError.InvalidBaseType, Format(type), Format(baseType));
            }
        }

        private bool IsValueType(EntityHandle typeHandle)
        {
            return _module.GetType(typeHandle).IsValueType;
        }

        private bool IsValidBaseTypeSpecification(TypeSpecificationHandle typeSpecificationHandle)
        {
            try
            {
                TypeSpecification typeSpecification = _module.MetadataReader.GetTypeSpecification(typeSpecificationHandle);
                BlobReader signatureReader = _module.MetadataReader.GetBlobReader(typeSpecification.Signature);

                if (signatureReader.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                {
                    return false;
                }

                int genericTypeKind = signatureReader.ReadCompressedInteger();
                if (genericTypeKind != (int)SignatureTypeKind.Class)
                {
                    return false;
                }

                EntityHandle genericTypeHandle = signatureReader.ReadTypeHandle();
                if (genericTypeHandle.Kind != HandleKind.TypeDefinition &&
                    genericTypeHandle.Kind != HandleKind.TypeReference)
                {
                    return false;
                }

                if (IsValueType(genericTypeHandle))
                {
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        public void VerifyInterfaces()
        {
            TypeDefinition typeDefinition = _module.MetadataReader.GetTypeDefinition(_typeDefinitionHandle);
            EcmaType type = _module.GetType(_typeDefinitionHandle);

            if (type.IsInterface)
            {
                return;
            }

            InterfaceImplementationHandleCollection interfaceHandles = typeDefinition.GetInterfaceImplementations();
            int count = interfaceHandles.Count;
            if (count == 0)
            {
                return;
            }

            // Look for duplicates and prepare distinct list of implemented interfaces to avoid
            // subsequent error duplication
            List<InterfaceMetadataObjects> implementedInterfaces = new List<InterfaceMetadataObjects>();
            foreach (InterfaceImplementationHandle interfaceHandle in interfaceHandles)
            {
                InterfaceImplementation interfaceImplementation = _module.MetadataReader.GetInterfaceImplementation(interfaceHandle);
                TypeDesc interfaceTypeDesc = _module.GetType(interfaceImplementation.Interface) as TypeDesc;
                if (interfaceTypeDesc == null)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, type);
                }

                InterfaceMetadataObjects imo = new InterfaceMetadataObjects
                {
                    InterfaceType = interfaceTypeDesc,
                    InterfaceImplementation = interfaceImplementation
                };

                if (!implementedInterfaces.Contains(imo))
                {
                    implementedInterfaces.Add(imo);
                }
                else
                {
                    VerificationError(VerifierError.InterfaceImplHasDuplicate, Format(type), Format(imo.InterfaceType, _module, imo.InterfaceImplementation));
                }
            }

            foreach (InterfaceMetadataObjects implementedInterface in implementedInterfaces)
            {
                if (!type.IsAbstract)
                {
                    // Look for missing method implementation
                    foreach (MethodDesc method in implementedInterface.InterfaceType.GetAllMethods())
                    {
                        if (!method.IsAbstract)
                        {
                            continue;
                        }

                        if (type.ResolveInterfaceMethodTarget(method) is not MethodDesc resolvedMethod)
                        {
                            type.ResolveInterfaceMethodToDefaultImplementationOnType(method, out resolvedMethod);
                        }

                        if (resolvedMethod is null)
                        {
                            VerificationError(VerifierError.InterfaceMethodNotImplemented, Format(type), Format(implementedInterface.InterfaceType, _module, implementedInterface.InterfaceImplementation), Format(method));
                        }
                    }
                }
            }
        }

        private string Format(TypeDesc type)
        {
            if (_verifierOptions.IncludeMetadataTokensInErrorMessages)
            {
                // type can be an InstantiatedType, so use the TypeDef to get the metadata token.
                TypeDesc typeDesc = type.GetTypeDefinition();
                EcmaModule module = (EcmaModule)((MetadataType)typeDesc).Module;

                return string.Format("{0}([{1}]0x{2:X8})", type, module, module.MetadataReader.GetToken(((EcmaType)typeDesc).Handle));
            }
            else
            {
                return type.ToString();
            }
        }

        private string Format(EntityHandle handle)
        {
            if (handle.IsNil)
            {
                return "nil";
            }

            if (handle.Kind == HandleKind.TypeDefinition ||
                handle.Kind == HandleKind.TypeReference ||
                handle.Kind == HandleKind.TypeSpecification)
            {
                return Format(_module.GetType(handle));
            }

            if (_verifierOptions.IncludeMetadataTokensInErrorMessages)
            {
                return string.Format("{0}([{1}]0x{2:X8})", handle.Kind, _module, _module.MetadataReader.GetToken(handle));
            }
            else
            {
                return handle.Kind.ToString();
            }
        }

        private string Format(TypeDesc interfaceTypeDesc, EcmaModule module, InterfaceImplementation interfaceImplementation)
        {
            if (_verifierOptions.IncludeMetadataTokensInErrorMessages)
            {
                return string.Format("{0}([{1}]0x{2:X8})", interfaceTypeDesc, module, module.MetadataReader.GetToken(interfaceImplementation.Interface));
            }
            else
            {
                return interfaceTypeDesc.ToString();
            }
        }

        private string Format(MethodDesc methodDesc)
        {
            if (_verifierOptions.IncludeMetadataTokensInErrorMessages)
            {
                TypeDesc typeDesc = methodDesc.OwningType.GetTypeDefinition();
                EcmaModule module = (EcmaModule)((MetadataType)typeDesc).Module;

                return string.Format("{0}([{1}]0x{2:X8})", methodDesc, module, module.MetadataReader.GetToken(((EcmaMethod)methodDesc.GetTypicalMethodDefinition()).Handle));
            }
            else
            {
                return methodDesc.ToString();
            }
        }

        private class InterfaceMetadataObjects : IEquatable<InterfaceMetadataObjects>
        {
            public TypeDesc InterfaceType { get; set; }
            public InterfaceImplementation InterfaceImplementation { get; set; }
            public bool Equals(InterfaceMetadataObjects other)
            {
                return other.InterfaceType == InterfaceType;
            }
        }
    }
}
