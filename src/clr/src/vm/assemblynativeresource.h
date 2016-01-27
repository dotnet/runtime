// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////////
// ResFile.H
// This handles Win32Resources
// 



#pragma once

class CFile;

class Win32Res {
public:
    Win32Res();
    ~Win32Res();

    VOID SetInfo(LPCWSTR szFile, 
                 LPCWSTR szTitle, 
                 LPCWSTR szIconName, 
                 LPCWSTR szDescription,
                 LPCWSTR szCopyright, 
                 LPCWSTR szTrademark, 
                 LPCWSTR szCompany, 
                 LPCWSTR szProduct, 
                 LPCWSTR szProductVersion,
                 LPCWSTR szFileVersion, 
                 LCID lcid, 
                 BOOL fDLL);
    VOID MakeResFile(const void **pData, DWORD  *pcbData);

private:
#define PadKeyLen(cb) ((((cb) + 5) & ~3) - 2)
#define PadValLen(cb) ((cb + 3) & ~3)
#define KEYSIZE(sz) (PadKeyLen(sizeof(sz)*sizeof(WCHAR))/sizeof(WCHAR))
#define KEYBYTES(sz) (KEYSIZE(sz)*sizeof(WCHAR))
#define HDRSIZE (3 * sizeof(WORD))

    static WORD             SizeofVerString(LPCWSTR lpszKey, LPCWSTR lpszValue);
    VOID                    WriteVerString(LPCWSTR lpszKey, LPCWSTR lpszValue);
    VOID                    WriteVerResource();
    VOID                    WriteIconResource();
             
    VOID                    Write(LPCVOID pData, size_t len);
    LPCWSTR     m_szFile;
    LPCWSTR     m_Icon;
	enum {
		v_Description, 
		v_Title, 
		v_Copyright, 
		v_Trademark, 
		v_Product, 
		v_ProductVersion, 
		v_Company, 
		v_FileVersion, 
		NUM_VALUES
		};
    LPCWSTR     m_Values[NUM_VALUES];
	ULONG		m_Version[4];
    int         m_lcid;
    BOOL        m_fDll;
    PBYTE       m_pData;
    PBYTE       m_pCur;
    PBYTE       m_pEnd;


    // RES file structs (borrowed from MSDN)
#pragma pack( push)
#pragma pack(1)
    struct RESOURCEHEADER {
        DWORD DataSize;
        DWORD HeaderSize;
        WORD  Magic1;
        WORD  Type;
        WORD  Magic2;
        WORD  Name;
        DWORD DataVersion;
        WORD  MemoryFlags;
        WORD  LanguageId;
        DWORD Version;
        DWORD Characteristics;
    };

    struct ICONDIRENTRY {
        BYTE  bWidth;
        BYTE  bHeight;
        BYTE  bColorCount;
        BYTE  bReserved;
        WORD  wPlanes;
        WORD  wBitCount;
        DWORD dwBytesInRes;
        DWORD dwImageOffset;
    };

    struct ICONRESDIR {
        BYTE  Width;        // = ICONDIRENTRY.bWidth;
        BYTE  Height;       // = ICONDIRENTRY.bHeight;
        BYTE  ColorCount;   // = ICONDIRENTRY.bColorCount;
        BYTE  reserved;     // = ICONDIRENTRY.bReserved;
        WORD  Planes;       // = ICONDIRENTRY.wPlanes;
        WORD  BitCount;     // = ICONDIRENTRY.wBitCount;
        DWORD BytesInRes;   // = ICONDIRENTRY.dwBytesInRes;
        WORD  IconId;       // = RESOURCEHEADER.Name
    };
    struct EXEVERRESOURCE {
        WORD cbRootBlock;                                     // size of whole resource
        WORD cbRootValue;                                     // size of VS_FIXEDFILEINFO structure
        WORD fRootText;                                       // root is text?
        WCHAR szRootKey[KEYSIZE("VS_VERSION_INFO")];          // Holds "VS_VERSION_INFO"
        VS_FIXEDFILEINFO vsFixed;                             // fixed information.
        WORD cbVarBlock;                                      //   size of VarFileInfo block
        WORD cbVarValue;                                      //   always 0
        WORD fVarText;                                        //   VarFileInfo is text?
        WCHAR szVarKey[KEYSIZE("VarFileInfo")];               //   Holds "VarFileInfo"
        WORD cbTransBlock;                                    //     size of Translation block
        WORD cbTransValue;                                    //     size of Translation value
        WORD fTransText;                                      //     Translation is text?
        WCHAR szTransKey[KEYSIZE("Translation")];             //     Holds "Translation"
        WORD langid;                                          //     language id
        WORD codepage;                                        //     codepage id
        WORD cbStringBlock;                                   //   size of StringFileInfo block
        WORD cbStringValue;                                   //   always 0
        WORD fStringText;                                     //   StringFileInfo is text?
        WCHAR szStringKey[KEYSIZE("StringFileInfo")];         //   Holds "StringFileInfo"
        WORD cbLangCpBlock;                                   //     size of language/codepage block
        WORD cbLangCpValue;                                   //     always 0
        WORD fLangCpText;                                     //     LangCp is text?
        WCHAR szLangCpKey[KEYSIZE("12345678")];               //     Holds hex version of language/codepage
        // followed by strings
    };
#pragma pack( pop)
};
