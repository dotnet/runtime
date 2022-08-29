// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++





--*/

#ifndef __PAL_ENDIAN_H__
#define __PAL_ENDIAN_H__

#ifdef __cplusplus
extern "C++" {
inline UINT16 SWAP16(UINT16 x)
{
    return (UINT16)((x >> 8) | (x << 8));
}

inline UINT32 SWAP32(UINT32 x)
{
    return  (x >> 24) |
            ((x >> 8) & 0x0000FF00L) |
            ((x & 0x0000FF00L) << 8) |
            (x << 24);
}

}
#endif // __cplusplus

#if BIGENDIAN
#ifdef __cplusplus
extern "C++" {
inline UINT16 VAL16(UINT16 x)
{
    return SWAP16(x);
}

inline UINT32 VAL32(UINT32 x)
{
    return SWAP32(x);
}

inline UINT64 VAL64(UINT64 x)
{
    return ((UINT64)VAL32(x) << 32) | VAL32(x >> 32);
}

inline void SwapString(WCHAR *szString)
{
    unsigned i;
    for (i = 0; szString[i] != L'\0'; i++)
    {
        szString[i] = VAL16(szString[i]);
    }
}

inline void SwapStringLength(WCHAR *szString, ULONG StringLength)
{
    unsigned i;
    for (i = 0; i < StringLength; i++)
    {
        szString[i] = VAL16(szString[i]);
    }
}

inline void SwapGuid(GUID *pGuid)
{
    pGuid->Data1 = VAL32(pGuid->Data1);
    pGuid->Data2 = VAL16(pGuid->Data2);
    pGuid->Data3 = VAL16(pGuid->Data3);
}
};
#else // __cplusplus
/* C Version of VAL functionality.  Swap functions omitted for lack of use in C code */
#define VAL16(x)    (((x) >> 8) | ((x) << 8))
#define VAL32(y)    (((y) >> 24) | (((y) >> 8) & 0x0000FF00L) | (((y) & 0x0000FF00L) << 8) | ((y) << 24))
#define VAL64(z)    (((UINT64)VAL32(z) << 32) | VAL32((z) >> 32))
#endif // __cplusplus

#else // !BIGENDIAN

#define VAL16(x) x
#define VAL32(x) x
#define VAL64(x) x
#define SwapString(x)
#define SwapStringLength(x, y)
#define SwapGuid(x)

#endif  // !BIGENDIAN

#ifdef HOST_64BIT
#define VALPTR(x) VAL64(x)
#else
#define VALPTR(x) VAL32(x)
#endif

#ifdef HOST_ARM
#define LOG2_PTRSIZE	2
#define ALIGN_ACCESS    ((1<<LOG2_PTRSIZE)-1)
#endif

#if defined(ALIGN_ACCESS) && !defined(_MSC_VER)
#ifdef __cplusplus
extern "C++" {
// Get Unaligned values from a potentially unaligned object
inline UINT16 GET_UNALIGNED_16(const void *pObject)
{
    UINT16 temp;
    memcpy(&temp, pObject, sizeof(temp));
    return temp;
}
inline UINT32 GET_UNALIGNED_32(const void *pObject)
{
    UINT32 temp;
    memcpy(&temp, pObject, sizeof(temp));
    return temp;
}
inline UINT64 GET_UNALIGNED_64(const void *pObject)
{
    UINT64 temp;
    memcpy(&temp, pObject, sizeof(temp));
    return temp;
}

// Set Value on an potentially unaligned object
inline void SET_UNALIGNED_16(void *pObject, UINT16 Value)
{
    memcpy(pObject, &Value, sizeof(UINT16));
}
inline void SET_UNALIGNED_32(void *pObject, UINT32 Value)
{
    memcpy(pObject, &Value, sizeof(UINT32));
}
inline void SET_UNALIGNED_64(void *pObject, UINT64 Value)
{
    memcpy(pObject, &Value, sizeof(UINT64));
}
}
#endif // __cplusplus

#else // defined(ALIGN_ACCESS) && !defined(_MSC_VER)

// Get Unaligned values from a potentially unaligned object
#define GET_UNALIGNED_16(_pObject)  (*(UINT16 UNALIGNED *)(_pObject))
#define GET_UNALIGNED_32(_pObject)  (*(UINT32 UNALIGNED *)(_pObject))
#define GET_UNALIGNED_64(_pObject)  (*(UINT64 UNALIGNED *)(_pObject))

// Set Value on an potentially unaligned object
#define SET_UNALIGNED_16(_pObject, _Value)  (*(UNALIGNED UINT16 *)(_pObject)) = (UINT16)(_Value)
#define SET_UNALIGNED_32(_pObject, _Value)  (*(UNALIGNED UINT32 *)(_pObject)) = (UINT32)(_Value)
#define SET_UNALIGNED_64(_pObject, _Value)  (*(UNALIGNED UINT64 *)(_pObject)) = (UINT64)(_Value)

#endif // defined(ALIGN_ACCESS) && !defined(_MSC_VER)

// Get Unaligned values from a potentially unaligned object and swap the value
#define GET_UNALIGNED_VAL16(_pObject) VAL16(GET_UNALIGNED_16(_pObject))
#define GET_UNALIGNED_VAL32(_pObject) VAL32(GET_UNALIGNED_32(_pObject))
#define GET_UNALIGNED_VAL64(_pObject) VAL64(GET_UNALIGNED_64(_pObject))

// Set a swap Value on an potentially unaligned object
#define SET_UNALIGNED_VAL16(_pObject, _Value) SET_UNALIGNED_16(_pObject, VAL16((UINT16)_Value))
#define SET_UNALIGNED_VAL32(_pObject, _Value) SET_UNALIGNED_32(_pObject, VAL32((UINT32)_Value))
#define SET_UNALIGNED_VAL64(_pObject, _Value) SET_UNALIGNED_64(_pObject, VAL64((UINT64)_Value))

#endif // __PAL_ENDIAN_H__
