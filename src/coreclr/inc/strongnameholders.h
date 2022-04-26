// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __STRONGNAME_HOLDERS_H__
#define __STRONGNAME_HOLDERS_H__

#include <holder.h>
#include <strongnameinternal.h>

//
// Holder classes for types returned from and used in strong name APIs
//

// Holder for any memory allocated by the strong name APIs
template<class T>
void VoidStrongNameFreeBuffer(_In_ T *pBuffer)
{
    StrongNameFreeBuffer(reinterpret_cast<BYTE *>(pBuffer));
}
template<typename _TYPE>
using StrongNameBufferHolder = SpecializedWrapper<_TYPE, VoidStrongNameFreeBuffer<_TYPE>>;

#endif // !__STRONGNAME_HOLDERS_H__
