// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: shstr.h
//
// ShStr class ported from shlwapi for urlpars.cpp (especially for Fusion)
// ===========================================================================

#ifndef _SHSTR_H_

//  default shstr to something small, so we don't waste too much stack space
//  MAX_PATH is used frequently, so we'd like to a factor of that - so that
//  if we do grow to MAX_PATH size, we don't waste any extra.
#define DEFAULT_SHSTR_LENGTH    (MAX_PATH_FNAME/4)


#ifdef UNICODE
#define ShStr ShStrW
#define UrlStr UrlStrW
#endif //UNICODE


class ShStrW
{
public:

    //
    //  Constructors
    //
    ShStrW();

    //
    //  Destructor
    //
    ~ShStrW()
        {Reset();}

    //
    // the first are the only ones that count
    //
    HRESULT SetStr(LPCSTR pszStr, DWORD cchStr);
    HRESULT SetStr(LPCSTR pszStr);
    HRESULT SetStr(LPCWSTR pwszStr, DWORD cchStr);

    // the rest just call into the first three
    HRESULT SetStr(LPCWSTR pwszStr)
        {return SetStr(pwszStr, (DWORD) -1);}
    HRESULT SetStr(ShStrW &shstr)
        {return SetStr(shstr._pszStr);}


    ShStrW& operator=(LPCSTR pszStr)
        {SetStr(pszStr); return *this;}
    ShStrW& operator=(LPCWSTR pwszStr)
        {SetStr(pwszStr); return *this;}
    ShStrW& operator=(ShStrW &shstr)
        {SetStr(shstr._pszStr); return *this;}


    LPCWSTR GetStr()
        {return _pszStr;}
    operator LPCWSTR()
        {return _pszStr;}

    LPWSTR GetInplaceStr(void)
        {return _pszStr;}

    // People want to play with the bytes in OUR internal buffer.  If they
    // call us correctly, and assume that the resulting pointer is only valid
    // as far as they want or as far as the current length, then let them.
    LPWSTR GetModifyableStr(DWORD cchSizeToModify)
        {
         if (cchSizeToModify > _cchSize)
            if (FAILED(SetSize(cchSizeToModify)))
                return NULL;
          return _pszStr;
        }

    HRESULT Append(LPCWSTR pszStr, DWORD cchStr);
    HRESULT Append(LPCWSTR pszStr)
        {return Append(pszStr, (DWORD) -1);}
    HRESULT Append(WCHAR ch)
        {return Append(&ch, 1);}

    
    VOID Reset();

#ifdef DEBUG
    BOOL IsValid();
#else
    BOOL IsValid() 
    {return (BOOL) (_pszStr ? TRUE : FALSE);}
#endif //DEBUG

    DWORD GetSize()
        {ASSERT(!(_cchSize % DEFAULT_SHSTR_LENGTH)); return (_pszStr ? _cchSize : 0);}

    HRESULT SetSize(DWORD cchSize);
    DWORD GetLen()
        {return lstrlenW(_pszStr);}



protected:
//    friend UrlStr;
/*
    TCHAR GetAt(DWORD cch)
        {return cch < _cchSize ? _pszStr[cch] : TEXT('\0');}
    TCHAR SetAt(TCHAR ch, DWORD cch)
        {return cch < _cchSize ? _pszStr[cch] = ch : TEXT('\0');}
*/
private:

    HRESULT _SetStr(LPCSTR psz);
    HRESULT _SetStr(LPCSTR psz, DWORD cb);
    HRESULT _SetStr(LPCWSTR pwszStr, DWORD cchStr);

    WCHAR _szDefaultBuffer[DEFAULT_SHSTR_LENGTH];
    LPWSTR _pszStr;
    DWORD _cchSize;


}; //ShStrW

#ifdef UNICODE
typedef ShStrW  SHSTR;
typedef ShStrW  *PSHSTR;
#endif //UNICODE

typedef ShStrW  SHSTRW;
typedef ShStrW  *PSHSTRW;


#endif // _SHSTR_H_
