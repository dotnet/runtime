// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class HandleTests
    {
        [Fact]
        public void HandleKindsMatchSpecAndDoNotChange()
        {
            // These are chosen to match their encoding in metadata tokens as specified by the CLI spec
            Assert.Equal(0x00, (int)HandleKind.ModuleDefinition);
            Assert.Equal(0x01, (int)HandleKind.TypeReference);
            Assert.Equal(0x02, (int)HandleKind.TypeDefinition);
            Assert.Equal(0x04, (int)HandleKind.FieldDefinition);
            Assert.Equal(0x06, (int)HandleKind.MethodDefinition);
            Assert.Equal(0x08, (int)HandleKind.Parameter);
            Assert.Equal(0x09, (int)HandleKind.InterfaceImplementation);
            Assert.Equal(0x0A, (int)HandleKind.MemberReference);
            Assert.Equal(0x0B, (int)HandleKind.Constant);
            Assert.Equal(0x0C, (int)HandleKind.CustomAttribute);
            Assert.Equal(0x0E, (int)HandleKind.DeclarativeSecurityAttribute);
            Assert.Equal(0x11, (int)HandleKind.StandaloneSignature);
            Assert.Equal(0x14, (int)HandleKind.EventDefinition);
            Assert.Equal(0x17, (int)HandleKind.PropertyDefinition);
            Assert.Equal(0x19, (int)HandleKind.MethodImplementation);
            Assert.Equal(0x1A, (int)HandleKind.ModuleReference);
            Assert.Equal(0x1B, (int)HandleKind.TypeSpecification);
            Assert.Equal(0x20, (int)HandleKind.AssemblyDefinition);
            Assert.Equal(0x26, (int)HandleKind.AssemblyFile);
            Assert.Equal(0x23, (int)HandleKind.AssemblyReference);
            Assert.Equal(0x27, (int)HandleKind.ExportedType);
            Assert.Equal(0x2A, (int)HandleKind.GenericParameter);
            Assert.Equal(0x2B, (int)HandleKind.MethodSpecification);
            Assert.Equal(0x2C, (int)HandleKind.GenericParameterConstraint);
            Assert.Equal(0x28, (int)HandleKind.ManifestResource);
            Assert.Equal(0x70, (int)HandleKind.UserString);

            // These values were chosen arbitrarily, but must still never change
            Assert.Equal(0x71, (int)HandleKind.Blob);
            Assert.Equal(0x72, (int)HandleKind.Guid);
            Assert.Equal(0x78, (int)HandleKind.String);
            Assert.Equal(0x7c, (int)HandleKind.NamespaceDefinition);
        }

        [Fact]
        public void HandleConversionGivesCorrectKind()
        {
            var expectedKinds = new SortedSet<HandleKind>((HandleKind[])Enum.GetValues(typeof(HandleKind)));

            Action<Handle, HandleKind> assert = (handle, expectedKind) =>
            {
                Assert.False(expectedKinds.Count == 0, "Repeat handle in tests below.");
                Assert.Equal(expectedKind, handle.Kind);
                expectedKinds.Remove(expectedKind);
            };

            assert(default(ModuleDefinitionHandle), HandleKind.ModuleDefinition);
            assert(default(AssemblyDefinitionHandle), HandleKind.AssemblyDefinition);
            assert(default(InterfaceImplementationHandle), HandleKind.InterfaceImplementation);
            assert(default(MethodDefinitionHandle), HandleKind.MethodDefinition);
            assert(default(MethodSpecificationHandle), HandleKind.MethodSpecification);
            assert(default(TypeDefinitionHandle), HandleKind.TypeDefinition);
            assert(default(ExportedTypeHandle), HandleKind.ExportedType);
            assert(default(TypeReferenceHandle), HandleKind.TypeReference);
            assert(default(TypeSpecificationHandle), HandleKind.TypeSpecification);
            assert(default(MemberReferenceHandle), HandleKind.MemberReference);
            assert(default(FieldDefinitionHandle), HandleKind.FieldDefinition);
            assert(default(EventDefinitionHandle), HandleKind.EventDefinition);
            assert(default(PropertyDefinitionHandle), HandleKind.PropertyDefinition);
            assert(default(StandaloneSignatureHandle), HandleKind.StandaloneSignature);
            assert(default(MemberReferenceHandle), HandleKind.MemberReference);
            assert(default(FieldDefinitionHandle), HandleKind.FieldDefinition);
            assert(default(EventDefinitionHandle), HandleKind.EventDefinition);
            assert(default(PropertyDefinitionHandle), HandleKind.PropertyDefinition);
            assert(default(ParameterHandle), HandleKind.Parameter);
            assert(default(GenericParameterHandle), HandleKind.GenericParameter);
            assert(default(GenericParameterConstraintHandle), HandleKind.GenericParameterConstraint);
            assert(default(ModuleReferenceHandle), HandleKind.ModuleReference);
            assert(default(CustomAttributeHandle), HandleKind.CustomAttribute);
            assert(default(DeclarativeSecurityAttributeHandle), HandleKind.DeclarativeSecurityAttribute);
            assert(default(ManifestResourceHandle), HandleKind.ManifestResource);
            assert(default(ConstantHandle), HandleKind.Constant);
            assert(default(ManifestResourceHandle), HandleKind.ManifestResource);
            assert(default(MethodImplementationHandle), HandleKind.MethodImplementation);
            assert(default(AssemblyFileHandle), HandleKind.AssemblyFile);
            assert(default(StringHandle), HandleKind.String);
            assert(default(AssemblyReferenceHandle), HandleKind.AssemblyReference);
            assert(default(UserStringHandle), HandleKind.UserString);
            assert(default(GuidHandle), HandleKind.Guid);
            assert(default(BlobHandle), HandleKind.Blob);
            assert(default(NamespaceDefinitionHandle), HandleKind.NamespaceDefinition);
            assert(default(DocumentHandle), HandleKind.Document);
            assert(default(MethodDebugInformationHandle), HandleKind.MethodDebugInformation);
            assert(default(LocalScopeHandle), HandleKind.LocalScope);
            assert(default(LocalConstantHandle), HandleKind.LocalConstant);
            assert(default(LocalVariableHandle), HandleKind.LocalVariable);
            assert(default(ImportScopeHandle), HandleKind.ImportScope);
            assert(default(CustomDebugInformationHandle), HandleKind.CustomDebugInformation);

            Assert.True(expectedKinds.Count == 0, "Some handles are missing from this test: " + string.Join("," + Environment.NewLine, expectedKinds));
        }

        [Fact]
        public void Conversions_Handles()
        {
            Assert.Equal(1, ((ModuleDefinitionHandle)new Handle((byte)HandleType.Module, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ModuleDefinitionHandle)new Handle((byte)HandleType.Module, 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyDefinitionHandle)new Handle((byte)HandleType.Assembly, 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyDefinitionHandle)new Handle((byte)HandleType.Assembly, 0x00ffffff)).RowId);

            Assert.Equal(1, ((InterfaceImplementationHandle)new Handle((byte)HandleType.InterfaceImpl, 1)).RowId);
            Assert.Equal(0x00ffffff, ((InterfaceImplementationHandle)new Handle((byte)HandleType.InterfaceImpl, 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodDefinitionHandle)new Handle((byte)HandleType.MethodDef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodDefinitionHandle)new Handle((byte)HandleType.MethodDef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodSpecificationHandle)new Handle((byte)HandleType.MethodSpec, 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodSpecificationHandle)new Handle((byte)HandleType.MethodSpec, 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeDefinitionHandle)new Handle((byte)HandleType.TypeDef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeDefinitionHandle)new Handle((byte)HandleType.TypeDef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ExportedTypeHandle)new Handle((byte)HandleType.ExportedType, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ExportedTypeHandle)new Handle((byte)HandleType.ExportedType, 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeReferenceHandle)new Handle((byte)HandleType.TypeRef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeReferenceHandle)new Handle((byte)HandleType.TypeRef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeSpecificationHandle)new Handle((byte)HandleType.TypeSpec, 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeSpecificationHandle)new Handle((byte)HandleType.TypeSpec, 0x00ffffff)).RowId);

            Assert.Equal(1, ((MemberReferenceHandle)new Handle((byte)HandleType.MemberRef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((MemberReferenceHandle)new Handle((byte)HandleType.MemberRef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((FieldDefinitionHandle)new Handle((byte)HandleType.FieldDef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((FieldDefinitionHandle)new Handle((byte)HandleType.FieldDef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((EventDefinitionHandle)new Handle((byte)HandleType.Event, 1)).RowId);
            Assert.Equal(0x00ffffff, ((EventDefinitionHandle)new Handle((byte)HandleType.Event, 0x00ffffff)).RowId);

            Assert.Equal(1, ((PropertyDefinitionHandle)new Handle((byte)HandleType.Property, 1)).RowId);
            Assert.Equal(0x00ffffff, ((PropertyDefinitionHandle)new Handle((byte)HandleType.Property, 0x00ffffff)).RowId);

            Assert.Equal(1, ((StandaloneSignatureHandle)new Handle((byte)HandleType.Signature, 1)).RowId);
            Assert.Equal(0x00ffffff, ((StandaloneSignatureHandle)new Handle((byte)HandleType.Signature, 0x00ffffff)).RowId);

            Assert.Equal(1, ((MemberReferenceHandle)new Handle((byte)HandleType.MemberRef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((MemberReferenceHandle)new Handle((byte)HandleType.MemberRef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((FieldDefinitionHandle)new Handle((byte)HandleType.FieldDef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((FieldDefinitionHandle)new Handle((byte)HandleType.FieldDef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((EventDefinitionHandle)new Handle((byte)HandleType.Event, 1)).RowId);
            Assert.Equal(0x00ffffff, ((EventDefinitionHandle)new Handle((byte)HandleType.Event, 0x00ffffff)).RowId);

            Assert.Equal(1, ((PropertyDefinitionHandle)new Handle((byte)HandleType.Property, 1)).RowId);
            Assert.Equal(0x00ffffff, ((PropertyDefinitionHandle)new Handle((byte)HandleType.Property, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ParameterHandle)new Handle((byte)HandleType.ParamDef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ParameterHandle)new Handle((byte)HandleType.ParamDef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((GenericParameterHandle)new Handle((byte)HandleType.GenericParam, 1)).RowId);
            Assert.Equal(0x00ffffff, ((GenericParameterHandle)new Handle((byte)HandleType.GenericParam, 0x00ffffff)).RowId);

            Assert.Equal(1, ((GenericParameterConstraintHandle)new Handle((byte)HandleType.GenericParamConstraint, 1)).RowId);
            Assert.Equal(0x00ffffff, ((GenericParameterConstraintHandle)new Handle((byte)HandleType.GenericParamConstraint, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ModuleReferenceHandle)new Handle((byte)HandleType.ModuleRef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ModuleReferenceHandle)new Handle((byte)HandleType.ModuleRef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((CustomAttributeHandle)new Handle((byte)HandleType.CustomAttribute, 1)).RowId);
            Assert.Equal(0x00ffffff, ((CustomAttributeHandle)new Handle((byte)HandleType.CustomAttribute, 0x00ffffff)).RowId);

            Assert.Equal(1, ((DeclarativeSecurityAttributeHandle)new Handle((byte)HandleType.DeclSecurity, 1)).RowId);
            Assert.Equal(0x00ffffff, ((DeclarativeSecurityAttributeHandle)new Handle((byte)HandleType.DeclSecurity, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ManifestResourceHandle)new Handle((byte)HandleType.ManifestResource, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ManifestResourceHandle)new Handle((byte)HandleType.ManifestResource, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ConstantHandle)new Handle((byte)HandleType.Constant, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ConstantHandle)new Handle((byte)HandleType.Constant, 0x00ffffff)).RowId);

            Assert.Equal(1, ((ManifestResourceHandle)new Handle((byte)HandleType.ManifestResource, 1)).RowId);
            Assert.Equal(0x00ffffff, ((ManifestResourceHandle)new Handle((byte)HandleType.ManifestResource, 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyFileHandle)new Handle((byte)HandleType.File, 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyFileHandle)new Handle((byte)HandleType.File, 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodImplementationHandle)new Handle((byte)HandleType.MethodImpl, 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodImplementationHandle)new Handle((byte)HandleType.MethodImpl, 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyReferenceHandle)new Handle((byte)HandleType.AssemblyRef, 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyReferenceHandle)new Handle((byte)HandleType.AssemblyRef, 0x00ffffff)).RowId);

            Assert.Equal(1, ((UserStringHandle)new Handle((byte)HandleType.UserString, 1)).GetHeapOffset());
            Assert.Equal(0x00ffffff, ((UserStringHandle)new Handle((byte)HandleType.UserString, 0x00ffffff)).GetHeapOffset());

            Assert.Equal(1, ((GuidHandle)new Handle((byte)HandleType.Guid, 1)).Index);
            Assert.Equal(0x1fffffff, ((GuidHandle)new Handle((byte)HandleType.Guid, 0x1fffffff)).Index);
            
            Assert.Equal(1, ((NamespaceDefinitionHandle)new Handle((byte)HandleType.Namespace, 1)).GetHeapOffset());
            Assert.Equal(0x1fffffff, ((NamespaceDefinitionHandle)new Handle((byte)HandleType.Namespace, 0x1fffffff)).GetHeapOffset());

            Assert.Equal(1, ((StringHandle)new Handle((byte)HandleType.String, 1)).GetHeapOffset());
            Assert.Equal(0x1fffffff, ((StringHandle)new Handle((byte)HandleType.String, 0x1fffffff)).GetHeapOffset());

            Assert.Equal(1, ((BlobHandle)new Handle((byte)HandleType.Blob, 1)).GetHeapOffset());
            Assert.Equal(0x1fffffff, ((BlobHandle)new Handle((byte)HandleType.Blob, 0x1fffffff)).GetHeapOffset());
        }

        [Fact]
        public void Conversions_EntityHandles()
        {
            Assert.Equal(1, ((ModuleDefinitionHandle)new EntityHandle(TokenTypeIds.Module | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ModuleDefinitionHandle)new EntityHandle(TokenTypeIds.Module | 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyDefinitionHandle)new EntityHandle(TokenTypeIds.Assembly | 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyDefinitionHandle)new EntityHandle(TokenTypeIds.Assembly | 0x00ffffff)).RowId);

            Assert.Equal(1, ((InterfaceImplementationHandle)new EntityHandle(TokenTypeIds.InterfaceImpl | 1)).RowId);
            Assert.Equal(0x00ffffff, ((InterfaceImplementationHandle)new EntityHandle(TokenTypeIds.InterfaceImpl | 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodDefinitionHandle)new EntityHandle(TokenTypeIds.MethodDef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodDefinitionHandle)new EntityHandle(TokenTypeIds.MethodDef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodSpecificationHandle)new EntityHandle(TokenTypeIds.MethodSpec | 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodSpecificationHandle)new EntityHandle(TokenTypeIds.MethodSpec | 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeDefinitionHandle)new EntityHandle(TokenTypeIds.TypeDef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeDefinitionHandle)new EntityHandle(TokenTypeIds.TypeDef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ExportedTypeHandle)new EntityHandle(TokenTypeIds.ExportedType | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ExportedTypeHandle)new EntityHandle(TokenTypeIds.ExportedType | 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeReferenceHandle)new EntityHandle(TokenTypeIds.TypeRef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeReferenceHandle)new EntityHandle(TokenTypeIds.TypeRef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((TypeSpecificationHandle)new EntityHandle(TokenTypeIds.TypeSpec | 1)).RowId);
            Assert.Equal(0x00ffffff, ((TypeSpecificationHandle)new EntityHandle(TokenTypeIds.TypeSpec | 0x00ffffff)).RowId);

            Assert.Equal(1, ((MemberReferenceHandle)new EntityHandle(TokenTypeIds.MemberRef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((MemberReferenceHandle)new EntityHandle(TokenTypeIds.MemberRef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((FieldDefinitionHandle)new EntityHandle(TokenTypeIds.FieldDef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((FieldDefinitionHandle)new EntityHandle(TokenTypeIds.FieldDef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((EventDefinitionHandle)new EntityHandle(TokenTypeIds.Event | 1)).RowId);
            Assert.Equal(0x00ffffff, ((EventDefinitionHandle)new EntityHandle(TokenTypeIds.Event | 0x00ffffff)).RowId);

            Assert.Equal(1, ((PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.Property | 1)).RowId);
            Assert.Equal(0x00ffffff, ((PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.Property | 0x00ffffff)).RowId);

            Assert.Equal(1, ((StandaloneSignatureHandle)new EntityHandle(TokenTypeIds.Signature | 1)).RowId);
            Assert.Equal(0x00ffffff, ((StandaloneSignatureHandle)new EntityHandle(TokenTypeIds.Signature | 0x00ffffff)).RowId);

            Assert.Equal(1, ((MemberReferenceHandle)new EntityHandle(TokenTypeIds.MemberRef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((MemberReferenceHandle)new EntityHandle(TokenTypeIds.MemberRef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((FieldDefinitionHandle)new EntityHandle(TokenTypeIds.FieldDef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((FieldDefinitionHandle)new EntityHandle(TokenTypeIds.FieldDef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((EventDefinitionHandle)new EntityHandle(TokenTypeIds.Event | 1)).RowId);
            Assert.Equal(0x00ffffff, ((EventDefinitionHandle)new EntityHandle(TokenTypeIds.Event | 0x00ffffff)).RowId);

            Assert.Equal(1, ((PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.Property | 1)).RowId);
            Assert.Equal(0x00ffffff, ((PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.Property | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ParameterHandle)new EntityHandle(TokenTypeIds.ParamDef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ParameterHandle)new EntityHandle(TokenTypeIds.ParamDef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((GenericParameterHandle)new EntityHandle(TokenTypeIds.GenericParam | 1)).RowId);
            Assert.Equal(0x00ffffff, ((GenericParameterHandle)new EntityHandle(TokenTypeIds.GenericParam | 0x00ffffff)).RowId);

            Assert.Equal(1, ((GenericParameterConstraintHandle)new EntityHandle(TokenTypeIds.GenericParamConstraint | 1)).RowId);
            Assert.Equal(0x00ffffff, ((GenericParameterConstraintHandle)new EntityHandle(TokenTypeIds.GenericParamConstraint | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ModuleReferenceHandle)new EntityHandle(TokenTypeIds.ModuleRef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ModuleReferenceHandle)new EntityHandle(TokenTypeIds.ModuleRef | 0x00ffffff)).RowId);

            Assert.Equal(1, ((CustomAttributeHandle)new EntityHandle(TokenTypeIds.CustomAttribute | 1)).RowId);
            Assert.Equal(0x00ffffff, ((CustomAttributeHandle)new EntityHandle(TokenTypeIds.CustomAttribute | 0x00ffffff)).RowId);

            Assert.Equal(1, ((DeclarativeSecurityAttributeHandle)new EntityHandle(TokenTypeIds.DeclSecurity | 1)).RowId);
            Assert.Equal(0x00ffffff, ((DeclarativeSecurityAttributeHandle)new EntityHandle(TokenTypeIds.DeclSecurity | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ManifestResourceHandle)new EntityHandle(TokenTypeIds.ManifestResource | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ManifestResourceHandle)new EntityHandle(TokenTypeIds.ManifestResource | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ConstantHandle)new EntityHandle(TokenTypeIds.Constant | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ConstantHandle)new EntityHandle(TokenTypeIds.Constant | 0x00ffffff)).RowId);

            Assert.Equal(1, ((ManifestResourceHandle)new EntityHandle(TokenTypeIds.ManifestResource | 1)).RowId);
            Assert.Equal(0x00ffffff, ((ManifestResourceHandle)new EntityHandle(TokenTypeIds.ManifestResource | 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyFileHandle)new EntityHandle(TokenTypeIds.File | 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyFileHandle)new EntityHandle(TokenTypeIds.File | 0x00ffffff)).RowId);

            Assert.Equal(1, ((MethodImplementationHandle)new EntityHandle(TokenTypeIds.MethodImpl | 1)).RowId);
            Assert.Equal(0x00ffffff, ((MethodImplementationHandle)new EntityHandle(TokenTypeIds.MethodImpl | 0x00ffffff)).RowId);

            Assert.Equal(1, ((AssemblyReferenceHandle)new EntityHandle(TokenTypeIds.AssemblyRef | 1)).RowId);
            Assert.Equal(0x00ffffff, ((AssemblyReferenceHandle)new EntityHandle(TokenTypeIds.AssemblyRef | 0x00ffffff)).RowId);
        }

        [Fact]
        public void Conversions_VirtualHandles()
        {
            Assert.Throws<InvalidCastException>(() => (ModuleDefinitionHandle)             new Handle((byte)(HandleType.VirtualBit | HandleType.Module ), 1));
            Assert.Throws<InvalidCastException>(() => (AssemblyDefinitionHandle)           new Handle((byte)(HandleType.VirtualBit | HandleType.Assembly ), 1));
            Assert.Throws<InvalidCastException>(() => (InterfaceImplementationHandle)      new Handle((byte)(HandleType.VirtualBit | HandleType.InterfaceImpl), 1));
            Assert.Throws<InvalidCastException>(() => (MethodDefinitionHandle)             new Handle((byte)(HandleType.VirtualBit | HandleType.MethodDef ), 1));
            Assert.Throws<InvalidCastException>(() => (MethodSpecificationHandle)          new Handle((byte)(HandleType.VirtualBit | HandleType.MethodSpec), 1));
            Assert.Throws<InvalidCastException>(() => (TypeDefinitionHandle)               new Handle((byte)(HandleType.VirtualBit | HandleType.TypeDef ), 1));
            Assert.Throws<InvalidCastException>(() => (ExportedTypeHandle)                 new Handle((byte)(HandleType.VirtualBit | HandleType.ExportedType), 1));
            Assert.Throws<InvalidCastException>(() => (TypeReferenceHandle)                new Handle((byte)(HandleType.VirtualBit | HandleType.TypeRef), 1));
            Assert.Throws<InvalidCastException>(() => (TypeSpecificationHandle)            new Handle((byte)(HandleType.VirtualBit | HandleType.TypeSpec), 1));
            Assert.Throws<InvalidCastException>(() => (MemberReferenceHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.MemberRef), 1));
            Assert.Throws<InvalidCastException>(() => (FieldDefinitionHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.FieldDef ), 1));
            Assert.Throws<InvalidCastException>(() => (EventDefinitionHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.Event ), 1));
            Assert.Throws<InvalidCastException>(() => (PropertyDefinitionHandle)           new Handle((byte)(HandleType.VirtualBit | HandleType.Property ), 1));
            Assert.Throws<InvalidCastException>(() => (StandaloneSignatureHandle)          new Handle((byte)(HandleType.VirtualBit | HandleType.Signature), 1));
            Assert.Throws<InvalidCastException>(() => (MemberReferenceHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.MemberRef), 1));
            Assert.Throws<InvalidCastException>(() => (FieldDefinitionHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.FieldDef ), 1));
            Assert.Throws<InvalidCastException>(() => (EventDefinitionHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.Event ), 1));
            Assert.Throws<InvalidCastException>(() => (PropertyDefinitionHandle)           new Handle((byte)(HandleType.VirtualBit | HandleType.Property ), 1));
            Assert.Throws<InvalidCastException>(() => (ParameterHandle)                    new Handle((byte)(HandleType.VirtualBit | HandleType.ParamDef), 1));
            Assert.Throws<InvalidCastException>(() => (GenericParameterHandle)             new Handle((byte)(HandleType.VirtualBit | HandleType.GenericParam), 1));
            Assert.Throws<InvalidCastException>(() => (GenericParameterConstraintHandle)   new Handle((byte)(HandleType.VirtualBit | HandleType.GenericParamConstraint), 1));
            Assert.Throws<InvalidCastException>(() => (ModuleReferenceHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.ModuleRef), 1));
            Assert.Throws<InvalidCastException>(() => (CustomAttributeHandle)              new Handle((byte)(HandleType.VirtualBit | HandleType.CustomAttribute), 1));
            Assert.Throws<InvalidCastException>(() => (DeclarativeSecurityAttributeHandle) new Handle((byte)(HandleType.VirtualBit | HandleType.DeclSecurity), 1));
            Assert.Throws<InvalidCastException>(() => (ManifestResourceHandle)             new Handle((byte)(HandleType.VirtualBit | HandleType.ManifestResource), 1));
            Assert.Throws<InvalidCastException>(() => (ConstantHandle)                     new Handle((byte)(HandleType.VirtualBit | HandleType.Constant), 1));
            Assert.Throws<InvalidCastException>(() => (ManifestResourceHandle)             new Handle((byte)(HandleType.VirtualBit | HandleType.ManifestResource), 1));
            Assert.Throws<InvalidCastException>(() => (AssemblyFileHandle)                 new Handle((byte)(HandleType.VirtualBit | HandleType.File), 1));
            Assert.Throws<InvalidCastException>(() => (MethodImplementationHandle)         new Handle((byte)(HandleType.VirtualBit | HandleType.MethodImpl), 1));
            Assert.Throws<InvalidCastException>(() => (UserStringHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.UserString), 1));
            Assert.Throws<InvalidCastException>(() => (GuidHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.Guid), 1));

            var x1 = (AssemblyReferenceHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.AssemblyRef), 1);
            var x2 = (StringHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.String), 1);
            var x3 = (BlobHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.Blob), 1);
            var x4 = (NamespaceDefinitionHandle)new Handle((byte)(HandleType.VirtualBit | HandleType.Namespace), 1);
        }

        [Fact]
        public void Conversions_VirtualEntityHandles()
        {
            Assert.Throws<InvalidCastException>(() => (ModuleDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Module | 1));
            Assert.Throws<InvalidCastException>(() => (AssemblyDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Assembly | 1));
            Assert.Throws<InvalidCastException>(() => (InterfaceImplementationHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.InterfaceImpl | 1));
            Assert.Throws<InvalidCastException>(() => (MethodDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.MethodDef | 1));
            Assert.Throws<InvalidCastException>(() => (MethodSpecificationHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.MethodSpec | 1));
            Assert.Throws<InvalidCastException>(() => (TypeDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.TypeDef | 1));
            Assert.Throws<InvalidCastException>(() => (ExportedTypeHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.ExportedType | 1));
            Assert.Throws<InvalidCastException>(() => (TypeReferenceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.TypeRef | 1));
            Assert.Throws<InvalidCastException>(() => (TypeSpecificationHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.TypeSpec | 1));
            Assert.Throws<InvalidCastException>(() => (MemberReferenceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.MemberRef | 1));
            Assert.Throws<InvalidCastException>(() => (FieldDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.FieldDef | 1));
            Assert.Throws<InvalidCastException>(() => (EventDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Event | 1));
            Assert.Throws<InvalidCastException>(() => (PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Property | 1));
            Assert.Throws<InvalidCastException>(() => (StandaloneSignatureHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Signature | 1));
            Assert.Throws<InvalidCastException>(() => (MemberReferenceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.MemberRef | 1));
            Assert.Throws<InvalidCastException>(() => (FieldDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.FieldDef | 1));
            Assert.Throws<InvalidCastException>(() => (EventDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Event | 1));
            Assert.Throws<InvalidCastException>(() => (PropertyDefinitionHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Property | 1));
            Assert.Throws<InvalidCastException>(() => (ParameterHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.ParamDef | 1));
            Assert.Throws<InvalidCastException>(() => (GenericParameterHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.GenericParam | 1));
            Assert.Throws<InvalidCastException>(() => (GenericParameterConstraintHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.GenericParamConstraint | 1));
            Assert.Throws<InvalidCastException>(() => (ModuleReferenceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.ModuleRef | 1));
            Assert.Throws<InvalidCastException>(() => (CustomAttributeHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.CustomAttribute | 1));
            Assert.Throws<InvalidCastException>(() => (DeclarativeSecurityAttributeHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.DeclSecurity | 1));
            Assert.Throws<InvalidCastException>(() => (ManifestResourceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.ManifestResource | 1));
            Assert.Throws<InvalidCastException>(() => (ConstantHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.Constant | 1));
            Assert.Throws<InvalidCastException>(() => (ManifestResourceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.ManifestResource | 1));
            Assert.Throws<InvalidCastException>(() => (AssemblyFileHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.File | 1));
            Assert.Throws<InvalidCastException>(() => (MethodImplementationHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.MethodImpl | 1));
            var x1 = (AssemblyReferenceHandle)new EntityHandle(TokenTypeIds.VirtualBit | TokenTypeIds.AssemblyRef | 1);
        }
        
        [Fact]
        public void IsNil()
        {
            Assert.False(ModuleDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(AssemblyDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(InterfaceImplementationHandle.FromRowId(1).IsNil);
            Assert.False(MethodDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(MethodSpecificationHandle.FromRowId(1).IsNil);
            Assert.False(TypeDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(ExportedTypeHandle.FromRowId(1).IsNil);
            Assert.False(TypeReferenceHandle.FromRowId(1).IsNil);
            Assert.False(TypeSpecificationHandle.FromRowId(1).IsNil);
            Assert.False(MemberReferenceHandle.FromRowId(1).IsNil);
            Assert.False(FieldDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(EventDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(PropertyDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(StandaloneSignatureHandle.FromRowId(1).IsNil);
            Assert.False(MemberReferenceHandle.FromRowId(1).IsNil);
            Assert.False(FieldDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(EventDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(PropertyDefinitionHandle.FromRowId(1).IsNil);
            Assert.False(ParameterHandle.FromRowId(1).IsNil);
            Assert.False(GenericParameterHandle.FromRowId(1).IsNil);
            Assert.False(GenericParameterConstraintHandle.FromRowId(1).IsNil);
            Assert.False(ModuleReferenceHandle.FromRowId(1).IsNil);
            Assert.False(CustomAttributeHandle.FromRowId(1).IsNil);
            Assert.False(DeclarativeSecurityAttributeHandle.FromRowId(1).IsNil);
            Assert.False(ManifestResourceHandle.FromRowId(1).IsNil);
            Assert.False(ConstantHandle.FromRowId(1).IsNil);
            Assert.False(ManifestResourceHandle.FromRowId(1).IsNil);
            Assert.False(AssemblyFileHandle.FromRowId(1).IsNil);
            Assert.False(MethodImplementationHandle.FromRowId(1).IsNil);
            Assert.False(AssemblyReferenceHandle.FromRowId(1).IsNil);

            Assert.False(((EntityHandle)ModuleDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)AssemblyDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)InterfaceImplementationHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)MethodDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)MethodSpecificationHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)TypeDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ExportedTypeHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)TypeReferenceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)TypeSpecificationHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)MemberReferenceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)FieldDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)EventDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)PropertyDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)StandaloneSignatureHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)MemberReferenceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)FieldDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)EventDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)PropertyDefinitionHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ParameterHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)GenericParameterHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)GenericParameterConstraintHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ModuleReferenceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)CustomAttributeHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)DeclarativeSecurityAttributeHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ManifestResourceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ConstantHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)ManifestResourceHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)AssemblyFileHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)MethodImplementationHandle.FromRowId(1)).IsNil);
            Assert.False(((EntityHandle)AssemblyReferenceHandle.FromRowId(1)).IsNil);

            Assert.False(StringHandle.FromOffset(1).IsNil);
            Assert.False(BlobHandle.FromOffset(1).IsNil);
            Assert.False(UserStringHandle.FromOffset(1).IsNil);
            Assert.False(GuidHandle.FromIndex(1).IsNil);
            Assert.False(DocumentNameBlobHandle.FromOffset(1).IsNil);

            Assert.False(((Handle)StringHandle.FromOffset(1)).IsNil);
            Assert.False(((Handle)BlobHandle.FromOffset(1)).IsNil);
            Assert.False(((Handle)UserStringHandle.FromOffset(1)).IsNil);
            Assert.False(((Handle)GuidHandle.FromIndex(1)).IsNil);
            Assert.False(((BlobHandle)DocumentNameBlobHandle.FromOffset(1)).IsNil);

            Assert.True(ModuleDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(AssemblyDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(InterfaceImplementationHandle.FromRowId(0).IsNil);
            Assert.True(MethodDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(MethodSpecificationHandle.FromRowId(0).IsNil);
            Assert.True(TypeDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(ExportedTypeHandle.FromRowId(0).IsNil);
            Assert.True(TypeReferenceHandle.FromRowId(0).IsNil);
            Assert.True(TypeSpecificationHandle.FromRowId(0).IsNil);
            Assert.True(MemberReferenceHandle.FromRowId(0).IsNil);
            Assert.True(FieldDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(EventDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(PropertyDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(StandaloneSignatureHandle.FromRowId(0).IsNil);
            Assert.True(MemberReferenceHandle.FromRowId(0).IsNil);
            Assert.True(FieldDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(EventDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(PropertyDefinitionHandle.FromRowId(0).IsNil);
            Assert.True(ParameterHandle.FromRowId(0).IsNil);
            Assert.True(GenericParameterHandle.FromRowId(0).IsNil);
            Assert.True(GenericParameterConstraintHandle.FromRowId(0).IsNil);
            Assert.True(ModuleReferenceHandle.FromRowId(0).IsNil);
            Assert.True(CustomAttributeHandle.FromRowId(0).IsNil);
            Assert.True(DeclarativeSecurityAttributeHandle.FromRowId(0).IsNil);
            Assert.True(ManifestResourceHandle.FromRowId(0).IsNil);
            Assert.True(ConstantHandle.FromRowId(0).IsNil);
            Assert.True(ManifestResourceHandle.FromRowId(0).IsNil);
            Assert.True(AssemblyFileHandle.FromRowId(0).IsNil);
            Assert.True(MethodImplementationHandle.FromRowId(0).IsNil);
            Assert.True(AssemblyReferenceHandle.FromRowId(0).IsNil);

            Assert.True(((EntityHandle)ModuleDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)AssemblyDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)InterfaceImplementationHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)MethodDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)MethodSpecificationHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)TypeDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ExportedTypeHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)TypeReferenceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)TypeSpecificationHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)MemberReferenceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)FieldDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)EventDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)PropertyDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)StandaloneSignatureHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)MemberReferenceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)FieldDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)EventDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)PropertyDefinitionHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ParameterHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)GenericParameterHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)GenericParameterConstraintHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ModuleReferenceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)CustomAttributeHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)DeclarativeSecurityAttributeHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ManifestResourceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ConstantHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)ManifestResourceHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)AssemblyFileHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)MethodImplementationHandle.FromRowId(0)).IsNil);
            Assert.True(((EntityHandle)AssemblyReferenceHandle.FromRowId(0)).IsNil);

            // heaps:
            Assert.True(StringHandle.FromOffset(0).IsNil);
            Assert.True(BlobHandle.FromOffset(0).IsNil);
            Assert.True(UserStringHandle.FromOffset(0).IsNil);
            Assert.True(GuidHandle.FromIndex(0).IsNil);
            Assert.True(DocumentNameBlobHandle.FromOffset(0).IsNil);

            Assert.True(((Handle)StringHandle.FromOffset(0)).IsNil);
            Assert.True(((Handle)BlobHandle.FromOffset(0)).IsNil);
            Assert.True(((Handle)UserStringHandle.FromOffset(0)).IsNil);
            Assert.True(((Handle)GuidHandle.FromIndex(0)).IsNil);
            Assert.True(((BlobHandle)DocumentNameBlobHandle.FromOffset(0)).IsNil);

            // virtual:
            Assert.False(AssemblyReferenceHandle.FromVirtualIndex(0).IsNil);
            Assert.False(StringHandle.FromVirtualIndex(0).IsNil);
            Assert.False(BlobHandle.FromVirtualIndex(0, 0).IsNil);

            Assert.False(((Handle)AssemblyReferenceHandle.FromVirtualIndex(0)).IsNil);
            Assert.False(((Handle)StringHandle.FromVirtualIndex(0)).IsNil);
            Assert.False(((Handle)BlobHandle.FromVirtualIndex(0, 0)).IsNil);
        }

        [Fact]
        public void IsVirtual()
        {
            Assert.False(AssemblyReferenceHandle.FromRowId(1).IsVirtual);
            Assert.False(StringHandle.FromOffset(1).IsVirtual);
            Assert.False(BlobHandle.FromOffset(1).IsVirtual);

            Assert.True(AssemblyReferenceHandle.FromVirtualIndex(0).IsVirtual);
            Assert.True(StringHandle.FromVirtualIndex(0).IsVirtual);
            Assert.True(BlobHandle.FromVirtualIndex(0, 0).IsVirtual);
        }

        [Fact]
        public void StringKinds()
        {
            var str = StringHandle.FromOffset(123);
            Assert.Equal(StringKind.Plain, str.StringKind);
            Assert.False(str.IsVirtual);
            Assert.Equal(123, str.GetHeapOffset());
            Assert.Equal(str, (Handle)str);
            Assert.Equal(str, (StringHandle)(Handle)str);
            Assert.Equal(0x78, ((Handle)str).VType);
            Assert.Equal(123, ((Handle)str).Offset);

            var vstr = StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.AttributeTargets);
            Assert.Equal(StringKind.Virtual, vstr.StringKind);
            Assert.True(vstr.IsVirtual);
            Assert.Equal(StringHandle.VirtualIndex.AttributeTargets, vstr.GetVirtualIndex());
            Assert.Equal(vstr, (Handle)vstr);
            Assert.Equal(vstr, (StringHandle)(Handle)vstr);
            Assert.Equal(0xF8, ((Handle)vstr).VType);
            Assert.Equal((int)StringHandle.VirtualIndex.AttributeTargets, ((Handle)vstr).Offset);

            var dot = StringHandle.FromOffset(123).WithDotTermination();
            Assert.Equal(StringKind.DotTerminated, dot.StringKind);
            Assert.False(dot.IsVirtual);
            Assert.Equal(123, dot.GetHeapOffset());
            Assert.Equal(dot, (Handle)dot);
            Assert.Equal(dot, (StringHandle)(Handle)dot);
            Assert.Equal(0x79, ((Handle)dot).VType);
            Assert.Equal(123, ((Handle)dot).Offset);

            var winrtPrefix = StringHandle.FromOffset(123).WithWinRTPrefix();
            Assert.Equal(StringKind.WinRTPrefixed, winrtPrefix.StringKind);
            Assert.True(winrtPrefix.IsVirtual);
            Assert.Equal(123, winrtPrefix.GetHeapOffset());
            Assert.Equal(winrtPrefix, (Handle)winrtPrefix);
            Assert.Equal(winrtPrefix, (StringHandle)(Handle)winrtPrefix);
            Assert.Equal(0xF9, ((Handle)winrtPrefix).VType);
            Assert.Equal(123, ((Handle)winrtPrefix).Offset);
        }

        [Fact]
        public void NamespaceKinds()
        {
            var full = NamespaceDefinitionHandle.FromFullNameOffset(123);
            Assert.False(full.IsVirtual);
            Assert.Equal(123, full.GetHeapOffset());
            Assert.Equal(full, (Handle)full);
            Assert.Equal(full, (NamespaceDefinitionHandle)(Handle)full);
            Assert.Equal(0x7C, ((Handle)full).VType);
            Assert.Equal(123, ((Handle)full).Offset);

            var virtual1 = NamespaceDefinitionHandle.FromVirtualIndex(123);
            Assert.True(virtual1.IsVirtual);
            Assert.Equal(virtual1, (Handle)virtual1);
            Assert.Equal(virtual1, (NamespaceDefinitionHandle)(Handle)virtual1);
            Assert.Equal(0xFC, ((Handle)virtual1).VType);
            Assert.Equal(123, ((Handle)virtual1).Offset);

            var virtual2 = NamespaceDefinitionHandle.FromVirtualIndex(uint.MaxValue >> 3);
            Assert.True(virtual2.IsVirtual);
            Assert.Equal(virtual2, (Handle)virtual2);
            Assert.Equal(virtual2, (NamespaceDefinitionHandle)(Handle)virtual2);
            Assert.Equal(0xFC, ((Handle)virtual2).VType);
            Assert.Equal((int)(uint.MaxValue >> 3), ((Handle)virtual2).Offset);

            Assert.Throws<BadImageFormatException>(() => NamespaceDefinitionHandle.FromVirtualIndex((uint.MaxValue >> 3) + 1));
        }

        [Fact]
        public void HandleKindHidesSpecialStringAndNamespaces()
        {
            foreach (int virtualBit in new[] { 0, (int)HandleType.VirtualBit })
            {
                for (int i = 0; i <= sbyte.MaxValue; i++)
                {
                    Handle handle = new Handle((byte)(virtualBit | i), 0);
                    Assert.True(handle.IsNil ^ handle.IsVirtual);
                    Assert.Equal(virtualBit != 0, handle.IsVirtual);
                    Assert.Equal(handle.EntityHandleType, (uint)i << TokenTypeIds.RowIdBitCount);

                    switch (i)
                    {
                        // String has two extra bits to represent its kind that are hidden from the handle type
                        case (int)HandleKind.String:
                        case (int)HandleKind.String + 1:
                        case (int)HandleKind.String + 2:
                        case (int)HandleKind.String + 3:
                            Assert.Equal(HandleKind.String, handle.Kind);
                            break;
                        // all other types surface token type directly.
                        default:
                            Assert.Equal((int)handle.Kind, i);
                            break;
                    }
                }
            }
        }

        [Fact]
        public void MethodDefToDebugInfo()
        {
            Assert.Equal(
                MethodDefinitionHandle.FromRowId(123).ToDebugInformationHandle(), 
                MethodDebugInformationHandle.FromRowId(123));

            Assert.Equal(
                MethodDebugInformationHandle.FromRowId(123).ToDefinitionHandle(),
                MethodDefinitionHandle.FromRowId(123));
        }
    }
}
