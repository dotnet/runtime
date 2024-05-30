// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACOFFSETS_H__
#define CDACOFFSETS_H__

// See data-descriptor.h
//
// If the offset of some field F in class C must be provided to cDAC, but the field is private, the
// class C should declare cdac_offsets<T> as a friend:
//
//     template<typename T> friend struct ::cdac_offsets;
//
// and provide a specialization cdac_offsets<C> with a constexpr size_t member providing the offset:
//
//     template<> struct cdac_offsets<C> {
//         static constexpr size_t F_Offset = offsetof(C, F);
//     };
template<typename T>
struct cdac_offsets
{
};

#endif// CDACOFFSETS_H__
