// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACDATA_H__
#define CDACDATA_H__

// See datadescriptor.h
//
// This struct enables exposing information that is private to a class to the cDAC. For example,
// if class C has private information that must be provided, declare cdac_data<T> as a friend:
//
//     template<typename T> friend struct ::cdac_data;
//
// and provide a specialization cdac_data<C> with constexpr members exposing the information.
// For example, if the offset of field F is required:
//
//     template<> struct cdac_data<C> {
//         static constexpr size_t F_Offset = offsetof(C, F);
//     };
template<typename T>
struct cdac_data
{
};

#endif// CDACDATA_H__
