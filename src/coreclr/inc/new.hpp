// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef __new__hpp
#define __new__hpp

#include <new>

using std::nothrow;

#ifdef _DEBUG
void DisableThrowCheck();
#endif

#endif
