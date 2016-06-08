// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata.Ecma335
{
    public static class CodedIndex
    {
        /// <summary>
        /// Calculates a HasCustomAttribute coded index for the specified handle.
        /// </summary>
        /// <param name="handle">
        /// <see cref="MethodDefinitionHandle"/>,
        /// <see cref="FieldDefinitionHandle"/>,
        /// <see cref="TypeReferenceHandle"/>,
        /// <see cref="TypeDefinitionHandle"/>,
        /// <see cref="ParameterHandle"/>,
        /// <see cref="InterfaceImplementationHandle"/>,
        /// <see cref="MemberReferenceHandle"/>,
        /// <see cref="ModuleDefinitionHandle"/>,
        /// <see cref="DeclarativeSecurityAttributeHandle"/>,
        /// <see cref="PropertyDefinitionHandle"/>,
        /// <see cref="EventDefinitionHandle"/>,
        /// <see cref="StandaloneSignatureHandle"/>,
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="TypeSpecificationHandle"/>,
        /// <see cref="AssemblyDefinitionHandle"/>,
        /// <see cref="AssemblyReferenceHandle"/>,
        /// <see cref="AssemblyFileHandle"/>,
        /// <see cref="ExportedTypeHandle"/>,
        /// <see cref="ManifestResourceHandle"/>,
        /// <see cref="GenericParameterHandle"/>,
        /// <see cref="GenericParameterConstraintHandle"/> or
        /// <see cref="MethodSpecificationHandle"/>.
        /// </param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasCustomAttribute(EntityHandle handle) => (handle.RowId << (int)HasCustomAttributeTag.BitCount) | (int)ToHasCustomAttributeTag(handle.Kind);

        /// <summary>
        /// Calculates a HasConstant coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="ParameterHandle"/>, <see cref="FieldDefinitionHandle"/>, or <see cref="PropertyDefinitionHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasConstant(EntityHandle handle) => (handle.RowId << (int)HasConstantTag.BitCount) | (int)ToHasConstantTag(handle.Kind);

        /// <summary>
        /// Calculates a CustomAttributeType coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int CustomAttributeType(EntityHandle handle) => (handle.RowId << (int)CustomAttributeTypeTag.BitCount) | (int)ToCustomAttributeTypeTag(handle.Kind);

        /// <summary>
        /// Calculates a HasDeclSecurity coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, or <see cref="AssemblyDefinitionHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasDeclSecurity(EntityHandle handle) => (handle.RowId << (int)HasDeclSecurityTag.BitCount) | (int)ToHasDeclSecurityTag(handle.Kind);

        /// <summary>
        /// Calculates a HasFieldMarshal coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref = "ParameterHandle" /> or <see cref="FieldDefinitionHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasFieldMarshal(EntityHandle handle) => (handle.RowId << (int)HasFieldMarshalTag.BitCount) | (int)ToHasFieldMarshalTag(handle.Kind);

        /// <summary>
        /// Calculates a HasSemantics coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="EventDefinitionHandle"/> or <see cref="PropertyDefinitionHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasSemantics(EntityHandle handle) => (handle.RowId << (int)HasSemanticsTag.BitCount) | (int)ToHasSemanticsTag(handle.Kind);

        /// <summary>
        /// Calculates a Implementation coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="AssemblyFileHandle"/>, <see cref="ExportedTypeHandle"/> or <see cref="AssemblyReferenceHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int Implementation(EntityHandle handle) => (handle.RowId << (int)ImplementationTag.BitCount) | (int)ToImplementationTag(handle.Kind);

        /// <summary>
        /// Calculates a MemberForwarded coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="FieldDefinition"/>, <see cref="MethodDefinition"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int MemberForwarded(EntityHandle handle) => (handle.RowId << (int)MemberForwardedTag.BitCount) | (int)ToMemberForwardedTag(handle.Kind);

        /// <summary>
        /// Calculates a MemberRefParent coded index for the specified handle.
        /// </summary>
        /// <param name="handle">
        /// <see cref="TypeDefinitionHandle"/>, 
        /// <see cref="TypeReferenceHandle"/>, 
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="MethodDefinitionHandle"/>, or 
        /// <see cref="TypeSpecificationHandle"/>.
        /// </param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int MemberRefParent(EntityHandle handle) => (handle.RowId << (int)MemberRefParentTag.BitCount) | (int)ToMemberRefParentTag(handle.Kind);

        /// <summary>
        /// Calculates a MethodDefOrRef coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int MethodDefOrRef(EntityHandle handle) => (handle.RowId << (int)MethodDefOrRefTag.BitCount) | (int)ToMethodDefOrRefTag(handle.Kind);

        /// <summary>
        /// Calculates a ResolutionScope coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="ModuleDefinitionHandle"/>, <see cref="ModuleReferenceHandle"/>, <see cref="AssemblyReferenceHandle"/> or <see cref="TypeReferenceHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int ResolutionScope(EntityHandle handle) => (handle.RowId << (int)ResolutionScopeTag.BitCount) | (int)ToResolutionScopeTag(handle.Kind);

        /// <summary>
        /// Calculates a TypeDefOrRef coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="TypeDefinitionHandle"/> or <see cref="TypeReferenceHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int TypeDefOrRef(EntityHandle handle) => (handle.RowId << (int)TypeDefOrRefTag.BitCount) | (int)ToTypeDefOrRefTag(handle.Kind);

        /// <summary>
        /// Calculates a TypeDefOrRefOrSpec coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int TypeDefOrRefOrSpec(EntityHandle handle) => (handle.RowId << (int)TypeDefOrRefOrSpecTag.BitCount) | (int)ToTypeDefOrRefOrSpecTag(handle.Kind);

        /// <summary>
        /// Calculates a TypeOrMethodDef coded index for the specified handle.
        /// </summary>
        /// <param name="handle"><see cref="TypeDefinitionHandle"/> or <see cref="MethodDefinitionHandle"/></param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int TypeOrMethodDef(EntityHandle handle) => (handle.RowId << (int)TypeOrMethodDefTag.BitCount) | (int)ToTypeOrMethodDefTag(handle.Kind);

        /// <summary>
        /// Calculates a HasCustomDebugInformation coded index for the specified handle.
        /// </summary>
        /// <param name="handle">
        /// <see cref="MethodDefinitionHandle"/>,
        /// <see cref="FieldDefinitionHandle"/>,
        /// <see cref="TypeReferenceHandle"/>,
        /// <see cref="TypeDefinitionHandle"/>,
        /// <see cref="ParameterHandle"/>,
        /// <see cref="InterfaceImplementationHandle"/>,
        /// <see cref="MemberReferenceHandle"/>,
        /// <see cref="ModuleDefinitionHandle"/>,
        /// <see cref="DeclarativeSecurityAttributeHandle"/>,
        /// <see cref="PropertyDefinitionHandle"/>,
        /// <see cref="EventDefinitionHandle"/>,
        /// <see cref="StandaloneSignatureHandle"/>,
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="TypeSpecificationHandle"/>,
        /// <see cref="AssemblyDefinitionHandle"/>,
        /// <see cref="AssemblyReferenceHandle"/>,
        /// <see cref="AssemblyFileHandle"/>,
        /// <see cref="ExportedTypeHandle"/>,
        /// <see cref="ManifestResourceHandle"/>,
        /// <see cref="GenericParameterHandle"/>,
        /// <see cref="GenericParameterConstraintHandle"/>,
        /// <see cref="MethodSpecificationHandle"/>,
        /// <see cref="DocumentHandle"/>,
        /// <see cref="LocalScopeHandle"/>,
        /// <see cref="LocalVariableHandle"/>,
        /// <see cref="LocalConstantHandle"/> or
        /// <see cref="ImportScopeHandle"/>.
        /// </param>
        /// <exception cref="ArgumentException">Unexpected handle kind.</exception>
        public static int HasCustomDebugInformation(EntityHandle handle) => (handle.RowId << (int)HasCustomDebugInformationTag.BitCount) | (int)ToHasCustomDebugInformationTag(handle.Kind);

        private enum HasCustomAttributeTag
        {
            MethodDef = 0,
            Field = 1,
            TypeRef = 2,
            TypeDef = 3,
            Param = 4,
            InterfaceImpl = 5,
            MemberRef = 6,
            Module = 7,
            DeclSecurity = 8,
            Property = 9,
            Event = 10,
            StandAloneSig = 11,
            ModuleRef = 12,
            TypeSpec = 13,
            Assembly = 14,
            AssemblyRef = 15,
            File = 16,
            ExportedType = 17,
            ManifestResource = 18,
            GenericParam = 19,
            GenericParamConstraint = 20,
            MethodSpec = 21,

            BitCount = 5
        }

        private static HasCustomAttributeTag ToHasCustomAttributeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return HasCustomAttributeTag.MethodDef;
                case HandleKind.FieldDefinition: return HasCustomAttributeTag.Field;
                case HandleKind.TypeReference: return HasCustomAttributeTag.TypeRef;
                case HandleKind.TypeDefinition: return HasCustomAttributeTag.TypeDef;
                case HandleKind.Parameter: return HasCustomAttributeTag.Param;
                case HandleKind.InterfaceImplementation: return HasCustomAttributeTag.InterfaceImpl;
                case HandleKind.MemberReference: return HasCustomAttributeTag.MemberRef;
                case HandleKind.ModuleDefinition: return HasCustomAttributeTag.Module;
                case HandleKind.DeclarativeSecurityAttribute: return HasCustomAttributeTag.DeclSecurity;
                case HandleKind.PropertyDefinition: return HasCustomAttributeTag.Property;
                case HandleKind.EventDefinition: return HasCustomAttributeTag.Event;
                case HandleKind.StandaloneSignature: return HasCustomAttributeTag.StandAloneSig;
                case HandleKind.ModuleReference: return HasCustomAttributeTag.ModuleRef;
                case HandleKind.TypeSpecification: return HasCustomAttributeTag.TypeSpec;
                case HandleKind.AssemblyDefinition: return HasCustomAttributeTag.Assembly;
                case HandleKind.AssemblyReference: return HasCustomAttributeTag.AssemblyRef;
                case HandleKind.AssemblyFile: return HasCustomAttributeTag.File;
                case HandleKind.ExportedType: return HasCustomAttributeTag.ExportedType;
                case HandleKind.ManifestResource: return HasCustomAttributeTag.ManifestResource;
                case HandleKind.GenericParameter: return HasCustomAttributeTag.GenericParam;
                case HandleKind.GenericParameterConstraint: return HasCustomAttributeTag.GenericParamConstraint;
                case HandleKind.MethodSpecification: return HasCustomAttributeTag.MethodSpec;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum HasConstantTag
        {
            Field = 0,
            Param = 1,
            Property = 2,

            BitCount = 2
        }

        private static HasConstantTag ToHasConstantTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return HasConstantTag.Field;
                case HandleKind.Parameter: return HasConstantTag.Param;
                case HandleKind.PropertyDefinition: return HasConstantTag.Property;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum CustomAttributeTypeTag
        {
            MethodDef = 2,
            MemberRef = 3,

            BitCount = 3
        }

        private static CustomAttributeTypeTag ToCustomAttributeTypeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return CustomAttributeTypeTag.MethodDef;
                case HandleKind.MemberReference: return CustomAttributeTypeTag.MemberRef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum HasDeclSecurityTag
        {
            TypeDef = 0,
            MethodDef = 1,
            Assembly = 2,

            BitCount = 2
        }

        private static HasDeclSecurityTag ToHasDeclSecurityTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return HasDeclSecurityTag.TypeDef;
                case HandleKind.MethodDefinition: return HasDeclSecurityTag.MethodDef;
                case HandleKind.AssemblyDefinition: return HasDeclSecurityTag.Assembly;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum HasFieldMarshalTag
        {
            Field = 0,
            Param = 1,

            BitCount = 1
        }

        private static HasFieldMarshalTag ToHasFieldMarshalTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return HasFieldMarshalTag.Field;
                case HandleKind.Parameter: return HasFieldMarshalTag.Param;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum HasSemanticsTag
        {
            Event = 0,
            Property = 1,

            BitCount = 1
        }

        private static HasSemanticsTag ToHasSemanticsTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.EventDefinition: return HasSemanticsTag.Event;
                case HandleKind.PropertyDefinition: return HasSemanticsTag.Property;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum ImplementationTag
        {
            File = 0,
            AssemblyRef = 1,
            ExportedType = 2,

            BitCount = 2
        }

        private static ImplementationTag ToImplementationTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.AssemblyFile: return ImplementationTag.File;
                case HandleKind.AssemblyReference: return ImplementationTag.AssemblyRef;
                case HandleKind.ExportedType: return ImplementationTag.ExportedType;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum MemberForwardedTag
        {
            Field = 0,
            MethodDef = 1,

            BitCount = 1
        }

        private static MemberForwardedTag ToMemberForwardedTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return MemberForwardedTag.Field;
                case HandleKind.MethodDefinition: return MemberForwardedTag.MethodDef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum MemberRefParentTag
        {
            TypeDef = 0,
            TypeRef = 1,
            ModuleRef = 2,
            MethodDef = 3,
            TypeSpec = 4,

            BitCount = 3
        }

        private static MemberRefParentTag ToMemberRefParentTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return MemberRefParentTag.TypeDef;
                case HandleKind.TypeReference: return MemberRefParentTag.TypeRef;
                case HandleKind.ModuleReference: return MemberRefParentTag.ModuleRef;
                case HandleKind.MethodDefinition: return MemberRefParentTag.MethodDef;
                case HandleKind.TypeSpecification: return MemberRefParentTag.TypeSpec;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum MethodDefOrRefTag
        {
            MethodDef = 0,
            MemberRef = 1,

            BitCount = 1
        }

        private static MethodDefOrRefTag ToMethodDefOrRefTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return MethodDefOrRefTag.MethodDef;
                case HandleKind.MemberReference: return MethodDefOrRefTag.MemberRef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum ResolutionScopeTag
        {
            Module = 0,
            ModuleRef = 1,
            AssemblyRef = 2,
            TypeRef = 3,

            BitCount = 2
        }

        private static ResolutionScopeTag ToResolutionScopeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.ModuleDefinition: return ResolutionScopeTag.Module;
                case HandleKind.ModuleReference: return ResolutionScopeTag.ModuleRef;
                case HandleKind.AssemblyReference: return ResolutionScopeTag.AssemblyRef;
                case HandleKind.TypeReference: return ResolutionScopeTag.TypeRef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum TypeDefOrRefOrSpecTag
        {
            TypeDef = 0,
            TypeRef = 1,
            TypeSpec = 2,

            BitCount = 2
        }

        private static TypeDefOrRefOrSpecTag ToTypeDefOrRefOrSpecTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return TypeDefOrRefOrSpecTag.TypeDef;
                case HandleKind.TypeReference: return TypeDefOrRefOrSpecTag.TypeRef;
                case HandleKind.TypeSpecification: return TypeDefOrRefOrSpecTag.TypeSpec;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum TypeDefOrRefTag
        {
            TypeDef = 0,
            TypeRef = 1,

            BitCount = 2
        }

        private static TypeDefOrRefTag ToTypeDefOrRefTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return TypeDefOrRefTag.TypeDef;
                case HandleKind.TypeReference: return TypeDefOrRefTag.TypeRef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum TypeOrMethodDefTag
        {
            TypeDef = 0,
            MethodDef = 1,

            BitCount = 1
        }

        private static TypeOrMethodDefTag ToTypeOrMethodDefTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return TypeOrMethodDefTag.TypeDef;
                case HandleKind.MethodDefinition: return TypeOrMethodDefTag.MethodDef;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }

        private enum HasCustomDebugInformationTag
        {
            MethodDef = 0,
            Field = 1,
            TypeRef = 2,
            TypeDef = 3,
            Param = 4,
            InterfaceImpl = 5,
            MemberRef = 6,
            Module = 7,
            DeclSecurity = 8,
            Property = 9,
            Event = 10,
            StandAloneSig = 11,
            ModuleRef = 12,
            TypeSpec = 13,
            Assembly = 14,
            AssemblyRef = 15,
            File = 16,
            ExportedType = 17,
            ManifestResource = 18,
            GenericParam = 19,
            GenericParamConstraint = 20,
            MethodSpec = 21,
            Document = 22,
            LocalScope = 23,
            LocalVariable = 24,
            LocalConstant = 25,
            ImportScope = 26,

            BitCount = 5
        }

        private static HasCustomDebugInformationTag ToHasCustomDebugInformationTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return HasCustomDebugInformationTag.MethodDef;
                case HandleKind.FieldDefinition: return HasCustomDebugInformationTag.Field;
                case HandleKind.TypeReference: return HasCustomDebugInformationTag.TypeRef;
                case HandleKind.TypeDefinition: return HasCustomDebugInformationTag.TypeDef;
                case HandleKind.Parameter: return HasCustomDebugInformationTag.Param;
                case HandleKind.InterfaceImplementation: return HasCustomDebugInformationTag.InterfaceImpl;
                case HandleKind.MemberReference: return HasCustomDebugInformationTag.MemberRef;
                case HandleKind.ModuleDefinition: return HasCustomDebugInformationTag.Module;
                case HandleKind.DeclarativeSecurityAttribute: return HasCustomDebugInformationTag.DeclSecurity;
                case HandleKind.PropertyDefinition: return HasCustomDebugInformationTag.Property;
                case HandleKind.EventDefinition: return HasCustomDebugInformationTag.Event;
                case HandleKind.StandaloneSignature: return HasCustomDebugInformationTag.StandAloneSig;
                case HandleKind.ModuleReference: return HasCustomDebugInformationTag.ModuleRef;
                case HandleKind.TypeSpecification: return HasCustomDebugInformationTag.TypeSpec;
                case HandleKind.AssemblyDefinition: return HasCustomDebugInformationTag.Assembly;
                case HandleKind.AssemblyReference: return HasCustomDebugInformationTag.AssemblyRef;
                case HandleKind.AssemblyFile: return HasCustomDebugInformationTag.File;
                case HandleKind.ExportedType: return HasCustomDebugInformationTag.ExportedType;
                case HandleKind.ManifestResource: return HasCustomDebugInformationTag.ManifestResource;
                case HandleKind.GenericParameter: return HasCustomDebugInformationTag.GenericParam;
                case HandleKind.GenericParameterConstraint: return HasCustomDebugInformationTag.GenericParamConstraint;
                case HandleKind.MethodSpecification: return HasCustomDebugInformationTag.MethodSpec;
                case HandleKind.Document: return HasCustomDebugInformationTag.Document;
                case HandleKind.LocalScope: return HasCustomDebugInformationTag.LocalScope;
                case HandleKind.LocalVariable: return HasCustomDebugInformationTag.LocalVariable;
                case HandleKind.LocalConstant: return HasCustomDebugInformationTag.LocalConstant;
                case HandleKind.ImportScope: return HasCustomDebugInformationTag.ImportScope;

                default:
                    Throw.InvalidArgument_UnexpectedHandleKind(kind);
                    return 0;
            }
        }
    }
}
