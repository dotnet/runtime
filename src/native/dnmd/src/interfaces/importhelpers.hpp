#ifndef _SRC_INTERFACES_IMPORTHELPERS_HPP
#define _SRC_INTERFACES_IMPORTHELPERS_HPP

#include <internal/dnmd_platform.hpp>
#include <internal/span.hpp>
#include <functional>

// Import a reference to a TypeDef row from one module and assembly pair to another.
HRESULT ImportReferenceToTypeDef(
    mdcursor_t sourceTypeDef,
    mdhandle_t sourceAssembly,
    span<uint8_t const> sourceAssemblyHash,
    mdhandle_t targetAssembly,
    mdhandle_t targetModule,
    bool alwaysImport, // Always import a reference to the TypeDef, even if the source and destination modules are the same.
    std::function<void(mdcursor_t row)> onRowEdited,
    mdcursor_t* targetTypeDef);

// Import a reference to a TypeDef, TypeRef, or TypeSpec row from one module and assembly pair to another, and return a TypeDef or TypeRef or TypeSpec token
// that can be used to refer to the imported type.
HRESULT ImportReferenceToTypeDefOrRefOrSpec(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<uint8_t const> sourceAssemblyHash,
    mdhandle_t targetAssembly,
    mdhandle_t targetModule,
    std::function<void(mdcursor_t)> onRowAdded,
    mdToken* importedToken);

// Import a reference to a MemberRef row from one module and assembly pair to another.
// This method works at the IMetadataEmit/Import level as it is implementation-agnostic.
HRESULT DefineImportMember(
    IMetaDataEmit* emit,
    IMetaDataAssemblyImport *pAssemImport,
    void const  *pbHashValue,
    ULONG        cbHashValue,
    IMetaDataImport *pImport,
    mdToken     mbMember,
    IMetaDataAssemblyEmit *pAssemEmit,
    mdToken     tkImport,
    mdMemberRef *pmr);

#endif // _SRC_INTERFACES_IMPORTHELPERS_HPP
