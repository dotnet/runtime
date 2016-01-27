// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// Fixed size string: no dynamic memory allocation, no destructor, no exception
template<typename T>
class FixedSizeString
{
    enum { FixedStringSize = 256 };

    T        m_string[FixedStringSize]; // Using template to support both char and wchar_t
    HRESULT  m_error;
    unsigned m_pos;

public:
    FixedSizeString()
    {
        Reset();
    }

    void Reset()
    {
        m_string[0] = 0;
        m_pos       = 0;
        m_error     = S_OK;
    }

    HRESULT GetError() const
    {
        return m_error;
    }

    void Append(char ch)
    {
        _ASSERTE((m_pos + 1) < FixedStringSize);

        if ((m_pos + 1) < FixedStringSize)
        {
            m_string[m_pos ++] = ch;
            m_string[m_pos ] = 0;
        }
        else
        {
            m_error = E_OUTOFMEMORY;
        }
    }

    void Append(const char * pStr)
    {
        while (* pStr)
        {
            _ASSERTE((m_pos + 1) < FixedStringSize);

            if ((m_pos + 1) < FixedStringSize)
            {
                _ASSERTE((pStr[0] & 0x80) == 0);
                m_string[m_pos ++] = * pStr ++;
            }
            else
            {
                m_error = E_OUTOFMEMORY;
                return;
            }
        }

        m_string[m_pos] = 0;
    }

    operator const T * () const
    {
        return m_string;
    }

    // Decode encoded assembly/function/field/attribute name back to string, add '.' as seperator
    void DecodeName(int count, const unsigned char * pCode, const LPCSTR * pDic)
    {
        Reset();

        for (int i = 0; i < count; i ++)
        {
            unsigned char code = pCode[i];

            if (code == 0)
            {
                break;
            }

            if (i != 0)
            {
                Append('.');
            }

            Append(pDic[code]);
        }

        _ASSERTE(SUCCEEDED(GetError()));
    }
};


// Same as fusion\utils\helpers.cpp HashString(wsKey, 0, dwHashSize, FALSE), duplicated here because cee_wks does not include fusion\utils\helpers.cpp

// Needs to match public static uint HashLCString(string str) in OptimizeFxRetarget.csscript
    
inline DWORD HashLCString(LPCSTR pKey)
{
    DWORD dwHash = 0;
    
    while (* pKey)
    {
        char ch = * pKey ++;

        if ((ch >= 'A') && (ch <= 'Z'))
        {
            ch += 32;
        }

        dwHash = (dwHash * 65599) + (DWORD) ch;
    }

    return dwHash;
}


inline DWORD HashLCString(LPCWSTR pKey)
{
    DWORD dwHash = 0;
    
    while (* pKey)
    {
        wchar_t ch = * pKey ++;

        if ((ch >= 'A') && (ch <= 'Z'))
        {
            ch += 32;
        }

        dwHash = (dwHash * 65599) + (DWORD) ch;
    }

    return dwHash;
}


// Enumerator for auto-generated hash table

// There are two arrays in the hash: hash array and collision array
// Each entry is two bytes : <index + 1, collision index>
// The first entry in collision array is always (0, 0) for termination

class StringHashEnumerator
{
    const BYTE * m_pHash;
    const BYTE * m_pCollision;

public:

    StringHashEnumerator(LPCWSTR pStr, const BYTE * pHash, size_t hashCount, const BYTE * pCollision)
    {
        // lower case string hashing, half the size of hash array
        DWORD hash = HashLCString(pStr) % ((DWORD) hashCount / 2);

        m_pCollision = pCollision;
        m_pHash      = pHash + hash * 2; // pointing to entry in hash array
    }

    StringHashEnumerator(LPCSTR pStr, const BYTE * pHash, size_t hashCount, const BYTE * pCollision)
    {
        // lower case string hashing, half the size of hash array
        DWORD hash = HashLCString(pStr) % ((DWORD) hashCount / 2);

        m_pCollision = pCollision;
        m_pHash      = pHash + hash * 2; // pointing to entry in hash array
    }

    int GetNext() // negative is ending
    {
        BYTE index = m_pHash[0];

        m_pHash = m_pCollision + m_pHash[1] * 2; // move to the next one: collision array

        return index - 1;
    }
};


