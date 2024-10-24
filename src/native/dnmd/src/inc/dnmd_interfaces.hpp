#ifndef _INC_DNMD_INTERFACES_HPP_
#define _INC_DNMD_INTERFACES_HPP_

#ifndef DNMD_EXPORT
#define DNMD_EXPORT
#endif // !DNMD_EXPORT

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

#endif // _INC_DNMD_INTERFACES_HPP_
