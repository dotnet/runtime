// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapHeaders.h
//

//
// Zapping of headers (IMAGE_COR20_HEADER, CORCOMPILE_HEADER, etc.)
// 
// ======================================================================================

#ifndef __ZAPHEADERS_H__
#define __ZAPHEADERS_H__

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
// Version Resource
//

class ZapVersionResource : public ZapNode
{
    ZapNode * m_pVersionData;

public:
    ZapVersionResource(ZapNode * pVersionData)
        : m_pVersionData(pVersionData)
    {
    }

    struct VersionResourceHeader {
        IMAGE_RESOURCE_DIRECTORY TypeDir;
        IMAGE_RESOURCE_DIRECTORY_ENTRY TypeEntry;
        IMAGE_RESOURCE_DIRECTORY NameDir;
        IMAGE_RESOURCE_DIRECTORY_ENTRY NameEntry;
        IMAGE_RESOURCE_DIRECTORY LangDir;
        IMAGE_RESOURCE_DIRECTORY_ENTRY LangEntry;
        IMAGE_RESOURCE_DATA_ENTRY DataEntry;
        CHAR Data[0];
    };

    virtual DWORD GetSize()
    {
        return sizeof(VersionResourceHeader);
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_VersionResource;
    }

    virtual void Save(ZapWriter * pZapWriter);
};


//
// Debug Directory
//

class ZapDebugDirectory : public ZapNode
{
    ZapNode * m_pNGenPdbDebugData;
    IMAGE_DEBUG_DIRECTORY m_debugDirectory;
    ZapNode * m_pDebugData;

public:
    ZapDebugDirectory(ZapNode *pNGenPdbDebugData, PIMAGE_DEBUG_DIRECTORY pDebugDirectory, ZapNode * pDebugData)
        : m_pNGenPdbDebugData(pNGenPdbDebugData),
          m_pDebugData(pDebugData)
    {
        if (pDebugDirectory == NULL)
            memset(&m_debugDirectory, 0, sizeof(IMAGE_DEBUG_DIRECTORY));
        else
            memcpy(&m_debugDirectory, pDebugDirectory, sizeof(IMAGE_DEBUG_DIRECTORY));
    }

    virtual DWORD GetSize()
    {
        return m_pDebugData ? sizeof(IMAGE_DEBUG_DIRECTORY) * 2 : sizeof(IMAGE_DEBUG_DIRECTORY);
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
