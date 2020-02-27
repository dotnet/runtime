// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NATIVEIMAGE_H
#define _NATIVEIMAGE_H

#include "readytoruninfo.h"

// This structure is used in NativeImage to map simple names of component assemblies
// to their indices within the component assembly header table.
struct AssemblyNameIndex
{
    SString Name;
    int32_t Index;
    
    AssemblyNameIndex() : Index(-1) {}
    AssemblyNameIndex(const SString& name, int32_t index);
};

class AssemblyNameIndexHashTraits : public NoRemoveSHashTraits< PtrSHashWithCleanupTraits<AssemblyNameIndex, const SString&> >
{
public:
    static AssemblyNameIndex *Null() { return NULL; }
    static bool IsNull(const AssemblyNameIndex *e) { return e == NULL; }

    static const SString& GetKey(const AssemblyNameIndex *assemblyNameIndex) { return assemblyNameIndex->Name; }
    static BOOL Equals(const SString& a, const SString& b) { return a.Equals(b); }
    static count_t Hash(const SString& a) { return a.Hash(); }
};

class AssemblyLoadContext;
class ReadyToRunInfo;
class PEFile;
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
    LPCUTF8 m_utf8SimpleName;
    
    NewHolder<ReadyToRunInfo> m_pReadyToRunInfo;
    IMDInternalImport *m_pManifestMetadata;
    PEImageLayout *m_pPeImageLayout;
    
    IMAGE_DATA_DIRECTORY *m_pComponentAssemblies;
    uint32_t m_componentAssemblyCount;
    SHash<AssemblyNameIndexHashTraits> m_assemblySimpleNameToIndexMap;
    
    Crst m_eagerFixupsLock;
    bool m_eagerFixupsHaveRun;

private:
    NativeImage(
        PEFile *peFile,
        PEImageLayout *peImageLayout,
        READYTORUN_HEADER *header,
        LPCUTF8 nativeImageName,
        LoaderAllocator *loaderAllocator,
        AllocMemTracker& amTracker);

public:
    ~NativeImage();

    static NativeImage *Open(
        PEFile *pPeFile,
        PEImageLayout *pPeImageLayout,
        LPCUTF8 nativeImageName,
        LoaderAllocator *pLoaderAllocator);

    bool Matches(LPCUTF8 utf8SimpleName) const;

    Crst *EagerFixupsLock() { return &m_eagerFixupsLock; }
    bool EagerFixupsHaveRun() const  { return m_eagerFixupsHaveRun; }
    void SetEagerFixupsHaveRun() { m_eagerFixupsHaveRun = true; }

    uint32_t GetComponentAssemblyCount() const { return m_componentAssemblyCount; }
    ReadyToRunInfo *GetReadyToRunInfo() const { return m_pReadyToRunInfo; }
    IMDInternalImport *GetManifestMetadata() const { return m_pManifestMetadata; }

    Assembly *LoadComponentAssembly(uint32_t rowid);
    
    PTR_READYTORUN_CORE_HEADER GetComponentAssemblyHeader(const SString& assemblySimpleName);
    
private:
    IMDInternalImport *LoadManifestMetadata();
};

#endif
