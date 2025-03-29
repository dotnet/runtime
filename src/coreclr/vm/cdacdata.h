// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACDATA_H__
#define CDACDATA_H__

// See datadescriptor.h
//
// This struct enables exposing information that is private to a class to the cDAC. For example,
// if class C has private information that must be provided, declare cdac_data<D> as a friend of C
// where D is the specialization of cdac_data that will expose the information. Then provide a
// specialization cdac_data<D> with constexpr members exposing the information.
//
// Note: in the common case, type D will be type C.
//
// For example, if the offset of field F in class C is required:
//
//      class C {
//      private:
//          int F;
//          friend struct ::cdac_data<C>;
//      };
//      template<> struct cdac_data<C> {
//          static constexpr size_t F_Offset = offsetof(C, F);
//      };
//
template<typename T>
struct cdac_data
{
};

#endif// CDACDATA_H__
