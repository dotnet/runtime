#ifndef _INC_DNMD_INTERFACES_HPP_
#define _INC_DNMD_INTERFACES_HPP_

#ifndef DNMD_EXPORT
#define DNMD_EXPORT
#endif // DNMD_EXPORT

// Given a pointer to a metadata blob, attempt to create an instance
// of the metadata related interface.
//
//  IMetaDataImport  - {7DAC8207-D3AE-4c75-9B67-92801A497D44}
//  IMetaDataImport2 - {FCE5EFA0-8BBA-4f8e-A036-8F2022B08466}
extern "C" DNMD_EXPORT
HRESULT ReadMetadata(
    void* data,
    size_t dataLen,
    REFGUID riid,
    void** ppObj);

#endif // _INC_DNMD_INTERFACES_HPP_