// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapMetadata.h
//

//
// Metadata zapping
// 
// ======================================================================================

#ifndef __ZAPMETADATA_H__
#define __ZAPMETADATA_H__

#ifdef CLR_STANDALONE_BINDER
#include "nativedata.h"
#endif

//-----------------------------------------------------------------------------
//
// ZapMetaData is the barebone ZapNode to save metadata scope
//

class ZapMetaData : public ZapNode
{
    DWORD m_dwSize;

protected:
    IMetaDataEmit * m_pEmit;

public:
#ifdef CLR_STANDALONE_BINDER
    ZapMetaData();
#else
    ZapMetaData()
    {
    }
#endif

    ~ZapMetaData()
    {
        SAFERELEASE(m_pEmit);
    }

    void SetMetaData(IUnknown * pEmit);

#ifdef CLR_STANDALONE_BINDER
    void SetAssembly(__in_z LPWSTR name,
                     __in_z LPWSTR culture,
                     NativeAssemblyData * nad);

    void SetAssemblyReference(__in_z LPWSTR name,
                              __in_z LPWSTR culture,
                              NativeAssemblyData *nad);
#endif

    virtual DWORD GetSize();

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_MetaData;
    }

    virtual void Save(ZapWriter * pZapWriter);
#ifdef CLR_STANDALONE_BINDER
protected:


    void FixupMetaData();
    ULONG AddString(__in_z LPWSTR pName, __in int length);
    int StripExtension(__in_z LPWSTR pName);
    ULONG AddString(LPCSTR pName, int length);
    int StripExtension(LPCSTR pName);
    ULONG AddBlob(LPCVOID blob, COUNT_T cbBlob);
    void SetMVIDOfModule(LPCVOID mvid);

    BOOL  m_bFixedUp;
    SArray<BYTE> m_metadataHeap;
    SArray<BYTE> m_stringHeap;
    SArray<BYTE> m_guidHeap;
    SArray<BYTE> m_blobHeap;
#endif
};

//-----------------------------------------------------------------------------
//
// Helper node to copy RVA data to from IL to the NGEN image.
//

class ZapRVADataNode : public ZapNode
{
    PVOID m_pData;
    DWORD m_dwSize;
    DWORD m_dwAlignment;

public:
    ZapRVADataNode(PVOID pData)
    {
        m_pData = pData;
        m_dwSize = 0;
        m_dwAlignment = 1;
    }

    void UpdateSizeAndAlignment(DWORD dwSize, DWORD dwAlignment)
    {
        if (dwSize > m_dwSize)
            m_dwSize = dwSize;

        if (dwAlignment > m_dwAlignment)
            m_dwAlignment = dwAlignment;
    }

    PVOID GetData()
    {
        return m_pData;
    }

    virtual DWORD GetSize()
    {
        return m_dwSize;
    }

    virtual UINT GetAlignment()
    {
        return m_dwAlignment;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_RVAFieldData;
    }

    virtual void Save(ZapWriter * pZapWriter)
    {
        pZapWriter->Write(m_pData, m_dwSize);
    }
};


//-----------------------------------------------------------------------------
//
// ZapILMetaData copies both the metadata and IL to the NGEN image.
//

class ZapILMetaData : public ZapMetaData
{
    ZapImage * m_pImage;

    class ILBlob : public ZapBlobPtr
    {
    public:
        ILBlob(PVOID pData, SIZE_T cbSize)
            : ZapBlobPtr(pData, cbSize)
        {
        }

        virtual UINT GetAlignment()
        {
            // The tiny header does not have any alignment requirements
            return ((COR_ILMETHOD_TINY *)GetData())->IsTiny() ? sizeof(BYTE) : sizeof(DWORD);
        }
    };

    // Hashtable with all IL method blobs. If two methods have same IL code
    // we store it just once.
    SHash< NoRemoveSHashTraits < ZapBlob::SHashTraits > >  m_blobs;

    struct ILMethod
    {
        mdMethodDef m_md;
        ZapBlob * m_pIL;
    };

    class ILMethodTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ILMethod> >
    {
    public:
        typedef const mdMethodDef key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.m_md;
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k) 
        {
            LIMITED_METHOD_CONTRACT;
            return k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; ILMethod e; e.m_md = 0; e.m_pIL = NULL; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.m_md == 0; }
    };

    SHash< ILMethodTraits > m_ILMethods;

public:
    ZapILMetaData(ZapImage * pImage)
        : m_pImage(pImage)
    {
#ifdef CLR_STANDALONE_BINDER
        m_metaDataStart = pImage->m_ModuleDecoder.GetMetadata(&m_metaDataSize);
        pImage->GetCompileInfo()->GetMetadataRvaInfo(&m_firstMethodRvaOffset,
            &m_methodDefRecordSize, &m_methodDefCount, &m_firstFieldRvaOffset, &m_fieldRvaRecordSize, &m_fieldRvaCount);
#endif
    }

    void Preallocate(COUNT_T cbILImage)
    {
        PREALLOCATE_HASHTABLE(ZapILMetaData::m_blobs, 0.0040, cbILImage);
        PREALLOCATE_HASHTABLE(ZapILMetaData::m_ILMethods, 0.0044, cbILImage);
    }

    void EmitMethodIL(mdMethodDef md);

    void CopyIL();
    void CopyMetaData();
    void CopyRVAFields();

#ifdef CLR_STANDALONE_BINDER
    virtual DWORD GetSize();
#endif

    virtual void Save(ZapWriter * pZapWriter);

    ZapRVADataNode * GetRVAField(void * pData);

private:
    class RVADataTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapRVADataNode *> >
    {
    public:
        typedef PVOID key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e->GetData();
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k) 
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    SHash< RVADataTraits > m_rvaData;

#ifdef CLR_STANDALONE_BINDER
public:
    void EmitFieldRVA(mdToken fieldDefToken, RVA fieldRVA);

private:
    LPCVOID m_metaDataStart;
    COUNT_T m_metaDataSize;

    DWORD   m_firstMethodRvaOffset;
    DWORD   m_methodDefRecordSize;
    DWORD   m_methodDefCount;
    DWORD   m_firstFieldRvaOffset;
    DWORD   m_fieldRvaRecordSize;
    DWORD   m_fieldRvaCount;

    MapSHash<mdToken, DWORD> m_fieldToRVAMapping;
#endif
};

#endif // __ZAPMETADATA_H__
