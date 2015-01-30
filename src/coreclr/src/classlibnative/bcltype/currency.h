//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
    static FCDECL2   (void, DoToDecimal,  DECIMAL * result, CY c);
};

#include <poppack.h>

#endif // _CURRENCY_H_
