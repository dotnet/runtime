// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DIAGNOSTICS_PROTOCOL_H__
#define __DIAGNOSTICS_PROTOCOL_H__

#ifdef FEATURE_PERFTRACING

template <typename T>
bool TryParse(uint8_t *&bufferCursor, uint32_t &bufferLen, T &result)
{
    static_assert(
        std::is_integral<T>::value || std::is_same<T, float>::value ||
        std::is_same<T, double>::value || std::is_same<T, CLSID>::value,
        "Can only be instantiated with integral and floating point types.");

    if (bufferLen < sizeof(T))
        return false;
    result = *(reinterpret_cast<T *>(bufferCursor));
    bufferCursor += sizeof(T);
    bufferLen -= sizeof(T);
    return true;
}

template <typename T>
bool TryParseString(uint8_t *&bufferCursor, uint32_t &bufferLen, const T *&result)
{
    static_assert(
        std::is_same<T, char>::value || std::is_same<T, wchar_t>::value,
        "Can only be instantiated with char and wchar_t types.");

    uint32_t stringLen = 0;
    if (!TryParse(bufferCursor, bufferLen, stringLen))
        return false;
    if (stringLen == 0)
    {
        result = nullptr;
        return true;
    }
    if (stringLen > (bufferLen / sizeof(T)))
        return false;
    if ((reinterpret_cast<const T *>(bufferCursor))[stringLen - 1] != 0)
        return false;
    result = reinterpret_cast<const T *>(bufferCursor);

    const uint32_t TotalStringLength = stringLen * sizeof(T);
    bufferCursor += TotalStringLength;
    bufferLen -= TotalStringLength;
    return true;
}

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTICS_PROTOCOL_H__
