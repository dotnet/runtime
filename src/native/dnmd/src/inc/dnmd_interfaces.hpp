#ifndef _INC_DNMD_INTERFACES_HPP_
#define _INC_DNMD_INTERFACES_HPP_

#ifndef DNMD_EXPORT
#define DNMD_EXPORT
#endif // !DNMD_EXPORT

struct IMDInternalImport;

// Create a metadata dispenser instance.
//
//  IMetaDataDispenser  - {809C652E-7396-11D2-9771-00A0C9B4D50C}
extern "C" DNMD_EXPORT
HRESULT GetDispenser(
    REFGUID riid,
    void** ppObj);

// Create a symbol binder instance.
//
//  ISymUnmanagedBinder  - {AA544D42-28CB-11d3-BD22-0000F80849BD}
extern "C" DNMD_EXPORT
HRESULT GetSymBinder(
    REFGUID riid,
    void** ppObj);

// Replace the metadata in an existing DNMD scope without changing its COM identity.
extern "C" DNMD_EXPORT
HRESULT ReOpenDNMDMetaDataWithMemory(
    IUnknown* scope,
    void const* data,
    ULONG size,
    DWORD flags);

// Convert a DNMD read-only internal importer to an independent writable scope.
// S_OK returns a new COM-owned interface; S_FALSE means input was already writable
// and returns the input pointer without adding a reference.
extern "C" DNMD_EXPORT
HRESULT ConvertDNMDInternalImport(
    IMDInternalImport* source,
    IMDInternalImport** converted);

// Return a public interface for an internal importer, converting an RO scope
// first so subsequent public-to-internal QI yields the writable importer.
extern "C" DNMD_EXPORT
HRESULT GetDNMDPublicInterfaceFromInternal(
    IMDInternalImport* source,
    REFIID riid,
    void** publicInterface);

#endif // _INC_DNMD_INTERFACES_HPP_
