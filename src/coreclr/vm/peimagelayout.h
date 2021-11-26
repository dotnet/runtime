// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
public:
    // ------------------------------------------------------------
    // Public constants
    // ------------------------------------------------------------
    enum
    {
        LAYOUT_FLAT   = 2,
        LAYOUT_LOADED = 4,
        LAYOUT_ANY = 0xf
    };

public:
#ifndef DACCESS_COMPILE
    static PEImageLayout* CreateFromByteArray(PEImage* pOwner, const BYTE* array, COUNT_T size);
#ifndef TARGET_UNIX
    static PEImageLayout* CreateFromHMODULE(HMODULE hModule,PEImage* pOwner);
#endif
    static PEImageLayout* Load(PEImage* pOwner, HRESULT* loadFailure);
    static PEImageLayout* LoadFlat(PEImage* pOwner);
    static PEImageLayout* LoadConverted(PEImage* pOwner, BOOL isInBundle = FALSE);
    static PEImageLayout* LoadNative(LPCWSTR fullPath);
    static PEImageLayout* Map(PEImage* pOwner);
#endif
    PEImageLayout();
    virtual ~PEImageLayout();
    static BOOL CompareBase(UPTR path, UPTR mapping);

    // Refcount above images.
    void AddRef();
    ULONG Release();

    void ApplyBaseRelocations();

public:
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

private:
    Volatile<LONG> m_refCount;
public:
    PEImage* m_pOwner;
};

typedef ReleaseHolder<PEImageLayout> PEImageLayoutHolder;

class FlatImageLayout;

// ConvertedImageView is for the case when we manually layout a flat image
class ConvertedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(ConvertedImageLayout,PEImageLayout)
protected:
    HandleHolder m_FileMap;
    CLRMapViewHolder m_FileView;
public:
#ifndef DACCESS_COMPILE
    ConvertedImageLayout(FlatImageLayout* source, BOOL isInBundle = FALSE);
    virtual ~ConvertedImageLayout();
#endif
private:
    PT_RUNTIME_FUNCTION m_pExceptionDir;
};

class MappedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(MappedImageLayout,PEImageLayout)
    VPTR_UNIQUE(0x15)
protected:
#ifndef TARGET_UNIX
    HandleHolder m_FileMap;
    CLRMapViewHolder m_FileView;
#else
    PALPEFileHolder m_LoadedFile;
#endif
public:
#ifndef DACCESS_COMPILE
    MappedImageLayout(PEImage* pOwner);
    virtual ~MappedImageLayout();
#endif
private:
    PT_RUNTIME_FUNCTION m_pExceptionDir;
};

#if !defined(TARGET_UNIX)
class LoadedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(LoadedImageLayout,PEImageLayout)
protected:
    HINSTANCE m_Module;
public:
#ifndef DACCESS_COMPILE
    LoadedImageLayout(PEImage* pOwner, HRESULT* returnDontThrow);
    LoadedImageLayout(PEImage* pOwner, HMODULE hModule);

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
#endif // !TARGET_UNIX

class FlatImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(FlatImageLayout,PEImageLayout)
    VPTR_UNIQUE(0x59)
protected:
    CLRMapViewHolder m_FileView;
public:
    HandleHolder m_FileMap;

#ifndef DACCESS_COMPILE
    FlatImageLayout(PEImage* pOwner);
    FlatImageLayout(PEImage* pOwner, const BYTE* array, COUNT_T size);
#endif

};

#ifndef DACCESS_COMPILE
class NativeImageLayout : public PEImageLayout
{
    VPTR_VTABLE_CLASS(NativeImageLayout, PEImageLayout)

public:
    NativeImageLayout(LPCWSTR fullPath);
};
#endif

#endif  // PEIMAGELAYOUT_H_

