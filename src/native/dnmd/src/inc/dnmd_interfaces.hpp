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

#endif // _INC_DNMD_INTERFACES_HPP_
