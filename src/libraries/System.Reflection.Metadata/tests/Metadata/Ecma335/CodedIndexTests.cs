// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class CodedIndexTests
    {
        [Fact]
        public void Errors()
        {
            var badHandleKind = CustomAttributeHandle.FromRowId(1);

            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasCustomAttribute(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasConstant(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.CustomAttributeType(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasDeclSecurity(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasFieldMarshal(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasSemantics(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.Implementation(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.MemberForwarded(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.MemberRefParent(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.MethodDefOrRef(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.ResolutionScope(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.TypeDefOrRef(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.TypeDefOrRefOrSpec(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.TypeOrMethodDef(badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => CodedIndex.HasCustomDebugInformation(badHandleKind));
        }

        [Fact]
        public void HasCustomAttribute()
        {
            Assert.Equal(0, CodedIndex.HasCustomAttribute(default(MethodDefinitionHandle)));
            Assert.Equal((0xffffff << 5) | 21, CodedIndex.HasCustomAttribute(MetadataTokens.MethodSpecificationHandle(0xffffff)));

            Assert.Equal(0x20 | 0, CodedIndex.HasCustomAttribute(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x20 | 1, CodedIndex.HasCustomAttribute(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(0x20 | 2, CodedIndex.HasCustomAttribute(MetadataTokens.TypeReferenceHandle(1)));
            Assert.Equal(0x20 | 3, CodedIndex.HasCustomAttribute(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x20 | 4, CodedIndex.HasCustomAttribute(MetadataTokens.ParameterHandle(1)));
            Assert.Equal(0x20 | 5, CodedIndex.HasCustomAttribute(MetadataTokens.InterfaceImplementationHandle(1)));
            Assert.Equal(0x20 | 6, CodedIndex.HasCustomAttribute(MetadataTokens.MemberReferenceHandle(1)));
            Assert.Equal(0x20 | 7, CodedIndex.HasCustomAttribute(EntityHandle.ModuleDefinition));
            Assert.Equal(0x20 | 8, CodedIndex.HasCustomAttribute(MetadataTokens.DeclarativeSecurityAttributeHandle(1)));
            Assert.Equal(0x20 | 9, CodedIndex.HasCustomAttribute(MetadataTokens.PropertyDefinitionHandle(1)));
            Assert.Equal(0x20 | 10, CodedIndex.HasCustomAttribute(MetadataTokens.EventDefinitionHandle(1)));
            Assert.Equal(0x20 | 11, CodedIndex.HasCustomAttribute(MetadataTokens.StandaloneSignatureHandle(1)));
            Assert.Equal(0x20 | 12, CodedIndex.HasCustomAttribute(MetadataTokens.ModuleReferenceHandle(1)));
            Assert.Equal(0x20 | 13, CodedIndex.HasCustomAttribute(MetadataTokens.TypeSpecificationHandle(1)));
            Assert.Equal(0x20 | 14, CodedIndex.HasCustomAttribute(EntityHandle.AssemblyDefinition));
            Assert.Equal(0x20 | 15, CodedIndex.HasCustomAttribute(MetadataTokens.AssemblyReferenceHandle(1)));
            Assert.Equal(0x20 | 16, CodedIndex.HasCustomAttribute(MetadataTokens.AssemblyFileHandle(1)));
            Assert.Equal(0x20 | 17, CodedIndex.HasCustomAttribute(MetadataTokens.ExportedTypeHandle(1)));
            Assert.Equal(0x20 | 18, CodedIndex.HasCustomAttribute(MetadataTokens.ManifestResourceHandle(1)));
            Assert.Equal(0x20 | 19, CodedIndex.HasCustomAttribute(MetadataTokens.GenericParameterHandle(1)));
            Assert.Equal(0x20 | 20, CodedIndex.HasCustomAttribute(MetadataTokens.GenericParameterConstraintHandle(1)));
            Assert.Equal(0x20 | 21, CodedIndex.HasCustomAttribute(MetadataTokens.MethodSpecificationHandle(1)));
        }

        [Fact]
        public void HasConstant()
        {
            Assert.Equal(0, CodedIndex.HasConstant(default(FieldDefinitionHandle)));
            Assert.Equal((0xffffff << 2) | 2, CodedIndex.HasConstant(MetadataTokens.PropertyDefinitionHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.HasConstant(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(0x04 | 1, CodedIndex.HasConstant(MetadataTokens.ParameterHandle(1)));
            Assert.Equal(0x04 | 2, CodedIndex.HasConstant(MetadataTokens.PropertyDefinitionHandle(1)));
        }

        [Fact]
        public void CustomAttributeType()
        {
            Assert.Equal(2, CodedIndex.CustomAttributeType(default(MethodDefinitionHandle)));
            Assert.Equal((0xffffff << 3) | 3, CodedIndex.CustomAttributeType(MetadataTokens.MemberReferenceHandle(0xffffff)));

            Assert.Equal(0x08 | 2, CodedIndex.CustomAttributeType(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x08 | 3, CodedIndex.CustomAttributeType(MetadataTokens.MemberReferenceHandle(1)));
        }

        [Fact]
        public void HasDeclSecurity()
        {
            Assert.Equal(0, CodedIndex.HasDeclSecurity(default(TypeDefinitionHandle)));
            Assert.Equal((0xffffff << 2) | 1, CodedIndex.HasDeclSecurity(MetadataTokens.MethodDefinitionHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.HasDeclSecurity(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x04 | 1, CodedIndex.HasDeclSecurity(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x04 | 2, CodedIndex.HasDeclSecurity(EntityHandle.AssemblyDefinition));
        }

        [Fact]
        public void HasFieldMarshal()
        {
            Assert.Equal(0, CodedIndex.HasFieldMarshal(default(FieldDefinitionHandle)));
            Assert.Equal((0xffffff << 1) | 1, CodedIndex.HasFieldMarshal(MetadataTokens.ParameterHandle(0xffffff)));

            Assert.Equal(0x02 | 0, CodedIndex.HasFieldMarshal(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(0x02 | 1, CodedIndex.HasFieldMarshal(MetadataTokens.ParameterHandle(1)));
        }

        [Fact]
        public void Implementation()
        {
            Assert.Equal(0, CodedIndex.Implementation(default(AssemblyFileHandle)));
            Assert.Equal((0xffffff << 2) | 2, CodedIndex.Implementation(MetadataTokens.ExportedTypeHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.Implementation(MetadataTokens.AssemblyFileHandle(1)));
            Assert.Equal(0x04 | 1, CodedIndex.Implementation(MetadataTokens.AssemblyReferenceHandle(1)));
            Assert.Equal(0x04 | 2, CodedIndex.Implementation(MetadataTokens.ExportedTypeHandle(1)));
        }

        [Fact]
        public void MemberForwarded()
        {
            Assert.Equal(0, CodedIndex.MemberForwarded(default(FieldDefinitionHandle)));
            Assert.Equal((0xffffff << 1) | 1, CodedIndex.MemberForwarded(MetadataTokens.MethodDefinitionHandle(0xffffff)));

            Assert.Equal(0x02 | 0, CodedIndex.MemberForwarded(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(0x02 | 1, CodedIndex.MemberForwarded(MetadataTokens.MethodDefinitionHandle(1)));
        }

        [Fact]
        public void MemberRefParent()
        {
            Assert.Equal(0, CodedIndex.MemberRefParent(default(TypeDefinitionHandle)));
            Assert.Equal((0xffffff << 3) | 4, CodedIndex.MemberRefParent(MetadataTokens.TypeSpecificationHandle(0xffffff)));

            Assert.Equal(0x08 | 0, CodedIndex.MemberRefParent(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x08 | 1, CodedIndex.MemberRefParent(MetadataTokens.TypeReferenceHandle(1)));
            Assert.Equal(0x08 | 2, CodedIndex.MemberRefParent(MetadataTokens.ModuleReferenceHandle(1)));
            Assert.Equal(0x08 | 3, CodedIndex.MemberRefParent(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x08 | 4, CodedIndex.MemberRefParent(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Fact]
        public void MethodDefOrRef()
        {
            Assert.Equal(0, CodedIndex.MethodDefOrRef(default(MethodDefinitionHandle)));
            Assert.Equal((0xffffff << 1) | 1, CodedIndex.MethodDefOrRef(MetadataTokens.MemberReferenceHandle(0xffffff)));

            Assert.Equal(0x02 | 0, CodedIndex.MethodDefOrRef(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x02 | 1, CodedIndex.MethodDefOrRef(MetadataTokens.MemberReferenceHandle(1)));
        }

        [Fact]
        public void ResolutionScope()
        {
            Assert.Equal(0, CodedIndex.ResolutionScope(default(ModuleDefinitionHandle)));
            Assert.Equal((0xffffff << 2) | 3, CodedIndex.ResolutionScope(MetadataTokens.TypeReferenceHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.ResolutionScope(EntityHandle.ModuleDefinition));
            Assert.Equal(0x04 | 1, CodedIndex.ResolutionScope(MetadataTokens.ModuleReferenceHandle(1)));
            Assert.Equal(0x04 | 2, CodedIndex.ResolutionScope(MetadataTokens.AssemblyReferenceHandle(1)));
            Assert.Equal(0x04 | 3, CodedIndex.ResolutionScope(MetadataTokens.TypeReferenceHandle(1)));
        }

        [Fact]
        public void TypeDefOrRef()
        {
            Assert.Equal(0, CodedIndex.TypeDefOrRef(default(TypeDefinitionHandle)));
            Assert.Equal((0xffffff << 2) | 1, CodedIndex.TypeDefOrRef(MetadataTokens.TypeReferenceHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.TypeDefOrRef(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x04 | 1, CodedIndex.TypeDefOrRef(MetadataTokens.TypeReferenceHandle(1)));
        }

        [Fact]
        public void TypeDefOrRefOrSpec()
        {
            Assert.Equal(0, CodedIndex.TypeDefOrRefOrSpec(default(TypeDefinitionHandle)));
            Assert.Equal((0xffffff << 2) | 2, CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.TypeSpecificationHandle(0xffffff)));

            Assert.Equal(0x04 | 0, CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x04 | 1, CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.TypeReferenceHandle(1)));
            Assert.Equal(0x04 | 2, CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Fact]
        public void TypeOrMethodDef()
        {
            Assert.Equal(0, CodedIndex.TypeOrMethodDef(default(TypeDefinitionHandle)));
            Assert.Equal((0xffffff << 1) | 1, CodedIndex.TypeOrMethodDef(MetadataTokens.MethodDefinitionHandle(0xffffff)));

            Assert.Equal(0x02 | 0, CodedIndex.TypeOrMethodDef(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x02 | 1, CodedIndex.TypeOrMethodDef(MetadataTokens.MethodDefinitionHandle(1)));
        }

        [Fact]
        public void HasCustomDebugInformation()
        {
            Assert.Equal(0, CodedIndex.HasCustomDebugInformation(default(MethodDefinitionHandle)));
            Assert.Equal((0xffffff << 5) | 26, CodedIndex.HasCustomDebugInformation(MetadataTokens.ImportScopeHandle(0xffffff)));

            Assert.Equal(0x20 | 0, CodedIndex.HasCustomDebugInformation(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(0x20 | 1, CodedIndex.HasCustomDebugInformation(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(0x20 | 2, CodedIndex.HasCustomDebugInformation(MetadataTokens.TypeReferenceHandle(1)));
            Assert.Equal(0x20 | 3, CodedIndex.HasCustomDebugInformation(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(0x20 | 4, CodedIndex.HasCustomDebugInformation(MetadataTokens.ParameterHandle(1)));
            Assert.Equal(0x20 | 5, CodedIndex.HasCustomDebugInformation(MetadataTokens.InterfaceImplementationHandle(1)));
            Assert.Equal(0x20 | 6, CodedIndex.HasCustomDebugInformation(MetadataTokens.MemberReferenceHandle(1)));
            Assert.Equal(0x20 | 7, CodedIndex.HasCustomDebugInformation(EntityHandle.ModuleDefinition));
            Assert.Equal(0x20 | 8, CodedIndex.HasCustomDebugInformation(MetadataTokens.DeclarativeSecurityAttributeHandle(1)));
            Assert.Equal(0x20 | 9, CodedIndex.HasCustomDebugInformation(MetadataTokens.PropertyDefinitionHandle(1)));
            Assert.Equal(0x20 | 10, CodedIndex.HasCustomDebugInformation(MetadataTokens.EventDefinitionHandle(1)));
            Assert.Equal(0x20 | 11, CodedIndex.HasCustomDebugInformation(MetadataTokens.StandaloneSignatureHandle(1)));
            Assert.Equal(0x20 | 12, CodedIndex.HasCustomDebugInformation(MetadataTokens.ModuleReferenceHandle(1)));
            Assert.Equal(0x20 | 13, CodedIndex.HasCustomDebugInformation(MetadataTokens.TypeSpecificationHandle(1)));
            Assert.Equal(0x20 | 14, CodedIndex.HasCustomDebugInformation(EntityHandle.AssemblyDefinition));
            Assert.Equal(0x20 | 15, CodedIndex.HasCustomDebugInformation(MetadataTokens.AssemblyReferenceHandle(1)));
            Assert.Equal(0x20 | 16, CodedIndex.HasCustomDebugInformation(MetadataTokens.AssemblyFileHandle(1)));
            Assert.Equal(0x20 | 17, CodedIndex.HasCustomDebugInformation(MetadataTokens.ExportedTypeHandle(1)));
            Assert.Equal(0x20 | 18, CodedIndex.HasCustomDebugInformation(MetadataTokens.ManifestResourceHandle(1)));
            Assert.Equal(0x20 | 19, CodedIndex.HasCustomDebugInformation(MetadataTokens.GenericParameterHandle(1)));
            Assert.Equal(0x20 | 20, CodedIndex.HasCustomDebugInformation(MetadataTokens.GenericParameterConstraintHandle(1)));
            Assert.Equal(0x20 | 21, CodedIndex.HasCustomDebugInformation(MetadataTokens.MethodSpecificationHandle(1)));
            Assert.Equal(0x20 | 22, CodedIndex.HasCustomDebugInformation(MetadataTokens.DocumentHandle(1)));
            Assert.Equal(0x20 | 23, CodedIndex.HasCustomDebugInformation(MetadataTokens.LocalScopeHandle(1)));
            Assert.Equal(0x20 | 24, CodedIndex.HasCustomDebugInformation(MetadataTokens.LocalVariableHandle(1)));
            Assert.Equal(0x20 | 25, CodedIndex.HasCustomDebugInformation(MetadataTokens.LocalConstantHandle(1)));
            Assert.Equal(0x20 | 26, CodedIndex.HasCustomDebugInformation(MetadataTokens.ImportScopeHandle(1)));
        }
    }
}
