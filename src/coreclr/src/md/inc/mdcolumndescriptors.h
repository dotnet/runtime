// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

static const BYTE s_ModuleCol[];
static const BYTE s_TypeRefCol[];
static const BYTE s_TypeDefCol[];
static const BYTE s_FieldPtrCol[];
static const BYTE s_FieldCol[];
static const BYTE s_MethodPtrCol[];
static const BYTE s_MethodCol[];
static const BYTE s_ParamPtrCol[];
static const BYTE s_ParamCol[];
static const BYTE s_InterfaceImplCol[];
static const BYTE s_MemberRefCol[];
static const BYTE s_ConstantCol[];
static const BYTE s_CustomAttributeCol[];
static const BYTE s_FieldMarshalCol[];
static const BYTE s_DeclSecurityCol[];
static const BYTE s_ClassLayoutCol[];
static const BYTE s_FieldLayoutCol[];
static const BYTE s_StandAloneSigCol[];
static const BYTE s_EventMapCol[];
static const BYTE s_EventPtrCol[];
static const BYTE s_EventCol[];
static const BYTE s_PropertyMapCol[];
static const BYTE s_PropertyPtrCol[];
static const BYTE* s_PropertyCol;
static const BYTE s_MethodSemanticsCol[];
static const BYTE s_MethodImplCol[];
static const BYTE s_ModuleRefCol[];
static const BYTE* s_TypeSpecCol;
static const BYTE s_ImplMapCol[];
static const BYTE* s_FieldRVACol;
static const BYTE s_ENCLogCol[];
static const BYTE s_ENCMapCol[];
static const BYTE s_AssemblyCol[];
static const BYTE* s_AssemblyProcessorCol;
static const BYTE s_AssemblyOSCol[];
static const BYTE s_AssemblyRefCol[];
static const BYTE s_AssemblyRefProcessorCol[];
static const BYTE s_AssemblyRefOSCol[];
static const BYTE s_FileCol[];
static const BYTE s_ExportedTypeCol[];
static const BYTE s_ManifestResourceCol[];
static const BYTE s_NestedClassCol[];
static const BYTE s_GenericParamCol[];
static const BYTE s_MethodSpecCol[];
static const BYTE s_GenericParamConstraintCol[];
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
// Dummy descriptors to fill the gap to 0x30
static const BYTE s_Dummy1Col[];
static const BYTE s_Dummy2Col[];
static const BYTE s_Dummy3Col[];
// Actual portable PDB tables descriptors
static const BYTE s_DocumentCol[];
static const BYTE s_MethodDebugInformationCol[];
static const BYTE s_LocalScopeCol[];
static const BYTE s_LocalVariableCol[];
static const BYTE s_LocalConstantCol[];
static const BYTE s_ImportScopeCol[];
// TODO:
// static const BYTE s_StateMachineMethodCol[];
// static const BYTE s_CustomDebugInformationCol[];
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
