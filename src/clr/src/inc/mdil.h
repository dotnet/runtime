// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef MDIL_H_
#define MDIL_H_

// READ BEFORE MODIFYING THIS FILE:
//
// When you modify MDILHeader or ClrCtlData, keep a few things in mind to
// ensure the binder stays backward compatible with existing MDIL files:
//
// 1. If you change anything, increment MDIL_VERSION_CURRENT. This ensures older
//    binders won't attempt to load newer MDIL that they can't read. Only change
//    MDIL_VERSION_MIN if your change means new binders won't be able to read existing MDIL,
//    so this should be a rare change. This usage replaces the MDIL_VERSION_X_Y defines; do not
//    create more of those since the binder needs to be able to make runtime decisions about handling
//    multiple versions.
// 2. Never remove any fields. If a field is no longer useful, just rename it and try to ignore it.
//    If you're really really sure you want to remove a field, set MDIL_VERSION_MIN to the current version
//    as this will break all existing MDIL files.
// 3. If you're adding new fields, always add them to the end of the structures. This ensures data from
//    existing MDIL files will still be read correctly. Note that this also means your new fields won't
//    necessarily be present in files you read, so you must add code to MdilModule::Init() to choose
//    a reasonable default or to calculate the value for older versions.
// 4. If all you do is add new MDIL pseudo instructions, you must at least increment MDIL_SUB_VERSION_CURRENT.
//    This will let older binders fall back to JIT for method bodies containining the new pseudo instructions.
//    This only makes sense if the new pseudo instructions are showing up very rarely, and if your changes
//    are such that older binders will create correct images if they don't encounter these pseudo instructions.
//    If you have reason to think your changes will trip up older binders even if they don't encounter
//    these instructions, you need to rev MDIL_VERSION_CURRENT as well.
// 5. Do not ever decrement (or even reset to 0) MDIL_SUB_VERSION_CURRENT - 
//    it really is meant to be monotonically increasing.

#define MDIL_VERSION_1_4
#define MDIL_VERSION_1_5
#define MDIL_VERSION_1_6
#define MDIL_VERSION_2_2
#ifdef REDHAWK
#define MDIL_VERSION_2_3
#define MDIL_VERSION_CURRENT (0x00020002)
#define MDIL_VERSION_MIN     (0x00020002)
#else
#define MDIL_VERSION_1_7
#define MDIL_VERSION_CURRENT (0x0002000C)
#define MDIL_VERSION_MIN     (0x0002000C)
#endif // REDHAWK

// 
#define MDIL_SUB_VERSION_CURRENT 0

struct  MDILHeader
{
    DWORD   hdrSize;
    DWORD   magic;
    DWORD   version;
    DWORD   typeMapCount;
    DWORD   methodMapCount;
#if defined(MDIL_VERSION_2_2)
    DWORD   genericInstSize;
#endif
    DWORD   extModuleCount;
    DWORD   extTypeCount;
    DWORD   extMemberCount;
#if defined(MDIL_VERSION_1_4)
    DWORD   typeSpecCount;
#endif // MDIL_VERSION_1_4
#if defined(MDIL_VERSION_2_2)
	DWORD	methodSpecCount;
#endif
    DWORD   signatureCount;
    DWORD   namePoolSize;
    DWORD   typeSize;
#if defined(MDIL_VERSION_1_3) || defined(MDIL_VERSION_1_4)
    DWORD   userStringPoolSize;  // added in version 1.3
#endif // MDIL_VERSION_1_3 || MDIL_VERSION_1_4
    DWORD   codeSize;
    DWORD   stubSize;
    DWORD   stubAssocSize;
    DWORD   debugMapCount;
    DWORD   debugInfoSize;

#if defined(MDIL_VERSION_1_5)
    // Redhawk - add some stuff that is already in the IL image
    DWORD   timeDateStamp;
    DWORD   subsystem;
#ifdef  TARGET_X64
    DWORD   baseAddress; // temporary workaround - Bartok only puts a DWORD now...
#else
    LPCVOID baseAddress;
#endif
    DWORD   entryPointToken;
#endif
#if defined(MDIL_VERSION_1_6)
    enum Flags
    {
        EntryPointReturnsVoid = 0x01,
        WellKnownTypesPresent = 0x02,
        TargetArch_Mask       = 0x0c,
        TargetArch_X86        = 0x00,
        TargetArch_AMD64      = 0x04,
        TargetArch_IA64       = 0x08,
        TargetArch_ARM        = 0x0c,
        DebuggableILAssembly  = 0x10,       // original assembly was created with /debug option
        DebuggableMDILCode    = 0x20,       // MDIL code was created with /debug option
        IsEagerlyLoaded       = 0x40,       // assume this module is eagerly loaded
        CompilerRelaxationNoStringInterning = 0x100,      // Assembly allows CompilerRelaxation.NoStringInterning
        RuntimeCompatibilityRuntimeWrappedExceptions = 0x200,      // Assembly requires non-exception throws to be wrapped in RuntimeWrappedException objects
        MinimalMDILImage = 0x400,      // This mdil information is minimal and only suitable for binding against a dependency. It is not suitable for actually generating a native image
        NoMDILImage = 0x800,           // There is no actual MDIL information in this file, generate a fake native image instead of reading this further.

        MdilModuleSecurityDescriptorFlags_Mask                          = 0x01FF0000,
        MdilModuleSecurityDescriptorFlags_None                          = 0x00000000,
        MdilModuleSecurityDescriptorFlags_IsAPTCA                       = 0x00020000,       // The assembly allows partially trusted callers
        MdilModuleSecurityDescriptorFlags_IsAllCritical                 = 0x00040000,       // Every type and method introduced by the assembly is critical
        MdilModuleSecurityDescriptorFlags_IsAllTransparent              = 0x00080000,       // Every type and method in the assembly is transparent
        MdilModuleSecurityDescriptorFlags_IsTreatAsSafe                 = 0x00100000,       // Combined with IsAllCritical - every type and method introduced by the assembly is safe critical
        MdilModuleSecurityDescriptorFlags_IsOpportunisticallyCritical   = 0x00200000,       // Ensure that the assembly follows all transparency rules by making all methods critical or safe critical as needed
        MdilModuleSecurityDescriptorFlags_SkipFullTrustVerification     = 0x00400000,       // Fully trusted transparent code does not require verification
        MdilModuleSecurityDescriptorFlags_TransparentDueToPartialTrust  = 0x00800000,       // Whether we made the assembly all transparent because it was partially-trusted
        MdilModuleSecurityDescriptorFlags_IsMicrosoftPlatform           = 0x01000000,       // Whether we made the assembly microsoft platform. Stored in ngen image to determine if the ngen 
                                                                                            // was generated as microsoft platform assembly (full trust) or not.
    };

    DWORD   flags;
#endif
    DWORD cerReliabilityContract;
#if defined(MDIL_VERSION_1_7)
    enum PlatformID
    {
        PlatformID_Unknown  = 0,
        PlatformID_Triton   = 1,
        PlatformID_Redhawk  = 2,

#if defined(CLR_STANDALONE_BINDER)
        My_PlatformID       = PlatformID_Triton
#elif defined(REDHAWK)
        My_PlatformID       = PlatformID_Redhawk
#else
        My_PlatformID       = PlatformID_Unknown
#endif
    };

    DWORD platformID;
#endif  // MDIL_VERSION_1_7
    DWORD blobDataSize;

    DWORD genericCodeSize;
    DWORD genericDebugInfoSize;

    WORD  compilerVersionMajor;
    WORD  compilerVersionMinor;
    WORD  compilerVersionBuildNumber;
    WORD  compilerVersionPrivateBuildNumber;

    DWORD subVersion;
};

#if defined(MDIL_VERSION_1_7) || defined(MDIL_VERSION_2_2)
struct ClrCtlData
{
    DWORD   hdrSize;
    DWORD   firstMethodRvaOffset;
    DWORD   methodDefRecordSize;
    DWORD   methodDefCount;
    DWORD   firstFieldRvaOffset;
    DWORD   fieldRvaRecordSize;
    DWORD   fieldRvaCount;
#if defined(MDIL_VERSION_2_2)
    DWORD   assemblyName;
    DWORD   locale;
    GUID    MVID;
    USHORT  majorVersion;
    USHORT  minorVersion;
    USHORT  buildNumber;
    USHORT  revisionNumber;
    DWORD   hasPublicKey:1;
    DWORD   cbPublicKey:15;
    DWORD   publicKeyBlob;
    DWORD   hasPublicKeyToken:1;
    DWORD   cbPublicKeyToken:15;
    BYTE    publicKeyToken[8];
    DWORD   ilImageSize;
    WORD    wcbSNHash;
    DWORD   snHashBlob;
    BYTE    cbTPBandName;
    DWORD   tpBandNameBlob;
    DWORD   extTypeRefExtendCount;
    DWORD   extMemberRefExtendCount;
    ULONG   neutralResourceCultureNameLen;
    DWORD   neutralResourceCultureName;
    USHORT  neutralResourceFallbackLocation;
#endif //VERSION_2_2
};
#endif // VERSION_1_7 || VERSION_2_2

#define DEFINE_MDIL_SPECIAL_TYPE(x) SPECIAL_TYPE_ ## x,
enum SPECIAL_TYPE
{
    SPECIAL_TYPE_INVALID = 0,
#include "mdilspecialtypes.h"
    SPECIAL_TYPE_COUNT,
};
#undef DEFINE_MDIL_SPECIAL_TYPE

enum EXTENDED_TYPE_FLAGS
{
    // Security transparency flags.
    EXTENDED_TYPE_FLAG_SF_UNKNOWN = 0,
    EXTENDED_TYPE_FLAG_SF_TRANSPARENT = 1,
    EXTENDED_TYPE_FLAG_SF_ALL_TRANSPARENT = 2,
    EXTENDED_TYPE_FLAG_SF_CRTIICAL = 3,
    EXTENDED_TYPE_FLAG_SF_CRITICAL_TAS = 4,
    EXTENDED_TYPE_FLAG_SF_ALLCRITICAL = 5,
    EXTENDED_TYPE_FLAG_SF_ALLCRITICAL_TAS = 6,
    EXTENDED_TYPE_FLAG_SF_TAS_NOTCRITICAL= 7,
    EXTENDED_TYPE_FLAG_SF_MASK = 0x7,

    // Platform and version specific flags. These shall only be present on assemblies where the mdil for the assembly is expected to version with the runtime. (Non-versionable)
    EXTENDED_TYPE_FLAG_PLATFORM_NEEDS_PER_TYPE_RCW_DATA = 0x8,
};
#endif //MDIL_H_
