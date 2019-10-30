// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// versioninfo.inl
//
// representation of version#
// 
// Note: must be platform independent
//
// ======================================================================================


// default cctor
inline VersionInfo::VersionInfo()
{
    m_wMajor=0;
    m_wMinor=0;
    m_wBuild=0;
    m_wRevision=0;
}

// constructor
inline VersionInfo::VersionInfo( unsigned short major,
                                                    unsigned short minor,
                                                    unsigned short build,
                                                    unsigned short revision
                                                    )
{
    m_wMajor=major;
    m_wMinor=minor;
    m_wBuild=build;
    m_wRevision=revision;
}

// field accessor
inline unsigned short VersionInfo::Major() const 
{
    return m_wMajor; 
};

// field accessor
inline unsigned short VersionInfo::Minor() const 
{
    return m_wMinor; 
};

// field accessor
inline unsigned short VersionInfo::Build() const 
{
    return m_wBuild; 
};

// field accessor
inline unsigned short VersionInfo::Revision() const 
{
    return m_wRevision; 
};

// Compares against the given version
//
// Input:
// version - the version info
//
// Output: 
// return value:
// -1 given version is newer
//  1 given version is older
//  0 given version is the same
inline int VersionInfo::Compare(const VersionInfo& version) const 
{
    if (Major() > version.Major())
        return 1;
    if (Major() < version.Major())
        return -1;
    if (Minor() > version.Minor())
        return 1;
    if (Minor() < version.Minor())
        return -1;
    if (Build() > version.Build())
        return 1;
    if (Build() < version.Build())
        return -1;
    if (Revision() > version.Revision())
        return 1;
    if (Revision() < version.Revision())
        return -1;
    return 0;
}


// Parses the given string into VersionInfo
//
// Input:
// szString - the string to parse, "x.x.x.x" 
//
// Output: 
// return value: count of fields parsed (<=4) or -1 if an error
inline int VersionInfo::Parse(LPCTSTR szString, VersionInfo* result)
{
    // sscanf is nice but we need an exact format match and no 0s
    size_t iLen = _tcslen(szString);
    
    unsigned short wVersion[4] = {0};
    int iVerIdx = 0;
    unsigned int dwCurrentValue = 0;
    bool bFirstChar = true;
        
    for (size_t i=0; i<= iLen; i++)
    {
        if(szString[i] == _T('\0'))
        {
            if(!bFirstChar)
                wVersion[iVerIdx++] = (unsigned short)(dwCurrentValue & 0xffff);
            break;
        }
        else
        if (szString[i] == _T('.') )
        {
            if(bFirstChar)
                return -1;
            
            // fill in
            wVersion[iVerIdx++] = (unsigned short)(dwCurrentValue & 0xffff);

            //check for extra characters
            if (iVerIdx > sizeof(wVersion)/sizeof(wVersion[0]))
            {
                if (szString[i+1] == _T('\0'))
                    break;
                else
                    return -1;
            }
            
            //reset 
            dwCurrentValue=0;
            bFirstChar=true;
            continue;
        }
        else
        if (szString[i] < _T('0'))
        {
            return -1;
        }
        else
        if (szString[i] > _T('9'))
        {
            return -1;
        }        
        else
        if (szString[i] == _T('0') && bFirstChar && szString[i+1]!= _T('.') && szString[i+1]!= _T('\0') ) 
        {
            return -1;
        }

        // the character is a digit
        dwCurrentValue=dwCurrentValue*10+szString[i]-_T('0');
        if(dwCurrentValue > 0xffff)
            return -1;

        bFirstChar=false;
        
    }

    //successfully parsed
    *result = VersionInfo(wVersion[0], wVersion[1], wVersion[2], wVersion[3]);
    return iVerIdx;
    
}

