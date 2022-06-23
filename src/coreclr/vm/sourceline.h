// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



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

	bool	LoadDataFromPdb( _In_z_ LPWSTR wszFilename );
#endif // ENABLE_DIAGNOSTIC_SYMBOL_READING

public:
	SourceLine( _In_z_ LPWSTR pszFileName );

	bool IsInitialized() { return initialized_; }

	//
	// Given function token (methoddef) and offset, return filename and line number
	//
	HRESULT GetSourceLine( DWORD dwFunctionToken, DWORD dwOffset, _Out_writes_z_(dwFileNameMaxLen) LPWSTR wszFileName, DWORD dwFileNameMaxLen, PDWORD pdwLineNumber );

	//
	// Given function token (methoddef) and slot, return name of the local variable
	//
	HRESULT GetLocalName( DWORD dwFunctionToken, DWORD dwSlot, _Out_writes_z_(dwNameMaxLen) LPWSTR wszName, DWORD dwNameMaxLen );
};

#endif // __SOURCELINE_H__
