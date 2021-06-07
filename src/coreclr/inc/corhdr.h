// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** CorHdr.h - contains definitions for the Runtime structures,             **
**

 **            needed to work with metadata.                                **
 **                                                                         **
 *****************************************************************************/
//
// The top most managed code structure in a EXE or DLL is the IMAGE_COR20_HEADER
// see code:#ManagedHeader for more

#ifndef __CORHDR_H__
#define __CORHDR_H__

#include <stdint.h>

#ifdef _MSC_VER
#pragma warning(disable:4200) // nonstandard extension used : zero-sized array in struct/union.
#endif
typedef void*  mdScope;                // Obsolete; not used in the runtime.
typedef uint32_t mdToken;                // Generic token


// Token  definitions


typedef mdToken mdModule;               // Module token (roughly, a scope)
typedef mdToken mdTypeRef;              // TypeRef reference (this or other scope)
typedef mdToken mdTypeDef;              // TypeDef in this scope
typedef mdToken mdFieldDef;             // Field in this scope
typedef mdToken mdMethodDef;            // Method in this scope
typedef mdToken mdParamDef;             // param token
typedef mdToken mdInterfaceImpl;        // interface implementation token

typedef mdToken mdMemberRef;            // MemberRef (this or other scope)
typedef mdToken mdCustomAttribute;      // attribute token
typedef mdToken mdPermission;           // DeclSecurity

typedef mdToken mdSignature;            // Signature object
typedef mdToken mdEvent;                // event token
typedef mdToken mdProperty;             // property token

typedef mdToken mdModuleRef;            // Module reference (for the imported modules)

// Assembly tokens.
typedef mdToken mdAssembly;             // Assembly token.
typedef mdToken mdAssemblyRef;          // AssemblyRef token.
typedef mdToken mdFile;                 // File token.
typedef mdToken mdExportedType;         // ExportedType token.
typedef mdToken mdManifestResource;     // ManifestResource token.

typedef mdToken mdTypeSpec;             // TypeSpec object

typedef mdToken mdGenericParam;         // formal parameter to generic type or method
typedef mdToken mdMethodSpec;           // instantiation of a generic method
typedef mdToken mdGenericParamConstraint; // constraint on a formal generic parameter

// Application string.
typedef mdToken mdString;               // User literal string token.

typedef mdToken mdCPToken;              // constantpool token

#ifndef MACROS_NOT_SUPPORTED
typedef uint32_t RID;
#else
typedef unsigned RID;
#endif // MACROS_NOT_SUPPORTED

typedef enum ReplacesGeneralNumericDefines
{
// Directory entry macro for CLR data.
#ifndef IMAGE_DIRECTORY_ENTRY_COMHEADER
    IMAGE_DIRECTORY_ENTRY_COMHEADER     =14,
#endif // IMAGE_DIRECTORY_ENTRY_COMHEADER
} ReplacesGeneralNumericDefines;


// The COMIMAGE_FLAGS_32BITREQUIRED and COMIMAGE_FLAGS_32BITPREFERRED flags defined below interact as a pair
// in order to get the performance profile we desire for platform neutral assemblies while retaining backwards
// compatibility with pre-4.5 runtimes/OSs, which don't know about COMIMAGE_FLAGS_32BITPREFERRED.
//
// COMIMAGE_FLAGS_32BITREQUIRED originally meant "this assembly is x86-only" (required to distinguish platform
// neutral assemblies which also mark their PE MachineType as IMAGE_FILE_MACHINE_I386).
//
// COMIMAGE_FLAGS_32BITPREFERRED has been added so we can create a sub-class of platform neutral assembly that
// prefers to be loaded into 32-bit environment for perf reasons, but is still compatible with 64-bit
// environments.
//
// In order to retain maximum backwards compatibility you cannot simply read or write one of these flags
// however. You must treat them as a pair, a two-bit field with the following meanings:
//
//  32BITREQUIRED  32BITPREFERRED
//        0               0         :   no special meaning, MachineType and ILONLY flag determine image requirements
//        0               1         :   illegal, reserved for future use
//        1               0         :   image is x86-specific
//        1               1         :   image is platform neutral and prefers to be loaded 32-bit when possible
//
// To simplify manipulation of these flags the following macros are provided below.

#define COR_IS_32BIT_REQUIRED(_flags) \
    (((_flags) & (COMIMAGE_FLAGS_32BITREQUIRED|COMIMAGE_FLAGS_32BITPREFERRED)) == (COMIMAGE_FLAGS_32BITREQUIRED))

#define COR_IS_32BIT_PREFERRED(_flags) \
    (((_flags) & (COMIMAGE_FLAGS_32BITREQUIRED|COMIMAGE_FLAGS_32BITPREFERRED)) == (COMIMAGE_FLAGS_32BITREQUIRED|COMIMAGE_FLAGS_32BITPREFERRED))

#define COR_SET_32BIT_REQUIRED(_flagsfield) \
    do { _flagsfield = (_flagsfield & ~COMIMAGE_FLAGS_32BITPREFERRED) | COMIMAGE_FLAGS_32BITREQUIRED; } while (false)

#define COR_SET_32BIT_PREFERRED(_flagsfield) \
    do { _flagsfield |= COMIMAGE_FLAGS_32BITPREFERRED|COMIMAGE_FLAGS_32BITREQUIRED; } while (false)

#define COR_CLEAR_32BIT_REQUIRED(_flagsfield) \
    do { _flagsfield &= ~(COMIMAGE_FLAGS_32BITREQUIRED|COMIMAGE_FLAGS_32BITPREFERRED); } while (false)

#define COR_CLEAR_32BIT_PREFERRED(_flagsfield) \
    do { _flagsfield &= ~(COMIMAGE_FLAGS_32BITREQUIRED|COMIMAGE_FLAGS_32BITPREFERRED); } while (false)


#ifndef __IMAGE_COR20_HEADER_DEFINED__
#define __IMAGE_COR20_HEADER_DEFINED__

typedef enum ReplacesCorHdrNumericDefines
{
// COM+ Header entry point flags.
    COMIMAGE_FLAGS_ILONLY               =0x00000001,
    COMIMAGE_FLAGS_32BITREQUIRED        =0x00000002,    // *** Do not manipulate this bit directly (see notes above)
    COMIMAGE_FLAGS_IL_LIBRARY           =0x00000004,
    COMIMAGE_FLAGS_STRONGNAMESIGNED     =0x00000008,
    COMIMAGE_FLAGS_NATIVE_ENTRYPOINT    =0x00000010,
    COMIMAGE_FLAGS_TRACKDEBUGDATA       =0x00010000,
    COMIMAGE_FLAGS_32BITPREFERRED       =0x00020000,    // *** Do not manipulate this bit directly (see notes above)


// Version flags for image.
    COR_VERSION_MAJOR_V2                =2,
    COR_VERSION_MAJOR                   =COR_VERSION_MAJOR_V2,
    COR_VERSION_MINOR                   =5,
    COR_DELETED_NAME_LENGTH             =8,
    COR_VTABLEGAP_NAME_LENGTH           =8,

// Maximum size of a NativeType descriptor.
    NATIVE_TYPE_MAX_CB                  =1,
    COR_ILMETHOD_SECT_SMALL_MAX_DATASIZE=0xFF,

// V-table constants
    COR_VTABLE_32BIT                    =0x01,          // V-table slots are 32-bits in size.
    COR_VTABLE_64BIT                    =0x02,          // V-table slots are 64-bits in size.
    COR_VTABLE_FROM_UNMANAGED           =0x04,          // If set, transition from unmanaged.
    COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN=0x08,    // NEW
    COR_VTABLE_CALL_MOST_DERIVED        =0x10,          // Call most derived method described by

// EATJ constants
    IMAGE_COR_EATJ_THUNK_SIZE           = 32,           // Size of a jump thunk reserved range.

// Max name lengths
    //@todo: Change to unlimited name lengths.
    MAX_CLASS_NAME                      =1024,
    MAX_PACKAGE_NAME                    =1024,
} ReplacesCorHdrNumericDefines;

//
// Directory format.
//
#ifndef IMAGE_DATA_DIRECTORY_DEFINED

#define IMAGE_DATA_DIRECTORY_DEFINED
typedef struct _IMAGE_DATA_DIRECTORY {
    uint32_t   VirtualAddress;
    uint32_t   Size;
} IMAGE_DATA_DIRECTORY, *PIMAGE_DATA_DIRECTORY;

#endif // IMAGE_DATA_DIRECTORY_DEFINED

// #ManagedHeader
//
// A managed code EXE or DLL uses the same basic format that unmanaged executables use call the Portable
// Executable (PE) format. See http://en.wikipedia.org/wiki/Portable_Executable or
// http://msdn.microsoft.com/msdnmag/issues/02/02/PE/default.aspx for more on this format and RVAs.
//
// PE files define fixed table of well known entry pointers call Directory entries. Each entry holds the
// relative virtual address (RVA) and length of a blob of data within the PE file. You can see these using
// the command
//
// link /dump /headers <EXENAME>
//
//
// Managed code has defined one of these entries (the 14th see code:IMAGE_DIRECTORY_ENTRY_COMHEADER) and the RVA points
// that the IMAGE_COR20_HEADER.  This header shows up in the previous dump as the following line
//
// // Managed code is identified by is following line
//
//             2008 [      48] RVA [size] of COM Descriptor Directory
//
// The IMAGE_COR20_HEADER is mostly just RVA:Length pairs (pointers) to other interesting data structures.
// The most important of these is the MetaData tables.   The easiest way of looking at meta-data is using
// the IlDasm.exe tool.
//
// MetaData holds most of the information in the IL image.  The exceptions are resource blobs and the IL
// instructions streams for individual methods.  Instead the Meta-data for a method holds an RVA to a
// code:IMAGE_COR_ILMETHOD which holds all the IL stream (and exception handling information).
//
// Precompiled (NGEN) images use the same IMAGE_COR20_HEADER but also use the ManagedNativeHeader field to
// point at structures that only exist in precompiled images.
//
typedef struct IMAGE_COR20_HEADER
{
    // Header versioning
    uint32_t                cb;
    uint16_t                MajorRuntimeVersion;
    uint16_t                MinorRuntimeVersion;

    // Symbol table and startup information
    IMAGE_DATA_DIRECTORY    MetaData;
    uint32_t                Flags;

	// The main program if it is an EXE (not used if a DLL?)
    // If COMIMAGE_FLAGS_NATIVE_ENTRYPOINT is not set, EntryPointToken represents a managed entrypoint.
	// If COMIMAGE_FLAGS_NATIVE_ENTRYPOINT is set, EntryPointRVA represents an RVA to a native entrypoint
	// (depricated for DLLs, use modules constructors intead).
    union {
        uint32_t            EntryPointToken;
        uint32_t            EntryPointRVA;
    };

    // This is the blob of managed resources. Fetched using code:AssemblyNative.GetResource and
    // code:PEFile.GetResource and accessible from managed code from
	// System.Assembly.GetManifestResourceStream.  The meta data has a table that maps names to offsets into
	// this blob, so logically the blob is a set of resources.
    IMAGE_DATA_DIRECTORY    Resources;
	// IL assemblies can be signed with a public-private key to validate who created it.  The signature goes
	// here if this feature is used.
    IMAGE_DATA_DIRECTORY    StrongNameSignature;

    IMAGE_DATA_DIRECTORY    CodeManagerTable;			// Depricated, not used
	// Used for manged codee that has unmaanaged code inside it (or exports methods as unmanaged entry points)
    IMAGE_DATA_DIRECTORY    VTableFixups;
    IMAGE_DATA_DIRECTORY    ExportAddressTableJumps;

	// null for ordinary IL images. In NGEN images it points at a code:CORCOMPILE_HEADER structure.
	// In Ready2Run images it points to a READYTORUN_HEADER.
    IMAGE_DATA_DIRECTORY    ManagedNativeHeader;

} IMAGE_COR20_HEADER, *PIMAGE_COR20_HEADER;

#else // !__IMAGE_COR20_HEADER_DEFINED__

// <TODO>@TODO: This is required because we pull in the COM+ 2.0 PE header
// definition from WinNT.h, and these constants have not yet propogated to there.</TODO>
//
#define COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN 0x08
#define COMIMAGE_FLAGS_32BITPREFERRED              0x00020000

#endif // __IMAGE_COR20_HEADER_DEFINED__


// The most recent version.

#define COR_CTOR_METHOD_NAME        ".ctor"
#define COR_CTOR_METHOD_NAME_W      W(".ctor")
#define COR_CCTOR_METHOD_NAME       ".cctor"
#define COR_CCTOR_METHOD_NAME_W     W(".cctor")

#define COR_ENUM_FIELD_NAME         "value__"
#define COR_ENUM_FIELD_NAME_W       W("value__")

// The predefined name for deleting a typeDef,MethodDef, FieldDef, Property and Event
#define COR_DELETED_NAME_A          "_Deleted"
#define COR_DELETED_NAME_W          W("_Deleted")
#define COR_VTABLEGAP_NAME_A        "_VtblGap"
#define COR_VTABLEGAP_NAME_W        W("_VtblGap")

// We intentionally use strncmp so that we will ignore any suffix
#define IsDeletedName(strName)      (strncmp(strName, COR_DELETED_NAME_A, COR_DELETED_NAME_LENGTH) == 0)
#define IsVtblGapName(strName)      (strncmp(strName, COR_VTABLEGAP_NAME_A, COR_VTABLEGAP_NAME_LENGTH) == 0)

// TypeDef/ExportedType attr bits, used by DefineTypeDef.
typedef enum CorTypeAttr
{
    // Use this mask to retrieve the type visibility information.
    tdVisibilityMask        =   0x00000007,
    tdNotPublic             =   0x00000000,     // Class is not public scope.
    tdPublic                =   0x00000001,     // Class is public scope.
    tdNestedPublic          =   0x00000002,     // Class is nested with public visibility.
    tdNestedPrivate         =   0x00000003,     // Class is nested with private visibility.
    tdNestedFamily          =   0x00000004,     // Class is nested with family visibility.
    tdNestedAssembly        =   0x00000005,     // Class is nested with assembly visibility.
    tdNestedFamANDAssem     =   0x00000006,     // Class is nested with family and assembly visibility.
    tdNestedFamORAssem      =   0x00000007,     // Class is nested with family or assembly visibility.

    // Use this mask to retrieve class layout information
    tdLayoutMask            =   0x00000018,
    tdAutoLayout            =   0x00000000,     // Class fields are auto-laid out
    tdSequentialLayout      =   0x00000008,     // Class fields are laid out sequentially
    tdExplicitLayout        =   0x00000010,     // Layout is supplied explicitly
    // end layout mask

    // Use this mask to retrieve class semantics information.
    tdClassSemanticsMask    =   0x00000020,
    tdClass                 =   0x00000000,     // Type is a class.
    tdInterface             =   0x00000020,     // Type is an interface.
    // end semantics mask

    // Special semantics in addition to class semantics.
    tdAbstract              =   0x00000080,     // Class is abstract
    tdSealed                =   0x00000100,     // Class is concrete and may not be extended
    tdSpecialName           =   0x00000400,     // Class name is special.  Name describes how.

    // Implementation attributes.
    tdImport                =   0x00001000,     // Class / interface is imported
    tdSerializable          =   0x00002000,     // The class is Serializable.
    tdWindowsRuntime        =   0x00004000,     // The type is a Windows Runtime type

    // Use tdStringFormatMask to retrieve string information for native interop
    tdStringFormatMask      =   0x00030000,
    tdAnsiClass             =   0x00000000,     // LPTSTR is interpreted as ANSI in this class
    tdUnicodeClass          =   0x00010000,     // LPTSTR is interpreted as UNICODE
    tdAutoClass             =   0x00020000,     // LPTSTR is interpreted automatically
    tdCustomFormatClass     =   0x00030000,     // A non-standard encoding specified by CustomFormatMask
    tdCustomFormatMask      =   0x00C00000,     // Use this mask to retrieve non-standard encoding information for native interop. The meaning of the values of these 2 bits is unspecified.

    // end string format mask

    tdBeforeFieldInit       =   0x00100000,     // Initialize the class any time before first static field access.
    tdForwarder             =   0x00200000,     // This ExportedType is a type forwarder.

    // Flags reserved for runtime use.
    tdReservedMask          =   0x00040800,
    tdRTSpecialName         =   0x00000800,     // Runtime should check name encoding.
    tdHasSecurity           =   0x00040000,     // Class has security associate with it.
} CorTypeAttr;


// Macros for accessing the members of the CorTypeAttr.
#define IsTdNotPublic(x)                    (((x) & tdVisibilityMask) == tdNotPublic)
#define IsTdPublic(x)                       (((x) & tdVisibilityMask) == tdPublic)
#define IsTdNestedPublic(x)                 (((x) & tdVisibilityMask) == tdNestedPublic)
#define IsTdNestedPrivate(x)                (((x) & tdVisibilityMask) == tdNestedPrivate)
#define IsTdNestedFamily(x)                 (((x) & tdVisibilityMask) == tdNestedFamily)
#define IsTdNestedAssembly(x)               (((x) & tdVisibilityMask) == tdNestedAssembly)
#define IsTdNestedFamANDAssem(x)            (((x) & tdVisibilityMask) == tdNestedFamANDAssem)
#define IsTdNestedFamORAssem(x)             (((x) & tdVisibilityMask) == tdNestedFamORAssem)
#define IsTdNested(x)                       (((x) & tdVisibilityMask) >= tdNestedPublic)

#define IsTdAutoLayout(x)                   (((x) & tdLayoutMask) == tdAutoLayout)
#define IsTdSequentialLayout(x)             (((x) & tdLayoutMask) == tdSequentialLayout)
#define IsTdExplicitLayout(x)               (((x) & tdLayoutMask) == tdExplicitLayout)

#define IsTdClass(x)                        (((x) & tdClassSemanticsMask) == tdClass)
#define IsTdInterface(x)                    (((x) & tdClassSemanticsMask) == tdInterface)

#define IsTdAbstract(x)                     ((x) & tdAbstract)
#define IsTdSealed(x)                       ((x) & tdSealed)
#define IsTdSpecialName(x)                  ((x) & tdSpecialName)

#define IsTdImport(x)                       ((x) & tdImport)
#define IsTdSerializable(x)                 ((x) & tdSerializable)
#define IsTdWindowsRuntime(x)               ((x) & tdWindowsRuntime)

#define IsTdAnsiClass(x)                    (((x) & tdStringFormatMask) == tdAnsiClass)
#define IsTdUnicodeClass(x)                 (((x) & tdStringFormatMask) == tdUnicodeClass)
#define IsTdAutoClass(x)                    (((x) & tdStringFormatMask) == tdAutoClass)
#define IsTdCustomFormatClass(x)            (((x) & tdStringFormatMask) == tdCustomFormatClass)
#define IsTdBeforeFieldInit(x)              ((x) & tdBeforeFieldInit)
#define IsTdForwarder(x)                    ((x) & tdForwarder)

#define IsTdRTSpecialName(x)                ((x) & tdRTSpecialName)
#define IsTdHasSecurity(x)                  ((x) & tdHasSecurity)

// MethodDef attr bits, Used by DefineMethod.
typedef enum CorMethodAttr
{
    // member access mask - Use this mask to retrieve accessibility information.
    mdMemberAccessMask          =   0x0007,
    mdPrivateScope              =   0x0000,     // Member not referenceable.
    mdPrivate                   =   0x0001,     // Accessible only by the parent type.
    mdFamANDAssem               =   0x0002,     // Accessible by sub-types only in this Assembly.
    mdAssem                     =   0x0003,     // Accessibly by anyone in the Assembly.
    mdFamily                    =   0x0004,     // Accessible only by type and sub-types.
    mdFamORAssem                =   0x0005,     // Accessibly by sub-types anywhere, plus anyone in assembly.
    mdPublic                    =   0x0006,     // Accessibly by anyone who has visibility to this scope.
    // end member access mask

    // method contract attributes.
    mdStatic                    =   0x0010,     // Defined on type, else per instance.
    mdFinal                     =   0x0020,     // Method may not be overridden.
    mdVirtual                   =   0x0040,     // Method virtual.
    mdHideBySig                 =   0x0080,     // Method hides by name+sig, else just by name.

    // vtable layout mask - Use this mask to retrieve vtable attributes.
    mdVtableLayoutMask          =   0x0100,
    mdReuseSlot                 =   0x0000,     // The default.
    mdNewSlot                   =   0x0100,     // Method always gets a new slot in the vtable.
    // end vtable layout mask

    // method implementation attributes.
    mdCheckAccessOnOverride     =   0x0200,     // Overridability is the same as the visibility.
    mdAbstract                  =   0x0400,     // Method does not provide an implementation.
    mdSpecialName               =   0x0800,     // Method is special.  Name describes how.

    // interop attributes
    mdPinvokeImpl               =   0x2000,     // Implementation is forwarded through pinvoke.
    mdUnmanagedExport           =   0x0008,     // Managed method exported via thunk to unmanaged code.

    // Reserved flags for runtime use only.
    mdReservedMask              =   0xd000,
    mdRTSpecialName             =   0x1000,     // Runtime should check name encoding.
    mdHasSecurity               =   0x4000,     // Method has security associate with it.
    mdRequireSecObject          =   0x8000,     // Method calls another method containing security code.

} CorMethodAttr;

// Macros for accessing the members of CorMethodAttr.
#define IsMdPrivateScope(x)                 (((x) & mdMemberAccessMask) == mdPrivateScope)
#define IsMdPrivate(x)                      (((x) & mdMemberAccessMask) == mdPrivate)
#define IsMdFamANDAssem(x)                  (((x) & mdMemberAccessMask) == mdFamANDAssem)
#define IsMdAssem(x)                        (((x) & mdMemberAccessMask) == mdAssem)
#define IsMdFamily(x)                       (((x) & mdMemberAccessMask) == mdFamily)
#define IsMdFamORAssem(x)                   (((x) & mdMemberAccessMask) == mdFamORAssem)
#define IsMdPublic(x)                       (((x) & mdMemberAccessMask) == mdPublic)

#define IsMdStatic(x)                       ((x) & mdStatic)
#define IsMdFinal(x)                        ((x) & mdFinal)
#define IsMdVirtual(x)                      ((x) & mdVirtual)
#define IsMdHideBySig(x)                    ((x) & mdHideBySig)

#define IsMdReuseSlot(x)                    (((x) & mdVtableLayoutMask) == mdReuseSlot)
#define IsMdNewSlot(x)                      (((x) & mdVtableLayoutMask) == mdNewSlot)

#define IsMdCheckAccessOnOverride(x)        ((x) & mdCheckAccessOnOverride)
#define IsMdAbstract(x)                     ((x) & mdAbstract)
#define IsMdSpecialName(x)                  ((x) & mdSpecialName)

#define IsMdPinvokeImpl(x)                  ((x) & mdPinvokeImpl)
#define IsMdUnmanagedExport(x)              ((x) & mdUnmanagedExport)

#define IsMdRTSpecialName(x)                ((x) & mdRTSpecialName)
#define IsMdInstanceInitializer(x, str)     (((x) & mdRTSpecialName) && !strcmp((str), COR_CTOR_METHOD_NAME))
#define IsMdInstanceInitializerW(x, str)    (((x) & mdRTSpecialName) && !wcscmp((str), COR_CTOR_METHOD_NAME_W))
#define IsMdClassConstructor(x, str)        (((x) & mdRTSpecialName) && !strcmp((str), COR_CCTOR_METHOD_NAME))
#define IsMdClassConstructorW(x, str)       (((x) & mdRTSpecialName) && !wcscmp((str), COR_CCTOR_METHOD_NAME_W))
#define IsMdHasSecurity(x)                  ((x) & mdHasSecurity)
#define IsMdRequireSecObject(x)             ((x) & mdRequireSecObject)

// FieldDef attr bits, used by DefineField.
typedef enum CorFieldAttr
{
    // member access mask - Use this mask to retrieve accessibility information.
    fdFieldAccessMask           =   0x0007,
    fdPrivateScope              =   0x0000,     // Member not referenceable.
    fdPrivate                   =   0x0001,     // Accessible only by the parent type.
    fdFamANDAssem               =   0x0002,     // Accessible by sub-types only in this Assembly.
    fdAssembly                  =   0x0003,     // Accessibly by anyone in the Assembly.
    fdFamily                    =   0x0004,     // Accessible only by type and sub-types.
    fdFamORAssem                =   0x0005,     // Accessibly by sub-types anywhere, plus anyone in assembly.
    fdPublic                    =   0x0006,     // Accessibly by anyone who has visibility to this scope.
    // end member access mask

    // field contract attributes.
    fdStatic                    =   0x0010,     // Defined on type, else per instance.
    fdInitOnly                  =   0x0020,     // Field may only be initialized, not written to after init.
    fdLiteral                   =   0x0040,     // Value is compile time constant.
    fdNotSerialized             =   0x0080,     // Field does not have to be serialized when type is remoted.

    fdSpecialName               =   0x0200,     // field is special.  Name describes how.

    // interop attributes
    fdPinvokeImpl               =   0x2000,     // Implementation is forwarded through pinvoke.

    // Reserved flags for runtime use only.
    fdReservedMask              =   0x9500,
    fdRTSpecialName             =   0x0400,     // Runtime(metadata internal APIs) should check name encoding.
    fdHasFieldMarshal           =   0x1000,     // Field has marshalling information.
    fdHasDefault                =   0x8000,     // Field has default.
    fdHasFieldRVA               =   0x0100,     // Field has RVA.
} CorFieldAttr;

// Macros for accessing the members of CorFieldAttr.
#define IsFdPrivateScope(x)                 (((x) & fdFieldAccessMask) == fdPrivateScope)
#define IsFdPrivate(x)                      (((x) & fdFieldAccessMask) == fdPrivate)
#define IsFdFamANDAssem(x)                  (((x) & fdFieldAccessMask) == fdFamANDAssem)
#define IsFdAssembly(x)                     (((x) & fdFieldAccessMask) == fdAssembly)
#define IsFdFamily(x)                       (((x) & fdFieldAccessMask) == fdFamily)
#define IsFdFamORAssem(x)                   (((x) & fdFieldAccessMask) == fdFamORAssem)
#define IsFdPublic(x)                       (((x) & fdFieldAccessMask) == fdPublic)

#define IsFdStatic(x)                       ((x) & fdStatic)
#define IsFdInitOnly(x)                     ((x) & fdInitOnly)
#define IsFdLiteral(x)                      ((x) & fdLiteral)
#define IsFdNotSerialized(x)                ((x) & fdNotSerialized)

#define IsFdPinvokeImpl(x)                  ((x) & fdPinvokeImpl)
#define IsFdSpecialName(x)                  ((x) & fdSpecialName)
#define IsFdHasFieldRVA(x)                  ((x) & fdHasFieldRVA)

#define IsFdRTSpecialName(x)                ((x) & fdRTSpecialName)
#define IsFdHasFieldMarshal(x)              ((x) & fdHasFieldMarshal)
#define IsFdHasDefault(x)                   ((x) & fdHasDefault)

// Param attr bits, used by DefineParam.
typedef enum CorParamAttr
{
    pdIn                        =   0x0001,     // Param is [In]
    pdOut                       =   0x0002,     // Param is [out]
    pdOptional                  =   0x0010,     // Param is optional

    // Reserved flags for Runtime use only.
    pdReservedMask              =   0xf000,
    pdHasDefault                =   0x1000,     // Param has default value.
    pdHasFieldMarshal           =   0x2000,     // Param has FieldMarshal.

    pdUnused                    =   0xcfe0,
} CorParamAttr;

// Macros for accessing the members of CorParamAttr.
#define IsPdIn(x)                           ((x) & pdIn)
#define IsPdOut(x)                          ((x) & pdOut)
#define IsPdOptional(x)                     ((x) & pdOptional)

#define IsPdHasDefault(x)                   ((x) & pdHasDefault)
#define IsPdHasFieldMarshal(x)              ((x) & pdHasFieldMarshal)


// Property attr bits, used by DefineProperty.
typedef enum CorPropertyAttr
{
    prSpecialName           =   0x0200,     // property is special.  Name describes how.

    // Reserved flags for Runtime use only.
    prReservedMask          =   0xf400,
    prRTSpecialName         =   0x0400,     // Runtime(metadata internal APIs) should check name encoding.
    prHasDefault            =   0x1000,     // Property has default

    prUnused                =   0xe9ff,
} CorPropertyAttr;

// Macros for accessing the members of CorPropertyAttr.
#define IsPrSpecialName(x)                  ((x) & prSpecialName)

#define IsPrRTSpecialName(x)                ((x) & prRTSpecialName)
#define IsPrHasDefault(x)                   ((x) & prHasDefault)

// Event attr bits, used by DefineEvent.
typedef enum CorEventAttr
{
    evSpecialName           =   0x0200,     // event is special.  Name describes how.

    // Reserved flags for Runtime use only.
    evReservedMask          =   0x0400,
    evRTSpecialName         =   0x0400,     // Runtime(metadata internal APIs) should check name encoding.
} CorEventAttr;

// Macros for accessing the members of CorEventAttr.
#define IsEvSpecialName(x)                  ((x) & evSpecialName)

#define IsEvRTSpecialName(x)                ((x) & evRTSpecialName)


// MethodSemantic attr bits, used by DefineProperty, DefineEvent.
typedef enum CorMethodSemanticsAttr
{
    msSetter    =   0x0001,     // Setter for property
    msGetter    =   0x0002,     // Getter for property
    msOther     =   0x0004,     // other method for property or event
    msAddOn     =   0x0008,     // AddOn method for event
    msRemoveOn  =   0x0010,     // RemoveOn method for event
    msFire      =   0x0020,     // Fire method for event
} CorMethodSemanticsAttr;

// Macros for accessing the members of CorMethodSemanticsAttr.
#define IsMsSetter(x)                       ((x) & msSetter)
#define IsMsGetter(x)                       ((x) & msGetter)
#define IsMsOther(x)                        ((x) & msOther)
#define IsMsAddOn(x)                        ((x) & msAddOn)
#define IsMsRemoveOn(x)                     ((x) & msRemoveOn)
#define IsMsFire(x)                         ((x) & msFire)


// DeclSecurity attr bits, used by DefinePermissionSet.
typedef enum CorDeclSecurity
{
    dclActionMask               =   0x001f,     // Mask allows growth of enum.
    dclActionNil                =   0x0000,     //
    dclRequest                  =   0x0001,     //
    dclDemand                   =   0x0002,     //
    dclAssert                   =   0x0003,     //
    dclDeny                     =   0x0004,     //
    dclPermitOnly               =   0x0005,     //
    dclLinktimeCheck            =   0x0006,     //
    dclInheritanceCheck         =   0x0007,     //
    dclRequestMinimum           =   0x0008,     //
    dclRequestOptional          =   0x0009,     //
    dclRequestRefuse            =   0x000a,     //
    dclPrejitGrant              =   0x000b,     // Persisted grant set at prejit time
    dclPrejitDenied             =   0x000c,     // Persisted denied set at prejit time
    dclNonCasDemand             =   0x000d,     //
    dclNonCasLinkDemand         =   0x000e,     //
    dclNonCasInheritance        =   0x000f,     //
    dclMaximumValue             =   0x000f,     // Maximum legal value
} CorDeclSecurity;

// Macros for accessing the members of CorDeclSecurity.
#define IsDclActionNil(x)                   (((x) & dclActionMask) == dclActionNil)

// Is this a demand that can trigger a stackwalk?
#define IsDclActionAnyStackModifier(x)              ((((x) & dclActionMask) == dclAssert) || \
                                                    (((x) & dclActionMask) == dclDeny)  || \
                                                    (((x) & dclActionMask) == dclPermitOnly))

// Is this an assembly level attribute (i.e. not applicable on Type/Member)?
#define IsAssemblyDclAction(x)              (((x) >= dclRequestMinimum)  && \
                                             ((x) <= dclRequestRefuse))

// Is this an NGen only attribute?
#define IsNGenOnlyDclAction(x)              (((x) == dclPrejitGrant)  || \
                                             ((x) == dclPrejitDenied))


// MethodImpl attr bits, used by DefineMethodImpl.
typedef enum CorMethodImpl
{
    // code impl mask
    miCodeTypeMask       =   0x0003,   // Flags about code type.
    miIL                 =   0x0000,   // Method impl is IL.
    miNative             =   0x0001,   // Method impl is native.
    miOPTIL              =   0x0002,   // Method impl is OPTIL
    miRuntime            =   0x0003,   // Method impl is provided by the runtime.
    // end code impl mask

    // managed mask
    miManagedMask        =   0x0004,   // Flags specifying whether the code is managed or unmanaged.
    miUnmanaged          =   0x0004,   // Method impl is unmanaged, otherwise managed.
    miManaged            =   0x0000,   // Method impl is managed.
    // end managed mask

    // implementation info and interop
    miForwardRef         =   0x0010,   // Indicates method is defined; used primarily in merge scenarios.
    miPreserveSig        =   0x0080,   // Indicates method sig is not to be mangled to do HRESULT conversion.

    miInternalCall       =   0x1000,   // Reserved for internal use.

    miSynchronized       =   0x0020,   // Method is single threaded through the body.
    miNoInlining         =   0x0008,   // Method may not be inlined.
    miAggressiveInlining =   0x0100,   // Method should be inlined if possible.
    miNoOptimization     =   0x0040,   // Method may not be optimized.
    miAggressiveOptimization = 0x0200, // Method may contain hot code and should be aggressively optimized.

    // These are the flags that are allowed in MethodImplAttribute's Value
    // property. This should include everything above except the code impl
    // flags (which are used for MethodImplAttribute's MethodCodeType field).
    miUserMask           =   miManagedMask | miForwardRef | miPreserveSig |
                             miInternalCall | miSynchronized |
                             miNoInlining | miAggressiveInlining |
                             miNoOptimization | miAggressiveOptimization,

    miMaxMethodImplVal   =   0xffff,   // Range check value
} CorMethodImpl;

// Macros for accesing the members of CorMethodImpl.
#define IsMiIL(x)                           (((x) & miCodeTypeMask) == miIL)
#define IsMiNative(x)                       (((x) & miCodeTypeMask) == miNative)
#define IsMiOPTIL(x)                        (((x) & miCodeTypeMask) == miOPTIL)
#define IsMiRuntime(x)                      (((x) & miCodeTypeMask) == miRuntime)

#define IsMiUnmanaged(x)                    (((x) & miManagedMask) == miUnmanaged)
#define IsMiManaged(x)                      (((x) & miManagedMask) == miManaged)

#define IsMiForwardRef(x)                   ((x) & miForwardRef)
#define IsMiPreserveSig(x)                  ((x) & miPreserveSig)

#define IsMiInternalCall(x)                 ((x) & miInternalCall)

#define IsMiSynchronized(x)                 ((x) & miSynchronized)
#define IsMiNoInlining(x)                   ((x) & miNoInlining)
#define IsMiAggressiveInlining(x)           ((x) & miAggressiveInlining)
#define IsMiNoOptimization(x)               ((x) & miNoOptimization)
#define IsMiAggressiveOptimization(x)       (((x) & (miAggressiveOptimization | miNoOptimization)) == miAggressiveOptimization)

// PinvokeMap attr bits, used by DefinePinvokeMap.
typedef enum  CorPinvokeMap
{
    pmNoMangle          = 0x0001,   // Pinvoke is to use the member name as specified.

    // Use this mask to retrieve the CharSet information.
    pmCharSetMask       = 0x0006,
    pmCharSetNotSpec    = 0x0000,
    pmCharSetAnsi       = 0x0002,
    pmCharSetUnicode    = 0x0004,
    pmCharSetAuto       = 0x0006,


    pmBestFitUseAssem   = 0x0000,
    pmBestFitEnabled    = 0x0010,
    pmBestFitDisabled   = 0x0020,
    pmBestFitMask       = 0x0030,

    pmThrowOnUnmappableCharUseAssem   = 0x0000,
    pmThrowOnUnmappableCharEnabled    = 0x1000,
    pmThrowOnUnmappableCharDisabled   = 0x2000,
    pmThrowOnUnmappableCharMask       = 0x3000,

    pmSupportsLastError = 0x0040,   // Information about target function. Not relevant for fields.

    // None of the calling convention flags is relevant for fields.
    pmCallConvMask      = 0x0700,
    pmCallConvWinapi    = 0x0100,   // Pinvoke will use native callconv appropriate to target windows platform.
    pmCallConvCdecl     = 0x0200,
    pmCallConvStdcall   = 0x0300,
    pmCallConvThiscall  = 0x0400,   // In M9, pinvoke will raise exception.
    pmCallConvFastcall  = 0x0500,

    pmMaxValue          = 0xFFFF,
} CorPinvokeMap;

// Macros for accessing the members of CorPinvokeMap
#define IsPmNoMangle(x)                     ((x) & pmNoMangle)

#define IsPmCharSetNotSpec(x)               (((x) & pmCharSetMask) == pmCharSetNotSpec)
#define IsPmCharSetAnsi(x)                  (((x) & pmCharSetMask) == pmCharSetAnsi)
#define IsPmCharSetUnicode(x)               (((x) & pmCharSetMask) == pmCharSetUnicode)
#define IsPmCharSetAuto(x)                  (((x) & pmCharSetMask) == pmCharSetAuto)

#define IsPmSupportsLastError(x)            ((x) & pmSupportsLastError)

#define IsPmCallConvWinapi(x)               (((x) & pmCallConvMask) == pmCallConvWinapi)
#define IsPmCallConvCdecl(x)                (((x) & pmCallConvMask) == pmCallConvCdecl)
#define IsPmCallConvStdcall(x)              (((x) & pmCallConvMask) == pmCallConvStdcall)
#define IsPmCallConvThiscall(x)             (((x) & pmCallConvMask) == pmCallConvThiscall)
#define IsPmCallConvFastcall(x)             (((x) & pmCallConvMask) == pmCallConvFastcall)

#define IsPmBestFitEnabled(x)                 (((x) & pmBestFitMask) == pmBestFitEnabled)
#define IsPmBestFitDisabled(x)                (((x) & pmBestFitMask) == pmBestFitDisabled)
#define IsPmBestFitUseAssem(x)                (((x) & pmBestFitMask) == pmBestFitUseAssem)

#define IsPmThrowOnUnmappableCharEnabled(x)   (((x) & pmThrowOnUnmappableCharMask) == pmThrowOnUnmappableCharEnabled)
#define IsPmThrowOnUnmappableCharDisabled(x)  (((x) & pmThrowOnUnmappableCharMask) == pmThrowOnUnmappableCharDisabled)
#define IsPmThrowOnUnmappableCharUseAssem(x)  (((x) & pmThrowOnUnmappableCharMask) == pmThrowOnUnmappableCharUseAssem)

// Assembly attr bits, used by DefineAssembly.
typedef enum CorAssemblyFlags
{
    afPublicKey             =   0x0001,     // The assembly ref holds the full (unhashed) public key.

    afPA_None               =   0x0000,     // Processor Architecture unspecified
    afPA_MSIL               =   0x0010,     // Processor Architecture: neutral (PE32)
    afPA_x86                =   0x0020,     // Processor Architecture: x86 (PE32)
    afPA_IA64               =   0x0030,     // Processor Architecture: Itanium (PE32+)
    afPA_AMD64              =   0x0040,     // Processor Architecture: AMD X64 (PE32+)
    afPA_ARM                =   0x0050,     // Processor Architecture: ARM (PE32)
    afPA_ARM64              =   0x0060,     // Processor Architecture: ARM64 (PE32+)
    afPA_NoPlatform         =   0x0070,      // applies to any platform but cannot run on any (e.g. reference assembly), should not have "specified" set
    afPA_Specified          =   0x0080,     // Propagate PA flags to AssemblyRef record
    afPA_Mask               =   0x0070,     // Bits describing the processor architecture
    afPA_FullMask           =   0x00F0,     // Bits describing the PA incl. Specified
    afPA_Shift              =   0x0004,     // NOT A FLAG, shift count in PA flags <--> index conversion

    afEnableJITcompileTracking   =  0x8000, // From "DebuggableAttribute".
    afDisableJITcompileOptimizer =  0x4000, // From "DebuggableAttribute".
    afDebuggableAttributeMask    =  0xc000,

    afRetargetable          =   0x0100,     // The assembly can be retargeted (at runtime) to an
                                            //  assembly from a different publisher.

    afContentType_Default         = 0x0000,
    afContentType_WindowsRuntime  = 0x0200,
    afContentType_Mask            = 0x0E00, // Bits describing ContentType
} CorAssemblyFlags;

// Macros for accessing the members of CorAssemblyFlags.
#define IsAfRetargetable(x)                 ((x) & afRetargetable)
#define IsAfContentType_Default(x)          (((x) & afContentType_Mask) == afContentType_Default)
#define IsAfContentType_WindowsRuntime(x)   (((x) & afContentType_Mask) == afContentType_WindowsRuntime)

// Macros for accessing the Processor Architecture flags of CorAssemblyFlags.
#define IsAfPA_MSIL(x) (((x) & afPA_Mask) == afPA_MSIL)
#define IsAfPA_x86(x) (((x) & afPA_Mask) == afPA_x86)
#define IsAfPA_IA64(x) (((x) & afPA_Mask) == afPA_IA64)
#define IsAfPA_AMD64(x) (((x) & afPA_Mask) == afPA_AMD64)
#define IsAfPA_ARM(x) (((x) & afPA_Mask) == afPA_ARM)
#define IsAfPA_ARM64(x) (((x) & afPA_Mask) == afPA_ARM64)
#define IsAfPA_NoPlatform(x) (((x) & afPA_FullMask) == afPA_NoPlatform)
#define IsAfPA_Specified(x) ((x) & afPA_Specified)
#define PAIndex(x) (((x) & afPA_Mask) >> afPA_Shift)
#define PAFlag(x)  (((x) << afPA_Shift) & afPA_Mask)
#define PrepareForSaving(x) ((x) & (((x) & afPA_Specified) ? ~afPA_Specified : ~afPA_FullMask))

#define IsAfEnableJITcompileTracking(x)     ((x) & afEnableJITcompileTracking)
#define IsAfDisableJITcompileOptimizer(x)   ((x) & afDisableJITcompileOptimizer)

// Macros for accessing the public key flags of CorAssemblyFlags.
#define IsAfPublicKey(x)                    ((x) & afPublicKey)
#define IsAfPublicKeyToken(x)               (((x) & afPublicKey) == 0)


// ManifestResource attr bits, used by DefineManifestResource.
typedef enum CorManifestResourceFlags
{
    mrVisibilityMask        =   0x0007,
    mrPublic                =   0x0001,     // The Resource is exported from the Assembly.
    mrPrivate               =   0x0002,     // The Resource is private to the Assembly.
} CorManifestResourceFlags;

// Macros for accessing the members of CorManifestResourceFlags.
#define IsMrPublic(x)                       (((x) & mrVisibilityMask) == mrPublic)
#define IsMrPrivate(x)                      (((x) & mrVisibilityMask) == mrPrivate)


// File attr bits, used by DefineFile.
typedef enum CorFileFlags
{
    ffContainsMetaData      =   0x0000,     // This is not a resource file
    ffContainsNoMetaData    =   0x0001,     // This is a resource file or other non-metadata-containing file
} CorFileFlags;

// Macros for accessing the members of CorFileFlags.
#define IsFfContainsMetaData(x)             (!((x) & ffContainsNoMetaData))
#define IsFfContainsNoMetaData(x)           ((x) & ffContainsNoMetaData)

// PE file kind bits, returned by IMetaDataImport2::GetPEKind()
typedef enum CorPEKind
{
    peNot       = 0x00000000,   // not a PE file
    peILonly    = 0x00000001,   // flag IL_ONLY is set in COR header
    pe32BitRequired=0x00000002,  // flag 32BITREQUIRED is set and 32BITPREFERRED is clear in COR header
    pe32Plus    = 0x00000004,   // PE32+ file (64 bit)
    pe32Unmanaged=0x00000008,    // PE32 without COR header
    pe32BitPreferred=0x00000010  // flags 32BITREQUIRED and 32BITPREFERRED are set in COR header
} CorPEKind;


// GenericParam bits, used by DefineGenericParam.
typedef enum CorGenericParamAttr
{
    // Variance of type parameters, only applicable to generic parameters
    // for generic interfaces and delegates
    gpVarianceMask          =   0x0003,
    gpNonVariant            =   0x0000,
    gpCovariant             =   0x0001,
    gpContravariant         =   0x0002,

    // Special constraints, applicable to any type parameters
    gpSpecialConstraintMask =  0x001C,
    gpNoSpecialConstraint   =   0x0000,
    gpReferenceTypeConstraint = 0x0004,      // type argument must be a reference type
    gpNotNullableValueTypeConstraint   =   0x0008,      // type argument must be a value type but not Nullable
    gpDefaultConstructorConstraint = 0x0010, // type argument must have a public default constructor
} CorGenericParamAttr;

// structures and enums moved from COR.H
typedef uint8_t COR_SIGNATURE;

typedef COR_SIGNATURE* PCOR_SIGNATURE;      // pointer to a cor sig.  Not void* so that
                                            // the bytes can be incremented easily
typedef const COR_SIGNATURE* PCCOR_SIGNATURE;


typedef const char * MDUTF8CSTR;
typedef char * MDUTF8STR;

//*****************************************************************************
//
// Element type for Cor signature
//
//*****************************************************************************

typedef enum CorElementType
{
    ELEMENT_TYPE_END            = 0x00,
    ELEMENT_TYPE_VOID           = 0x01,
    ELEMENT_TYPE_BOOLEAN        = 0x02,
    ELEMENT_TYPE_CHAR           = 0x03,
    ELEMENT_TYPE_I1             = 0x04,
    ELEMENT_TYPE_U1             = 0x05,
    ELEMENT_TYPE_I2             = 0x06,
    ELEMENT_TYPE_U2             = 0x07,
    ELEMENT_TYPE_I4             = 0x08,
    ELEMENT_TYPE_U4             = 0x09,
    ELEMENT_TYPE_I8             = 0x0a,
    ELEMENT_TYPE_U8             = 0x0b,
    ELEMENT_TYPE_R4             = 0x0c,
    ELEMENT_TYPE_R8             = 0x0d,
    ELEMENT_TYPE_STRING         = 0x0e,

    // every type above PTR will be simple type
    ELEMENT_TYPE_PTR            = 0x0f,     // PTR <type>
    ELEMENT_TYPE_BYREF          = 0x10,     // BYREF <type>

    // Please use ELEMENT_TYPE_VALUETYPE. ELEMENT_TYPE_VALUECLASS is deprecated.
    ELEMENT_TYPE_VALUETYPE      = 0x11,     // VALUETYPE <class Token>
    ELEMENT_TYPE_CLASS          = 0x12,     // CLASS <class Token>
    ELEMENT_TYPE_VAR            = 0x13,     // a class type variable VAR <number>
    ELEMENT_TYPE_ARRAY          = 0x14,     // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
    ELEMENT_TYPE_GENERICINST    = 0x15,     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
    ELEMENT_TYPE_TYPEDBYREF     = 0x16,     // TYPEDREF  (it takes no args) a typed referece to some other type

    ELEMENT_TYPE_I              = 0x18,     // native integer size
    ELEMENT_TYPE_U              = 0x19,     // native unsigned integer size
    ELEMENT_TYPE_FNPTR          = 0x1b,     // FNPTR <complete sig for the function including calling convention>
    ELEMENT_TYPE_OBJECT         = 0x1c,     // Shortcut for System.Object
    ELEMENT_TYPE_SZARRAY        = 0x1d,     // Shortcut for single dimension zero lower bound array
                                            // SZARRAY <type>
    ELEMENT_TYPE_MVAR           = 0x1e,     // a method type variable MVAR <number>

    // This is only for binding
    ELEMENT_TYPE_CMOD_REQD      = 0x1f,     // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>
    ELEMENT_TYPE_CMOD_OPT       = 0x20,     // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>

    // This is for signatures generated internally (which will not be persisted in any way).
    ELEMENT_TYPE_INTERNAL       = 0x21,     // INTERNAL <typehandle>

    // Note that this is the max of base type excluding modifiers
    ELEMENT_TYPE_MAX            = 0x22,     // first invalid element type


    ELEMENT_TYPE_MODIFIER       = 0x40,
    ELEMENT_TYPE_SENTINEL       = 0x01 | ELEMENT_TYPE_MODIFIER, // sentinel for varargs
    ELEMENT_TYPE_PINNED         = 0x05 | ELEMENT_TYPE_MODIFIER,

} CorElementType;


//*****************************************************************************
//
// Serialization types for Custom attribute support
//
//*****************************************************************************

typedef enum CorSerializationType
{
    SERIALIZATION_TYPE_UNDEFINED    = 0,
    SERIALIZATION_TYPE_BOOLEAN      = ELEMENT_TYPE_BOOLEAN,
    SERIALIZATION_TYPE_CHAR         = ELEMENT_TYPE_CHAR,
    SERIALIZATION_TYPE_I1           = ELEMENT_TYPE_I1,
    SERIALIZATION_TYPE_U1           = ELEMENT_TYPE_U1,
    SERIALIZATION_TYPE_I2           = ELEMENT_TYPE_I2,
    SERIALIZATION_TYPE_U2           = ELEMENT_TYPE_U2,
    SERIALIZATION_TYPE_I4           = ELEMENT_TYPE_I4,
    SERIALIZATION_TYPE_U4           = ELEMENT_TYPE_U4,
    SERIALIZATION_TYPE_I8           = ELEMENT_TYPE_I8,
    SERIALIZATION_TYPE_U8           = ELEMENT_TYPE_U8,
    SERIALIZATION_TYPE_R4           = ELEMENT_TYPE_R4,
    SERIALIZATION_TYPE_R8           = ELEMENT_TYPE_R8,
    SERIALIZATION_TYPE_STRING       = ELEMENT_TYPE_STRING,
    SERIALIZATION_TYPE_SZARRAY      = ELEMENT_TYPE_SZARRAY, // Shortcut for single dimension zero lower bound array
    SERIALIZATION_TYPE_TYPE         = 0x50,
    SERIALIZATION_TYPE_TAGGED_OBJECT= 0x51,
    SERIALIZATION_TYPE_FIELD        = 0x53,
    SERIALIZATION_TYPE_PROPERTY     = 0x54,
    SERIALIZATION_TYPE_ENUM         = 0x55
} CorSerializationType;

//
// Calling convention flags.
//

typedef enum CorUnmanagedCallingConvention
{
    IMAGE_CEE_UNMANAGED_CALLCONV_C         = 0x1,
    IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL   = 0x2,
    IMAGE_CEE_UNMANAGED_CALLCONV_THISCALL  = 0x3,
    IMAGE_CEE_UNMANAGED_CALLCONV_FASTCALL  = 0x4,
} CorUnmanagedCallingConvention;

typedef enum CorCallingConvention
{
    IMAGE_CEE_CS_CALLCONV_DEFAULT       = 0x0,
    IMAGE_CEE_CS_CALLCONV_C         = IMAGE_CEE_UNMANAGED_CALLCONV_C,
    IMAGE_CEE_CS_CALLCONV_STDCALL   = IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL,
    IMAGE_CEE_CS_CALLCONV_THISCALL  = IMAGE_CEE_UNMANAGED_CALLCONV_THISCALL,
    IMAGE_CEE_CS_CALLCONV_FASTCALL  = IMAGE_CEE_UNMANAGED_CALLCONV_FASTCALL,
    IMAGE_CEE_CS_CALLCONV_VARARG        = 0x5,
    IMAGE_CEE_CS_CALLCONV_FIELD         = 0x6,
    IMAGE_CEE_CS_CALLCONV_LOCAL_SIG     = 0x7,
    IMAGE_CEE_CS_CALLCONV_PROPERTY      = 0x8,
    IMAGE_CEE_CS_CALLCONV_UNMANAGED     = 0x9,  // Unmanaged calling convention encoded as modopts
    IMAGE_CEE_CS_CALLCONV_GENERICINST   = 0xa,  // generic method instantiation
    IMAGE_CEE_CS_CALLCONV_NATIVEVARARG  = 0xb,  // used ONLY for 64bit vararg PInvoke calls
    IMAGE_CEE_CS_CALLCONV_MAX           = 0xc,  // first invalid calling convention


        // The high bits of the calling convention convey additional info
    IMAGE_CEE_CS_CALLCONV_MASK      = 0x0f,  // Calling convention is bottom 4 bits
    IMAGE_CEE_CS_CALLCONV_HASTHIS   = 0x20,  // Top bit indicates a 'this' parameter
    IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 0x40,  // This parameter is explicitly in the signature
    IMAGE_CEE_CS_CALLCONV_GENERIC   = 0x10,  // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
    // 0x80 is reserved for internal use
} CorCallingConvention;

#define IMAGE_CEE_CS_CALLCONV_INSTANTIATION IMAGE_CEE_CS_CALLCONV_GENERICINST


typedef enum CorArgType
{
    IMAGE_CEE_CS_END        = 0x0,
    IMAGE_CEE_CS_VOID       = 0x1,
    IMAGE_CEE_CS_I4         = 0x2,
    IMAGE_CEE_CS_I8         = 0x3,
    IMAGE_CEE_CS_R4         = 0x4,
    IMAGE_CEE_CS_R8         = 0x5,
    IMAGE_CEE_CS_PTR        = 0x6,
    IMAGE_CEE_CS_OBJECT     = 0x7,
    IMAGE_CEE_CS_STRUCT4    = 0x8,
    IMAGE_CEE_CS_STRUCT32   = 0x9,
    IMAGE_CEE_CS_BYVALUE    = 0xA,
} CorArgType;


//*****************************************************************************
//
// Native type for N-Direct
//
//*****************************************************************************

typedef enum CorNativeType
{

    // Kepp this in-synch with ndp\clr\src\BCL\System\runtime\interopservices\attributes.cs

    NATIVE_TYPE_END         = 0x0,    //DEPRECATED
    NATIVE_TYPE_VOID        = 0x1,    //DEPRECATED
    NATIVE_TYPE_BOOLEAN     = 0x2,    // (4 byte boolean value: TRUE = non-zero, FALSE = 0)
    NATIVE_TYPE_I1          = 0x3,
    NATIVE_TYPE_U1          = 0x4,
    NATIVE_TYPE_I2          = 0x5,
    NATIVE_TYPE_U2          = 0x6,
    NATIVE_TYPE_I4          = 0x7,
    NATIVE_TYPE_U4          = 0x8,
    NATIVE_TYPE_I8          = 0x9,
    NATIVE_TYPE_U8          = 0xa,
    NATIVE_TYPE_R4          = 0xb,
    NATIVE_TYPE_R8          = 0xc,
    NATIVE_TYPE_SYSCHAR     = 0xd,    //DEPRECATED
    NATIVE_TYPE_VARIANT     = 0xe,    //DEPRECATED
    NATIVE_TYPE_CURRENCY    = 0xf,
    NATIVE_TYPE_PTR         = 0x10,   //DEPRECATED

    NATIVE_TYPE_DECIMAL     = 0x11,   //DEPRECATED
    NATIVE_TYPE_DATE        = 0x12,   //DEPRECATED
    NATIVE_TYPE_BSTR        = 0x13,   //COMINTEROP
    NATIVE_TYPE_LPSTR       = 0x14,
    NATIVE_TYPE_LPWSTR      = 0x15,
    NATIVE_TYPE_LPTSTR      = 0x16,
    NATIVE_TYPE_FIXEDSYSSTRING  = 0x17,
    NATIVE_TYPE_OBJECTREF   = 0x18,   //DEPRECATED
    NATIVE_TYPE_IUNKNOWN    = 0x19,   //COMINTEROP
    NATIVE_TYPE_IDISPATCH   = 0x1a,   //COMINTEROP
    NATIVE_TYPE_STRUCT      = 0x1b,
    NATIVE_TYPE_INTF        = 0x1c,   //COMINTEROP
    NATIVE_TYPE_SAFEARRAY   = 0x1d,   //COMINTEROP
    NATIVE_TYPE_FIXEDARRAY  = 0x1e,
    NATIVE_TYPE_INT         = 0x1f,
    NATIVE_TYPE_UINT        = 0x20,

    NATIVE_TYPE_NESTEDSTRUCT  = 0x21, //DEPRECATED (use NATIVE_TYPE_STRUCT)

    NATIVE_TYPE_BYVALSTR    = 0x22,   //COMINTEROP

    NATIVE_TYPE_ANSIBSTR    = 0x23,   //COMINTEROP

    NATIVE_TYPE_TBSTR       = 0x24, // select BSTR or ANSIBSTR depending on platform
                                      //COMINTEROP

    NATIVE_TYPE_VARIANTBOOL = 0x25, // (2-byte boolean value: TRUE = -1, FALSE = 0)
                                      //COMINTEROP
    NATIVE_TYPE_FUNC        = 0x26,

    NATIVE_TYPE_ASANY       = 0x28,

    NATIVE_TYPE_ARRAY       = 0x2a,
    NATIVE_TYPE_LPSTRUCT    = 0x2b,

    NATIVE_TYPE_CUSTOMMARSHALER = 0x2c,  // Custom marshaler native type. This must be followed
                                         // by a string of the following format:
                                         // "Native type name/0Custom marshaler type name/0Optional cookie/0"
                                         // Or
                                         // "{Native type GUID}/0Custom marshaler type name/0Optional cookie/0"

    NATIVE_TYPE_ERROR       = 0x2d, // This native type coupled with ELEMENT_TYPE_I4 will map to VT_HRESULT
                                    //COMINTEROP

    NATIVE_TYPE_IINSPECTABLE = 0x2e,
    NATIVE_TYPE_HSTRING     = 0x2f,
    NATIVE_TYPE_LPUTF8STR   = 0x30, // utf-8 string
    NATIVE_TYPE_MAX         = 0x50, // first invalid element type
} CorNativeType;


enum
{
    DESCR_GROUP_METHODDEF = 0,          // DESCR group for MethodDefs
    DESCR_GROUP_METHODIMPL,             // DESCR group for MethodImpls
};

/***********************************************************************************/
// a COR_ILMETHOD_SECT is a generic container for attributes that are private
// to a particular method.  The COR_ILMETHOD structure points to one of these
// (see GetSect()).  COR_ILMETHOD_SECT can decode the Kind of attribute (but not
// its internal data layout, and can skip past the current attibute to find the
// Next one.   The overhead for COR_ILMETHOD_SECT is a minimum of 2 bytes.

typedef enum CorILMethodSect                             // codes that identify attributes
{
    CorILMethod_Sect_Reserved    = 0,
    CorILMethod_Sect_EHTable     = 1,
    CorILMethod_Sect_OptILTable  = 2,

    CorILMethod_Sect_KindMask    = 0x3F,        // The mask for decoding the type code
    CorILMethod_Sect_FatFormat   = 0x40,        // fat format
    CorILMethod_Sect_MoreSects   = 0x80,        // there is another attribute after this one
} CorILMethodSect;

/************************************/
/* NOTE this structure must be DWORD aligned!! */

typedef struct IMAGE_COR_ILMETHOD_SECT_SMALL
{
    uint8_t Kind;
    uint8_t DataSize;

} IMAGE_COR_ILMETHOD_SECT_SMALL;



/************************************/
/* NOTE this structure must be DWORD aligned!! */
typedef struct IMAGE_COR_ILMETHOD_SECT_FAT
{
    unsigned Kind : 8;
    unsigned DataSize : 24;

} IMAGE_COR_ILMETHOD_SECT_FAT;



/***********************************************************************************/
/* If COR_ILMETHOD_SECT_HEADER::Kind() = CorILMethod_Sect_EHTable then the attribute
   is a list of exception handling clauses.  There are two formats, fat or small
*/
typedef enum CorExceptionFlag                       // definitions for the Flags field below (for both big and small)
{
    COR_ILEXCEPTION_CLAUSE_NONE,                    // This is a typed handler
    COR_ILEXCEPTION_CLAUSE_OFFSETLEN = 0x0000,      // Deprecated
    COR_ILEXCEPTION_CLAUSE_DEPRECATED = 0x0000,     // Deprecated
    COR_ILEXCEPTION_CLAUSE_FILTER  = 0x0001,        // If this bit is on, then this EH entry is for a filter
    COR_ILEXCEPTION_CLAUSE_FINALLY = 0x0002,        // This clause is a finally clause
    COR_ILEXCEPTION_CLAUSE_FAULT = 0x0004,          // Fault clause (finally that is called on exception only)
    COR_ILEXCEPTION_CLAUSE_DUPLICATED = 0x0008,     // duplicated clause. This clause was duplicated to a funclet which was pulled out of line
} CorExceptionFlag;

/***********************************/
typedef struct IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT
{
    CorExceptionFlag    Flags;
    uint32_t            TryOffset;
    uint32_t            TryLength;      // relative to start of try block
    uint32_t            HandlerOffset;
    uint32_t            HandlerLength;  // relative to start of handler
    union {
        uint32_t        ClassToken;     // use for type-based exception handlers
        uint32_t        FilterOffset;   // use for filter-based exception handlers (COR_ILEXCEPTION_FILTER is set)
    };
} IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT;

typedef struct IMAGE_COR_ILMETHOD_SECT_EH_FAT
{
    IMAGE_COR_ILMETHOD_SECT_FAT   SectFat;
    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT Clauses[1];     // actually variable size
} IMAGE_COR_ILMETHOD_SECT_EH_FAT;

/***********************************/
typedef struct IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL
{
#ifdef HOST_64BIT
    unsigned            Flags         : 16;
#else // !HOST_64BIT
    CorExceptionFlag    Flags         : 16;
#endif
    unsigned            TryOffset     : 16;
    unsigned            TryLength     : 8;  // relative to start of try block
    unsigned            HandlerOffset : 16;
    unsigned            HandlerLength : 8;  // relative to start of handler
    union {
        uint32_t        ClassToken;
        uint32_t        FilterOffset;
    };
} IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL;

/***********************************/
typedef struct IMAGE_COR_ILMETHOD_SECT_EH_SMALL
{
    IMAGE_COR_ILMETHOD_SECT_SMALL SectSmall;
    uint16_t Reserved;
    IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL Clauses[1];   // actually variable size
} IMAGE_COR_ILMETHOD_SECT_EH_SMALL;



typedef union IMAGE_COR_ILMETHOD_SECT_EH
{
    IMAGE_COR_ILMETHOD_SECT_EH_SMALL Small;
    IMAGE_COR_ILMETHOD_SECT_EH_FAT Fat;
} IMAGE_COR_ILMETHOD_SECT_EH;


/***********************************************************************************/
// Legal values for
// * code:IMAGE_COR_ILMETHOD_FAT::Flags or
// * code:IMAGE_COR_ILMETHOD_TINY::Flags_CodeSize fields.
//
// The only semantic flag at present is CorILMethod_InitLocals
typedef enum CorILMethodFlags
{
    CorILMethod_InitLocals      = 0x0010,           // call default constructor on all local vars
    CorILMethod_MoreSects       = 0x0008,           // there is another attribute after this one

    CorILMethod_CompressedIL    = 0x0040,           // Not used.

        // Indicates the format for the COR_ILMETHOD header
    CorILMethod_FormatShift     = 3,
    CorILMethod_FormatMask      = ((1 << CorILMethod_FormatShift) - 1),
    CorILMethod_TinyFormat      = 0x0002,         // use this code if the code size is even
    CorILMethod_SmallFormat     = 0x0000,
    CorILMethod_FatFormat       = 0x0003,
    CorILMethod_TinyFormat1     = 0x0006,         // use this code if the code size is odd
} CorILMethodFlags;

/***************************************************************************/
/* Used when the method is tiny (< 64 bytes), and there are no local vars */
typedef struct IMAGE_COR_ILMETHOD_TINY
{
    uint8_t Flags_CodeSize;
} IMAGE_COR_ILMETHOD_TINY;

/************************************/
// This strucuture is the 'fat' layout, where no compression is attempted.
// Note that this structure can be added on at the end, thus making it extensible
typedef struct IMAGE_COR_ILMETHOD_FAT
{
    unsigned Flags    : 12;     // Flags see code:CorILMethodFlags
    unsigned Size     :  4;     // size in DWords of this structure (currently 3)
    unsigned MaxStack : 16;     // maximum number of items (I4, I, I8, obj ...), on the operand stack
    uint32_t CodeSize;          // size of the code
    mdSignature   LocalVarSigTok;     // token that indicates the signature of the local vars (0 means none)

} IMAGE_COR_ILMETHOD_FAT;

// an IMAGE_COR_ILMETHOD holds the IL instructions for a individual method.  To save space they come in two
// flavors Fat and Tiny.  Conceptually Tiny is just a compressed version of Fat, so code:IMAGE_COR_ILMETHOD_FAT
// is the logical structure for all headers.  Conceptually this blob holds the IL, the Exception Handling
// Tables, the local variable information and some flags.
typedef union IMAGE_COR_ILMETHOD
{
    IMAGE_COR_ILMETHOD_TINY       Tiny;
    IMAGE_COR_ILMETHOD_FAT        Fat;
} IMAGE_COR_ILMETHOD;

//*****************************************************************************
// Non VOS v-table entries.  Define an array of these pointed to by
// IMAGE_COR20_HEADER.VTableFixups.  Each entry describes a contiguous array of
// v-table slots.  The slots start out initialized to the meta data token value
// for the method they need to call.  At image load time, the CLR Loader will
// turn each entry into a pointer to machine code for the CPU and can be
// called directly.
//*****************************************************************************

typedef struct IMAGE_COR_VTABLEFIXUP
{
    uint32_t       RVA;                    // Offset of v-table array in image.
    uint16_t       Count;                  // How many entries at location.
    uint16_t       Type;                   // COR_VTABLE_xxx type of entries.
} IMAGE_COR_VTABLEFIXUP;





//*****************************************************************************
//*****************************************************************************
//
// M E T A - D A T A    D E C L A R A T I O N S
//
//*****************************************************************************
//*****************************************************************************

//*****************************************************************************
//
// Enums for SetOption API.
//
//*****************************************************************************

// flags for MetaDataCheckDuplicatesFor
typedef enum CorCheckDuplicatesFor
{
    MDDupAll                    = 0xffffffff,
    MDDupENC                    = MDDupAll,
    MDNoDupChecks               = 0x00000000,
    MDDupTypeDef                = 0x00000001,
    MDDupInterfaceImpl          = 0x00000002,
    MDDupMethodDef              = 0x00000004,
    MDDupTypeRef                = 0x00000008,
    MDDupMemberRef              = 0x00000010,
    MDDupCustomAttribute        = 0x00000020,
    MDDupParamDef               = 0x00000040,
    MDDupPermission             = 0x00000080,
    MDDupProperty               = 0x00000100,
    MDDupEvent                  = 0x00000200,
    MDDupFieldDef               = 0x00000400,
    MDDupSignature              = 0x00000800,
    MDDupModuleRef              = 0x00001000,
    MDDupTypeSpec               = 0x00002000,
    MDDupImplMap                = 0x00004000,
    MDDupAssemblyRef            = 0x00008000,
    MDDupFile                   = 0x00010000,
    MDDupExportedType           = 0x00020000,
    MDDupManifestResource       = 0x00040000,
    MDDupGenericParam           = 0x00080000,
    MDDupMethodSpec             = 0x00100000,
    MDDupGenericParamConstraint = 0x00200000,
    // gap for debug junk
    MDDupAssembly               = 0x10000000,

    // This is the default behavior on metadata. It will check duplicates for TypeRef, MemberRef, Signature, TypeSpec and MethodSpec.
    MDDupDefault = MDNoDupChecks | MDDupTypeRef | MDDupMemberRef | MDDupSignature | MDDupTypeSpec | MDDupMethodSpec,
} CorCheckDuplicatesFor;

// flags for MetaDataRefToDefCheck
typedef enum CorRefToDefCheck
{
    // default behavior is to always perform TypeRef to TypeDef and MemberRef to MethodDef/FieldDef optimization
    MDRefToDefDefault           = 0x00000003,
    MDRefToDefAll               = 0xffffffff,
    MDRefToDefNone              = 0x00000000,
    MDTypeRefToDef              = 0x00000001,
    MDMemberRefToDef            = 0x00000002
} CorRefToDefCheck;


// MetaDataNotificationForTokenMovement
typedef enum CorNotificationForTokenMovement
{
    // default behavior is to notify TypeRef, MethodDef, MemberRef, and FieldDef token remaps
    MDNotifyDefault             = 0x0000000f,
    MDNotifyAll                 = 0xffffffff,
    MDNotifyNone                = 0x00000000,
    MDNotifyMethodDef           = 0x00000001,
    MDNotifyMemberRef           = 0x00000002,
    MDNotifyFieldDef            = 0x00000004,
    MDNotifyTypeRef             = 0x00000008,

    MDNotifyTypeDef             = 0x00000010,
    MDNotifyParamDef            = 0x00000020,
    MDNotifyInterfaceImpl       = 0x00000040,
    MDNotifyProperty            = 0x00000080,
    MDNotifyEvent               = 0x00000100,
    MDNotifySignature           = 0x00000200,
    MDNotifyTypeSpec            = 0x00000400,
    MDNotifyCustomAttribute     = 0x00000800,
    MDNotifySecurityValue       = 0x00001000,
    MDNotifyPermission          = 0x00002000,
    MDNotifyModuleRef           = 0x00004000,

    MDNotifyNameSpace           = 0x00008000,

    MDNotifyAssemblyRef         = 0x01000000,
    MDNotifyFile                = 0x02000000,
    MDNotifyExportedType        = 0x04000000,
    MDNotifyResource            = 0x08000000,
} CorNotificationForTokenMovement;


typedef enum CorSetENC
{
    MDSetENCOn                  = 0x00000001,   // Deprecated name.
    MDSetENCOff                 = 0x00000002,   // Deprecated name.

    MDUpdateENC                 = 0x00000001,   // ENC mode.  Tokens don't move; can be updated.
    MDUpdateFull                = 0x00000002,   // "Normal" update mode.
    MDUpdateExtension           = 0x00000003,   // Extension mode.  Tokens don't move, adds only.
    MDUpdateIncremental         = 0x00000004,   // Incremental compilation
    MDUpdateDelta               = 0x00000005,   // If ENC on, save only deltas.
    MDUpdateMask                = 0x00000007,


} CorSetENC;

#define IsENCDelta(x)                       (((x) & MDUpdateMask) == MDUpdateDelta)

// flags used in SetOption when pair with MetaDataErrorIfEmitOutOfOrder guid
typedef enum CorErrorIfEmitOutOfOrder
{
    MDErrorOutOfOrderDefault    = 0x00000000,   // default not to generate any error
    MDErrorOutOfOrderNone       = 0x00000000,   // do not generate error for out of order emit
    MDErrorOutOfOrderAll        = 0xffffffff,   // generate out of order emit for method, field, param, property, and event
    MDMethodOutOfOrder          = 0x00000001,   // generate error when methods are emitted out of order
    MDFieldOutOfOrder           = 0x00000002,   // generate error when fields are emitted out of order
    MDParamOutOfOrder           = 0x00000004,   // generate error when params are emitted out of order
    MDPropertyOutOfOrder        = 0x00000008,   // generate error when properties are emitted out of order
    MDEventOutOfOrder           = 0x00000010,   // generate error when events are emitted out of order
} CorErrorIfEmitOutOfOrder;


// flags used in SetOption when pair with MetaDataImportOption guid
typedef enum CorImportOptions
{
    MDImportOptionDefault       = 0x00000000,   // default to skip over deleted records
    MDImportOptionAll           = 0xFFFFFFFF,   // Enumerate everything
    MDImportOptionAllTypeDefs   = 0x00000001,   // all of the typedefs including the deleted typedef
    MDImportOptionAllMethodDefs = 0x00000002,   // all of the methoddefs including the deleted ones
    MDImportOptionAllFieldDefs  = 0x00000004,   // all of the fielddefs including the deleted ones
    MDImportOptionAllProperties = 0x00000008,   // all of the properties including the deleted ones
    MDImportOptionAllEvents     = 0x00000010,   // all of the events including the deleted ones
    MDImportOptionAllCustomAttributes = 0x00000020, // all of the custom attributes including the deleted ones
    MDImportOptionAllExportedTypes  = 0x00000040,   // all of the ExportedTypes including the deleted ones

} CorImportOptions;


// flags for MetaDataThreadSafetyOptions
typedef enum CorThreadSafetyOptions
{
    // default behavior is to have thread safety turn off. This means that MetaData APIs will not take reader/writer
    // lock. Clients is responsible to make sure the properly thread synchornization when using MetaData APIs.
    MDThreadSafetyDefault       = 0x00000000,
    MDThreadSafetyOff           = 0x00000000,
    MDThreadSafetyOn            = 0x00000001,
} CorThreadSafetyOptions;


// flags for MetaDataLinkerOptions
typedef enum CorLinkerOptions
{
    // default behavior is not to keep private types
    MDAssembly          = 0x00000000,
    MDNetModule         = 0x00000001,
} CorLinkerOptions;

// flags for MetaDataMergeOptions
typedef enum MergeFlags
{
    MergeFlagsNone      =   0,
    MergeManifest       =   0x00000001,
    DropMemberRefCAs    =   0x00000002,
    NoDupCheck          =   0x00000004,
    MergeExportedTypes  =   0x00000008
} MergeFlags;

// flags for MetaDataPreserveLocalRefs
typedef enum CorLocalRefPreservation
{
    MDPreserveLocalRefsNone     = 0x00000000,
    MDPreserveLocalTypeRef      = 0x00000001,
    MDPreserveLocalMemberRef    = 0x00000002
} CorLocalRefPreservation;

//
// struct used to retrieve field offset
// used by GetClassLayout and SetClassLayout
//

#ifndef _COR_FIELD_OFFSET_
#define _COR_FIELD_OFFSET_

typedef struct COR_FIELD_OFFSET
{
    mdFieldDef  ridOfField;
    uint32_t       ulOffset;
} COR_FIELD_OFFSET;

#endif


//
// Token tags.
//
typedef enum CorTokenType
{
    mdtModule               = 0x00000000,       //
    mdtTypeRef              = 0x01000000,       //
    mdtTypeDef              = 0x02000000,       //
    mdtFieldDef             = 0x04000000,       //
    mdtMethodDef            = 0x06000000,       //
    mdtParamDef             = 0x08000000,       //
    mdtInterfaceImpl        = 0x09000000,       //
    mdtMemberRef            = 0x0a000000,       //
    mdtCustomAttribute      = 0x0c000000,       //
    mdtPermission           = 0x0e000000,       //
    mdtSignature            = 0x11000000,       //
    mdtEvent                = 0x14000000,       //
    mdtProperty             = 0x17000000,       //
    mdtMethodImpl           = 0x19000000,       //
    mdtModuleRef            = 0x1a000000,       //
    mdtTypeSpec             = 0x1b000000,       //
    mdtAssembly             = 0x20000000,       //
    mdtAssemblyRef          = 0x23000000,       //
    mdtFile                 = 0x26000000,       //
    mdtExportedType         = 0x27000000,       //
    mdtManifestResource     = 0x28000000,       //
    mdtNestedClass          = 0x29000000,       //
    mdtGenericParam         = 0x2a000000,       //
    mdtMethodSpec           = 0x2b000000,       //
    mdtGenericParamConstraint = 0x2c000000,

    mdtString               = 0x70000000,       //
    mdtName                 = 0x71000000,       //
    mdtBaseType             = 0x72000000,       // Leave this on the high end value. This does not correspond to metadata table
} CorTokenType;

//
// Build / decompose tokens.
//
#define RidToToken(rid,tktype) ((rid) |= (tktype))
#define TokenFromRid(rid,tktype) ((rid) | (tktype))
#define RidFromToken(tk) ((RID) ((tk) & 0x00ffffff))
#define TypeFromToken(tk) ((ULONG32)((tk) & 0xff000000))
#define IsNilToken(tk) ((RidFromToken(tk)) == 0)

//
// Nil tokens
//
#define mdTokenNil                  ((mdToken)0)
#define mdModuleNil                 ((mdModule)mdtModule)
#define mdTypeRefNil                ((mdTypeRef)mdtTypeRef)
#define mdTypeDefNil                ((mdTypeDef)mdtTypeDef)
#define mdFieldDefNil               ((mdFieldDef)mdtFieldDef)
#define mdMethodDefNil              ((mdMethodDef)mdtMethodDef)
#define mdParamDefNil               ((mdParamDef)mdtParamDef)
#define mdInterfaceImplNil          ((mdInterfaceImpl)mdtInterfaceImpl)
#define mdMemberRefNil              ((mdMemberRef)mdtMemberRef)
#define mdCustomAttributeNil        ((mdCustomAttribute)mdtCustomAttribute)
#define mdPermissionNil             ((mdPermission)mdtPermission)
#define mdSignatureNil              ((mdSignature)mdtSignature)
#define mdEventNil                  ((mdEvent)mdtEvent)
#define mdPropertyNil               ((mdProperty)mdtProperty)
#define mdModuleRefNil              ((mdModuleRef)mdtModuleRef)
#define mdTypeSpecNil               ((mdTypeSpec)mdtTypeSpec)
#define mdAssemblyNil               ((mdAssembly)mdtAssembly)
#define mdAssemblyRefNil            ((mdAssemblyRef)mdtAssemblyRef)
#define mdFileNil                   ((mdFile)mdtFile)
#define mdExportedTypeNil           ((mdExportedType)mdtExportedType)
#define mdManifestResourceNil       ((mdManifestResource)mdtManifestResource)

#define mdGenericParamNil           ((mdGenericParam)mdtGenericParam)
#define mdGenericParamConstraintNil ((mdGenericParamConstraint)mdtGenericParamConstraint)
#define mdMethodSpecNil             ((mdMethodSpec)mdtMethodSpec)

#define mdStringNil                 ((mdString)mdtString)

//
// Open bits.
//
typedef enum CorOpenFlags
{
    ofRead              =   0x00000000,     // Open scope for read
    ofWrite             =   0x00000001,     // Open scope for write.
    ofReadWriteMask     =   0x00000001,     // Mask for read/write bit.

    ofCopyMemory        =   0x00000002,     // Open scope with memory. Ask metadata to maintain its own copy of memory.

    ofReadOnly          =   0x00000010,     // Open scope for read. Will be unable to QI for a IMetadataEmit* interface
    ofTakeOwnership     =   0x00000020,     // The memory was allocated with CoTaskMemAlloc and will be freed by the metadata

    // These are obsolete and are ignored.
    // ofCacheImage     =   0x00000004,     // EE maps but does not do relocations or verify image
    // ofManifestMetadata = 0x00000008,     // Open scope on ngen image, return the manifest metadata instead of the IL metadata
    ofNoTypeLib         =   0x00000080,     // Don't OpenScope on a typelib.
    ofNoTransform       =   0x00001000,     // Disable automatic transforms of .winmd files.

    // Internal bits
    ofReserved1         =   0x00000100,     // Reserved for internal use.
    ofReserved2         =   0x00000200,     // Reserved for internal use.
    ofReserved3         =   0x00000400,     // Reserved for internal use.
    ofReserved          =   0xffffef40      // All the reserved bits.

} CorOpenFlags;

#define IsOfRead(x)                         (((x) & ofReadWriteMask) == ofRead)
#define IsOfReadWrite(x)                    (((x) & ofReadWriteMask) == ofWrite)

#define IsOfCopyMemory(x)                   ((x) & ofCopyMemory)

#define IsOfReadOnly(x)                     ((x) & ofReadOnly)
#define IsOfTakeOwnership(x)                ((x) & ofTakeOwnership)

#define IsOfReserved(x)                     (((x) & ofReserved) != 0)

//
// Type of file mapping returned by code:IMetaDataInfo::GetFileMapping.
//
typedef enum CorFileMapping
{
    fmFlat            = 0,  // Flat file mapping - file is mapped as data file (code:SEC_IMAGE flag was not
                            // passed to code:CreateFileMapping).
    fmExecutableImage = 1,  // Executable image file mapping - file is mapped for execution
                            // (either via code:LoadLibrary or code:CreateFileMapping with code:SEC_IMAGE flag).
} CorFileMapping;


typedef CorTypeAttr CorRegTypeAttr;

//
// Opaque type for an enumeration handle.
//
typedef void *HCORENUM;


// Note that this must be kept in sync with System.AttributeTargets.
typedef enum CorAttributeTargets
{
    catAssembly      = 0x0001,
    catModule        = 0x0002,
    catClass         = 0x0004,
    catStruct        = 0x0008,
    catEnum          = 0x0010,
    catConstructor   = 0x0020,
    catMethod        = 0x0040,
    catProperty      = 0x0080,
    catField         = 0x0100,
    catEvent         = 0x0200,
    catInterface     = 0x0400,
    catParameter     = 0x0800,
    catDelegate      = 0x1000,
    catGenericParameter = 0x4000,

    catAll           = catAssembly | catModule | catClass | catStruct | catEnum | catConstructor |
                    catMethod | catProperty | catField | catEvent | catInterface | catParameter | catDelegate | catGenericParameter,
    catClassMembers  = catClass | catStruct | catEnum | catConstructor | catMethod | catProperty | catField | catEvent | catDelegate | catInterface,

} CorAttributeTargets;

#ifndef MACROS_NOT_SUPPORTED
//
// Some well-known custom attributes
//
#ifndef IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS
  #define IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS (IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS)
#endif

#define INTEROP_DISPID_TYPE_W                   W("System.Runtime.InteropServices.DispIdAttribute")
#define INTEROP_DISPID_TYPE                     "System.Runtime.InteropServices.DispIdAttribute"
#define INTEROP_DISPID_SIG                      {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4}

#define INTEROP_INTERFACETYPE_TYPE_W            W("System.Runtime.InteropServices.InterfaceTypeAttribute")
#define INTEROP_INTERFACETYPE_TYPE              "System.Runtime.InteropServices.InterfaceTypeAttribute"
#define INTEROP_INTERFACETYPE_SIG               {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_CLASSINTERFACE_TYPE_W           W("System.Runtime.InteropServices.ClassInterfaceAttribute")
#define INTEROP_CLASSINTERFACE_TYPE             "System.Runtime.InteropServices.ClassInterfaceAttribute"
#define INTEROP_CLASSINTERFACE_SIG              {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_COMVISIBLE_TYPE_W               W("System.Runtime.InteropServices.ComVisibleAttribute")
#define INTEROP_COMVISIBLE_TYPE                 "System.Runtime.InteropServices.ComVisibleAttribute"
#define INTEROP_COMVISIBLE_SIG                  {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_BOOLEAN}

#define INTEROP_COMREGISTERFUNCTION_TYPE_W      W("System.Runtime.InteropServices.ComRegisterFunctionAttribute")
#define INTEROP_COMREGISTERFUNCTION_TYPE        "System.Runtime.InteropServices.ComRegisterFunctionAttribute"
#define INTEROP_COMREGISTERFUNCTION_SIG         {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_COMUNREGISTERFUNCTION_TYPE_W    W("System.Runtime.InteropServices.ComUnregisterFunctionAttribute")
#define INTEROP_COMUNREGISTERFUNCTION_TYPE      "System.Runtime.InteropServices.ComUnregisterFunctionAttribute"
#define INTEROP_COMUNREGISTERFUNCTION_SIG       {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_IMPORTEDFROMTYPELIB_TYPE_W      W("System.Runtime.InteropServices.ImportedFromTypeLibAttribute")
#define INTEROP_IMPORTEDFROMTYPELIB_TYPE        "System.Runtime.InteropServices.ImportedFromTypeLibAttribute"
#define INTEROP_IMPORTEDFROMTYPELIB_SIG         {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_PRIMARYINTEROPASSEMBLY_TYPE_W   W("System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute")
#define INTEROP_PRIMARYINTEROPASSEMBLY_TYPE     "System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute"
#define INTEROP_PRIMARYINTEROPASSEMBLY_SIG      {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_I4}

#define INTEROP_IDISPATCHIMPL_TYPE_W            W("System.Runtime.InteropServices.IDispatchImplAttribute")
#define INTEROP_IDISPATCHIMPL_TYPE              "System.Runtime.InteropServices.IDispatchImplAttribute"
#define INTEROP_IDISPATCHIMPL_SIG               {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_COMSOURCEINTERFACES_TYPE_W      W("System.Runtime.InteropServices.ComSourceInterfacesAttribute")
#define INTEROP_COMSOURCEINTERFACES_TYPE        "System.Runtime.InteropServices.ComSourceInterfacesAttribute"
#define INTEROP_COMSOURCEINTERFACES_SIG         {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_COMDEFAULTINTERFACE_TYPE_W      W("System.Runtime.InteropServices.ComDefaultInterfaceAttribute")
#define INTEROP_COMDEFAULTINTERFACE_TYPE        "System.Runtime.InteropServices.ComDefaultInterfaceAttribute"

#define INTEROP_COMCONVERSIONLOSS_TYPE_W        W("System.Runtime.InteropServices.ComConversionLossAttribute")
#define INTEROP_COMCONVERSIONLOSS_TYPE          "System.Runtime.InteropServices.ComConversionLossAttribute"
#define INTEROP_COMCONVERSIONLOSS_SIG           {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_BESTFITMAPPING_TYPE_W           W("System.Runtime.InteropServices.BestFitMappingAttribute")
#define INTEROP_BESTFITMAPPING_TYPE             "System.Runtime.InteropServices.BestFitMappingAttribute"
#define INTEROP_BESTFITMAPPING_SIG              {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_BOOLEAN}

#define INTEROP_TYPELIBTYPE_TYPE_W              W("System.Runtime.InteropServices.TypeLibTypeAttribute")
#define INTEROP_TYPELIBTYPE_TYPE                "System.Runtime.InteropServices.TypeLibTypeAttribute"
#define INTEROP_TYPELIBTYPE_SIG                 {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_TYPELIBFUNC_TYPE_W              W("System.Runtime.InteropServices.TypeLibFuncAttribute")
#define INTEROP_TYPELIBFUNC_TYPE                "System.Runtime.InteropServices.TypeLibFuncAttribute"
#define INTEROP_TYPELIBFUNC_SIG                 {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_TYPELIBVAR_TYPE_W               W("System.Runtime.InteropServices.TypeLibVarAttribute")
#define INTEROP_TYPELIBVAR_TYPE                 "System.Runtime.InteropServices.TypeLibVarAttribute"
#define INTEROP_TYPELIBVAR_SIG                  {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_MARSHALAS_TYPE_W                W("System.Runtime.InteropServices.MarshalAsAttribute")
#define INTEROP_MARSHALAS_TYPE                  "System.Runtime.InteropServices.MarshalAsAttribute"
#define INTEROP_MARSHALAS_SIG                   {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2}

#define INTEROP_COMIMPORT_TYPE_W                W("System.Runtime.InteropServices.ComImportAttribute")
#define INTEROP_COMIMPORT_TYPE                  "System.Runtime.InteropServices.ComImportAttribute"
#define INTEROP_COMIMPORT_SIG                   {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_GUID_TYPE_W                     W("System.Runtime.InteropServices.GuidAttribute")
#define INTEROP_GUID_TYPE                       "System.Runtime.InteropServices.GuidAttribute"
#define INTEROP_GUID_SIG                        {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_DEFAULTMEMBER_TYPE_W            W("System.Reflection.DefaultMemberAttribute")
#define INTEROP_DEFAULTMEMBER_TYPE              "System.Reflection.DefaultMemberAttribute"
#define INTEROP_DEFAULTMEMBER_SIG               {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_COMEMULATE_TYPE_W               W("System.Runtime.InteropServices.ComEmulateAttribute")
#define INTEROP_COMEMULATE_TYPE                 "System.Runtime.InteropServices.ComEmulateAttribute"
#define INTEROP_COMEMULATE_SIG                  {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_PRESERVESIG_TYPE_W              W("System.Runtime.InteropServices.PreserveSigAttribure")
#define INTEROP_PRESERVESIG_TYPE                "System.Runtime.InteropServices.PreserveSigAttribure"
#define INTEROP_PRESERVESIG_SIG                 {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_BOOLEAN}

#define INTEROP_IN_TYPE_W                       W("System.Runtime.InteropServices.InAttribute")
#define INTEROP_IN_TYPE                         "System.Runtime.InteropServices.InAttribute"
#define INTEROP_IN_SIG                          {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_OUT_TYPE_W                      W("System.Runtime.InteropServices.OutAttribute")
#define INTEROP_OUT_TYPE                        "System.Runtime.InteropServices.OutAttribute"
#define INTEROP_OUT_SIG                         {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_COMALIASNAME_TYPE_W             W("System.Runtime.InteropServices.ComAliasNameAttribute")
#define INTEROP_COMALIASNAME_TYPE               "System.Runtime.InteropServices.ComAliasNameAttribute"
#define INTEROP_COMALIASNAME_SIG                {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define INTEROP_PARAMARRAY_TYPE_W               W("System.ParamArrayAttribute")
#define INTEROP_PARAMARRAY_TYPE                 "System.ParamArrayAttribute"
#define INTEROP_PARAMARRAY_SIG                  {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_LCIDCONVERSION_TYPE_W           W("System.Runtime.InteropServices.LCIDConversionAttribute")
#define INTEROP_LCIDCONVERSION_TYPE             "System.Runtime.InteropServices.LCIDConversionAttribute"
#define INTEROP_LCIDCONVERSION_SIG              {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4}

#define INTEROP_COMSUBSTITUTABLEINTERFACE_TYPE_W    W("System.Runtime.InteropServices.ComSubstitutableInterfaceAttribute")
#define INTEROP_COMSUBSTITUTABLEINTERFACE_TYPE      "System.Runtime.InteropServices.ComSubstitutableInterfaceAttribute"
#define INTEROP_COMSUBSTITUTABLEINTERFACE_SIG       {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_DECIMALVALUE_TYPE_W             W("System.Runtime.CompilerServices.DecimalConstantAttribute")
#define INTEROP_DECIMALVALUE_TYPE               "System.Runtime.CompilerServices.DecimalConstantAttribute"
#define INTEROP_DECIMALVALUE_SIG                {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 5, ELEMENT_TYPE_VOID, ELEMENT_TYPE_U1, ELEMENT_TYPE_U1, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4, ELEMENT_TYPE_U4}

#define INTEROP_DATETIMEVALUE_TYPE_W            W("System.Runtime.CompilerServices.DateTimeConstantAttribute")
#define INTEROP_DATETIMEVALUE_TYPE              "System.Runtime.CompilerServices.DateTimeConstantAttribute"
#define INTEROP_DATETIMEVALUE_SIG               {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8}

#define INTEROP_IUNKNOWNVALUE_TYPE_W            W("System.Runtime.CompilerServices.IUnknownConstantAttribute")
#define INTEROP_IUNKNOWNVALUE_TYPE               "System.Runtime.CompilerServices.IUnknownConstantAttribute"
#define INTEROP_IUNKNOWNVALUE_SIG               {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_IDISPATCHVALUE_TYPE_W           W("System.Runtime.CompilerServices.IDispatchConstantAttribute")
#define INTEROP_IDISPATCHVALUE_TYPE              "System.Runtime.CompilerServices.IDispatchConstantAttribute"
#define INTEROP_IDISPATCHVALUE_SIG              {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_AUTOPROXY_TYPE_W                W("System.Runtime.InteropServices.AutomationProxyAttribute")
#define INTEROP_AUTOPROXY_TYPE                  "System.Runtime.InteropServices.AutomationProxyAttribute"
#define INTEROP_AUTOPROXY_SIG                   {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_BOOLEAN}

#define INTEROP_TYPELIBIMPORTCLASS_TYPE_W       W("System.Runtime.InteropServices.TypeLibImportClassAttribute")
#define INTEROP_TYPELIBIMPORTCLASS_TYPE         "System.Runtime.InteropServices.TypeLibImportClassAttribute"


#define INTEROP_TYPELIBVERSION_TYPE_W           W("System.Runtime.InteropServices.TypeLibVersionAttribute")
#define INTEROP_TYPELIBVERSION_TYPE             "System.Runtime.InteropServices.TypeLibVersionAttribute"
#define INTEROP_TYPELIBVERSION_SIG              {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2, ELEMENT_TYPE_I2}

#define INTEROP_COMCOMPATIBLEVERSION_TYPE_W     W("System.Runtime.InteropServices.ComCompatibleVersionAttribute")
#define INTEROP_COMCOMPATIBLEVERSION_TYPE       "System.Runtime.InteropServices.ComCompatibleVersionAttribute"
#define INTEROP_COMCOMPATIBLEVERSION_SIG        {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 4, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I2, ELEMENT_TYPE_I2, ELEMENT_TYPE_I2, ELEMENT_TYPE_I2}

#define INTEROP_COMEVENTINTERFACE_TYPE_W        W("System.Runtime.InteropServices.ComEventInterfaceAttribute")
#define INTEROP_COMEVENTINTERFACE_TYPE          "System.Runtime.InteropServices.ComEventInterfaceAttribute"

#define INTEROP_COCLASS_TYPE_W                  W("System.Runtime.InteropServices.CoClassAttribute")
#define INTEROP_COCLASS_TYPE                    "System.Runtime.InteropServices.CoClassAttribute"

#define INTEROP_SERIALIZABLE_TYPE_W             W("System.SerializableAttribute")
#define INTEROP_SERIALIZABLE_TYPE               "System.SerializableAttribute"
#define INTEROP_SERIALIZABLE_SIG                {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define INTEROP_SETWIN32CONTEXTINIDISPATCHATTRIBUTE_TYPE_W  W("System.Runtime.InteropServices.SetWin32ContextInIDispatchAttribute")
#define INTEROP_SETWIN32CONTEXTINIDISPATCHATTRIBUTE_TYPE     "System.Runtime.InteropServices.SetWin32ContextInIDispatchAttribute"
#define INTEROP_SETWIN32CONTEXTINIDISPATCHATTRIBUTE_SIG     {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define FORWARD_INTEROP_STUB_METHOD_TYPE_W      W("System.Runtime.InteropServices.ManagedToNativeComInteropStubAttribute")
#define FORWARD_INTEROP_STUB_METHOD_TYPE        "System.Runtime.InteropServices.ManagedToNativeComInteropStubAttribute"

#define FRIEND_ASSEMBLY_TYPE_W                  W("System.Runtime.CompilerServices.InternalsVisibleToAttribute")
#define FRIEND_ASSEMBLY_TYPE                    "System.Runtime.CompilerServices.InternalsVisibleToAttribute"
#define FRIEND_ASSEMBLY_TYPE_NAMESPACE          "System.Runtime.CompilerServices"
#define FRIEND_ASSEMBLY_TYPE_NAME               "InternalsVisibleToAttribute"
#define FRIEND_ASSEMBLY_SIG                     {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING, ELEMENT_TYPE_BOOLEAN}

#define SUBJECT_ASSEMBLY_TYPE_W                 W("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute")
#define SUBJECT_ASSEMBLY_TYPE                   "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute"
#define SUBJECT_ASSEMBLY_TYPE_NAMESPACE         "System.Runtime.CompilerServices"
#define SUBJECT_ASSEMBLY_TYPE_NAME              "IgnoresAccessChecksToAttribute"
#define SUBJECT_ASSEMBLY_SIG                    {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING}

#define DEFAULTDOMAIN_STA_TYPE_W                W("System.STAThreadAttribute")
#define DEFAULTDOMAIN_STA_TYPE                   "System.STAThreadAttribute"
#define DEFAULTDOMAIN_STA_SIG                   {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define DEFAULTDOMAIN_MTA_TYPE_W                W("System.MTAThreadAttribute")
#define DEFAULTDOMAIN_MTA_TYPE                   "System.MTAThreadAttribute"
#define DEFAULTDOMAIN_MTA_SIG                   {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_VOID}

#define NONVERSIONABLE_TYPE_W                   W("System.Runtime.Versioning.NonVersionableAttribute")
#define NONVERSIONABLE_TYPE                      "System.Runtime.Versioning.NonVersionableAttribute"

#define DEBUGGABLE_ATTRIBUTE_TYPE_W             W("System.Diagnostics.DebuggableAttribute")
#define DEBUGGABLE_ATTRIBUTE_TYPE               "System.Diagnostics.DebuggableAttribute"
#define DEBUGGABLE_ATTRIBUTE_TYPE_NAMESPACE     "System.Diagnostics"
#define DEBUGGABLE_ATTRIBUTE_TYPE_NAME          "DebuggableAttribute"


// Keep in sync with CompilationRelaxations.cs
typedef enum CompilationRelaxationsEnum
{
    CompilationRelaxations_NoStringInterning       = 0x0008,

} CompilationRelaxationEnum;

#define COMPILATIONRELAXATIONS_TYPE_W           W("System.Runtime.CompilerServices.CompilationRelaxationsAttribute")
#define COMPILATIONRELAXATIONS_TYPE             "System.Runtime.CompilerServices.CompilationRelaxationsAttribute"


// Keep in sync with RuntimeCompatibilityAttribute.cs
#define RUNTIMECOMPATIBILITY_TYPE_W             W("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute")
#define RUNTIMECOMPATIBILITY_TYPE               "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute"


// Keep in sync with AssemblySettingAttributes.cs

typedef enum NGenHintEnum
{
    NGenDefault             = 0x0000, // No preference specified

    NGenEager               = 0x0001, // NGen at install time
    NGenLazy                = 0x0002, // NGen after install time
    NGenNever               = 0x0003  // Assembly should not be ngened
} NGenHintEnum;

typedef enum LoadHintEnum
{
    LoadDefault             = 0x0000, // No preference specified

    LoadAlways              = 0x0001, // Dependency is always loaded
    LoadSometimes           = 0x0002, // Dependency is sometimes loaded
    LoadNever               = 0x0003  // Dependency is never loaded
} LoadHintEnum;

#define DEFAULTDEPENDENCY_TYPE_W                W("System.Runtime.CompilerServices.DefaultDependencyAttribute")
#define DEFAULTDEPENDENCY_TYPE                  "System.Runtime.CompilerServices.DefaultDependencyAttribute"

#define DEPENDENCY_TYPE_W                       W("System.Runtime.CompilerServices.DependencyAttribute")
#define DEPENDENCY_TYPE                         "System.Runtime.CompilerServices.DependencyAttribute"

#define TARGET_FRAMEWORK_TYPE_W                 W("System.Runtime.Versioning.TargetFrameworkAttribute")
#define TARGET_FRAMEWORK_TYPE                   "System.Runtime.Versioning.TargetFrameworkAttribute"

#define ASSEMBLY_METADATA_TYPE_W                W("System.Reflection.AssemblyMetadataAttribute")
#define ASSEMBLY_METADATA_TYPE                  "System.Reflection.AssemblyMetadataAttribute"


#define CMOD_CALLCONV_NAMESPACE_OLD             "System.Runtime.InteropServices"
#define CMOD_CALLCONV_NAMESPACE                 "System.Runtime.CompilerServices"
#define CMOD_CALLCONV_NAME_CDECL                "CallConvCdecl"
#define CMOD_CALLCONV_NAME_STDCALL              "CallConvStdcall"
#define CMOD_CALLCONV_NAME_THISCALL             "CallConvThiscall"
#define CMOD_CALLCONV_NAME_FASTCALL             "CallConvFastcall"
#define CMOD_CALLCONV_NAME_SUPPRESSGCTRANSITION "CallConvSuppressGCTransition"
#define CMOD_CALLCONV_NAME_MEMBERFUNCTION       "CallConvMemberFunction"

#endif // MACROS_NOT_SUPPORTED

//
// GetSaveSize accuracy
//
#ifndef _CORSAVESIZE_DEFINED_
#define _CORSAVESIZE_DEFINED_
typedef enum CorSaveSize
{
    cssAccurate             = 0x0000,               // Find exact save size, accurate but slower.
    cssQuick                = 0x0001,               // Estimate save size, may pad estimate, but faster.
    cssDiscardTransientCAs  = 0x0002,               // remove all of the CAs of discardable types
} CorSaveSize;
#endif

#define COR_IS_METHOD_MANAGED_IL(flags)         (((flags) & 0xf) == (miIL | miManaged))
#define COR_IS_METHOD_MANAGED_OPTIL(flags)      (((flags) & 0xf) == (miOPTIL | miManaged))
#define COR_IS_METHOD_MANAGED_NATIVE(flags)     (((flags) & 0xf) == (miNative | miManaged))
#define COR_IS_METHOD_UNMANAGED_NATIVE(flags)   (((flags) & 0xf) == (miNative | miUnmanaged))

//
// Enum used with NATIVE_TYPE_ARRAY.
//
typedef enum NativeTypeArrayFlags
{
    ntaSizeParamIndexSpecified = 0x0001,
    ntaReserved                = 0xfffe      // All the reserved bits.
} NativeTypeArrayFlags;

//
// Enum used for HFA type recognition.
// Supported across architectures, so that it can be used in altjits and cross-compilation.
typedef enum CorInfoHFAElemType : unsigned {
    CORINFO_HFA_ELEM_NONE,
    CORINFO_HFA_ELEM_FLOAT,
    CORINFO_HFA_ELEM_DOUBLE,
    CORINFO_HFA_ELEM_VECTOR64,
    CORINFO_HFA_ELEM_VECTOR128,
} CorInfoHFAElemType;

//
// Opaque types for security properties and values.
//
typedef void  *  PSECURITY_PROPS ;
typedef void  *  PSECURITY_VALUE ;
typedef void ** PPSECURITY_PROPS ;
typedef void ** PPSECURITY_VALUE ;

//-------------------------------------
//--- Security data structures
//-------------------------------------

// Descriptor for a single security custom attribute.
typedef struct COR_SECATTR {
    mdMemberRef     tkCtor;         // Ref to constructor of security attribute.
    const void     *pCustomAttribute;  // Blob describing ctor args and field/property values.
    uint32_t        cbCustomAttribute;  // Length of the above blob.
} COR_SECATTR;

#endif // __CORHDR_H__
