#include <cstddef>
#include <cstdint>

#define MINIPAL_COM_DEFINE_GUID
#include <minipal_com.h>

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        EXTERN_GUID(name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8)

// Define the IMetaDataDispenserEx option Guids here. They're declared in cor.h
#ifndef DNMD_USE_CORECLR_GUIDS
MIDL_DEFINE_GUID(GUID, MetaDataSetUpdate, 0x2eee315c, 0xd7db, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);
MIDL_DEFINE_GUID(GUID, MetaDataInitialSize, 0x2675b6bf, 0xf504, 0x4cb4, 0xa4, 0xd5, 0x08, 0x4e, 0xea, 0x77, 0x0d, 0xdc);
#endif
