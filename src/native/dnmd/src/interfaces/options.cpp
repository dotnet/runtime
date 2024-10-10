#include <cstddef>
#include <cstdint>

#define DNCP_DEFINE_GUID
#include <minipal_com.h>

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        EXTERN_GUID(name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8)

// Define the IMetaDataDispenserEx option Guids here. They're declared in cor.h
MIDL_DEFINE_GUID(GUID, MetaDataSetUpdate, 0x2eee315c, 0xd7db, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);