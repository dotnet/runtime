// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+----------------------------------------------------------------------------  
//  

//  
//  Purpose: Enable parsing of parameterized and non-parameterized typenames  
//
//  Adapted from Windows sources.  Modified to run on Windows version < Win8, so
//  that we can use this in CrossGen.
//  

//
//-----------------------------------------------------------------------------    

#include "common.h" // precompiled header

static const UINT32 g_uiMaxTypeName = 512;  

// Type name grammar:  
//  
// expression -> param  
//  
// pinterface_instance -> pinterface "<" params ">"  
// {  
//     if (count(pinterface.params) != num) { error }  
// }  
//  
// pinterface -> identifier "`" num  
//  
// params -> params "," param | param  
//  
// param -> identifier | pinterface_instance  
//  
// identifier -> all characters are allowed, except for white space, back tick, comma and left/right angle brackets.  
//  
// num -> [0-9]+    

typedef enum  
{  
    TTT_PINTERFACE,  
    TTT_IDENTIFIER,  
    TTT_INVALID  
} TYPENAME_TOKEN_TYPE;  
  
class TypeNameTokenizer  
{  
public:  
    _When_(return == S_OK, _At_(pphstrTypeNameParts, __deref_out_ecount(*pdwPartsCount)))  
    _When_(return != S_OK, _At_(pphstrTypeNameParts, __deref_out))  
    HRESULT TokenizeType(__in PCWSTR pszTypeName, __out DWORD *pdwPartsCount, SString **pphstrTypeNameParts);  
  
    ~TypeNameTokenizer()  
    {  
		if (_sphstrTypeNameParts != nullptr) 
            delete[] _sphstrTypeNameParts;
    }  
  
private:  
    HRESULT ParseNonParameterizedType();  
    HRESULT ParseParameterizedType();  
  
    int CountTokens();  
    TYPENAME_TOKEN_TYPE ReadNextToken();  
    void SkipWhitespace();  
    bool IsWhitespace(WCHAR ch);  
    bool TrimThenFetchAndCompareNextCharIfAny(__in WCHAR chExpectedSymbol);  
    bool TrimThenPeekAndCompareNextCharIfAny(__in WCHAR chExpectedSymbol);  
    HRESULT VerifyTrailingCloseBrackets(__in DWORD dwExpectedTrailingCloseBrackets);  
  
    SString* _sphstrTypeNameParts;  
    WCHAR _pszTypeName[g_uiMaxTypeName];  
    WCHAR *_pchTypeNamePtr;  
    WCHAR _pszCurrentToken[g_uiMaxTypeName];  
    DWORD _cCurrentTokenParameters;  
    DWORD _cTokens;  
};  
  
_When_(return == S_OK, _At_(typeNameParts, __deref_out_ecount(*partsCount)))  
_When_(return != S_OK, _At_(typeNameParts, __deref_out))  
__checkReturn  extern "C" HRESULT WINAPI CrossgenRoParseTypeName(  
    __in SString* typeName,  
    __out DWORD *partsCount,  
     SString **typeNameParts)  
{  
    HRESULT hr = S_OK;  
  
    // Clear output parameters.      
	*typeNameParts = nullptr;  
    *partsCount = 0;  
  
    if (typeName->IsEmpty() /*|| typeName.HasEmbeddedNull() */)  
    {  
        hr = E_INVALIDARG;  
    }  
  
    if (SUCCEEDED(hr))  
    {  
        TypeNameTokenizer typeNameTokenizer;  
        hr = typeNameTokenizer.TokenizeType(  
            typeName->GetUnicode(),  
            partsCount,  
            typeNameParts);  
    }  
  
    return hr;  
}  
  
_When_(return == S_OK, _At_(pphstrTypeNameParts, __deref_out_ecount(*pdwPartsCount)))  
_When_(return != S_OK, _At_(pphstrTypeNameParts, __deref_out))  
HRESULT TypeNameTokenizer::TokenizeType(__in PCWSTR pszTypeName, __out DWORD *pdwPartsCount, SString **pphstrTypeNameParts)  
{  
    _ASSERTE(pphstrTypeNameParts != nullptr);  
    _ASSERTE(pdwPartsCount != nullptr);  
    HRESULT hr = S_OK;  
  
    _cTokens = 0;  
    hr = StringCchCopy(_pszTypeName, ARRAYSIZE(_pszTypeName), pszTypeName);  
    _pchTypeNamePtr = _pszTypeName;  
  
    if (hr == STRSAFE_E_INSUFFICIENT_BUFFER)  
    {  
        hr = RO_E_METADATA_NAME_NOT_FOUND;  
    }  
  
    if (SUCCEEDED(hr))  
    {  
        *pdwPartsCount = CountTokens();  
  
        _sphstrTypeNameParts = new(nothrow) SString[*pdwPartsCount];
        if (_sphstrTypeNameParts == nullptr)  
        {  
            hr = E_OUTOFMEMORY;  
        }  
    }  
  
    if (SUCCEEDED(hr))  
    {  
        TYPENAME_TOKEN_TYPE tokenType = ReadNextToken();  
  
        if (tokenType == TTT_IDENTIFIER)  
        {  
            hr = ParseNonParameterizedType();  
        }  
        else if (tokenType == TTT_PINTERFACE)  
        {  
            hr = ParseParameterizedType();  
        }  
        else  
        {  
            hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
        }  
    }  
  
    if (SUCCEEDED(hr))  
    {  
        *pphstrTypeNameParts = _sphstrTypeNameParts;  
        _sphstrTypeNameParts = nullptr;
    }  
    else  
    {  
        *pdwPartsCount = 0;  
        *pphstrTypeNameParts = nullptr;  
    }  
  
    return hr;  
}  
  
int TypeNameTokenizer::CountTokens()  
{  
    const size_t cTypeNameLength = wcslen(_pszTypeName);  
    int nCount = 1;  
    WCHAR ch;  
  
    _ASSERTE(cTypeNameLength != 0);  
  
    for (UINT32 nIndex = 0; nIndex < cTypeNameLength; nIndex++)  
    {  
        ch = _pszTypeName[nIndex];  
  
        if ((ch == W(',')) || (ch == W('<')))  
        {  
            nCount++;  
        }  
    }  
  
    return nCount;  
}  
  
TYPENAME_TOKEN_TYPE TypeNameTokenizer::ReadNextToken()  
{  
    TYPENAME_TOKEN_TYPE tokenType = TTT_IDENTIFIER;  
    int nTokenIndex = 0;  
    WCHAR ch = *_pchTypeNamePtr;  
  
    while ((ch != W('\0')) &&  
            (ch != W('<')) &&  
            (ch != W('>')) &&  
            (ch != W(',')) &&  
            (!IsWhitespace(ch)))  
    {  
        _pszCurrentToken[nTokenIndex++] = ch;  
  
        if (ch == W('`'))  
        {  
            if (nTokenIndex > 1)  
            {  
                tokenType = TTT_PINTERFACE;  
  
                // Store the pinterface's parameters count (limited to a single digit).                  
				_pchTypeNamePtr++;  
                ch = *_pchTypeNamePtr;  
  
                if (isdigit(ch))  
                {  
                    _pszCurrentToken[nTokenIndex++] = ch;  
                    _cCurrentTokenParameters = ch - W('0');  
                    _pchTypeNamePtr++;  
                }  
                else  
                {  
                    tokenType = TTT_INVALID;  
                }  
            }  
            else  
            {  
                // The back tick (`) was the first character in the token.                  
				tokenType = TTT_INVALID;  
            }  
  
            break;  
        }  
  
        _pchTypeNamePtr++;  
        ch = *_pchTypeNamePtr;  
    }  
  
    // Empty token is invalid.      
	if (nTokenIndex == 0)  
    {  
        tokenType = TTT_INVALID;  
    }  
  
  
    if ((tokenType == TTT_PINTERFACE) && (_cCurrentTokenParameters == 0))  
    {  
        tokenType = TTT_INVALID;  
    }  
  
    _pszCurrentToken[nTokenIndex] = W('\0');  
  
    return tokenType;  
}  
  
bool TypeNameTokenizer::TrimThenPeekAndCompareNextCharIfAny(__in WCHAR chExpectedSymbol)  
{  
    // Trim leading spaces.      
    SkipWhitespace();  
  
    return (*_pchTypeNamePtr == chExpectedSymbol);  
}  
  
bool TypeNameTokenizer::TrimThenFetchAndCompareNextCharIfAny(__in WCHAR chExpectedSymbol)  
{  
    bool fSymbolsMatch;  
  
    // Trim leading spaces.      
    SkipWhitespace();  
  
    WCHAR ch = *_pchTypeNamePtr;  
  
    // Do not move the typename pointer past the end of the typename string.      
    if (ch != W('\0'))  
    {  
        _pchTypeNamePtr++;  
    }  
  
    fSymbolsMatch = (ch == chExpectedSymbol);  
  
    // Trim trailing spaces.      
    SkipWhitespace();  
  
    return fSymbolsMatch;  
}  
  
HRESULT TypeNameTokenizer::ParseNonParameterizedType()  
{  
    HRESULT hr = S_OK;  
  
    // There should be no trailing symbols or spaces after a non-parameterized type.      
    if (!TrimThenFetchAndCompareNextCharIfAny(W('\0')))  
    {  
        hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
    }  
  
    if (SUCCEEDED(hr))  
    {  
        _sphstrTypeNameParts[_cTokens++].Set(_pszCurrentToken);
        //hr = WindowsCreateString(_pszCurrentToken, static_cast<UINT32>(wcslen(_pszCurrentToken)), &_sphstrTypeNameParts[_cTokens++]);  
  
        //if (FAILED(hr))  
        //{  
        //    _cTokens--;  
        //}  
    }  
  
    //_ASSERTE(SUCCEEDED(hr) ? _cTokens == 1 : _cTokens == 0);  
  
    return hr;  
}  
  
HRESULT TypeNameTokenizer::ParseParameterizedType()  
{  
    HRESULT hr = S_OK;  
  
    // For every pinterface in the typename (base and nested), there will be a corresponding entry in the      
    // anRemainingParameters array to hold the number of parameters that need to be matched for that pinterface.      
    // The count of parameters for a given pinterface is decremented after parsing each paramter and when the      
    // count reaches zero, the corresponding pinterface is considered completely parsed.      
    int nInnermostPinterfaceIndex = -1;  
    SArray<int> anRemainingParameters;  
    DWORD dwExpectedTrailingCloseBrackets = 0;  
    TYPENAME_TOKEN_TYPE tokenType = TTT_PINTERFACE;  
  
    do  
    {  
        switch (tokenType)  
        {  
            case TTT_PINTERFACE:  
                {  
                    if (++nInnermostPinterfaceIndex > 0)  
                    {  
                        // This was a nested pinterface (i.e. a parameter of another pinterface), so we                          
                        // need to decrement the parameters count of its parent pinterface.                          
                        anRemainingParameters[nInnermostPinterfaceIndex - 1]--;  
                        if (anRemainingParameters[nInnermostPinterfaceIndex - 1] == 0)  
                        {  
                            nInnermostPinterfaceIndex--;  
                        }  
                    }  
  
                    // Store pinterface's parameters count.                      
                    if (nInnermostPinterfaceIndex < (int)anRemainingParameters.GetCount())  
                    {  
                        anRemainingParameters[nInnermostPinterfaceIndex] = _cCurrentTokenParameters;  
                    }  
                    else  
                    {  
                        anRemainingParameters.Append(_cCurrentTokenParameters);  
                    }  
  
                    if (!TrimThenFetchAndCompareNextCharIfAny(W('<')))  
                    {  
                        hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                    }  
  
                    dwExpectedTrailingCloseBrackets++;  
                }  
                break;  
  
            case TTT_IDENTIFIER:  
                {  
                    _ASSERTE(nInnermostPinterfaceIndex != -1);  
                    _ASSERTE(anRemainingParameters[nInnermostPinterfaceIndex] != 0);  
  
                    anRemainingParameters[nInnermostPinterfaceIndex]--;  
  
                    if (anRemainingParameters[nInnermostPinterfaceIndex] == 0)  
                    {  
                        // This was the last parameter for the given pinterface.                          
                        nInnermostPinterfaceIndex--;  
                        hr = VerifyTrailingCloseBrackets(1);  
  
                        if (SUCCEEDED(hr))  
                        {  
                            dwExpectedTrailingCloseBrackets--;  
  
                            if (nInnermostPinterfaceIndex == -1)  
                            {  
                                // No more unparsed pinterfaces                                  
                                hr = VerifyTrailingCloseBrackets(dwExpectedTrailingCloseBrackets);  
  
                                if (SUCCEEDED(hr))  
                                {  
                                    dwExpectedTrailingCloseBrackets = 0;  
  
                                    if (!TrimThenFetchAndCompareNextCharIfAny(W('\0')))  
                                    {  
                                        hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                                    }  
                                }  
                            }  
                            else  
                            {  
                                while (TrimThenPeekAndCompareNextCharIfAny(W('>')))  
                                {  
                                    if (dwExpectedTrailingCloseBrackets > 0)  
                                    {  
                                        TrimThenFetchAndCompareNextCharIfAny(W('>'));  
                                        dwExpectedTrailingCloseBrackets--;  
                                    }  
                                    else  
                                    {  
                                        hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                                        break;  
                                    }  
                                }  
  
                                // There are more parameters, so we expect a comma-separated list.                                  
                                if (!TrimThenFetchAndCompareNextCharIfAny(W(',')))  
                                {  
                                    hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                                }  
                            }  
                        }  
                    }  
                    else  
                    {  
                        // There are more parameters, so we expect a comma-separated list.                          
                        if (!TrimThenFetchAndCompareNextCharIfAny(W(',')))  
                        {  
                            hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                        }  
                    }  
                }  
                break;  
  
            default:  
                {  
                    hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
                }  
        }  
  
        // Store current token.          
        if (SUCCEEDED(hr))  
        {  
            _sphstrTypeNameParts[_cTokens++].Set(_pszCurrentToken);
            //hr = WindowsCreateString(_pszCurrentToken, static_cast<UINT32>(wcslen(_pszCurrentToken)), &_sphstrTypeNameParts[_cTokens++]);  
  
            //if (FAILED(hr))  
            //{  
            //    _cTokens--;  
            //}  
        }  
  
        tokenType = ReadNextToken();  
  
    } while (SUCCEEDED(hr) && (nInnermostPinterfaceIndex != -1));  
          
    return hr;  
}  
  
HRESULT TypeNameTokenizer::VerifyTrailingCloseBrackets(__in DWORD dwExpectedTrailingCloseBrackets)  
{  
    HRESULT hr = S_OK;  
  
    for (DWORD dwClosingBracket = 0; dwClosingBracket < dwExpectedTrailingCloseBrackets; dwClosingBracket++)  
    {  
        if (!TrimThenFetchAndCompareNextCharIfAny(W('>')))  
        {  
            hr = RO_E_METADATA_INVALID_TYPE_FORMAT;  
        }  
    }  
  
    return hr;  
}  
  
void TypeNameTokenizer::SkipWhitespace()  
{  
    while (IsWhitespace(*_pchTypeNamePtr))  
    {  
        _pchTypeNamePtr++;  
    }  
}  
  
bool TypeNameTokenizer::IsWhitespace(WCHAR ch)  
{  
    bool fIsWhitespace = false;  
  
    switch (ch)  
    {  
        case ' ':  
        case '\t':  
        case '\r':  
        case '\f':  
        case '\n':  
            fIsWhitespace = true;  
            break;  
    }  
  
    return fIsWhitespace;  
}  
