//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ---------------------------------------------------------------------------
// COMEx.cpp
//

//
// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "string.h"
#include "ex.h"
#include "holder.h"
#include "corerror.h"

// ---------------------------------------------------------------------------
// COMException class.  Implements exception API for standard COM-based error info
// ---------------------------------------------------------------------------

COMException::~COMException()
{
    WRAPPER_NO_CONTRACT;

    if (m_pErrorInfo != NULL)
        m_pErrorInfo->Release();
}

IErrorInfo *COMException::GetErrorInfo()
{
    LIMITED_METHOD_CONTRACT;

    IErrorInfo *pErrorInfo = m_pErrorInfo;
    if (pErrorInfo != NULL)
        pErrorInfo->AddRef();
    return pErrorInfo;
}

void COMException::GetMessage(SString &string)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
    if (m_pErrorInfo != NULL)
    {
        BSTRHolder message(NULL);
        if (SUCCEEDED(m_pErrorInfo->GetDescription(&message)))
            string.Set(message, SysStringLen(message));
    }
}

