// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Helpers;

internal partial class EcmaMetadataReader
{
    private enum ColumnType
    {
        Unknown,
        TwoByteConstant,
        FourByteConstant,
        Utf8String,
        Blob,
        Guid,
        Token
    }

    [Flags]
    private enum PtrTablesPresent
    {
        None = 0,
        Method = 1,
        Field = 2,
        Param = 4,
        Property = 8,
        Event = 16
    }

    private static readonly MetadataTable[] columnTable = GetColumnTables();
    private static readonly ColumnType[] columnTypes = GetColumnTypes();
    private static readonly Func<uint, uint>[][] columnTokenDecoders = GetColumnTokenDecoders();
    private static readonly MetadataTable[][] codedIndexDecoderRing = ColumnDecodeData.GetCodedIndexDecoderRing();

    private static ColumnType[] GetColumnTypes()
    {
        ColumnType[] columnTypes = new ColumnType[(int)MetadataColumnIndex.Count];

        columnTypes[(int)MetadataColumnIndex.Module_Generation] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Module_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.Module_Mvid] = ColumnType.Guid;
        columnTypes[(int)MetadataColumnIndex.Module_EncId] = ColumnType.Guid;
        columnTypes[(int)MetadataColumnIndex.Module_EncBaseId] = ColumnType.Guid;

        columnTypes[(int)MetadataColumnIndex.TypeRef_ResolutionScope] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.TypeRef_TypeName] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.TypeRef_TypeNamespace] = ColumnType.Utf8String;

        columnTypes[(int)MetadataColumnIndex.TypeDef_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.TypeDef_TypeName] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.TypeDef_TypeNamespace] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.TypeDef_Extends] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.TypeDef_FieldList] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.TypeDef_MethodList] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.FieldPtr_Field] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.Field_Flags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Field_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.Field_Signature] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.MethodPtr_Method] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.MethodDef_Rva] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.MethodDef_ImplFlags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.MethodDef_Flags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.MethodDef_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.MethodDef_Signature] = ColumnType.Blob;
        columnTypes[(int)MetadataColumnIndex.MethodDef_ParamList] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.ParamPtr_Param] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.Param_Flags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Param_Sequence] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Param_Name] = ColumnType.Utf8String;

        columnTypes[(int)MetadataColumnIndex.InterfaceImpl_Class] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.InterfaceImpl_Interface] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.MemberRef_Class] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.MemberRef_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.MemberRef_Signature] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.Constant_Type] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Constant_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.Constant_Value] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.CustomAttribute_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.CustomAttribute_Type] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.CustomAttribute_Value] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.FieldMarshal_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.FieldMarshal_NativeType] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.DeclSecurity_Action] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.DeclSecurity_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.DeclSecurity_PermissionSet] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.ClassLayout_PackingSize] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.ClassLayout_ClassSize] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ClassLayout_Parent] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.FieldLayout_Offset] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.FieldLayout_Field] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.StandAloneSig_Signature] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.EventMap_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.EventMap_EventList] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.EventPtr_Event] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.Event_EventFlags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Event_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.Event_EventType] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.PropertyMap_Parent] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.PropertyMap_PropertyList] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.PropertyPtr_Property] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.Property_Flags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Property_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.Property_Type] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.MethodSemantics_Semantics] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.MethodSemantics_Method] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.MethodSemantics_Association] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.MethodImpl_Class] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.MethodImpl_MethodBody] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.MethodImpl_MethodDeclaration] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.ModuleRef_Name] = ColumnType.Utf8String;

        columnTypes[(int)MetadataColumnIndex.TypeSpec_Signature] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.ImplMap_MappingFlags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.ImplMap_MemberForwarded] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.ImplMap_ImportName] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.ImplMap_ImportScope] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.FieldRva_Rva] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.FieldRva_Field] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.ENCLog_Token] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ENCLog_Op] = ColumnType.FourByteConstant;

        columnTypes[(int)MetadataColumnIndex.ENCMap_Token] = ColumnType.FourByteConstant;

        columnTypes[(int)MetadataColumnIndex.Assembly_HashAlgId] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_MajorVersion] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_MinorVersion] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_BuildNumber] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_RevisionNumber] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.Assembly_PublicKey] = ColumnType.Blob;
        columnTypes[(int)MetadataColumnIndex.Assembly_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.Assembly_Culture] = ColumnType.Utf8String;

        columnTypes[(int)MetadataColumnIndex.AssemblyRef_MajorVersion] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_MinorVersion] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_BuildNumber] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_RevisionNumber] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_PublicKeyOrToken] = ColumnType.Blob;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_Culture] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.AssemblyRef_HashValue] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.File_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.File_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.File_HashValue] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.ExportedType_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ExportedType_TypeDefId] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ExportedType_TypeName] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.ExportedType_TypeNamespace] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.ExportedType_Implementation] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.ManifestResource_Offset] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ManifestResource_Flags] = ColumnType.FourByteConstant;
        columnTypes[(int)MetadataColumnIndex.ManifestResource_Name] = ColumnType.Utf8String;
        columnTypes[(int)MetadataColumnIndex.ManifestResource_Implementation] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.NestedClass_NestedClass] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = ColumnType.Token;

        columnTypes[(int)MetadataColumnIndex.GenericParam_Number] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.GenericParam_Flags] = ColumnType.TwoByteConstant;
        columnTypes[(int)MetadataColumnIndex.GenericParam_Owner] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.GenericParam_Name] = ColumnType.Utf8String;

        columnTypes[(int)MetadataColumnIndex.MethodSpec_Method] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.MethodSpec_Instantiation] = ColumnType.Blob;

        columnTypes[(int)MetadataColumnIndex.GenericParamConstraint_Owner] = ColumnType.Token;
        columnTypes[(int)MetadataColumnIndex.GenericParamConstraint_Constraint] = ColumnType.Token;

        return columnTypes;
    }

    private static MetadataTable[] GetColumnTables()
    {
        MetadataTable[] metadataTables = new MetadataTable[(int)MetadataColumnIndex.Count];

        metadataTables[(int)MetadataColumnIndex.Module_Generation] = MetadataTable.Module;
        metadataTables[(int)MetadataColumnIndex.Module_Name] = MetadataTable.Module;
        metadataTables[(int)MetadataColumnIndex.Module_Mvid] = MetadataTable.Module;
        metadataTables[(int)MetadataColumnIndex.Module_EncId] = MetadataTable.Module;
        metadataTables[(int)MetadataColumnIndex.Module_EncBaseId] = MetadataTable.Module;

        metadataTables[(int)MetadataColumnIndex.TypeRef_ResolutionScope] = MetadataTable.TypeRef;
        metadataTables[(int)MetadataColumnIndex.TypeRef_TypeName] = MetadataTable.TypeRef;
        metadataTables[(int)MetadataColumnIndex.TypeRef_TypeNamespace] = MetadataTable.TypeRef;

        metadataTables[(int)MetadataColumnIndex.TypeDef_Flags] = MetadataTable.TypeDef;
        metadataTables[(int)MetadataColumnIndex.TypeDef_TypeName] = MetadataTable.TypeDef;
        metadataTables[(int)MetadataColumnIndex.TypeDef_TypeNamespace] = MetadataTable.TypeDef;
        metadataTables[(int)MetadataColumnIndex.TypeDef_Extends] = MetadataTable.TypeDef;
        metadataTables[(int)MetadataColumnIndex.TypeDef_FieldList] = MetadataTable.TypeDef;
        metadataTables[(int)MetadataColumnIndex.TypeDef_MethodList] = MetadataTable.TypeDef;

        metadataTables[(int)MetadataColumnIndex.FieldPtr_Field] = MetadataTable.FieldPtr;

        metadataTables[(int)MetadataColumnIndex.Field_Flags] = MetadataTable.Field;
        metadataTables[(int)MetadataColumnIndex.Field_Name] = MetadataTable.Field;
        metadataTables[(int)MetadataColumnIndex.Field_Signature] = MetadataTable.Field;

        metadataTables[(int)MetadataColumnIndex.MethodPtr_Method] = MetadataTable.MethodPtr;

        metadataTables[(int)MetadataColumnIndex.MethodDef_Rva] = MetadataTable.MethodDef;
        metadataTables[(int)MetadataColumnIndex.MethodDef_ImplFlags] = MetadataTable.MethodDef;
        metadataTables[(int)MetadataColumnIndex.MethodDef_Flags] = MetadataTable.MethodDef;
        metadataTables[(int)MetadataColumnIndex.MethodDef_Name] = MetadataTable.MethodDef;
        metadataTables[(int)MetadataColumnIndex.MethodDef_Signature] = MetadataTable.MethodDef;
        metadataTables[(int)MetadataColumnIndex.MethodDef_ParamList] = MetadataTable.MethodDef;

        metadataTables[(int)MetadataColumnIndex.ParamPtr_Param] = MetadataTable.ParamPtr;

        metadataTables[(int)MetadataColumnIndex.Param_Flags] = MetadataTable.Param;
        metadataTables[(int)MetadataColumnIndex.Param_Sequence] = MetadataTable.Param;
        metadataTables[(int)MetadataColumnIndex.Param_Name] = MetadataTable.Param;

        metadataTables[(int)MetadataColumnIndex.InterfaceImpl_Class] = MetadataTable.InterfaceImpl;
        metadataTables[(int)MetadataColumnIndex.InterfaceImpl_Interface] = MetadataTable.InterfaceImpl;

        metadataTables[(int)MetadataColumnIndex.MemberRef_Class] = MetadataTable.MemberRef;
        metadataTables[(int)MetadataColumnIndex.MemberRef_Name] = MetadataTable.MemberRef;
        metadataTables[(int)MetadataColumnIndex.MemberRef_Signature] = MetadataTable.MemberRef;

        metadataTables[(int)MetadataColumnIndex.Constant_Type] = MetadataTable.Constant;
        metadataTables[(int)MetadataColumnIndex.Constant_Parent] = MetadataTable.Constant;
        metadataTables[(int)MetadataColumnIndex.Constant_Value] = MetadataTable.Constant;

        metadataTables[(int)MetadataColumnIndex.CustomAttribute_Parent] = MetadataTable.CustomAttribute;
        metadataTables[(int)MetadataColumnIndex.CustomAttribute_Type] = MetadataTable.CustomAttribute;
        metadataTables[(int)MetadataColumnIndex.CustomAttribute_Value] = MetadataTable.CustomAttribute;

        metadataTables[(int)MetadataColumnIndex.FieldMarshal_Parent] = MetadataTable.FieldMarshal;
        metadataTables[(int)MetadataColumnIndex.FieldMarshal_NativeType] = MetadataTable.FieldMarshal;

        metadataTables[(int)MetadataColumnIndex.DeclSecurity_Action] = MetadataTable.DeclSecurity;
        metadataTables[(int)MetadataColumnIndex.DeclSecurity_Parent] = MetadataTable.DeclSecurity;
        metadataTables[(int)MetadataColumnIndex.DeclSecurity_PermissionSet] = MetadataTable.DeclSecurity;

        metadataTables[(int)MetadataColumnIndex.ClassLayout_PackingSize] = MetadataTable.ClassLayout;
        metadataTables[(int)MetadataColumnIndex.ClassLayout_ClassSize] = MetadataTable.ClassLayout;
        metadataTables[(int)MetadataColumnIndex.ClassLayout_Parent] = MetadataTable.ClassLayout;

        metadataTables[(int)MetadataColumnIndex.FieldLayout_Offset] = MetadataTable.FieldLayout;
        metadataTables[(int)MetadataColumnIndex.FieldLayout_Field] = MetadataTable.FieldLayout;

        metadataTables[(int)MetadataColumnIndex.StandAloneSig_Signature] = MetadataTable.StandAloneSig;

        metadataTables[(int)MetadataColumnIndex.EventMap_Parent] = MetadataTable.EventMap;
        metadataTables[(int)MetadataColumnIndex.EventMap_EventList] = MetadataTable.EventMap;

        metadataTables[(int)MetadataColumnIndex.EventPtr_Event] = MetadataTable.EventPtr;

        metadataTables[(int)MetadataColumnIndex.Event_EventFlags] = MetadataTable.Event;
        metadataTables[(int)MetadataColumnIndex.Event_Name] = MetadataTable.Event;
        metadataTables[(int)MetadataColumnIndex.Event_EventType] = MetadataTable.Event;

        metadataTables[(int)MetadataColumnIndex.PropertyMap_Parent] = MetadataTable.PropertyMap;
        metadataTables[(int)MetadataColumnIndex.PropertyMap_PropertyList] = MetadataTable.PropertyMap;

        metadataTables[(int)MetadataColumnIndex.PropertyPtr_Property] = MetadataTable.PropertyPtr;

        metadataTables[(int)MetadataColumnIndex.Property_Flags] = MetadataTable.Property;
        metadataTables[(int)MetadataColumnIndex.Property_Name] = MetadataTable.Property;
        metadataTables[(int)MetadataColumnIndex.Property_Type] = MetadataTable.Property;

        metadataTables[(int)MetadataColumnIndex.MethodSemantics_Semantics] = MetadataTable.MethodSemantics;
        metadataTables[(int)MetadataColumnIndex.MethodSemantics_Method] = MetadataTable.MethodSemantics;
        metadataTables[(int)MetadataColumnIndex.MethodSemantics_Association] = MetadataTable.MethodSemantics;

        metadataTables[(int)MetadataColumnIndex.MethodImpl_Class] = MetadataTable.MethodImpl;
        metadataTables[(int)MetadataColumnIndex.MethodImpl_MethodBody] = MetadataTable.MethodImpl;
        metadataTables[(int)MetadataColumnIndex.MethodImpl_MethodDeclaration] = MetadataTable.MethodImpl;

        metadataTables[(int)MetadataColumnIndex.ModuleRef_Name] = MetadataTable.ModuleRef;

        metadataTables[(int)MetadataColumnIndex.TypeSpec_Signature] = MetadataTable.TypeSpec;

        metadataTables[(int)MetadataColumnIndex.ImplMap_MappingFlags] = MetadataTable.ImplMap;
        metadataTables[(int)MetadataColumnIndex.ImplMap_MemberForwarded] = MetadataTable.ImplMap;
        metadataTables[(int)MetadataColumnIndex.ImplMap_ImportName] = MetadataTable.ImplMap;
        metadataTables[(int)MetadataColumnIndex.ImplMap_ImportScope] = MetadataTable.ImplMap;

        metadataTables[(int)MetadataColumnIndex.FieldRva_Rva] = MetadataTable.FieldRva;
        metadataTables[(int)MetadataColumnIndex.FieldRva_Field] = MetadataTable.FieldRva;

        metadataTables[(int)MetadataColumnIndex.ENCLog_Token] = MetadataTable.ENCLog;
        metadataTables[(int)MetadataColumnIndex.ENCLog_Op] = MetadataTable.ENCLog;

        metadataTables[(int)MetadataColumnIndex.ENCMap_Token] = MetadataTable.ENCMap;

        metadataTables[(int)MetadataColumnIndex.Assembly_HashAlgId] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_MajorVersion] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_MinorVersion] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_BuildNumber] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_RevisionNumber] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_Flags] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_PublicKey] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_Name] = MetadataTable.Assembly;
        metadataTables[(int)MetadataColumnIndex.Assembly_Culture] = MetadataTable.Assembly;

        metadataTables[(int)MetadataColumnIndex.AssemblyRef_MajorVersion] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_MinorVersion] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_BuildNumber] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_RevisionNumber] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_Flags] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_PublicKeyOrToken] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_Name] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_Culture] = MetadataTable.AssemblyRef;
        metadataTables[(int)MetadataColumnIndex.AssemblyRef_HashValue] = MetadataTable.AssemblyRef;

        metadataTables[(int)MetadataColumnIndex.File_Flags] = MetadataTable.File;
        metadataTables[(int)MetadataColumnIndex.File_Name] = MetadataTable.File;
        metadataTables[(int)MetadataColumnIndex.File_HashValue] = MetadataTable.File;

        metadataTables[(int)MetadataColumnIndex.ExportedType_Flags] = MetadataTable.ExportedType;
        metadataTables[(int)MetadataColumnIndex.ExportedType_TypeDefId] = MetadataTable.ExportedType;
        metadataTables[(int)MetadataColumnIndex.ExportedType_TypeName] = MetadataTable.ExportedType;
        metadataTables[(int)MetadataColumnIndex.ExportedType_TypeNamespace] = MetadataTable.ExportedType;
        metadataTables[(int)MetadataColumnIndex.ExportedType_Implementation] = MetadataTable.ExportedType;

        metadataTables[(int)MetadataColumnIndex.ManifestResource_Offset] = MetadataTable.ManifestResource;
        metadataTables[(int)MetadataColumnIndex.ManifestResource_Flags] = MetadataTable.ManifestResource;
        metadataTables[(int)MetadataColumnIndex.ManifestResource_Name] = MetadataTable.ManifestResource;
        metadataTables[(int)MetadataColumnIndex.ManifestResource_Implementation] = MetadataTable.ManifestResource;

        metadataTables[(int)MetadataColumnIndex.NestedClass_NestedClass] = MetadataTable.NestedClass;
        metadataTables[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = MetadataTable.NestedClass;

        metadataTables[(int)MetadataColumnIndex.GenericParam_Number] = MetadataTable.GenericParam;
        metadataTables[(int)MetadataColumnIndex.GenericParam_Flags] = MetadataTable.GenericParam;
        metadataTables[(int)MetadataColumnIndex.GenericParam_Owner] = MetadataTable.GenericParam;
        metadataTables[(int)MetadataColumnIndex.GenericParam_Name] = MetadataTable.GenericParam;

        metadataTables[(int)MetadataColumnIndex.MethodSpec_Method] = MetadataTable.MethodSpec;
        metadataTables[(int)MetadataColumnIndex.MethodSpec_Instantiation] = MetadataTable.MethodSpec;

        metadataTables[(int)MetadataColumnIndex.GenericParamConstraint_Owner] = MetadataTable.GenericParamConstraint;
        metadataTables[(int)MetadataColumnIndex.GenericParamConstraint_Constraint] = MetadataTable.GenericParamConstraint;

        return metadataTables;
    }

    private static Func<uint, uint>[][] GetColumnTokenDecoders()
    {
        Func<uint, uint>[][] decoders = new Func<uint, uint>[32][];
        for (int i = 0; i < 32; i++)
        {
            List<MetadataTable> ptrTablesPresent = new();
            PtrTablesPresent tablesPresent = (PtrTablesPresent)i;
            if (tablesPresent.HasFlag(PtrTablesPresent.Field))
            {
                ptrTablesPresent.Add(MetadataTable.FieldPtr);
            }
            if (tablesPresent.HasFlag(PtrTablesPresent.Param))
            {
                ptrTablesPresent.Add(MetadataTable.ParamPtr);
            }
            if (tablesPresent.HasFlag(PtrTablesPresent.Param))
            {
                ptrTablesPresent.Add(MetadataTable.ParamPtr);
            }
            if (tablesPresent.HasFlag(PtrTablesPresent.Property))
            {
                ptrTablesPresent.Add(MetadataTable.PropertyPtr);
            }
            if (tablesPresent.HasFlag(PtrTablesPresent.Event))
            {
                ptrTablesPresent.Add(MetadataTable.EventPtr);
            }

            decoders[i] = GetColumnTokenDecode(ptrTablesPresent);
        }
        return decoders;
    }

    private static class ColumnDecodeData
    {
        private static readonly MetadataTable[] TypeDefOrRef = { MetadataTable.TypeDef, MetadataTable.TypeRef, MetadataTable.TypeSpec };
        private static readonly MetadataTable[] HasConstant = { MetadataTable.Field, MetadataTable.Param, MetadataTable.Property };
        private static readonly MetadataTable[] HasCustomAttribute =
            {
            MetadataTable.MethodDef,
            MetadataTable.Field,
            MetadataTable.TypeRef,
            MetadataTable.TypeDef,
            MetadataTable.Param,
            MetadataTable.InterfaceImpl,
            MetadataTable.MemberRef,
            MetadataTable.Module,
            MetadataTable.DeclSecurity,
            MetadataTable.Property,
            MetadataTable.Event,
            MetadataTable.StandAloneSig,
            MetadataTable.ModuleRef,
            MetadataTable.TypeSpec,
            MetadataTable.Assembly,
            MetadataTable.AssemblyRef,
            MetadataTable.File,
            MetadataTable.ExportedType,
            MetadataTable.ManifestResource,
            MetadataTable.GenericParam,
            MetadataTable.GenericParamConstraint,
            MetadataTable.MethodSpec };

        private static readonly MetadataTable[] HasFieldMarshal = { MetadataTable.Field, MetadataTable.Param };
        private static readonly MetadataTable[] HasDeclSecurity = { MetadataTable.TypeDef, MetadataTable.MethodDef, MetadataTable.Assembly };
        private static readonly MetadataTable[] MemberRefParent = { MetadataTable.TypeDef, MetadataTable.TypeRef, MetadataTable.ModuleRef, MetadataTable.MethodDef, MetadataTable.TypeSpec };
        private static readonly MetadataTable[] HasSemantics = { MetadataTable.Event, MetadataTable.Property };
        private static readonly MetadataTable[] MethodDefOrRef = { MetadataTable.MethodDef, MetadataTable.MemberRef };
        private static readonly MetadataTable[] MemberForwarded = { MetadataTable.Field, MetadataTable.MethodDef };
        private static readonly MetadataTable[] Implementation = { MetadataTable.File, MetadataTable.AssemblyRef, MetadataTable.ExportedType };
        private static readonly MetadataTable[] CustomAttributeType = { MetadataTable.Unused, MetadataTable.Unused, MetadataTable.MethodDef, MetadataTable.MemberRef, MetadataTable.Unused };
        private static readonly MetadataTable[] ResolutionScope = { MetadataTable.Module, MetadataTable.ModuleRef, MetadataTable.AssemblyRef, MetadataTable.TypeRef };
        private static readonly MetadataTable[] TypeOrMethodDef = { MetadataTable.TypeDef, MetadataTable.MethodDef };

        private static readonly MetadataTable[] FieldOrFieldPtr = { (MetadataTable)(-2), MetadataTable.Field, MetadataTable.FieldPtr };
        private static readonly MetadataTable[] MethodDefOrMethodPtr = { (MetadataTable)(-2), MetadataTable.MethodDef, MetadataTable.MethodPtr };
        private static readonly MetadataTable[] ParamOrParamPtr = { (MetadataTable)(-2), MetadataTable.Param, MetadataTable.ParamPtr };
        private static readonly MetadataTable[] EventOrEventPtr = { (MetadataTable)(-2), MetadataTable.Event, MetadataTable.EventPtr };
        private static readonly MetadataTable[] PropertyOrPropertyPtr = { (MetadataTable)(-2), MetadataTable.Property, MetadataTable.PropertyPtr };

        public static MetadataTable[][] GetCodedIndexDecoderRing()
        {
            MetadataTable[][] decoderRing = new MetadataTable[(int)MetadataColumnIndex.Count][];

            decoderRing[(int)MetadataColumnIndex.TypeRef_ResolutionScope] = ResolutionScope;

            decoderRing[(int)MetadataColumnIndex.TypeDef_Extends] = TypeDefOrRef;
            decoderRing[(int)MetadataColumnIndex.TypeDef_FieldList] = FieldOrFieldPtr;
            decoderRing[(int)MetadataColumnIndex.TypeDef_MethodList] = MethodDefOrMethodPtr;

            decoderRing[(int)MetadataColumnIndex.FieldPtr_Field] = new[] { MetadataTable.Field };

            decoderRing[(int)MetadataColumnIndex.MethodPtr_Method] = new[] { MetadataTable.MethodDef };

            decoderRing[(int)MetadataColumnIndex.MethodDef_ParamList] = ParamOrParamPtr;

            decoderRing[(int)MetadataColumnIndex.ParamPtr_Param] = new[] { MetadataTable.Param };

            decoderRing[(int)MetadataColumnIndex.InterfaceImpl_Class] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.InterfaceImpl_Interface] = TypeDefOrRef;

            decoderRing[(int)MetadataColumnIndex.MemberRef_Class] = MemberRefParent;

            decoderRing[(int)MetadataColumnIndex.Constant_Parent] = HasConstant;

            decoderRing[(int)MetadataColumnIndex.CustomAttribute_Parent] = HasCustomAttribute;
            decoderRing[(int)MetadataColumnIndex.CustomAttribute_Type] = CustomAttributeType;

            decoderRing[(int)MetadataColumnIndex.FieldMarshal_Parent] = HasFieldMarshal;

            decoderRing[(int)MetadataColumnIndex.DeclSecurity_Parent] = HasDeclSecurity;

            decoderRing[(int)MetadataColumnIndex.ClassLayout_Parent] = new[] { MetadataTable.TypeDef };

            decoderRing[(int)MetadataColumnIndex.FieldLayout_Field] = new[] { MetadataTable.Field };

            decoderRing[(int)MetadataColumnIndex.EventMap_Parent] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.EventMap_EventList] = EventOrEventPtr;

            decoderRing[(int)MetadataColumnIndex.EventPtr_Event] = new[] { MetadataTable.Event };

            decoderRing[(int)MetadataColumnIndex.Event_EventType] = TypeDefOrRef;

            decoderRing[(int)MetadataColumnIndex.PropertyMap_Parent] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.PropertyMap_PropertyList] = PropertyOrPropertyPtr;

            decoderRing[(int)MetadataColumnIndex.PropertyPtr_Property] = new[] { MetadataTable.Property };

            decoderRing[(int)MetadataColumnIndex.MethodSemantics_Method] = new[] { MetadataTable.MethodDef };
            decoderRing[(int)MetadataColumnIndex.MethodSemantics_Association] = HasSemantics;

            decoderRing[(int)MetadataColumnIndex.MethodImpl_Class] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.MethodImpl_MethodBody] = MethodDefOrRef;
            decoderRing[(int)MetadataColumnIndex.MethodImpl_MethodDeclaration] = MethodDefOrRef;

            decoderRing[(int)MetadataColumnIndex.ImplMap_MemberForwarded] = MemberForwarded;
            decoderRing[(int)MetadataColumnIndex.ImplMap_ImportScope] = new[] { MetadataTable.ModuleRef };

            decoderRing[(int)MetadataColumnIndex.FieldRva_Field] = new[] { MetadataTable.ModuleRef };

            decoderRing[(int)MetadataColumnIndex.ExportedType_Implementation] = Implementation;

            decoderRing[(int)MetadataColumnIndex.ManifestResource_Implementation] = Implementation;

            decoderRing[(int)MetadataColumnIndex.NestedClass_NestedClass] = new[] { MetadataTable.TypeDef };
            decoderRing[(int)MetadataColumnIndex.NestedClass_EnclosingClass] = new[] { MetadataTable.TypeDef };

            decoderRing[(int)MetadataColumnIndex.GenericParam_Owner] = TypeOrMethodDef;

            decoderRing[(int)MetadataColumnIndex.MethodSpec_Method] = MethodDefOrRef;

            decoderRing[(int)MetadataColumnIndex.GenericParamConstraint_Owner] = new[] { MetadataTable.GenericParam };
            decoderRing[(int)MetadataColumnIndex.GenericParamConstraint_Constraint] = TypeDefOrRef;

            return decoderRing;
        }
    }

    private static uint DecodeCodedIndex(uint input, ReadOnlySpan<MetadataTable> tablesEncoded)
    {
        uint encodingMask = BitOperations.RoundUpToPowerOf2((uint)tablesEncoded.Length) - 1;
        int bitsForTableEncoding = 32 - BitOperations.LeadingZeroCount(BitOperations.RoundUpToPowerOf2((uint)tablesEncoded.Length) - 1);
        MetadataTable table = tablesEncoded[(int)(input & encodingMask)];
        uint rid = input >> bitsForTableEncoding;
        return CreateToken(table, rid);
    }

    private static Func<uint, uint>[] GetColumnTokenDecode(List<MetadataTable> ptrTablesPresent)
    {
        Func<uint, uint>[] columnTokenDecode = new Func<uint, uint>[(int)MetadataColumnIndex.Count];
        MetadataTable[][] decoderRing = ColumnDecodeData.GetCodedIndexDecoderRing();
        for (int i = 0; i < decoderRing.Length; i++)
        {
            if (decoderRing[i] != null)
            {
                columnTokenDecode[i] = ComputeDecoder(decoderRing[i]);
            }
        }

        return columnTokenDecode;

        Func<uint, uint> ComputeDecoder(MetadataTable[] decoderData)
        {
            Func<uint, uint> result;

            if (decoderData.Length == 1)
            {
                MetadataTable metadataTable = decoderData[0];
                result = delegate (uint input) { return CreateToken(metadataTable, input); };
            }
            else
            {
                if ((decoderData.Length == 1) && decoderData[0] == (MetadataTable)(-2))
                {
                    MetadataTable metadataTable = decoderData[0];
                    if (!ptrTablesPresent.Contains(decoderData[2]))
                    {
                        metadataTable = decoderData[1];
                    }
                    else
                    {
                        metadataTable = decoderData[2];
                    }
                    result = delegate (uint input) { return CreateToken(metadataTable, input); };
                }
                else
                {
                    result = delegate (uint input) { return DecodeCodedIndex(input, decoderData); };
                }
            }

            return result;
        }
    }
}
