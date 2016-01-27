// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//

#pragma once

#ifndef WindowsString_h
#define WindowsString_h

#include <tchar.h>       // Required by strsafe.h
#include <intsafe.h>     // For SizeTToUInt32
#include <strsafe.h>     // For StringCchLengthW.
#include <winstring.h> // The Windows SDK header file for HSTRING and HSTRING_HEADER.

//---------------------------------------------------------------------------------------------------------------------------
// Forward declarations
void DECLSPEC_NORETURN ThrowHR(HRESULT hr);

//---------------------------------------------------------------------------------------------------------------------------
namespace clr
{
    namespace winrt
    {
        //-------------------------------------------------------------------------------------------------------------------
        // The internal Windows Runtime String wrapper class which doesn't throw exception when a failure occurs
        // Note String class doesn't provide copy constructor and copy assigment. This is because the *fast* string duplicate
        // can fail, which makes the copy constructor unusable in contexts where exceptions are not expected because it would
        // need to throw on failure. However, a move constructor and move assignment are provided. These require a String &&
        // argument, which prevents a *fast* string from being moved (StringReference can be cast to const String&, but not
        // String&&).
        class String
        {
        public:
            String() throw() : _hstring(nullptr)
            {
                STATIC_CONTRACT_LIMITED_METHOD;
            }

            // Move Constructor
            String(__inout String&& other) throw()
                : _hstring(other._hstring)
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                other._hstring = nullptr;
            }

            // Move assignment
            String & operator = (__inout String&& other) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                Release();
                _hstring = other._hstring;
                other._hstring = nullptr;
                return *this;
            }

            // Initialize this string from a source string. A copy is made in this call.
            // The str parameter doesn't need to be null terminated, and it may have embedded NUL characters.
            HRESULT Initialize(_In_reads_opt_(length) const wchar_t *str, UINT32 length) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsCreateString(str, length, &local);
                return FreeAndAssignOnSuccess(hr, local, &_hstring);
            }

            // Initialize this string from a source string. A copy is made in this call.  The input string must have a terminating NULL.
            HRESULT Initialize(__in PCWSTR str) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HRESULT hr = S_OK;
                
                if (nullptr == str)
                {   // HSTRING functions promote null string pointers to the empty string, so we should too.
                    str = L"";
                }
                
                size_t length = 0;
                if (SUCCEEDED(hr))
                {
                    hr = StringCchLengthW(str, STRSAFE_MAX_CCH, &length);
                }
                
                HSTRING local = nullptr;
                if (SUCCEEDED(hr))
                {
                    hr = WindowsCreateString(str, static_cast<UINT32>(length), &local);
                }
                
                return FreeAndAssignOnSuccess(hr, local, &_hstring);
            }

            // Initialize this string from an HSTRING. A copy is made in this call.
            HRESULT Initialize(const HSTRING& other) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr =  WindowsDuplicateString(other, &local);
                return FreeAndAssignOnSuccess(hr, local, &_hstring);
            }

            ~String() throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                if (_hstring)
                {
                    WindowsDeleteString(_hstring);
                }
            }

            // Release the current HSTRING object and reset the member variable to empty
            void Release() throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                if (_hstring)
                {
                    WindowsDeleteString(_hstring);
                    _hstring = nullptr;
                }
            }

            // Detach the current HSTRING
            void Detach(__out HSTRING *phstring) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                *phstring = _hstring;
                _hstring = nullptr;
            }

            // Duplicate from another String.
            HRESULT Duplicate(__in const String& other) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsDuplicateString(other, &local);
                return FreeAndAssignOnSuccess(hr, local, &_hstring);
            }

            // Copy/duplicate into a bare HSTRING
            HRESULT CopyTo(__out HSTRING *phstring) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return WindowsDuplicateString(this->_hstring, phstring);
            }

            // HSTRING operator
            operator const HSTRING&() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _hstring;
            }

            // Explicit conversion to HSTRING
            HSTRING Get() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _hstring;
            }
            
            // Retrieve the address of the held hstring
            HSTRING* Address()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return &_hstring;
            }
            
            // Return the address of the internal HSTRING so that the caller can overwrite it,
            // trusting that the caller will not leak the previously held value
            HSTRING* GetAddressOf()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return &_hstring;
            }

            // Return the address of the internal HSTRING so that the caller can overwrite it,
            // but release the previous HSTRING to prevent a leak
            HSTRING* ReleaseAndGetAddressOf()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                if (_hstring != nullptr)
                {
                    WindowsDeleteString(_hstring);
                    _hstring = nullptr;
                }
                return &_hstring;
            }

            // Allow the wrapper to assign a new HSTRING to this wrapper, releasing the old HSTRING
            void Attach(__in_opt HSTRING string)
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                WindowsDeleteString(_hstring);
                _hstring = string;
            }

            // Data Access
            UINT32 length() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return WindowsGetStringLen(_hstring);
            }

            // The size() function is an alias for length(), included to parallel stl conventions.
            // The length() function is preferred.
            UINT32 size() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return length();
            }

            BOOL IsEmpty() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return WindowsIsStringEmpty(_hstring);
            }

            BOOL HasEmbeddedNull() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                BOOL answer;
                // Not capturing HRESULT
                WindowsStringHasEmbeddedNull(_hstring, &answer);
                return answer;
            }

            LPCWSTR GetRawBuffer(__out_opt UINT32 *length = nullptr) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return WindowsGetStringRawBuffer(_hstring, length);
            }

            HRESULT GetLpcwstr(__deref_out LPCWSTR *ppsz) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                if (HasEmbeddedNull())
                {
                    *ppsz = nullptr;
                    return E_INVALIDARG;
                }
                *ppsz = WindowsGetStringRawBuffer(_hstring, nullptr);
                return S_OK;
            }

            // CompareOrdinal
            INT32 CompareOrdinal(const String& other) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                INT32 result = 0;

                // Ignore the HRESULT from the following call.
                WindowsCompareStringOrdinal(_hstring, other, &result);

                return result;
            }

            // Concatenation
            HRESULT Concat(const String& string, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsConcatString(_hstring, string, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

            // Trim
            HRESULT TrimStart(const String& trimString, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsTrimStringStart(_hstring, trimString, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

            HRESULT TrimEnd(const String& trimString, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsTrimStringEnd(_hstring, trimString, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

            // Substring
            HRESULT Substring(UINT32 startIndex, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsSubstring(_hstring, startIndex, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

            HRESULT Substring(UINT32 startIndex, UINT32 length, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsSubstringWithSpecifiedLength(_hstring, startIndex, length, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

            // Replace
            HRESULT Replace(const String& stringReplaced, const String& stringReplaceWith, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HSTRING local;
                HRESULT hr = WindowsReplaceString(_hstring, stringReplaced, stringReplaceWith, &local);
                return FreeAndAssignOnSuccess(hr, local, &newString._hstring);
            }

        private:

            // No Copy Constructor
            String(const String& other);

            // No Copy assignment because if it can fail
            String & operator = (const String& other);

            //
            // helper function, always returns the passed in HRESULT
            //
            // if the HRESULT indicates success, frees any previous *target string,
            // and over-writes it with newValue
            //
            // if the HRESULT indicates failure, does nothing
            //
            static HRESULT FreeAndAssignOnSuccess(HRESULT hr, HSTRING newValue, __inout HSTRING *target)
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                if (SUCCEEDED(hr))
                {
                    // InterlockedExchangePointer wouldn't have much value, unless we also modified
                    //  all readers of *target to insert a ReadBarrier.
                    HSTRING oldValue = *target;
                    *target = newValue;
                    WindowsDeleteString(oldValue);
                }
                return hr;
            }

            HSTRING _hstring;
        };

        static_assert(sizeof(String[2]) == sizeof(HSTRING[2]), "clr::winrt::String must be same size as HSTRING!");

        //-------------------------------------------------------------------------------------------------------------------
        // String Comparison Operators
        inline
        bool operator == (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return 0 == result;
        }

        inline
        bool operator != (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return 0 != result;
        }

        inline
        bool operator < (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return -1 == result;
        }

        inline
        bool operator <= (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return -1 == result || 0 == result;
        }

        inline
        bool operator > (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return 1 == result;
        }

        inline
        bool operator >= (const String& left, const String& right) throw()
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            INT32 result = 0;
            // Ignore the HRESULT from the following call.
            WindowsCompareStringOrdinal(left, right, &result);

            return 1 == result || 0 == result;
        }


        //-------------------------------------------------------------------------------------------------------------------
        // The internal Windows Runtime String wrapper class for passing a reference of an existing string buffer.
        // This class is allocated on stack.
        class StringReference
        {
        public:

            // Constructor which takes an existing string buffer and its length as the parameters.
            // It fills an HSTRING_HEADER struct with the parameter.
            //
            // Warning: The caller must ensure the lifetime of the buffer outlives this
            // object as it does not make a copy of the wide string memory.
            StringReference(__in_opt PCWSTR stringRef, UINT32 length) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                HRESULT hr = WindowsCreateStringReference(stringRef, length, &_header, &_hstring);
                
                // Failfast if internal developers try to create a reference to a non-NUL terminated string
                if (FAILED(hr))
                {
                    RaiseException(static_cast<DWORD>(STATUS_INVALID_PARAMETER), EXCEPTION_NONCONTINUABLE, 0, nullptr);
                }
            }

            // Constructor for use with string literals.
            // It fills an HSTRING_HEADER struct with the parameter.
            template <UINT32 N>
            StringReference(__in WCHAR const (&stringRef)[N]) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;

                HRESULT hr = WindowsCreateStringReference(stringRef, N - 1 /* remove terminating NUL from length */, &_header, &_hstring);

                // Failfast if internal developers try to create a reference to a non-NUL terminated string. This constructor
                // should only be used with string literals, but someone could mistakenly use this with a local WCHAR array and
                // forget to NUL-terminate it.
                if (FAILED(hr))
                {
                    RaiseException(static_cast<DWORD>(STATUS_INVALID_PARAMETER), EXCEPTION_NONCONTINUABLE, 0, nullptr);
                }
            }

            // Contructor which takes an HSTRING as the parameter. The new StringReference will not create a new copy of the original HSTRING.
            //
            // Warning: The caller must ensure the lifetime of the hstring argument outlives this
            // object as it does not make a copy.
            explicit StringReference(const HSTRING& hstring) throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                // Create the StringReference without using the _header member, but instead with whatever header is used in hstring so that we
                // prevent copying when Duplicate() is called on this object.  There is no addref, nor decrement in the destructor, since we
                // don't know or care if it's refcounted or truly a stack allocated reference.
                _hstring = hstring;
            }

            // const String& operator
            operator const String&() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString();
            }

            // const HSTRING& operator
            operator const HSTRING&() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _hstring;
            }

            // Explicit conversion to HSTRING
            HSTRING Get() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _hstring;
            }
            
            // CompareOrdinal
            INT32 CompareOrdinal(const String& other) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().CompareOrdinal(other);
            }

            // Data Access
            UINT32 length() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().length();
            }

            UINT32 size() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().size();
            }

            BOOL IsEmpty() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().IsEmpty();
            }

            BOOL HasEmbeddedNull() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().HasEmbeddedNull();
            }

            LPCWSTR GetRawBuffer(__out_opt UINT32 *length) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().GetRawBuffer(length);
            }

            HRESULT GetLpcwstr(__deref_out LPCWSTR *ppsz) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().GetLpcwstr(ppsz);
            }

            HRESULT CopyTo(__out HSTRING *phstring) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return  WindowsDuplicateString(this->_hstring, phstring);
            }

            // Concatenation
            HRESULT Concat(const String& otherString, __out String& newString)  const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().Concat(otherString, newString);
            }

            // Trim
            HRESULT TrimStart(const String& trimString, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().TrimStart(trimString, newString);
            }

            HRESULT TrimEnd(const String& trimString, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().TrimEnd(trimString, newString);
            }

            // Substring
            HRESULT Substring(UINT32 startIndex, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().Substring(startIndex, newString);
            }

            HRESULT Substring(UINT32 startIndex, UINT32 length, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().Substring(startIndex, length, newString);
            }

            // Replace
            HRESULT Replace(const String& stringReplaced, const String& stringReplaceWith, __out String& newString) const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return _AsString().Replace(stringReplaced, stringReplaceWith, newString);
            }

        private:
            // No Copy Constructor
            StringReference(const String& other);

            // No non-const WCHAR array constructor
            template <UINT32 N>
            StringReference(__in WCHAR (&stringRef)[N]);

            // No Copy assigment
            const StringReference & operator = (const String& other);

            // No new operator
            static void * operator new(size_t size);

            // No delete operator
            static void operator delete(void *p, size_t size);

            // const String& operator
            const String& _AsString() const throw()
            {
                STATIC_CONTRACT_LIMITED_METHOD;
                return reinterpret_cast<const String&>(_hstring);
            }
            
            HSTRING             _hstring;
            HSTRING_HEADER      _header;
        };
    } // namespace winrt
} // namespace clr

typedef clr::winrt::String           WinRtString;
typedef clr::winrt::StringReference  WinRtStringRef;

// ==========================================================
// WinRT-specific DuplicateString variations.

LPWSTR DuplicateString(
    LPCWSTR wszString,
    size_t  cchString);

LPWSTR DuplicateStringThrowing(
    LPCWSTR wszString,
    size_t cchString);

inline
LPWSTR DuplicateString(WinRtString const & str)
{
    STATIC_CONTRACT_NOTHROW;
    UINT32 cchStr;
    LPCWSTR wzStr = str.GetRawBuffer(&cchStr);
    return DuplicateString(wzStr, cchStr);
}

inline
LPWSTR DuplicateStringThrowing(WinRtString const & str)
{
    STATIC_CONTRACT_THROWS;
    UINT32 cchStr;
    LPCWSTR wzStr = str.GetRawBuffer(&cchStr);
    return DuplicateStringThrowing(wzStr, cchStr);
}

inline
LPWSTR DuplicateString(HSTRING const & hStr)
{
    STATIC_CONTRACT_NOTHROW;
    WinRtStringRef str(hStr);
    UINT32 cchStr;
    LPCWSTR wzStr = str.GetRawBuffer(&cchStr);
    return DuplicateString(wzStr, cchStr);
}

inline
LPWSTR DuplicateStringThrowing(HSTRING const & hStr)
{
    STATIC_CONTRACT_THROWS;
    WinRtStringRef str(hStr);
    UINT32 cchStr;
    LPCWSTR wzStr = str.GetRawBuffer(&cchStr);
    return DuplicateStringThrowing(wzStr, cchStr);
}

// ==========================================================
// Convenience overloads of StringCchLength

// A convenience overload that assumes cchMax is STRSAFE_MAX_CCH.
inline
HRESULT StringCchLength(
    __in  LPCWSTR wz,
    __out size_t  *pcch)
{
    // To align with HSTRING functionality (which always promotes null
    // string pointers to the empty string), this wrapper also promotes
    // null string pointers to empty string before forwarding to Windows'
    // implementation. Don't skip the call to StringCchLength for null
    // pointers because we want to continue to align with the return value
    // when passed a null length out parameter.
    return StringCchLengthW(wz == nullptr ? L"" : wz, size_t(STRSAFE_MAX_CCH), pcch);
}

#ifdef _WIN64
    // A UINT32-specific overload with built-in overflow check.
    inline
    HRESULT StringCchLength(
        __in  LPCWSTR wz,
        __out UINT32  *pcch)
    {
        if (pcch == nullptr)
            return E_INVALIDARG;
    
        size_t cch;
        HRESULT hr = StringCchLength(wz, &cch);
        if (FAILED(hr))
            return hr;
    
        return SizeTToUInt32(cch, pcch);
    }
#endif // _WIN64

#ifndef DACCESS_COMPILE
    //=====================================================================================================================
    // Holder of CoTaskMem-allocated array of HSTRING (helper class for WinRT binders - e.g. code:CLRPrivBinderWinRT::GetFileNameListForNamespace).
    class CoTaskMemHSTRINGArrayHolder
    {
    public:
        CoTaskMemHSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;
        
            m_cValues = 0;
            m_rgValues = nullptr;
        }
        ~CoTaskMemHSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;
            Destroy();
        }
        
        // Destroys current array and holds new array rgValues of size cValues.
        void Init(HSTRING * rgValues, DWORD cValues)
        {
            LIMITED_METHOD_CONTRACT;
            
            Destroy();
            _ASSERTE(m_cValues == 0);
            
            _ASSERTE(((cValues == 0) && (rgValues == nullptr)) || 
                     ((cValues > 0) && (rgValues != nullptr)));
            
            m_rgValues = rgValues;
            m_cValues = cValues;
        }
        
        HSTRING GetAt(DWORD index) const
        {
            LIMITED_METHOD_CONTRACT;
            return m_rgValues[index];
        }
        
        DWORD GetCount()
        {
            LIMITED_METHOD_CONTRACT;
            return m_cValues;
        }
        
    private:
        void Destroy()
        {
            LIMITED_METHOD_CONTRACT;
            
            for (DWORD i = 0; i < m_cValues; i++)
            {
                if (m_rgValues[i] != nullptr)
                {
                    WindowsDeleteString(m_rgValues[i]);
                }
            }
            m_cValues = 0;
            
            if (m_rgValues != nullptr)
            {
                CoTaskMemFree(m_rgValues);
                m_rgValues = nullptr;
            }
        }
        
    private:
        DWORD     m_cValues;
        HSTRING * m_rgValues;
    };  // class CoTaskMemHSTRINGArrayHolder
#endif //!DACCESS_COMPILE


#endif // WindowsString_h

