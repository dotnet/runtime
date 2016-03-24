// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include <stdio.h>
#include <xplatform.h>

extern "C" DLL_EXPORT BOOL __stdcall Marshal_IUnknown(/*[in]*/IUnknown *o)
{
	//Call AddRef and Release on the passed IUnknown
	//test if the ref counts get updated as expected
	unsigned long refCount = o->AddRef();
	if((refCount-1) != o->Release())
		return FALSE;
	return TRUE;
}
