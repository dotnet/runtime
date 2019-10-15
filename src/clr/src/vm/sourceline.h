// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef __SOURCELINE_H__
#define __SOURCELINE_H__

#ifdef ENABLE_DIAGNOSTIC_SYMBOL_READING
#include "dia2.h"
#endif // ENABLE_DIAGNOSTIC_SYMBOL_READING

#define CComPtr(x) x*

class SourceLine
{
	bool initialized_;

#ifdef ENABLE_DIAGNOSTIC_SYMBOL_READING
	CComPtr(IDiaDataSource) pSource_;
	CComPtr(IDiaSymbol)     pGlobal_;
	CComPtr(IDiaSession)    pSession_;

	bool	LoadDataFromPdb( __in_z LPWSTR wszFilename );
#endif // ENABLE_DIAGNOSTIC_SYMBOL_READING

public:
	SourceLine( __in_z LPWSTR pszFileName );

	bool IsInitialized() { return initialized_; }

	//
	// Given function token (methoddef) and offset, return filename and line number
	//
	HRESULT GetSourceLine( DWORD dwFunctionToken, DWORD dwOffset, __out_ecount(dwFileNameMaxLen) __out_z LPWSTR wszFileName, DWORD dwFileNameMaxLen, PDWORD pdwLineNumber );
	
	//
	// Given function token (methoddef) and slot, return name of the local variable
	//
	HRESULT GetLocalName( DWORD dwFunctionToken, DWORD dwSlot, __out_ecount(dwNameMaxLen) __out_z LPWSTR wszName, DWORD dwNameMaxLen );
};

#endif // __SOURCELINE_H__
