// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef _StringCopyHolder_h_
#define _StringCopyHolder_h_


//-----------------------------------------------------------------------------
// Simple holder to keep a copy of a string.
// Implements IStringHolder so we can pass instances through IDacDbiInterface
// and have it fill in the contents.
//-----------------------------------------------------------------------------
class StringCopyHolder : public IDacDbiInterface::IStringHolder
{
public:
    StringCopyHolder();

    // Free the memory allocated for the string contents
    ~StringCopyHolder();

    // Make a copy of the provided null-terminated unicode string
    virtual HRESULT AssignCopy(const WCHAR * pCopy);

    // Reset the string to NULL and free memory
    void Clear();

    // Returns true if the string has been set to a non-NULL value
    bool IsSet()
    {
        return (m_szData != NULL);
    }

    // Returns true if an empty string is stored.  IsSet must be true to call this.
    bool IsEmpty()
    {
        _ASSERTE(m_szData != NULL);
        return m_szData[0] == W('\0');
    }

    // Returns the pointer to the string contents
    operator WCHAR* () const
    {
        return m_szData;
    }

private:
    // Disallow copying (to prevent double-free) - no implementation
    StringCopyHolder( const StringCopyHolder& rhs );
    StringCopyHolder& operator=( const StringCopyHolder& rhs );

    WCHAR * m_szData;

};


#endif // StringCopyHolder
