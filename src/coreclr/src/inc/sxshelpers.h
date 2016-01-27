// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//****************************************************************************
//
//   SxSHelpers.h
//
//   Some helping classes and methods for SxS in mscoree and mscorwks/mscorsvr
//

//****************************************************************************


#ifndef SXSHELPERS_H_
#define SXSHELPERS_H_

#define V1_VERSION_NUM W("v1.0.3705")

// This string is the magic string located in the registry which determines that a key is actually
// a version.
// 
// For example:
//
// HCR/clsid/xxxx-xx-xx-xx/InprocServer32/1.0.3705.0
//									    /1.0.3705.125
//
// If this SBSVERSIONVALUE is set as a ValueName in these
// version keys, then we're saying that there is an implementation of the object
// for that version of the runtime.
//
// i.e., if 1.0.3705.0 has the value name of SBSVERSIONVALUE, then 1.0.3705.0 implements
// this class id.
#define SBSVERSIONVALUE	W("ImplementedInThisVersion")

// Find the runtime version from registry for rclsid
// If succeeded, *ppwzRuntimeVersion will have the runtime version 
//      corresponding to the highest version
// If failed, *ppwzRuntimeVersion will be NULL
// 
// Note: If succeeded, this function will allocate memory for 
//      *ppwzRuntimeVersion. It if the caller's repsonsibility to
//      release that memory
HRESULT FindRuntimeVersionFromRegistry(
    REFCLSID rclsid,
    __deref_out_z LPWSTR *ppwzRuntimeVersion,
    __deref_out_opt LPWSTR *ppwzSupportedVersions);

// Find assembly info from registry for rclsid
// If succeeded, *ppwzClassName, *ppwzAssemblyString, *ppwzCodeBase
//      will have their value corresponding to the highest version
// If failed, they will be set to NULL
// Note: If succeeded, this function will allocate memory for 
//      *ppwzClassName, *ppwzAssemblyString and *ppwzCodeBase. 
//      Caller is responsible to release them.
//
HRESULT FindShimInfoFromRegistry(
    REFCLSID rclsid,
    BOOL bLoadRecord,
    WORD wHighestRuntimeMajorVersion,
    WORD wHighestRuntimeMinorVersion,
    __deref_out_z LPWSTR *ppwzClassName,
    __deref_out_z LPWSTR *ppwzAssemblyString,
    __deref_out_z LPWSTR *ppwzCodeBase);

// Find assembly info from Win32 activattion context for rclsid
// If succeeded, *ppwzRuntimeVersion, *ppwzClassName, *ppwzAssemblyString, 
//      will have their value corresponding to the highest version
// If failed, they will be set to NULL
// Note: If succeeded, this function will allocate memory for 
//      *ppwzClassName, *ppwzAssemblyString and *ppwzCodeBase. 
//      Caller is responsible to release them.
//      Also notice codebase is not supported in Win32 case.
//
HRESULT FindShimInfoFromWin32(
    REFCLSID rclsid,
    BOOL bLoadRecord,
    __deref_opt_out_opt LPWSTR *ppwzRuntimeVersion,
    __deref_opt_out_opt LPWSTR *ppwszSupportedRuntimeVersions,
    __deref_opt_out_opt LPWSTR *ppwzClassName,
    __deref_opt_out_opt LPWSTR *ppwzAssemblyString,
    BOOL *pfRegFreePIA);

// Get information from the Win32 fusion about the config file and the application base.
HRESULT GetConfigFileFromWin32Manifest(__out_ecount_part(dwBuffer, *pSize) WCHAR* buffer, SIZE_T dwBuffer, SIZE_T* pSize);
HRESULT GetApplicationPathFromWin32Manifest(__out_ecount_part(dwBuffer, *pSize) WCHAR* buffer, SIZE_T dwBuffer, SIZE_T* pSize);


//****************************************************************************
//  AssemblyVersion
//  
//  class to handle assembly version
//  Since only functions in this file will use it,
//  we declare it in the cpp file so other people won't use it.
//
//****************************************************************************
class AssemblyVersion
{
    public:
        // constructors
        inline AssemblyVersion();

        inline AssemblyVersion(AssemblyVersion& version);
        
        // Init
        HRESULT Init(__in_z LPCWSTR pwzVersion, BOOL bStartsWithV);
        inline HRESULT Init(WORD major, WORD minor, WORD build, WORD revision);

        // Mofifiers.
        inline void SetBuild(WORD build);
        inline void SetRevision(WORD revision);

        // assign operator
        inline AssemblyVersion& operator=(const AssemblyVersion& version);

        // Comparison operator
        friend BOOL operator==(const AssemblyVersion& version1,
                               const AssemblyVersion& version2);
        friend BOOL operator>=(const AssemblyVersion& version1,
                               const AssemblyVersion& version2);

    private:

        // pwzVersion must have format of "a.b.c.d",
        // where a,b,c,d are all numbers
        HRESULT ValidateVersion(LPCWSTR pwzVersion);

    private:
        WORD        _major;
        WORD        _minor;
        WORD        _build;
        WORD        _revision;
};
extern BOOL operator==(const AssemblyVersion& version1, 
                       const AssemblyVersion& version2);

extern BOOL operator>=(const AssemblyVersion& version1,
                       const AssemblyVersion& version2);
inline BOOL operator<(const AssemblyVersion& version1,
                      const AssemblyVersion& version2);
#include <sxshelpers.inl>
#endif  // SXSHELPERS_H_
