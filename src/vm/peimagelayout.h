// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEImageLayout.h
// 

// --------------------------------------------------------------------------------


#ifndef PEIMAGELAYOUT_H_
#define PEIMAGELAYOUT_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include "clrtypes.h"
#include "pedecoder.h"
#include "holder.h"

// --------------------------------------------------------------------------------
// Forward declarations
// --------------------------------------------------------------------------------

class Crst;
class PEImage;


typedef VPTR(class PEImageLayout) PTR_PEImageLayout;

class PEImageLayout : public PEDecoder
{
    VPTR_BASE_CONCRETE_VTABLE_CLASS(PEImageLayout)
    friend class PEModule;
public:
    // ------------------------------------------------------------
    // Public constants
    // ------------------------------------------------------------
    enum 
    {
        LAYOUT_MAPPED =1,
        LAYOUT_FLAT =2,
        LAYOUT_LOADED =4,
        LAYOUT_LOADED_FOR_INTROSPECTION =8,
        LAYOUT_ANY =0xf
    };


public:
#ifndef DACCESS_COMPILE
    static PEImageLayout* CreateFlat(const void *flat, COUNT_T size,PEImage* pOwner);
    static PEImageLayout* CreateFromStream(IStream* pIStream, PEImage* pOwner);
    static PEImageLayout* CreateFromHMODULE(HMODULE mappedbase,PEImage* pOwner, BOOL bTakeOwnership);
    static PEImageLayout* LoadFromFlat(PEImageLayout* pflatimage);
    static PEImageLayout* Load(PEImage* pOwner, BOOL bNTSafeLoad, BOOL bThrowOnError = TRUE);
    static PEImageLayout* LoadFlat(HANDLE hFile, PEImage* pOwner);
    static PEImageLayout* Map (HANDLE hFile, PEImage* pOwner);
#endif    
    PEImageLayout();
    virtual ~PEImageLayout();
    static void Startup();
    static CHECK CheckStartup();
    static BOOL CompareBase(UPTR path, UPTR mapping);
    
    // Refcount above images.
    void AddRef();
    ULONG Release();
    const SString& GetPath();

    void ApplyBaseRelocations();

public:
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

private:
    Volatile<LONG> m_refCount;
public:    
    PEImage* m_pOwner;
    DWORD m_Layout;
};

typedef ReleaseHolder<PEImageLayout> PEImageLayoutHolder;


//RawImageView is built on external data, does not need cleanup
class RawImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(RawImageLayout,PEImageLayout)
protected:
    CLRMapViewHolder m_DataCopy;
#ifndef FEATURE_PAL
    HModuleHolder m_LibraryHolder;
#endif // !FEATURE_PAL
    
public:
    RawImageLayout(const void *flat, COUNT_T size,PEImage* pOwner);    
    RawImageLayout(const void *mapped, PEImage* pOwner, BOOL bTakeOwnerShip, BOOL bFixedUp);    
};

// ConvertedImageView is for the case when we manually layout a flat image
class ConvertedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(ConvertedImageLayout,PEImageLayout)
protected:
    HandleHolder m_FileMap;  
    CLRMapViewHolder m_FileView;
public:
#ifndef DACCESS_COMPILE    
    ConvertedImageLayout(PEImageLayout* source);    
#endif
};

class MappedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(MappedImageLayout,PEImageLayout)
    VPTR_UNIQUE(0x15)
protected:
#ifndef FEATURE_PAL
    HandleHolder m_FileMap;
    CLRMapViewHolder m_FileView;
#else
    PALPEFileHolder m_LoadedFile;
#endif
public:
#ifndef DACCESS_COMPILE    
    MappedImageLayout(HANDLE hFile, PEImage* pOwner);    
#endif
};

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_PAL)
class LoadedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(LoadedImageLayout,PEImageLayout)
protected:
    HINSTANCE m_Module;
public:
#ifndef DACCESS_COMPILE    
    LoadedImageLayout(PEImage* pOwner, BOOL bNTSafeLoad, BOOL bThrowOnError);
    ~LoadedImageLayout()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        if (m_Module)
            CLRFreeLibrary(m_Module);
    }
#endif // !DACCESS_COMPILE
};
#endif // !CROSSGEN_COMPILE && !FEATURE_PAL

class FlatImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(FlatImageLayout,PEImageLayout)
    VPTR_UNIQUE(0x59)
protected:
    HandleHolder m_FileMap;
    CLRMapViewHolder m_FileView;
public:
#ifndef DACCESS_COMPILE    
    FlatImageLayout(HANDLE hFile, PEImage* pOwner);   
#endif

};



#endif  // PEIMAGELAYOUT_H_

