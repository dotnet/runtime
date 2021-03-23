// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModelColumnDefs.h -- Table definitions for MetaData.
//

//
//*****************************************************************************

#if METAMODEL_MAJOR_VER != 2
#if METAMODEL_MAJOR_VER != 1
#error "METAMODEL_MAJOR_VER other than 1 or 2 is not implemented"
#endif
#endif
    //
    // These are used by #defining appropriately, then #including this file.
    //
    //-------------------------------------------------------------------------
    //Module
    SCHEMA_TABLE_START(Module)
    SCHEMA_ITEM(Module, USHORT, Generation)
    SCHEMA_ITEM_STRING(Module, Name)
    SCHEMA_ITEM_GUID(Module, Mvid)
    SCHEMA_ITEM_GUID(Module, EncId)
    SCHEMA_ITEM_GUID(Module, EncBaseId)
    SCHEMA_TABLE_END(Module)

    //-------------------------------------------------------------------------
    //TypeRef
    SCHEMA_TABLE_START(TypeRef)
    SCHEMA_ITEM_CDTKN(TypeRef, ResolutionScope, ResolutionScope)
    SCHEMA_ITEM_STRING(TypeRef, Name)
    SCHEMA_ITEM_STRING(TypeRef, Namespace)
    SCHEMA_TABLE_END(TypeRef)

    //-------------------------------------------------------------------------
    // TypeDef
    SCHEMA_TABLE_START(TypeDef)
    SCHEMA_ITEM(TypeDef, ULONG, Flags)
    SCHEMA_ITEM_STRING(TypeDef, Name)
    SCHEMA_ITEM_STRING(TypeDef, Namespace)
    SCHEMA_ITEM_CDTKN(TypeDef, Extends, TypeDefOrRef)
    SCHEMA_ITEM_RID(TypeDef, FieldList, Field)
    SCHEMA_ITEM_RID(TypeDef, MethodList, Method)
    SCHEMA_TABLE_END(TypeDef)

    //-------------------------------------------------------------------------
    //FieldPtr
    SCHEMA_TABLE_START(FieldPtr)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(FieldPtr, Field, Field)
    SCHEMA_TABLE_END(FieldPtr)

    //-------------------------------------------------------------------------
    //Field
    SCHEMA_TABLE_START(Field)
    SCHEMA_ITEM(Field, USHORT, Flags)
    SCHEMA_ITEM_STRING(Field,Name)
    SCHEMA_ITEM_BLOB(Field,Signature)
    SCHEMA_TABLE_END(Field)

    //-------------------------------------------------------------------------
    //MethodPtr
    SCHEMA_TABLE_START(MethodPtr)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(MethodPtr, Method, Method)
    SCHEMA_TABLE_END(MethodPtr)

    //-------------------------------------------------------------------------
    //Method
    SCHEMA_TABLE_START(Method)
    SCHEMA_ITEM(Method, ULONG, RVA)
    SCHEMA_ITEM(Method, USHORT, ImplFlags)
    SCHEMA_ITEM(Method, USHORT, Flags)
    SCHEMA_ITEM_STRING(Method,Name)
    SCHEMA_ITEM_BLOB(Method,Signature)
    SCHEMA_ITEM_RID(Method,ParamList,Param)
    SCHEMA_TABLE_END(Method)

    //-------------------------------------------------------------------------
    //ParamPtr
    SCHEMA_TABLE_START(ParamPtr)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(ParamPtr, Param, Param)
    SCHEMA_TABLE_END(ParamPtr)

    //-------------------------------------------------------------------------
    // Param
    SCHEMA_TABLE_START(Param)
    SCHEMA_ITEM(Param, USHORT, Flags)
    SCHEMA_ITEM(Param, USHORT, Sequence)
    SCHEMA_ITEM_STRING(Param,Name)
    SCHEMA_TABLE_END(Param)

    //-------------------------------------------------------------------------
    //InterfaceImpl
    SCHEMA_TABLE_START(InterfaceImpl)
    SCHEMA_ITEM_RID(InterfaceImpl,Class,TypeDef)
    SCHEMA_ITEM_CDTKN(InterfaceImpl,Interface,TypeDefOrRef)
    SCHEMA_TABLE_END(InterfaceImpl)

    //-------------------------------------------------------------------------
    //MemberRef
    SCHEMA_TABLE_START(MemberRef)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_CDTKN(MemberRef,Class,MemberRefParent)
    SCHEMA_ITEM_STRING(MemberRef,Name)
    SCHEMA_ITEM_BLOB(MemberRef,Signature)
    SCHEMA_TABLE_END(MemberRef)

    //-------------------------------------------------------------------------
    //Constant
    SCHEMA_TABLE_START(Constant)
    SCHEMA_ITEM(Constant, BYTE, Type)
    SCHEMA_ITEM_CDTKN(Constant,Parent,HasConstant)
    SCHEMA_ITEM_BLOB(Constant,Value)
    SCHEMA_TABLE_END(Constant)

    //-------------------------------------------------------------------------
    //CustomAttribute
    SCHEMA_TABLE_START(CustomAttribute)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_CDTKN(CustomAttribute,Parent,HasCustomAttribute)
    SCHEMA_ITEM_CDTKN(CustomAttribute,Type,CustomAttributeType)
    SCHEMA_ITEM_BLOB(CustomAttribute,Value)
    SCHEMA_TABLE_END(CustomAttribute)

    //-------------------------------------------------------------------------
    //FieldMarshal
    SCHEMA_TABLE_START(FieldMarshal)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_CDTKN(FieldMarshal,Parent,HasFieldMarshal)
    SCHEMA_ITEM_BLOB(FieldMarshal,NativeType)
    SCHEMA_TABLE_END(FieldMarshal)

    //-------------------------------------------------------------------------
    //DeclSecurity
    SCHEMA_TABLE_START(DeclSecurity)
    SCHEMA_ITEM(DeclSecurity, SHORT, Action)
    SCHEMA_ITEM_CDTKN(DeclSecurity,Parent,HasDeclSecurity)
    SCHEMA_ITEM_BLOB(DeclSecurity,PermissionSet)
    SCHEMA_TABLE_END(DeclSecurity)

    //-------------------------------------------------------------------------
    //ClassLayout
    SCHEMA_TABLE_START(ClassLayout)
    SCHEMA_ITEM(ClassLayout, USHORT, PackingSize)
    SCHEMA_ITEM(ClassLayout, ULONG, ClassSize)
    SCHEMA_ITEM_RID(ClassLayout,Parent,TypeDef)
    SCHEMA_TABLE_END(ClassLayout)

    //-------------------------------------------------------------------------
    //FieldLayout
    SCHEMA_TABLE_START(FieldLayout)
    SCHEMA_ITEM(FieldLayout, ULONG, OffSet)
    SCHEMA_ITEM_RID(FieldLayout, Field, Field)
    SCHEMA_TABLE_END(FieldLayout)

    //-------------------------------------------------------------------------
    //StandAloneSig
    SCHEMA_TABLE_START(StandAloneSig)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_BLOB(StandAloneSig,Signature)
    SCHEMA_TABLE_END(StandAloneSig)

    //-------------------------------------------------------------------------
    //EventMap
    SCHEMA_TABLE_START(EventMap)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(EventMap,Parent,TypeDef)
    SCHEMA_ITEM_RID(EventMap,EventList,Event)
    SCHEMA_TABLE_END(EventMap)

    //-------------------------------------------------------------------------
    //EventPtr
    SCHEMA_TABLE_START(EventPtr)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(EventPtr, Event, Event)
    SCHEMA_TABLE_END(EventPtr)

    //-------------------------------------------------------------------------
    //Event
    SCHEMA_TABLE_START(Event)
    SCHEMA_ITEM(Event, USHORT, EventFlags)
    SCHEMA_ITEM_STRING(Event,Name)
    SCHEMA_ITEM_CDTKN(Event,EventType,TypeDefOrRef)
    SCHEMA_TABLE_END(Event)

    //-------------------------------------------------------------------------
    //PropertyMap
    SCHEMA_TABLE_START(PropertyMap)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(PropertyMap,Parent,TypeDef)
    SCHEMA_ITEM_RID(PropertyMap,PropertyList,Property)
    SCHEMA_TABLE_END(PropertyMap)

    //-------------------------------------------------------------------------
    //PropertyPtr
    SCHEMA_TABLE_START(PropertyPtr)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_RID(PropertyPtr, Property, Property)
    SCHEMA_TABLE_END(PropertyPtr)

    //-------------------------------------------------------------------------
    //Property
    SCHEMA_TABLE_START(Property)
    SCHEMA_ITEM(Property, USHORT, PropFlags)
    SCHEMA_ITEM_STRING(Property,Name)
    SCHEMA_ITEM_BLOB(Property,Type)
    SCHEMA_TABLE_END(Property)

    //-------------------------------------------------------------------------
    //MethodSemantics
    SCHEMA_TABLE_START(MethodSemantics)
    SCHEMA_ITEM(MethodSemantics, USHORT, Semantic)
    SCHEMA_ITEM_RID(MethodSemantics,Method,Method)
    SCHEMA_ITEM_CDTKN(MethodSemantics,Association,HasSemantic)
    SCHEMA_TABLE_END(MethodSemantics)

    //-------------------------------------------------------------------------
    //MethodImpl
    SCHEMA_TABLE_START(MethodImpl)
    SCHEMA_ITEM_RID(MethodImpl,Class,TypeDef)
    SCHEMA_ITEM_CDTKN(MethodImpl,MethodBody,MethodDefOrRef)
    SCHEMA_ITEM_CDTKN(MethodImpl, MethodDeclaration, MethodDefOrRef)
    SCHEMA_TABLE_END(MethodImpl)

    //-------------------------------------------------------------------------
    //ModuleRef
    SCHEMA_TABLE_START(ModuleRef)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_STRING(ModuleRef, Name)
    SCHEMA_TABLE_END(ModuleRef)

    //-------------------------------------------------------------------------
    // TypeSpec
    SCHEMA_TABLE_START(TypeSpec)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_BLOB(TypeSpec,Signature)
    SCHEMA_TABLE_END(TypeSpec)

    //-------------------------------------------------------------------------
    // ENCLog
    SCHEMA_TABLE_START(ENCLog)
    SCHEMA_ITEM(ENCLog, ULONG, Token)
    SCHEMA_ITEM(ENCLog, ULONG, FuncCode)
    SCHEMA_TABLE_END(ENCLog)

    //-------------------------------------------------------------------------
    // ImplMap
    SCHEMA_TABLE_START(ImplMap)
    SCHEMA_ITEM(ImplMap, USHORT, MappingFlags)
    SCHEMA_ITEM_CDTKN(ImplMap, MemberForwarded, MemberForwarded)
    SCHEMA_ITEM_STRING(ImplMap, ImportName)
    SCHEMA_ITEM_RID(ImplMap, ImportScope, ModuleRef)
    SCHEMA_TABLE_END(ImplMap)

    //-------------------------------------------------------------------------
    // ENCMap
    SCHEMA_TABLE_START(ENCMap)
    SCHEMA_ITEM(ENCMap, ULONG, Token)
    SCHEMA_TABLE_END(ENCMap)

    //-------------------------------------------------------------------------
    // FieldRVA
    SCHEMA_TABLE_START(FieldRVA)
    SCHEMA_ITEM(FieldRVA, ULONG, RVA)
    SCHEMA_ITEM_RID(FieldRVA, Field, Field)
    SCHEMA_TABLE_END(FieldRVA)

    //-------------------------------------------------------------------------
    // Assembly
    SCHEMA_TABLE_START(Assembly)
    SCHEMA_ITEM(Assembly, ULONG, HashAlgId)
    SCHEMA_ITEM(Assembly, USHORT, MajorVersion)
    SCHEMA_ITEM(Assembly, USHORT, MinorVersion)
    SCHEMA_ITEM(Assembly, USHORT, BuildNumber)
    SCHEMA_ITEM(Assembly, USHORT, RevisionNumber)
    SCHEMA_ITEM(Assembly, ULONG, Flags)
    SCHEMA_ITEM_BLOB(Assembly, PublicKey)
    SCHEMA_ITEM_STRING(Assembly, Name)
    SCHEMA_ITEM_STRING(Assembly, Locale)
    SCHEMA_TABLE_END(Assembly)

    //-------------------------------------------------------------------------
    // AssemblyProcessor
    SCHEMA_TABLE_START(AssemblyProcessor)
    SCHEMA_ITEM(AssemblyProcessor, ULONG, Processor)
    SCHEMA_TABLE_END(AssemblyProcessor)

    //-------------------------------------------------------------------------
    // AssemblyOS
    SCHEMA_TABLE_START(AssemblyOS)
    SCHEMA_ITEM(AssemblyOS, ULONG, OSPlatformId)
    SCHEMA_ITEM(AssemblyOS, ULONG, OSMajorVersion)
    SCHEMA_ITEM(AssemblyOS, ULONG, OSMinorVersion)
    SCHEMA_TABLE_END(AssemblyOS)

    //-------------------------------------------------------------------------
    // AssemblyRef
    SCHEMA_TABLE_START(AssemblyRef)
    SCHEMA_ITEM(AssemblyRef, USHORT, MajorVersion)
    SCHEMA_ITEM(AssemblyRef, USHORT, MinorVersion)
    SCHEMA_ITEM(AssemblyRef, USHORT, BuildNumber)
    SCHEMA_ITEM(AssemblyRef, USHORT, RevisionNumber)
    SCHEMA_ITEM(AssemblyRef, ULONG, Flags)
    SCHEMA_ITEM_BLOB(AssemblyRef, PublicKeyOrToken)
    SCHEMA_ITEM_STRING(AssemblyRef, Name)
    SCHEMA_ITEM_STRING(AssemblyRef, Locale)
    SCHEMA_ITEM_BLOB(AssemblyRef, HashValue)
    SCHEMA_TABLE_END(AssemblyRef)

    //-------------------------------------------------------------------------
    // AssemblyRefProcessor
    SCHEMA_TABLE_START(AssemblyRefProcessor)
    SCHEMA_ITEM(AssemblyRefProcessor, ULONG, Processor)
    SCHEMA_ITEM_RID(AssemblyRefProcessor, AssemblyRef, AssemblyRef)
    SCHEMA_TABLE_END(AssemblyRefProcessor)

    //-------------------------------------------------------------------------
    // AssemblyRefOS
    SCHEMA_TABLE_START(AssemblyRefOS)
    SCHEMA_ITEM(AssemblyRefOS, ULONG, OSPlatformId)
    SCHEMA_ITEM(AssemblyRefOS, ULONG, OSMajorVersion)
    SCHEMA_ITEM(AssemblyRefOS, ULONG, OSMinorVersion)
    SCHEMA_ITEM_RID(AssemblyRefOS, AssemblyRef, AssemblyRef)
    SCHEMA_TABLE_END(AssemblyRefOS)

    //-------------------------------------------------------------------------
    // File
    SCHEMA_TABLE_START(File)
    SCHEMA_ITEM(File, ULONG, Flags)
    SCHEMA_ITEM_STRING(File, Name)
    SCHEMA_ITEM_BLOB(File, HashValue)
    SCHEMA_TABLE_END(File)

    //-------------------------------------------------------------------------
    // ExportedType
    SCHEMA_TABLE_START(ExportedType)
    SCHEMA_ITEM(ExportedType, ULONG, Flags)
    SCHEMA_ITEM(ExportedType, ULONG, TypeDefId)
    SCHEMA_ITEM_STRING(ExportedType, TypeName)
    SCHEMA_ITEM_STRING(ExportedType, TypeNamespace)
    SCHEMA_ITEM_CDTKN(ExportedType, Implementation, Implementation)
    SCHEMA_TABLE_END(ExportedType)

    //-------------------------------------------------------------------------
    // ManifestResource
    SCHEMA_TABLE_START(ManifestResource)
    SCHEMA_ITEM(ManifestResource, ULONG, Offset)
    SCHEMA_ITEM(ManifestResource, ULONG, Flags)
    SCHEMA_ITEM_STRING(ManifestResource, Name)
    SCHEMA_ITEM_CDTKN(ManifestResource, Implementation, Implementation)
    SCHEMA_TABLE_END(ManifestResource)

    //-------------------------------------------------------------------------
    // NestedClass
    SCHEMA_TABLE_START(NestedClass)
    SCHEMA_ITEM_RID(NestedClass, NestedClass, TypeDef)
    SCHEMA_ITEM_RID(NestedClass, EnclosingClass, TypeDef)
    SCHEMA_TABLE_END(NestedClass)


    //-------------------------------------------------------------------------
    // GenericParam
    SCHEMA_TABLE_START(GenericParam)
    SCHEMA_ITEM(GenericParam, USHORT, Number)
    SCHEMA_ITEM(GenericParam, USHORT, Flags)
    SCHEMA_ITEM_CDTKN(GenericParam, Owner, TypeOrMethodDef)
    SCHEMA_ITEM_STRING(GenericParam, Name)
    SCHEMA_TABLE_END(GenericParam)

    //-------------------------------------------------------------------------
    // Transitional table for Metadata v1.1 for GenericParam
    SCHEMA_TABLE_START(GenericParamV1_1)
    SCHEMA_ITEM(GenericParam, USHORT, Number)
    SCHEMA_ITEM(GenericParam, USHORT, Flags)
    SCHEMA_ITEM_CDTKN(GenericParam, Owner, TypeOrMethodDef)
    SCHEMA_ITEM_STRING(GenericParam, Name)
    SCHEMA_ITEM_CDTKN(GenericParam, Kind, TypeDefOrRef)
    SCHEMA_TABLE_END(GenericParam)



    //-------------------------------------------------------------------------
    //MethodSpec
    SCHEMA_TABLE_START(MethodSpec)
    SCHEMA_ITEM_NOFIXED()
    SCHEMA_ITEM_CDTKN(MethodSpec, Method, MethodDefOrRef)
    SCHEMA_ITEM_BLOB(MethodSpec, Instantiation)
    SCHEMA_TABLE_END(MethodSpec)

    //-------------------------------------------------------------------------
    // GenericParamConstraint
    SCHEMA_TABLE_START(GenericParamConstraint)
    SCHEMA_ITEM_RID(GenericParamConstraint, Owner, GenericParam)
    SCHEMA_ITEM_CDTKN(GenericParamConstraint, Constraint, TypeDefOrRef)
    SCHEMA_TABLE_END(GenericParamConstraint)

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    //-------------------------------------------------------------------------
    //Document
    SCHEMA_TABLE_START(Document)
    SCHEMA_ITEM_BLOB(Document, Name)
    SCHEMA_ITEM_GUID(Document, HashAlgorithm)
    SCHEMA_ITEM_BLOB(Document, Hash)
    SCHEMA_ITEM_GUID(Document, Language)
    SCHEMA_TABLE_END(Document)

    //-------------------------------------------------------------------------
    //MethodDebugInformation
    SCHEMA_TABLE_START(MethodDebugInformation)
    SCHEMA_ITEM_RID(MethodDebugInformation, Document, Document)
    SCHEMA_ITEM_BLOB(MethodDebugInformation, SequencePoints)
    SCHEMA_TABLE_END(MethodDebugInformation)

    //-------------------------------------------------------------------------
    //LocalScope
    SCHEMA_TABLE_START(LocalScope)
    SCHEMA_ITEM_RID(LocalScope, Method, Method)
    SCHEMA_ITEM_RID(LocalScope, ImportScope, ImportScope)
    SCHEMA_ITEM_RID(LocalScope, VariableList, LocalVariable)
    SCHEMA_ITEM_RID(LocalScope, ConstantList, LocalConstant)
    SCHEMA_ITEM(LocalScope, ULONG, StartOffset)
    SCHEMA_ITEM(LocalScope, ULONG, Length)
    SCHEMA_TABLE_END(LocalScope)

    //-------------------------------------------------------------------------
    //LocalVariable
    SCHEMA_TABLE_START(LocalVariable)
    SCHEMA_ITEM(LocalVariable, USHORT, Attributes)
    SCHEMA_ITEM(LocalVariable, USHORT, Index)
    SCHEMA_ITEM_STRING(LocalVariable, Name)
    SCHEMA_TABLE_END(LocalVariable)

    //-------------------------------------------------------------------------
    //LocalConstant
    SCHEMA_TABLE_START(LocalConstant)
    SCHEMA_ITEM_STRING(LocalConstant, Name)
    SCHEMA_ITEM_BLOB(LocalConstant, Signature)
    SCHEMA_TABLE_END(LocalConstant)

    //-------------------------------------------------------------------------
    //ImportScope
    SCHEMA_TABLE_START(ImportScope)
    SCHEMA_ITEM_RID(ImportScope, Parent, ImportScope)
    SCHEMA_ITEM_BLOB(ImportScope, Imports)
    SCHEMA_TABLE_END(ImportScope)

    // TODO:
    // StateMachineMethod
    // CustomDebugInformation
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
// eof ------------------------------------------------------------------------
