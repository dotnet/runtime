// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_GUID_H
#define MINIPAL_GUID_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

typedef struct minipal_guid__
{
    uint32_t data1;
    uint16_t data2;
    uint16_t data3;
    uint8_t  data4[8];
} minipal_guid_t;

bool minipal_guid_v4_create(minipal_guid_t* guid);

bool minipal_guid_equals(minipal_guid_t const* g1, minipal_guid_t const* g2);

#ifdef __cplusplus
    }
#endif // __cplusplus

#ifdef __cplusplus
inline bool operator==(minipal_guid_t const& a, minipal_guid_t const& b)
{
    return minipal_guid_equals(&a, &b);
}

inline bool operator!=(minipal_guid_t const& a, minipal_guid_t const& b)
{
    return !(a == b);
}

template<typename T>
bool minipal_guid_v4_create(T* guid)
{
    static_assert(sizeof(T) == sizeof(minipal_guid_t), "minipal_guid_t size mismatch");
    return minipal_guid_v4_create(reinterpret_cast<minipal_guid_t*>(guid));
}
#endif

#endif // MINIPAL_GUID_H
