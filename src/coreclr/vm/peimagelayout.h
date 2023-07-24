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
    static PEImageLayout* LoadConverted(PEImage* pOwner, bool disableMapping);
    static PEImageLayout* LoadNative(LPCWSTR fullPath);
#endif
    PEImageLayout();
    virtual ~PEImageLayout();
    static BOOL CompareBase(UPTR path, UPTR mapping);

    // Refcount above images.
    void AddRef();
    ULONG Release();

    void ApplyBaseRelocations(bool relocationMustWriteCopy);

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

// A simple layout where data stays the same as in the input (file or a byte array)
class FlatImageLayout : public PEImageLayout
{
    VPTR_VTABLE_CLASS(FlatImageLayout, PEImageLayout)
        VPTR_UNIQUE(0x59)
protected:
    CLRMapViewHolder m_FileView;
public:
    HandleHolder m_FileMap;

#ifndef DACCESS_COMPILE
    FlatImageLayout(PEImage* pOwner);
    FlatImageLayout(PEImage* pOwner, const BYTE* array, COUNT_T size);
    void* LoadImageByCopyingParts(SIZE_T* m_imageParts) const;
#if TARGET_WINDOWS
    void* LoadImageByMappingParts(SIZE_T* m_imageParts) const;
#endif
#endif
};

// ConvertedImageView is for the case when we construct a loaded
// layout by mapping or copying portions of a flat layout
class ConvertedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(ConvertedImageLayout,PEImageLayout)
public:
    static const int MAX_PARTS = 16;
#ifndef DACCESS_COMPILE
    ConvertedImageLayout(FlatImageLayout* source, bool disableMapping);
    virtual ~ConvertedImageLayout();
    void  FreeImageParts();
#endif
private:
    PT_RUNTIME_FUNCTION m_pExceptionDir;
    SIZE_T              m_imageParts[MAX_PARTS];
};

// LoadedImageLayout is for the case when we construct a loaded layout directly
class LoadedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(LoadedImageLayout,PEImageLayout)
protected:
#ifndef TARGET_UNIX
    HINSTANCE m_Module;
#else
    PALPEFileHolder m_LoadedFile;
#endif
public:
#ifndef DACCESS_COMPILE
    LoadedImageLayout(PEImage* pOwner, HRESULT* returnDontThrow);
#if !defined(TARGET_UNIX)
    LoadedImageLayout(PEImage* pOwner, HMODULE hModule);
#endif // !TARGET_UNIX
    ~LoadedImageLayout();
#endif // !DACCESS_COMPILE
};

#ifndef DACCESS_COMPILE
// A special layout that is used to load standalone composite r2r files.
// This layout is not owned by a PEImage and created by simply loading the file
// at the given path.
class NativeImageLayout : public PEImageLayout
{
    VPTR_VTABLE_CLASS(NativeImageLayout, PEImageLayout)

public:
    NativeImageLayout(LPCWSTR fullPath);
};
#endif

#endif  // PEIMAGELAYOUT_H_

