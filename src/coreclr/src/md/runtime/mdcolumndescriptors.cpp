// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"

const BYTE CMiniMdBase::s_ModuleCol[] = {2,
  97,0,2,  101,2,2,  102,4,2,  102,6,2,  102,8,2,
  97,0,2,  101,2,4,  102,6,2,  102,8,2,  102,10,2,
};
const BYTE CMiniMdBase::s_TypeRefCol[] = {2,
  75,0,2,  101,2,2,  101,4,2,
  75,0,2,  101,2,4,  101,6,4,
};
const BYTE CMiniMdBase::s_TypeDefCol[] = {2,
  99,0,4,  101,4,2,  101,6,2,  64,8,2,  4,10,2,  6,12,2,
  99,0,4,  101,4,4,  101,8,4,  64,12,2,  4,14,2,  6,16,2,
};
const BYTE CMiniMdBase::s_FieldPtrCol[] = {1,
  4,0,2,
};
const BYTE CMiniMdBase::s_FieldCol[] = {3,
  97,0,2,  101,2,2,  103,4,2,
  97,0,2,  101,2,4,  103,6,4,
  97,0,2,  101,2,4,  103,6,2,
};
const BYTE CMiniMdBase::s_MethodPtrCol[] = {1,
  6,0,2,
};
const BYTE CMiniMdBase::s_MethodCol[] = {3,
  99,0,4,  97,4,2,  97,6,2,  101,8,2,  103,10,2,  8,12,2,
  99,0,4,  97,4,2,  97,6,2,  101,8,4,  103,12,4,  8,16,2,
  99,0,4,  97,4,2,  97,6,2,  101,8,4,  103,12,2,  8,14,2,
};
const BYTE CMiniMdBase::s_ParamPtrCol[] = {1,
  8,0,2,
};
const BYTE CMiniMdBase::s_ParamCol[] = {2,
  97,0,2,  97,2,2,  101,4,2,
  97,0,2,  97,2,2,  101,4,4,
};
const BYTE CMiniMdBase::s_InterfaceImplCol[] = {1,
  2,0,2,  64,2,2,
};
const BYTE CMiniMdBase::s_MemberRefCol[] = {3,
  69,0,2,  101,2,2,  103,4,2,
  69,0,4,  101,4,4,  103,8,4,
  69,0,2,  101,2,4,  103,6,2,
};
const BYTE CMiniMdBase::s_ConstantCol[] = {3,
  100,0,1,  65,2,2,  103,4,2,
  100,0,1,  65,2,4,  103,6,4,
  100,0,1,  65,2,2,  103,4,4,
};
const BYTE CMiniMdBase::s_CustomAttributeCol[] = {3,
  66,0,2,  74,2,2,  103,4,2,
  66,0,4,  74,4,4,  103,8,4,
  66,0,4,  74,4,2,  103,6,2,
};
const BYTE CMiniMdBase::s_FieldMarshalCol[] = {2,
  67,0,2,  103,2,2,
  67,0,2,  103,2,4,
};
const BYTE CMiniMdBase::s_DeclSecurityCol[] = {3,
  96,0,2,  68,2,2,  103,4,2,
  96,0,2,  68,2,4,  103,6,4,
  96,0,2,  68,2,2,  103,4,4,
};
const BYTE CMiniMdBase::s_ClassLayoutCol[] = {1,
  97,0,2,  99,2,4,  2,6,2,
};
const BYTE CMiniMdBase::s_FieldLayoutCol[] = {1,
  99,0,4,  4,4,2,
};
const BYTE CMiniMdBase::s_StandAloneSigCol[] = {2,
  103,0,2,
  103,0,4,
};
const BYTE CMiniMdBase::s_EventMapCol[] = {1,
  2,0,2,  20,2,2,
};
const BYTE CMiniMdBase::s_EventPtrCol[] = {1,
  20,0,2,
};
const BYTE CMiniMdBase::s_EventCol[] = {2,
  97,0,2,  101,2,2,  64,4,2,
  97,0,2,  101,2,4,  64,6,2,
};
const BYTE CMiniMdBase::s_PropertyMapCol[] = {1,
  2,0,2,  23,2,2,
};
const BYTE CMiniMdBase::s_PropertyPtrCol[] = {1,
  23,0,2,
};
const BYTE* CMiniMdBase::s_PropertyCol = s_FieldCol;
const BYTE CMiniMdBase::s_MethodSemanticsCol[] = {1,
  97,0,2,  6,2,2,  70,4,2,
};
const BYTE CMiniMdBase::s_MethodImplCol[] = {1,
  2,0,2,  71,2,2,  71,4,2,
};
const BYTE CMiniMdBase::s_ModuleRefCol[] = {2,
  101,0,2,
  101,0,4,
};
const BYTE* CMiniMdBase::s_TypeSpecCol = s_StandAloneSigCol;
const BYTE CMiniMdBase::s_ImplMapCol[] = {2,
  97,0,2,  72,2,2,  101,4,2,  26,6,2,
  97,0,2,  72,2,2,  101,4,4,  26,8,2,
};
const BYTE* CMiniMdBase::s_FieldRVACol = s_FieldLayoutCol;
const BYTE CMiniMdBase::s_ENCLogCol[] = {1,
  99,0,4,  99,4,4,
};
const BYTE CMiniMdBase::s_ENCMapCol[] = {1,
  99,0,4,
};
const BYTE CMiniMdBase::s_AssemblyCol[] = {3,
  99,0,4,  97,4,2,  97,6,2,  97,8,2,  97,10,2,  99,12,4,  103,16,2,  101,18,2,  101,20,2,
  99,0,4,  97,4,2,  97,6,2,  97,8,2,  97,10,2,  99,12,4,  103,16,4,  101,20,4,  101,24,4,
  99,0,4,  97,4,2,  97,6,2,  97,8,2,  97,10,2,  99,12,4,  103,16,2,  101,18,4,  101,22,4,
};
const BYTE* CMiniMdBase::s_AssemblyProcessorCol = s_ENCMapCol;
const BYTE CMiniMdBase::s_AssemblyOSCol[] = {1,
  99,0,4,  99,4,4,  99,8,4,
};
const BYTE CMiniMdBase::s_AssemblyRefCol[] = {3,
  97,0,2,  97,2,2,  97,4,2,  97,6,2,  99,8,4,  103,12,2,  101,14,2,  101,16,2,  103,18,2,
  97,0,2,  97,2,2,  97,4,2,  97,6,2,  99,8,4,  103,12,4,  101,16,4,  101,20,4,  103,24,4,
  97,0,2,  97,2,2,  97,4,2,  97,6,2,  99,8,4,  103,12,2,  101,14,4,  101,18,4,  103,22,2,
};
const BYTE CMiniMdBase::s_AssemblyRefProcessorCol[] = {1,
  99,0,4,  35,4,2,
};
const BYTE CMiniMdBase::s_AssemblyRefOSCol[] = {1,
  99,0,4,  99,4,4,  99,8,4,  35,12,2,
};
const BYTE CMiniMdBase::s_FileCol[] = {3,
  99,0,4,  101,4,2,  103,6,2,
  99,0,4,  101,4,4,  103,8,4,
  99,0,4,  101,4,4,  103,8,2,
};
const BYTE CMiniMdBase::s_ExportedTypeCol[] = {2,
  99,0,4,  99,4,4,  101,8,2,  101,10,2,  73,12,2,
  99,0,4,  99,4,4,  101,8,4,  101,12,4,  73,16,2,
};
const BYTE CMiniMdBase::s_ManifestResourceCol[] = {2,
  99,0,4,  99,4,4,  101,8,2,  73,10,2,
  99,0,4,  99,4,4,  101,8,4,  73,12,2,
};
const BYTE CMiniMdBase::s_NestedClassCol[] = {1,
  2,0,2,  2,2,2,
};
const BYTE CMiniMdBase::s_GenericParamCol[] = {2,
  97,0,2,  97,2,2,  76,4,2,  101,6,2,  64,8,2,  64,10,2,
  97,0,2,  97,2,2,  76,4,2,  101,6,4,  64,10,2,  64,12,2,
};
const BYTE CMiniMdBase::s_MethodSpecCol[] = {2,
  71,0,2,  103,2,2,
  71,0,2,  103,2,4,
};
const BYTE CMiniMdBase::s_GenericParamConstraintCol[] = {1,
  42,0,2,  64,2,2,
};
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
// Dummy descriptors to fill the gap to 0x30
const BYTE CMiniMdBase::s_Dummy1Col[] = { NULL };
const BYTE CMiniMdBase::s_Dummy2Col[] = { NULL };
const BYTE CMiniMdBase::s_Dummy3Col[] = { NULL };
// Actual portable PDB tables descriptors
const BYTE CMiniMdBase::s_DocumentCol[] = { 2,
  103,0,2, 102,2,2, 103,4,2, 102,6,2,
  103,0,4, 102,4,2, 103,6,2, 102,8,4,
};
const BYTE CMiniMdBase::s_MethodDebugInformationCol[] = { 2,
  48,0,2,  103,2,2,
  48,0,2,  103,2,4,
};
const BYTE CMiniMdBase::s_LocalScopeCol[] = { 1,
  6,0,2,   53,2,2,  51,4,2,  52,6,2,  99,8,4,  99,12,4
};
const BYTE CMiniMdBase::s_LocalVariableCol[] = { 2,
  97,0,2,  97,2,2,  101,4,2,
  97,0,2,  97,2,2,  101,4,4
};
const BYTE CMiniMdBase::s_LocalConstantCol[] = { 3,
  101,0,2, 103,2,2,
  101,0,4, 103,4,4,
  101,0,4, 103,4,2,
};
const BYTE CMiniMdBase::s_ImportScopeCol[] = { 2,
  53,0,2,  103,2,2,
  53,0,2,  103,2,4
};
// TODO:
// const BYTE CMiniMdBase::s_StateMachineMethodCol[] = {};
// const BYTE CMiniMdBase::s_CustomDebugInformationCol[] = {};
#endif // #ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB

const BYTE* const CMiniMdBase::s_TableColumnDescriptors[] = {
s_ModuleCol,
s_TypeRefCol,
s_TypeDefCol,
s_FieldPtrCol,
s_FieldCol,
s_MethodPtrCol,
s_MethodCol,
s_ParamPtrCol,
s_ParamCol,
s_InterfaceImplCol,
s_MemberRefCol,
s_ConstantCol,
s_CustomAttributeCol,
s_FieldMarshalCol,
s_DeclSecurityCol,
s_ClassLayoutCol,
s_FieldLayoutCol,
s_StandAloneSigCol,
s_EventMapCol,
s_EventPtrCol,
s_EventCol,
s_PropertyMapCol,
s_PropertyPtrCol,
s_FieldCol,
s_MethodSemanticsCol,
s_MethodImplCol,
s_ModuleRefCol,
s_StandAloneSigCol,
s_ImplMapCol,
s_FieldLayoutCol,
s_ENCLogCol,
s_ENCMapCol,
s_AssemblyCol,
s_ENCMapCol,
s_AssemblyOSCol,
s_AssemblyRefCol,
s_AssemblyRefProcessorCol,
s_AssemblyRefOSCol,
s_FileCol,
s_ExportedTypeCol,
s_ManifestResourceCol,
s_NestedClassCol,
s_GenericParamCol,
s_MethodSpecCol,
s_GenericParamConstraintCol,
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
// Dummy descriptors to fill the gap to 0x30
s_Dummy1Col,
s_Dummy2Col,
s_Dummy3Col,
// Actual portable PDB tables descriptors
s_DocumentCol,
s_MethodDebugInformationCol,
s_LocalScopeCol,
s_LocalVariableCol,
s_LocalConstantCol,
s_ImportScopeCol,
// TODO:
// s_StateMachineMethodCol,
// s_CustomDebugInformationCol,
#endif // #ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
};
