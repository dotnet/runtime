// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapHeaders.h
//

//
// Zapping of headers (IMAGE_COR20_HEADER, CORCOMPILE_HEADER, etc.)
//
// ======================================================================================

#ifndef __ZAPHEADERS_H__
#define __ZAPHEADERS_H__

#include <clr_std/vector>

//
// IMAGE_COR20_HEADER
//

class ZapCorHeader : public ZapNode
{
public:
    ZapCorHeader(ZapImage * pImage)
    {
    }

    virtual DWORD GetSize()
    {
        return sizeof(IMAGE_COR20_HEADER);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_CorHeader;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage::GetImage(pZapWriter)->SaveCorHeader();
    }
};

//
// CORCOMPILE_HEADER
//

class ZapNativeHeader : public ZapNode
{
public:
    ZapNativeHeader(ZapImage * pImage)
    {
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_HEADER);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_NativeHeader;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage::GetImage(pZapWriter)->SaveNativeHeader();
    }
};

//
// CORCOMPILE_VERSION_INFO
//
class ZapVersionInfo : public ZapNode
{
    CORCOMPILE_VERSION_INFO m_versionInfo;

public:
    ZapVersionInfo(CORCOMPILE_VERSION_INFO * pVersionInfo)
    {
        memcpy(&m_versionInfo, pVersionInfo, sizeof(m_versionInfo));
    }

    CORCOMPILE_VERSION_INFO * GetData()
    {
        return &m_versionInfo;
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_VERSION_INFO);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_VersionInfo;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        pZapWriter->Write(&m_versionInfo, sizeof(m_versionInfo));
    }
};

//
// CORCOMPILE_CORCOMPILE_DEPENDENCY
//
class ZapDependencies : public ZapNode
{
    DWORD m_cDependencies;
    CORCOMPILE_DEPENDENCY * m_pDependencies;

public:
    ZapDependencies(CORCOMPILE_DEPENDENCY * pDependencies, DWORD cDependencies)
        : m_cDependencies(cDependencies), m_pDependencies(pDependencies)
    {
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_DEPENDENCY) * m_cDependencies;
    }

    virtual UINT GetAlignment()
    {
        return sizeof(ULARGE_INTEGER);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Dependencies;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        pZapWriter->Write(m_pDependencies, sizeof(CORCOMPILE_DEPENDENCY) * m_cDependencies);
    }
};

//
// CORCOMPILE_CODE_MANAGER_ENTRY
//

class ZapCodeManagerEntry : public ZapNode
{
public:
    ZapCodeManagerEntry(ZapImage * pImage)
    {
    }

    virtual DWORD GetSize()
    {
        return sizeof(CORCOMPILE_CODE_MANAGER_ENTRY);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_CodeManagerEntry;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        ZapImage::GetImage(pZapWriter)->SaveCodeManagerEntry();
    }
};

//
// Win32 Resources
//

class ZapWin32ResourceString : public ZapNode
{
    //
    // This ZapNode maps the IMAGE_RESOURCE_DIR_STRING_U resource data structure for storing strings.
    //

    LPWSTR m_pString;

public:
    ZapWin32ResourceString(LPCWSTR pString)
    {
        size_t strLen = wcslen(pString);
        _ASSERT(pString != NULL && strLen < 0xffff);

        m_pString = new WCHAR[strLen + 1];
        wcscpy(m_pString, pString);
        m_pString[strLen] = L'\0';
    }

    LPCWSTR GetString() { return m_pString; }

    virtual DWORD GetSize()
    {
        return sizeof(WORD) + sizeof(WCHAR) * (DWORD)wcslen(m_pString);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(WORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Blob;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        WORD size = (WORD)wcslen(m_pString);
        pZapWriter->Write(&size, sizeof(WORD));
        pZapWriter->Write((PVOID)m_pString, sizeof(WCHAR) * size);
    }
};

class ZapWin32ResourceDirectory : public ZapNode
{
    //
    // This ZapNode maps the IMAGE_RESOURCE_DIRECTORY resource data structure for storing a resource directory. Each directory
    // is then followed by a number of IMAGE_RESOURCE_DIRECTORY_ENTRY entries, which can either point to other resource directories (RVAs
    // to other ZapWin32ResourceDirectory nodes), or point to actual resource data (RVAs to a number of IMAGE_RESOURCE_DATA_ENTRY entries
    // that immediately follow the IMAGE_RESOURCE_DIRECTORY_ENTRY entries).
    //
    // Refer to the PE resources format for more information (https://docs.microsoft.com/en-us/windows/desktop/debug/pe-format#the-rsrc-section)
    //

    struct DataOrSubDirectoryEntry
    {
        PVOID m_pNameOrId;
        bool m_nameOrIdIsString;
        ZapNode* m_pDataOrSubDirectory;
        bool m_dataIsSubDirectory;
    };
    std::vector<DataOrSubDirectoryEntry> m_entries;
    ZapVirtualSection* m_pWin32ResourceSection;

public:
    ZapWin32ResourceDirectory(ZapVirtualSection* pWin32ResourceSection)
        : m_pWin32ResourceSection(pWin32ResourceSection)
    { }

    void AddEntry(PVOID pNameOrId, bool nameOrIdIsString, ZapNode* pDataOrSubDirectory, bool dataIsSubDirectory)
    {
        DataOrSubDirectoryEntry entry;
        entry.m_pDataOrSubDirectory = pDataOrSubDirectory;
        entry.m_dataIsSubDirectory = dataIsSubDirectory;
        entry.m_pNameOrId = pNameOrId;
        entry.m_nameOrIdIsString = nameOrIdIsString;

        m_entries.push_back(entry);
    }

    virtual DWORD GetSize()
    {
        DWORD size = sizeof(IMAGE_RESOURCE_DIRECTORY) + (DWORD)m_entries.size() * sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY);
        for (auto& entry : m_entries)
        {
            if (!entry.m_dataIsSubDirectory)
                size += sizeof(IMAGE_RESOURCE_DATA_ENTRY);
        }
        return size;
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Win32Resources;
    }

    void PlaceNodeAndDependencies(ZapVirtualSection* pWin32ResourceSection)
    {
        pWin32ResourceSection->Place(this);

        for (auto& entry : m_entries)
        {
            if (entry.m_dataIsSubDirectory)
            {
                ZapWin32ResourceDirectory* pSubDirNode = (ZapWin32ResourceDirectory*)entry.m_pDataOrSubDirectory;
                pSubDirNode->PlaceNodeAndDependencies(pWin32ResourceSection);
            }
        }
    }

    virtual void Save(ZapWriter * pZapWriter);
};


//
// Debug Directory
//

class ZapDebugDirectory : public ZapNode
{
    ZapNode * m_pNGenPdbDebugData;
    DWORD m_nDebugDirectory;
    IMAGE_DEBUG_DIRECTORY * m_pDebugDirectory;
    ZapNode ** m_ppDebugData;

public:
    ZapDebugDirectory(ZapNode *pNGenPdbDebugData, DWORD nDebugDirectory, PIMAGE_DEBUG_DIRECTORY pDebugDirectory, ZapNode ** ppDebugData)
        : m_pNGenPdbDebugData(pNGenPdbDebugData),
          m_nDebugDirectory(nDebugDirectory),
          m_pDebugDirectory(pDebugDirectory),
          m_ppDebugData(ppDebugData)
    {
    }

    virtual DWORD GetSize()
    {
#if defined(NO_NGENPDB)
        return sizeof(IMAGE_DEBUG_DIRECTORY) * m_nDebugDirectory;
#else
        // Add one for NGen PDB debug directory entry
        return sizeof(IMAGE_DEBUG_DIRECTORY) * (m_nDebugDirectory + 1);
#endif // NO_NGENPDB
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_DebugDirectory;
    }

    void SaveOriginalDebugDirectoryEntry(ZapWriter *pZapWriter);
    void SaveNGenDebugDirectoryEntry(ZapWriter *pZapWriter);
    virtual void Save(ZapWriter * pZapWriter);
};

//
// PE Style exports.  Currently can only save an empty list of exports
// but this is useful because it avoids the DLL being seen as Resource Only
// (which then causes SymServer to avoid copying its PDB to the cloud).
//

class ZapPEExports : public ZapNode
{
	LPCWSTR m_dllFileName;	// Just he DLL name without the path.

public:
	ZapPEExports(LPCWSTR dllPath);
	virtual DWORD GetSize();
	virtual UINT GetAlignment() { return sizeof(DWORD);  }
	virtual void Save(ZapWriter * pZapWriter);
};

//
// List of all sections for diagnostic purposes

class ZapVirtualSectionsTable : public ZapNode
{
    ZapImage * m_pImage;

public:

    ZapVirtualSectionsTable(ZapImage * pImage)
        : m_pImage(pImage)
    {
    }

    virtual DWORD GetSize();

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_VirtualSectionsTable;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

#endif // __ZAPHEADERS_H__
