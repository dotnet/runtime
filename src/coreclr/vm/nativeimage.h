// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NATIVEIMAGE_H
#define _NATIVEIMAGE_H

#include "readytoruninfo.h"

// This structure is used in NativeImage to map simple names of component assemblies
// to their indices within the component assembly header table.
struct AssemblyNameIndex
{
    LPCUTF8 Name;
    int32_t Index;
    
    AssemblyNameIndex() : Name(NULL), Index(-1) {}
    AssemblyNameIndex(LPCUTF8 name, int32_t index) : Name(name), Index(index) {}
    
    static AssemblyNameIndex GetNull() { return AssemblyNameIndex(); }
    bool IsNull() const { return Index < 0; }
};

class AssemblyNameIndexHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<AssemblyNameIndex> >
{
public:
    // Similar to BaseAssemblySpec::CompareStrings, we're using temporary SStrings that throw
    // for case-insensitive UTF8 assembly name comparisons.
    static const bool s_NoThrow = false;

    typedef LPCUTF8 key_t;

    static AssemblyNameIndex Null() { return AssemblyNameIndex::GetNull(); }
    static bool IsNull(const AssemblyNameIndex& e) { return e.IsNull(); }

    static LPCUTF8 GetKey(const AssemblyNameIndex& assemblyNameIndex) { return assemblyNameIndex.Name; }
    static BOOL Equals(LPCUTF8 a, LPCUTF8 b);
    static count_t Hash(LPCUTF8 a);
};

typedef DPTR(class NativeImage) PTR_NativeImage;

class NativeImageIndexTraits : public NoRemoveSHashTraits<MapSHashTraits<LPCUTF8, PTR_NativeImage>>
{
public:
    // Similar to BaseAssemblySpec::CompareStrings, we're using temporary SStrings that throw
    // for case-insensitive UTF8 assembly name comparisons.
    static const bool s_NoThrow = false;

    static LPCUTF8 GetKey(const KeyValuePair<LPCUTF8, PTR_NativeImage>& e) { return e.Key(); }
    static BOOL Equals(LPCUTF8 a, LPCUTF8 b);
    static count_t Hash(LPCUTF8 a);

    static KeyValuePair<LPCUTF8, PTR_NativeImage> Null() { LIMITED_METHOD_CONTRACT; return KeyValuePair<LPCUTF8, PTR_NativeImage>(nullptr, PTR_NativeImage(nullptr)); }
    static KeyValuePair<LPCUTF8, PTR_NativeImage> Deleted() { LIMITED_METHOD_CONTRACT; return KeyValuePair<LPCUTF8, PTR_NativeImage>(nullptr, PTR_NativeImage(nullptr)); }
    static bool IsNull(const KeyValuePair<LPCUTF8, PTR_NativeImage>& e) { LIMITED_METHOD_CONTRACT; return e.Key() == nullptr; }
    static bool IsDeleted(const KeyValuePair<LPCUTF8, PTR_NativeImage>& e) { return e.Key() == nullptr; }
};

class ReadyToRunInfo;
class PEAssembly;
class PEImage;

// This class represents a  ReadyToRun image with native OS-specific envelope. As of today,
// this file format is used as the compiled native code cache in composite R2R Crossgen2
// build mode. Moving forward we plan to add support for OS-specific native executables
// (ELF on Linux, MachO on OSX).
//
// The native image is identified by a well-known public export 'RTR_HEADER' pointing to the
// master READYTORUN_HEADER structure for the entire file. For composite R2R executables
// built by crossgenning a larger number of input MSIL assemblies the READYTORUN_HEADER
// contains a section named ComponentAssemblies that points to READYTORUN_CORE_HEADER
// structures representing the individual component assemblies and per-assembly sections.
class NativeImage
{
private:
    // Points to the OwnerCompositeExecutable section content within the component MSIL module
    LPCUTF8 m_fileName;

    AssemblyBinder *m_pAssemblyBinder;
    ReadyToRunInfo *m_pReadyToRunInfo;
    IMDInternalImport *m_pManifestMetadata;
    PEImageLayout *m_pImageLayout;
    PTR_Assembly *m_pNativeMetadataAssemblyRefMap;
    
    IMAGE_DATA_DIRECTORY *m_pComponentAssemblies;
    IMAGE_DATA_DIRECTORY *m_pComponentAssemblyMvids;
    uint32_t m_componentAssemblyCount;
    uint32_t m_manifestAssemblyCount;
    SHash<AssemblyNameIndexHashTraits> m_assemblySimpleNameToIndexMap;
    
    Crst m_eagerFixupsLock;
    bool m_eagerFixupsHaveRun;
    bool m_readyToRunCodeDisabled;

private:
    NativeImage(AssemblyBinder *pAssemblyBinder, PEImageLayout *peImageLayout, LPCUTF8 imageFileName);

protected:
    void Initialize(READYTORUN_HEADER *header, LoaderAllocator *loaderAllocator, AllocMemTracker *pamTracker);

public:
    ~NativeImage();

    static NativeImage *Open(
        Module *componentModule,
        LPCUTF8 nativeImageFileName,
        AssemblyBinder *pAssemblyBinder,
        LoaderAllocator *pLoaderAllocator,
        /* out */ bool *isNewNativeImage);

    Crst *EagerFixupsLock() { return &m_eagerFixupsLock; }
    bool EagerFixupsHaveRun() const { return m_eagerFixupsHaveRun; }
    void SetEagerFixupsHaveRun() { m_eagerFixupsHaveRun = true; }
    LPCUTF8 GetFileName() const { return m_fileName; }

    uint32_t GetComponentAssemblyCount() const { return m_componentAssemblyCount; }
    ReadyToRunInfo *GetReadyToRunInfo() const { return m_pReadyToRunInfo; }
    IMDInternalImport *GetManifestMetadata() const { return m_pManifestMetadata; }
    uint32_t GetManifestAssemblyCount() const { return m_manifestAssemblyCount; }
    PTR_Assembly *GetManifestMetadataAssemblyRefMap() { return m_pNativeMetadataAssemblyRefMap; }
    AssemblyBinder *GetAssemblyBinder() const { return m_pAssemblyBinder; }

    Assembly *LoadManifestAssembly(uint32_t rowid, DomainAssembly *pParentAssembly);
    
    PTR_READYTORUN_CORE_HEADER GetComponentAssemblyHeader(LPCUTF8 assemblySimpleName);

    void CheckAssemblyMvid(Assembly *assembly) const;

    void DisableAllR2RCode()
    {
        LIMITED_METHOD_CONTRACT;
        m_readyToRunCodeDisabled = true;
    }

    bool ReadyToRunCodeDisabled()
    {
        LIMITED_METHOD_CONTRACT;
        return m_readyToRunCodeDisabled;
    }

private:
    IMDInternalImport *LoadManifestMetadata();
};

#endif
