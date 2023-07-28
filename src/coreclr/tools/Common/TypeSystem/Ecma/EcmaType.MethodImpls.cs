// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    // This file has implementations of the .MethodImpl.cs logic from its base type.

    public sealed partial class EcmaType : MetadataType
    {
        // Virtual function related functionality
        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string declName)
        {
            MetadataReader metadataReader = _module.MetadataReader;
            var stringComparer = metadataReader.StringComparer;
            ArrayBuilder<MethodImplRecord> foundRecords = default(ArrayBuilder<MethodImplRecord>);

            foreach (var methodImplHandle in _typeDefinition.GetMethodImplementations())
            {
                MethodImplementation methodImpl = metadataReader.GetMethodImplementation(methodImplHandle);

                EntityHandle methodDeclCheckHandle = methodImpl.MethodDeclaration;
                HandleKind methodDeclHandleKind = methodDeclCheckHandle.Kind;

                // We want to check that the method name matches before actually getting the MethodDesc. For MethodSpecifications
                // we need to dereference that handle to the underlying member reference to look at name matching.
                if (methodDeclHandleKind == HandleKind.MethodSpecification)
                {
                    methodDeclCheckHandle = metadataReader.GetMethodSpecification((MethodSpecificationHandle)methodDeclCheckHandle).Method;
                    methodDeclHandleKind = methodDeclCheckHandle.Kind;
                }

                bool foundRecord = false;

                switch (methodDeclHandleKind)
                {
                    case HandleKind.MethodDefinition:
                        if (stringComparer.Equals(metadataReader.GetMethodDefinition((MethodDefinitionHandle)methodDeclCheckHandle).Name, declName))
                        {
                            foundRecord = true;
                        }
                        break;

                    case HandleKind.MemberReference:
                        if (stringComparer.Equals(metadataReader.GetMemberReference((MemberReferenceHandle)methodDeclCheckHandle).Name, declName))
                        {
                            foundRecord = true;
                        }
                        break;

                    default:
                        Debug.Fail("unexpected methodDeclHandleKind");
                        break;
                }

                if (foundRecord)
                {
                    MethodImplRecord newRecord = new MethodImplRecord(
                        (MethodDesc)_module.GetObject(methodImpl.MethodDeclaration),
                        (MethodDesc)_module.GetObject(methodImpl.MethodBody));

                    foundRecords.Add(newRecord);
                }
            }

            if (foundRecords.Count != 0)
                return foundRecords.ToArray();

            return null;
        }

        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType()
        {
            ArrayBuilder<MethodImplRecord> records = default(ArrayBuilder<MethodImplRecord>);

            MetadataReader metadataReader = _module.MetadataReader;

            foreach (var methodImplHandle in _typeDefinition.GetMethodImplementations())
            {
                MethodImplementation methodImpl = metadataReader.GetMethodImplementation(methodImplHandle);

                EntityHandle methodDeclCheckHandle = methodImpl.MethodDeclaration;
                HandleKind methodDeclHandleKind = methodDeclCheckHandle.Kind;

                // We want to check that the type is not an interface matches before actually getting the MethodDesc.
                // For MethodSpecifications we need to dereference that handle to the underlying member reference to
                // look at the owning type.
                if (methodDeclHandleKind == HandleKind.MethodSpecification)
                {
                    methodDeclCheckHandle = metadataReader.GetMethodSpecification((MethodSpecificationHandle)methodDeclCheckHandle).Method;
                    methodDeclHandleKind = methodDeclCheckHandle.Kind;
                }

                MetadataType owningType = null;
                switch (methodDeclHandleKind)
                {
                    case HandleKind.MethodDefinition:
                        owningType = ((MethodDesc)_module.GetObject(methodDeclCheckHandle)).OwningType as MetadataType;
                        break;

                    case HandleKind.MemberReference:
                        EntityHandle owningTypeHandle = metadataReader.GetMemberReference((MemberReferenceHandle)methodDeclCheckHandle).Parent;
                        owningType = _module.GetObject(owningTypeHandle) as MetadataType;
                        break;

                    default:
                        Debug.Fail("unexpected methodDeclHandleKind");
                        break;
                }

                if (!owningType.IsInterface)
                {
                    MethodImplRecord newRecord = new MethodImplRecord(
                        (MethodDesc)_module.GetObject(methodImpl.MethodDeclaration),
                        (MethodDesc)_module.GetObject(methodImpl.MethodBody));
                    records.Add(newRecord);
                }
            }

            return records.ToArray();
        }
    }
}
