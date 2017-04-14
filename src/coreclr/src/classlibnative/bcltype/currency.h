// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Currency.h
//

//

#ifndef _CURRENCY_H_
#define _CURRENCY_H_

#include <oleauto.h>
#include <pshpack1.h>

class COMCurrency 
{
public:
    static FCDECL2_IV(void, DoToDecimal,  DECIMAL * result, CY c);
};

#include <poppack.h>

#endif // _CURRENCY_H_
