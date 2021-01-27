// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Mdperf.h
//

//
//*****************************************************************************

#ifndef __MDCOMPILERPERF_H__
#define __MDCOMPILERPERF_H__

//#define FEATURE_METADATA_PERF_STATS

#ifdef FEATURE_METADATA_PERF_STATS

// Avoid dynamic allocs to display the API names.
#define API_NAME_STR_SIZE 80

//-----------------------------------------------------------------------------
// In order to add instrumentation for an API, two changes have to be made.
// One, add the API name in the table below (MD_TABLE).
// Second, add two lines of code (shown below) in the implementation
// of the API itself. e.g.
//      RegMeta::MyNewMetataDataAPI(...)
//      {
//           LOG(...);
//           START_MD_PERF();        // <------ add this line as is.
//           ....
//           // API implementation
//       ErrExit:
//           STOP_MD_PERF(RegMeta_MyNewMetaDataAPI); // <---------- add this line with the appropriate name
//           return (hr);
//      ]
//
//-----------------------------------------------------------------------------
#define MD_COMPILER_PERF_TABLE\
    MD_FUNC(SaveToMemory)\
    MD_FUNC(DefineMethod)\
    MD_FUNC(DefineMethodImpl)\
    MD_FUNC(SetRVA)\
    MD_FUNC(DefineTypeRefByName)\
    MD_FUNC(DefineImportType)\
    MD_FUNC(DefineMemberRef)\
    MD_FUNC(DefineImportMember)\
    MD_FUNC(DefineEvent)\
    MD_FUNC(SetClassLayout)\
    MD_FUNC(DeleteClassLayout)\
    MD_FUNC(SetFieldMarshal)\
    MD_FUNC(DeleteFieldMarshal)\
    MD_FUNC(DefinePermissionSet)\
    MD_FUNC(SetMemberIndex)\
    MD_FUNC(GetTokenFromSig)\
    MD_FUNC(DefineModuleRef)\
    MD_FUNC(SetParent)\
    MD_FUNC(GetTokenFromTypeSpec)\
    MD_FUNC(DefineUserString)\
    MD_FUNC(DeleteToken)\
    MD_FUNC(SetTypeDefProps)\
    MD_FUNC(DefineNestedType)\
    MD_FUNC(SetMethodProps)\
    MD_FUNC(SetEventProps)\
    MD_FUNC(SetPermissionSetProps)\
    MD_FUNC(DefinePinvokeMap)\
    MD_FUNC(SetPinvokeMap)\
    MD_FUNC(DeletePinvokeMap)\
    MD_FUNC(DefineField)\
    MD_FUNC(DefineProperty)\
    MD_FUNC(DefineParam)\
    MD_FUNC(SetFieldProps)\
    MD_FUNC(SetPropertyProps)\
    MD_FUNC(SetParamProps)\
    MD_FUNC(EnumMembers)\
    MD_FUNC(EnumMembersWithName)\
    MD_FUNC(EnumMethods)\
    MD_FUNC(EnumMethodsWithName)\
    MD_FUNC(EnumFields)\
    MD_FUNC(EnumFieldsWithName)\
    MD_FUNC(EnumParams)\
    MD_FUNC(EnumMemberRefs)\
    MD_FUNC(EnumMethodImpls)\
    MD_FUNC(EnumPermissionSets)\
    MD_FUNC(FindMember)\
    MD_FUNC(FindMethod)\
    MD_FUNC(FindField)\
    MD_FUNC(FindMemberRef)\
    MD_FUNC(GetMethodProps)\
    MD_FUNC(GetMemberRefProps)\
    MD_FUNC(EnumProperties)\
    MD_FUNC(EnumEvents)\
    MD_FUNC(GetEventProps)\
    MD_FUNC(EnumMethodSemantics)\
    MD_FUNC(GetMethodSemantics)\
    MD_FUNC(GetClassLayout)\
    MD_FUNC(GetFieldMarshal)\
    MD_FUNC(GetRVA)\
    MD_FUNC(GetPermissionSetProps)\
    MD_FUNC(GetSigFromToken)\
    MD_FUNC(GetModuleRefProps)\
    MD_FUNC(EnumModuleRefs)\
    MD_FUNC(GetTypeSpecFromToken)\
    MD_FUNC(GetNameFromToken)\
    MD_FUNC(EnumUnresolvedMethods)\
    MD_FUNC(GetUserString)\
    MD_FUNC(GetPinvokeMap)\
    MD_FUNC(EnumSignatures)\
    MD_FUNC(EnumTypeSpecs)\
    MD_FUNC(EnumUserStrings)\
    MD_FUNC(GetParamForMethodIndex)\
    MD_FUNC(GetMemberProps)\
    MD_FUNC(GetFieldProps)\
    MD_FUNC(GetPropertyProps)\
    MD_FUNC(GetParamProps)\
    MD_FUNC(SetModuleProps)\
    MD_FUNC(Save)\
    MD_FUNC(SaveToStream)\
    MD_FUNC(GetSaveSize)\
    MD_FUNC(Merge)\
    MD_FUNC(DefineCustomAttribute)\
    MD_FUNC(SetCustomAttributeValue)\
    MD_FUNC(DefineSecurityAttributeSet)\
    MD_FUNC(UnmarkAll)\
    MD_FUNC(MarkToken)\
    MD_FUNC(IsTokenMarked)\
    MD_FUNC(DefineTypeDef)\
    MD_FUNC(SetHandler)\
    MD_FUNC(CountEnum)\
    MD_FUNC(ResetEnum)\
    MD_FUNC(EnumTypeDefs)\
    MD_FUNC(EnumInterfaceImpls)\
    MD_FUNC(EnumTypeRefs)\
    MD_FUNC(FindTypeDefByName)\
    MD_FUNC(FindTypeDefByGUID)\
    MD_FUNC(GetScopeProps)\
    MD_FUNC(GetModuleFromScope)\
    MD_FUNC(GetTypeDefProps)\
    MD_FUNC(GetInterfaceImplProps)\
    MD_FUNC(GetCustomAttributeByName)\
    MD_FUNC(GetTypeRefProps)\
    MD_FUNC(ResolveTypeRef)\
    MD_FUNC(EnumCustomAttributes)\
    MD_FUNC(GetCustomAttributeProps)\
    MD_FUNC(FindTypeRef)\
    MD_FUNC(RefToDefOptimization)\
    MD_FUNC(DefineAssembly)\
    MD_FUNC(DefineAssemblyRef)\
    MD_FUNC(DefineFile)\
    MD_FUNC(DefineExportedType)\
    MD_FUNC(DefineManifestResource)\
    MD_FUNC(DefineExecutionLocation)\
    MD_FUNC(SetAssemblyProps)\
    MD_FUNC(SetAssemblyRefProps)\
    MD_FUNC(SetFileProps)\
    MD_FUNC(SetExportedTypeProps)\
    MD_FUNC(GetAssemblyProps)\
    MD_FUNC(GetAssemblyRefProps)\
    MD_FUNC(GetFileProps)\
    MD_FUNC(GetExportedTypeProps)\
    MD_FUNC(GetManifestResourceProps)\
    MD_FUNC(EnumAssemblyRefs)\
    MD_FUNC(EnumFiles)\
    MD_FUNC(EnumExportedTypes)\
    MD_FUNC(EnumManifestResources)\
    MD_FUNC(EnumExecutionLocations)\
    MD_FUNC(GetAssemblyFromScope)\
    MD_FUNC(FindExportedTypeByName)\
    MD_FUNC(FindManifestResourceByName)\
    MD_FUNC(FindAssembliesByName)\
    MD_FUNC(SetGenericPars)\
    MD_FUNC(DefineGenericParam)\
    MD_FUNC(SetGenericParamProps)\
    MD_FUNC(EnumGenericParamConstraints)\
    MD_FUNC(GetGenericParamProps)\
    MD_FUNC(GetGenericParamConstraintProps)\
    MD_FUNC(GetPEKind)\
    MD_FUNC(GetVersionString)\
    MD_FUNC(GetAssemblyUnification)

//-----------------------------------------------------------------------------
// Create an enum of all the API names. This is the index to access the APIs.
//-----------------------------------------------------------------------------
#undef MD_FUNC
#define MD_FUNC(MDTag)\
    MDTag ## _ENUM,

typedef enum _MDAPIs
{
    MD_COMPILER_PERF_TABLE
    LAST_MD_API
} MDApis;

//-----------------------------------------------------------------------------
// Declare the struct which contais all the interesting stats for a particular
// API call.
//-----------------------------------------------------------------------------
typedef struct _MDAPIPerfData
{
    DWORD dwQueryPerfCycles;             // # of cycles spent in this call
    DWORD dwCalledNumTimes;              // # of times this API was called
} MDAPIPerfData;


//-----------------------------------------------------------------------------
// MDCompilerPerf
//-----------------------------------------------------------------------------
class MDCompilerPerf
{
public:
    MDCompilerPerf();
    ~MDCompilerPerf();

private:
    MDAPIPerfData MDPerfStats[LAST_MD_API];

    void MetaDataPerfReport ();
};

// Note that this macro declares a local var.
#define START_MD_PERF()\
    LARGE_INTEGER __startVal;\
    QueryPerformanceCounter(&__startVal);

#undef MD_FUNC
#define MD_FUNC(MDTag)\
    MDTag ## _ENUM

// Note that this macro uses the local var startVal declared in START_MD_PERF()
#define STOP_MD_PERF(MDTag)\
    LARGE_INTEGER __stopVal;\
    QueryPerformanceCounter(&__stopVal);\
    m_MDCompilerPerf.MDPerfStats[MD_FUNC(MDTag)].dwCalledNumTimes++;\
    m_MDCompilerPerf.MDPerfStats[MD_FUNC(MDTag)].dwQueryPerfCycles += (DWORD)(__stopVal.QuadPart - __startVal.QuadPart);

#else //!FEATURE_METADATA_PERF_STATS

#define START_MD_PERF()
#define STOP_MD_PERF(MDTag)

#endif //!FEATURE_METADATA_PERF_STATS

#endif // __MDCOMPILERPERF_H__
