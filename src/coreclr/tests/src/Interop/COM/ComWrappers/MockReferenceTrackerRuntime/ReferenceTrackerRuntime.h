// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#ifndef _EXTERNAL_H_
#define _EXTERNAL_H_

#include <xplatform.h>

struct DECLSPEC_UUID("42951130-245C-485E-B60B-4ED4254256F8") IExternalObject : public IUnknown
{
    STDMETHOD(AddObjectRef)(_In_ IUnknown* c) = 0;
    STDMETHOD(DropObjectRef)(_In_ IUnknown * c) = 0;
    STDMETHOD(UseObjectRefs)() = 0;
};

// Create external object
EXPORT IExternalObject* STDMETHODCALLTYPE CreateExternalObject();

#endif // _EXTERNAL_H_
