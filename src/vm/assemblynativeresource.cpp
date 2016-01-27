// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////////
// ResFile.CPP



#include "common.h"

#include "assemblynativeresource.h"
#include <limits.h>

#ifndef CP_WINUNICODE
 #define CP_WINUNICODE   1200
#endif

#ifndef MAKEINTRESOURCE
 #define MAKEINTRESOURCE MAKEINTRESOURCEW
#endif

Win32Res::Win32Res()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_szFile = NULL;
    m_Icon = NULL;
    int i;
    for (i = 0; i < NUM_VALUES; i++)
        m_Values[i] = NULL;
    for (i = 0; i < NUM_VALUES; i++)
        m_Values[i] = NULL;
    m_fDll = false;
    m_pData = NULL;
    m_pCur = NULL;
    m_pEnd = NULL;
}

Win32Res::~Win32Res()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_szFile = NULL;
    m_Icon = NULL;
    int i;
    for (i = 0; i < NUM_VALUES; i++)
        m_Values[i] = NULL;
    for (i = 0; i < NUM_VALUES; i++)
        m_Values[i] = NULL;
    m_fDll = false;
    if (m_pData)
        delete [] m_pData;
    m_pData = NULL;
    m_pCur = NULL;

    m_pEnd = NULL;
}

//*****************************************************************************
// Initializes the structures with version information.
//*****************************************************************************
VOID Win32Res::SetInfo(
    LPCWSTR     szFile, 
    LPCWSTR     szTitle, 
    LPCWSTR     szIconName, 
    LPCWSTR     szDescription,
    LPCWSTR     szCopyright, 
    LPCWSTR     szTrademark, 
    LPCWSTR     szCompany, 
    LPCWSTR     szProduct, 
    LPCWSTR     szProductVersion,
    LPCWSTR     szFileVersion, 
    LCID        lcid, 
    BOOL        fDLL)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(szFile != NULL);

    m_szFile = szFile;
    if (szIconName && szIconName[0] != 0)
        m_Icon = szIconName;    // a non-mepty string

#define NonNull(sz) (sz == NULL || *sz == W('\0') ? W(" ") : sz)
    m_Values[v_Description]     = NonNull(szDescription);
    m_Values[v_Title]           = NonNull(szTitle);
    m_Values[v_Copyright]       = NonNull(szCopyright);
    m_Values[v_Trademark]       = NonNull(szTrademark);
    m_Values[v_Product]         = NonNull(szProduct);
    m_Values[v_ProductVersion]  = NonNull(szProductVersion);
    m_Values[v_Company]         = NonNull(szCompany);
    m_Values[v_FileVersion]     = NonNull(szFileVersion);
#undef NonNull

    m_fDll = fDLL;
    m_lcid = lcid;
}

VOID Win32Res::MakeResFile(const void **pData, DWORD  *pcbData)
{
    STANDARD_VM_CONTRACT;

    static const RESOURCEHEADER magic = { 0x00000000, 0x00000020, 0xFFFF, 0x0000, 0xFFFF, 0x0000,
                        0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 };
    _ASSERTE(pData != NULL && pcbData != NULL);

    *pData = NULL;
    *pcbData = 0;
    m_pData = new BYTE[(sizeof(RESOURCEHEADER) * 3 + sizeof(EXEVERRESOURCE))];

    m_pCur = m_pData;
    m_pEnd = m_pData + sizeof(RESOURCEHEADER) * 3 + sizeof(EXEVERRESOURCE);

    // inject the magic empty entry
    Write( &magic, sizeof(magic) );

    WriteVerResource();

    if (m_Icon)
    {
        WriteIconResource();
    }

    *pData = m_pData;
    *pcbData = (DWORD)(m_pCur - m_pData);
    return;
}


/*
 * WriteIconResource
 *   Writes the Icon resource into the RES file.
 *
 * RETURNS: TRUE on succes, FALSE on failure (errors reported to user)
 */
VOID Win32Res::WriteIconResource()
{
    STANDARD_VM_CONTRACT;

    HandleHolder hIconFile = INVALID_HANDLE_VALUE;
    WORD wTemp, wCount, resID = 2;  // Skip 1 for the version ID
    DWORD dwRead = 0, dwWritten = 0;

    RESOURCEHEADER grpHeader = { 0x00000000, 0x00000020, 0xFFFF, (WORD)(size_t)RT_GROUP_ICON, 0xFFFF, 0x7F00, // 0x7F00 == IDI_APPLICATION
                0x00000000, 0x1030, 0x0000, 0x00000000, 0x00000000 };

    // Read the icon
    hIconFile = WszCreateFile( m_Icon, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
        FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hIconFile == INVALID_HANDLE_VALUE) {
        COMPlusThrowWin32();
    }

    // Read the magic reserved WORD
    if (ReadFile( hIconFile, &wTemp, sizeof(WORD), &dwRead, NULL) == FALSE) {
        COMPlusThrowWin32();
    } else if (wTemp != 0 || dwRead != sizeof(WORD)) {
        COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
    }

    // Verify the Type WORD
    if (ReadFile( hIconFile, &wCount, sizeof(WORD), &dwRead, NULL) == FALSE) {
        COMPlusThrowWin32();
    } else if (wCount != 1 || dwRead != sizeof(WORD)) {
        COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
    }

    // Read the Count WORD
    if (ReadFile( hIconFile, &wCount, sizeof(WORD), &dwRead, NULL) == FALSE) {
        COMPlusThrowWin32();
    } else if (wCount == 0 || dwRead != sizeof(WORD)) {
        COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
    }

    NewArrayHolder<ICONRESDIR> grp = new ICONRESDIR[wCount];
    grpHeader.DataSize = 3 * sizeof(WORD) + wCount * sizeof(ICONRESDIR);

    // For each Icon
    for (WORD i = 0; i < wCount; i++) {
        ICONDIRENTRY ico;
        DWORD        icoPos, newPos;
        RESOURCEHEADER icoHeader = { 0x00000000, 0x00000020, 0xFFFF, (WORD)(size_t)RT_ICON, 0xFFFF, 0x0000,
                    0x00000000, 0x1010, 0x0000, 0x00000000, 0x00000000 };
        icoHeader.Name = resID++;

        // Read the Icon header
        if (ReadFile( hIconFile, &ico, sizeof(ICONDIRENTRY), &dwRead, NULL) == FALSE) {
            COMPlusThrowWin32();
        }
        else if (dwRead != sizeof(ICONDIRENTRY)) {
            COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
        }

        _ASSERTE(sizeof(ICONRESDIR) + sizeof(WORD) == sizeof(ICONDIRENTRY));
        memcpy(grp + i, &ico, sizeof(ICONRESDIR));
        grp[i].IconId = icoHeader.Name;
        icoHeader.DataSize = ico.dwBytesInRes;

        NewArrayHolder<BYTE> icoBuffer = new BYTE[icoHeader.DataSize];

        // Write the header to the RES file
        Write( &icoHeader, sizeof(RESOURCEHEADER) );

        // Position to read the Icon data
        icoPos = SetFilePointer( hIconFile, 0, NULL, FILE_CURRENT);
        if (icoPos == INVALID_SET_FILE_POINTER) {
            COMPlusThrowWin32();
        }
        newPos = SetFilePointer( hIconFile, ico.dwImageOffset, NULL, FILE_BEGIN);
        if (newPos == INVALID_SET_FILE_POINTER) {
            COMPlusThrowWin32();
        }

        // Actually read the data
        if (ReadFile( hIconFile, icoBuffer, icoHeader.DataSize, &dwRead, NULL) == FALSE) {
            COMPlusThrowWin32();
        }
        else if (dwRead != icoHeader.DataSize) {
            COMPlusThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
        }

        // Because Icon files don't seem to record the actual Planes and BitCount in 
        // the ICONDIRENTRY, get the info from the BITMAPINFOHEADER at the beginning
        // of the data here:
        grp[i].Planes = ((BITMAPINFOHEADER*)(BYTE*)icoBuffer)->biPlanes;
        grp[i].BitCount = ((BITMAPINFOHEADER*)(BYTE*)icoBuffer)->biBitCount;

        // Now write the data to the RES file
        Write( (BYTE*)icoBuffer, icoHeader.DataSize );
        
        // Reposition to read the next Icon header
        newPos = SetFilePointer( hIconFile, icoPos, NULL, FILE_BEGIN);
        if (newPos != icoPos) {
            COMPlusThrowWin32();
        }
    }

    // inject the icon group
    Write( &grpHeader, sizeof(RESOURCEHEADER) );

    // Write the header to the RES file
    wTemp = 0; // the reserved WORD
    Write( &wTemp, sizeof(WORD) );

    wTemp = RES_ICON; // the GROUP type
    Write( &wTemp, sizeof(WORD) );

    Write( &wCount, sizeof(WORD) );

    // now write the entries
    Write( grp, sizeof(ICONRESDIR) * wCount );

    return;
}

/*
 * WriteVerResource
 *   Writes the version resource into the RES file.
 *
 * RETURNS: TRUE on succes, FALSE on failure (errors reported to user)
 */
VOID Win32Res::WriteVerResource()
{
    STANDARD_VM_CONTRACT;

    WCHAR szLangCp[9];           // language/codepage string.
    EXEVERRESOURCE VerResource;
    WORD  cbStringBlocks;
    int i;
    bool bUseFileVer = false;
    WCHAR       rcFile[_MAX_PATH] = {0};              // Name of file without path
    WCHAR       rcFileExtension[_MAX_PATH] = {0};     // file extension
    WCHAR       rcFileName[_MAX_PATH];          // Name of file with extension but without path
    DWORD       cbTmp;

    SplitPath(m_szFile, 0, 0, 0, 0, rcFile, _MAX_PATH, rcFileExtension, _MAX_PATH);

    wcscpy_s(rcFileName, COUNTOF(rcFileName), rcFile);
    wcscat_s(rcFileName, COUNTOF(rcFileName), rcFileExtension);

    static const EXEVERRESOURCE VerResourceTemplate = {
        sizeof(EXEVERRESOURCE), sizeof(VS_FIXEDFILEINFO), 0, W("VS_VERSION_INFO"),
        {
            VS_FFI_SIGNATURE,           // Signature
            VS_FFI_STRUCVERSION,        // structure version
            0, 0,                       // file version number
            0, 0,                       // product version number
            VS_FFI_FILEFLAGSMASK,       // file flags mask
            0,                          // file flags
            VOS__WINDOWS32,
            VFT_APP,                    // file type
            0,                          // subtype
            0, 0                        // file date/time
        },
        sizeof(WORD) * 2 + 2 * HDRSIZE + KEYBYTES("VarFileInfo") + KEYBYTES("Translation"),
        0,
        1,
        W("VarFileInfo"),
        sizeof(WORD) * 2 + HDRSIZE + KEYBYTES("Translation"),
        sizeof(WORD) * 2,
        0,
        W("Translation"),
        0,
        0,
        2 * HDRSIZE + KEYBYTES("StringFileInfo") + KEYBYTES("12345678"),
        0,
        1,
        W("StringFileInfo"),
        HDRSIZE + KEYBYTES("12345678"),
        0,
        1,
        W("12345678")
    };
    static const WCHAR szComments[] = W("Comments");
    static const WCHAR szCompanyName[] = W("CompanyName");
    static const WCHAR szFileDescription[] = W("FileDescription");
    static const WCHAR szCopyright[] = W("LegalCopyright");
    static const WCHAR szTrademark[] = W("LegalTrademarks");
    static const WCHAR szProdName[] = W("ProductName");
    static const WCHAR szFileVerResName[] = W("FileVersion");
    static const WCHAR szProdVerResName[] = W("ProductVersion");
    static const WCHAR szInternalNameResName[] = W("InternalName");
    static const WCHAR szOriginalNameResName[] = W("OriginalFilename");
    
    // If there's no product version, use the file version
    if (m_Values[v_ProductVersion][0] == 0) {
        m_Values[v_ProductVersion] = m_Values[v_FileVersion];
        bUseFileVer = true;
    }

    // Keep the two following arrays in the same order
#define MAX_KEY     10
    static const LPCWSTR szKeys [MAX_KEY] = {
        szComments,
        szCompanyName,
        szFileDescription,
        szFileVerResName,
        szInternalNameResName,
        szCopyright,
        szTrademark,
        szOriginalNameResName,
        szProdName,
        szProdVerResName,
    };
    LPCWSTR szValues [MAX_KEY] = {  // values for keys
        m_Values[v_Description],    //compiler->assemblyDescription == NULL ? W("") : compiler->assemblyDescription,
        m_Values[v_Company],        // Company Name
        m_Values[v_Title],          // FileDescription  //compiler->assemblyTitle == NULL ? W("") : compiler->assemblyTitle,
        m_Values[v_FileVersion],    // FileVersion
        rcFileName,                 // InternalName
        m_Values[v_Copyright],      // Copyright
        m_Values[v_Trademark],      // Trademark
        rcFileName,                 // OriginalName
        m_Values[v_Product],        // Product Name     //compiler->assemblyTitle == NULL ? W("") : compiler->assemblyTitle,
        m_Values[v_ProductVersion]  // Product Version
    };

    memcpy(&VerResource, &VerResourceTemplate, sizeof(VerResource));

    if (m_fDll)
        VerResource.vsFixed.dwFileType = VFT_DLL;
    else
        VerResource.vsFixed.dwFileType = VFT_APP;

    // Extract the numeric version from the string.
    m_Version[0] = m_Version[1] = m_Version[2] = m_Version[3] = 0;
    int nNumStrings = swscanf_s(m_Values[v_FileVersion], W("%hu.%hu.%hu.%hu"), m_Version, m_Version + 1, m_Version + 2, m_Version + 3);

    // Fill in the FIXEDFILEINFO
    VerResource.vsFixed.dwFileVersionMS =
        ((DWORD)m_Version[0] << 16) + m_Version[1];

    VerResource.vsFixed.dwFileVersionLS =
        ((DWORD)m_Version[2] << 16) + m_Version[3];

    if (bUseFileVer) {
        VerResource.vsFixed.dwProductVersionLS = VerResource.vsFixed.dwFileVersionLS;
        VerResource.vsFixed.dwProductVersionMS = VerResource.vsFixed.dwFileVersionMS;
    }
    else {
        WORD v[4];
        v[0] = v[1] = v[2] = v[3] = 0;
        // Try to get the version numbers, but don't waste time or give any errors
        // just default to zeros
        nNumStrings = swscanf_s(m_Values[v_ProductVersion], W("%hu.%hu.%hu.%hu"), v, v + 1, v + 2, v + 3);

        VerResource.vsFixed.dwProductVersionMS =
            ((DWORD)v[0] << 16) + v[1];

        VerResource.vsFixed.dwProductVersionLS =
            ((DWORD)v[2] << 16) + v[3];
    }

    // There is no documentation on what units to use for the date!  So we use zero.
    // The Windows resource compiler does too.
    VerResource.vsFixed.dwFileDateMS = VerResource.vsFixed.dwFileDateLS = 0;

    // Fill in codepage/language -- we'll assume the IDE language/codepage
    // is the right one.
    if (m_lcid != -1)
        VerResource.langid = static_cast<WORD>(m_lcid);
    else 
        VerResource.langid = MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL); 
    VerResource.codepage = CP_WINUNICODE;   // Unicode codepage.

    swprintf_s(szLangCp, NumItems(szLangCp), W("%04x%04x"), VerResource.langid, VerResource.codepage);
    wcscpy_s(VerResource.szLangCpKey, COUNTOF(VerResource.szLangCpKey), szLangCp);

    // Determine the size of all the string blocks.
    cbStringBlocks = 0;
    for (i = 0; i < MAX_KEY; i++) {
        if (szValues[i] == NULL || wcslen(szValues[i]) == 0)
            continue;
        cbTmp = SizeofVerString( szKeys[i], szValues[i]);
        if ((cbStringBlocks + cbTmp) > USHRT_MAX / 2)
            COMPlusThrow(kArgumentException, W("Argument_VerStringTooLong"));
        cbStringBlocks += (WORD) cbTmp;
    }

    if ((cbStringBlocks + VerResource.cbLangCpBlock) > USHRT_MAX / 2)
        COMPlusThrow(kArgumentException, W("Argument_VerStringTooLong"));
    VerResource.cbLangCpBlock += cbStringBlocks;

    if ((cbStringBlocks + VerResource.cbStringBlock) > USHRT_MAX / 2)
        COMPlusThrow(kArgumentException, W("Argument_VerStringTooLong"));
    VerResource.cbStringBlock += cbStringBlocks;

    if ((cbStringBlocks + VerResource.cbRootBlock) > USHRT_MAX / 2)
        COMPlusThrow(kArgumentException, W("Argument_VerStringTooLong"));
    VerResource.cbRootBlock += cbStringBlocks;

    // Call this VS_VERSION_INFO
    RESOURCEHEADER verHeader = { 0x00000000, 0x0000003C, 0xFFFF, (WORD)(size_t)RT_VERSION, 0xFFFF, 0x0001,
                                 0x00000000, 0x0030, 0x0000, 0x00000000, 0x00000000 };
    verHeader.DataSize = VerResource.cbRootBlock;

    // Write the header
    Write( &verHeader, sizeof(RESOURCEHEADER) );

    // Write the version resource
    Write( &VerResource, sizeof(VerResource) );
    

    // Write each string block.
    for (i = 0; i < MAX_KEY; i++) {
        if (szValues[i] == NULL || wcslen(szValues[i]) == 0)
            continue;
        WriteVerString( szKeys[i], szValues[i] );
    }
#undef MAX_KEY

    return;
}

/*
 * SizeofVerString
 *    Determines the size of a version string to the given stream.
 * RETURNS: size of block in bytes.
 */
WORD Win32Res::SizeofVerString(LPCWSTR lpszKey, LPCWSTR lpszValue)
{
    STANDARD_VM_CONTRACT;

    size_t cbKey, cbValue;

    cbKey = (wcslen(lpszKey) + 1) * 2;  // Make room for the NULL
    cbValue = (wcslen(lpszValue) + 1) * 2;
    if (cbValue == 2)
        cbValue = 4;   // Empty strings need a space and NULL terminator (for Win9x)
    if (cbKey + cbValue >= 0xFFF0)
        COMPlusThrow(kArgumentException, W("Argument_VerStringTooLong"));

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6305) // "Potential mismatch between sizeof and countof quantities"
#endif

    return (WORD)(PadKeyLen(cbKey) +   // key, 0 padded to DWORD boundary
                  PadValLen(cbValue) + // value, 0 padded to dword boundary
                  HDRSIZE);             // block header.

#ifdef _PREFAST_
#pragma warning(pop)
#endif
}

/*----------------------------------------------------------------------------
 * WriteVerString
 *    Writes a version string to the given file.
 */
VOID Win32Res::WriteVerString( LPCWSTR lpszKey, LPCWSTR lpszValue)
{
    STANDARD_VM_CONTRACT;

    size_t cbKey, cbValue, cbBlock;
    bool bNeedsSpace = false;

    cbKey = (wcslen(lpszKey) + 1) * 2;     // includes terminating NUL
    cbValue = wcslen(lpszValue);
    if (cbValue > 0)
        cbValue++; // make room for NULL
    else {
        bNeedsSpace = true;
        cbValue = 2; // Make room for space and NULL (for Win9x)
    }
    cbBlock = SizeofVerString(lpszKey, lpszValue);

    NewArrayHolder<BYTE> pbBlock = new BYTE[(DWORD)cbBlock + HDRSIZE];
    ZeroMemory(pbBlock, (DWORD)cbBlock + HDRSIZE);

    _ASSERTE(cbValue < USHRT_MAX && cbKey < USHRT_MAX && cbBlock < USHRT_MAX);

    // Copy header, key and value to block.
    *(WORD *)((BYTE *)pbBlock) = (WORD)cbBlock;
    *(WORD *)(pbBlock + sizeof(WORD)) = (WORD)cbValue;
    *(WORD *)(pbBlock + 2 * sizeof(WORD)) = 1;   // 1 = text value
    // size = (cbBlock + HDRSIZE - HDRSIZE) / sizeof(WCHAR)
    wcscpy_s((WCHAR*)(pbBlock + HDRSIZE), (cbBlock / sizeof(WCHAR)), lpszKey);

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6305) // "Potential mismatch between sizeof and countof quantities"
#endif

    if (bNeedsSpace)
        *((WCHAR*)(pbBlock + (HDRSIZE + PadKeyLen(cbKey)))) = W(' ');
    else
    {
        wcscpy_s((WCHAR*)(pbBlock + (HDRSIZE + PadKeyLen(cbKey))),
                 //size = ((cbBlock + HDRSIZE) - (HDRSIZE + PadKeyLen(cbKey))) / sizeof(WCHAR)
                 (cbBlock - PadKeyLen(cbKey))/sizeof(WCHAR),
                 lpszValue);
    }

#ifdef _PREFAST_
#pragma warning(pop)
#endif

    // Write block
    Write( pbBlock, cbBlock);

    return;
}

VOID Win32Res::Write(LPCVOID pData, size_t len)
{
    STANDARD_VM_CONTRACT;

    if (m_pCur + len > m_pEnd) {
        // Grow
        size_t newSize = (m_pEnd - m_pData);

        // double the size unless we need more than that
        if (len > newSize)
            newSize += len;
        else
            newSize *= 2;

        LPBYTE pNew = new BYTE[newSize];
        memcpy(pNew, m_pData, m_pCur - m_pData);
        delete [] m_pData;
        // Relocate the pointers
        m_pCur = pNew + (m_pCur - m_pData);
        m_pData = pNew;
        m_pEnd = pNew + newSize;
    }

    // Copy it in
    memcpy(m_pCur, pData, len);
    m_pCur += len;
    return;
}

