// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NATIVEIMAGE_H
#define _NATIVEIMAGE_H

#include "readytoruninfo.h"

/// <summary>
/// This structure is used in NativeImage to map simple names of component assemblies
/// to their indices within the component assembly header table.
/// </summary>
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

class NativeImage
{
private:
    AssemblyLoadContext *m_pLoadContext;
    LPCUTF8 m_utf8SimpleName;
    uint32_t m_utf8SimpleNameLength;
    bool m_runEagerFixups;
    
    NewHolder<ReadyToRunInfo> m_pReadyToRunInfo;
    IMDInternalImport *m_pManifestMetadata;
    PEImage *m_pPeImage;
    
    IMAGE_DATA_DIRECTORY *m_pComponentAssemblies;
    uint32_t m_componentAssemblyCount;
    SHash<AssemblyNameIndexHashTraits> m_assemblySimpleNameToIndexMap;

private:
    NativeImage(
        PEFile *peFile,
        PEImage *peImage,
        READYTORUN_HEADER *header,
        LPCUTF8 nativeImageName,
        uint8_t nativeImageNameLength,
        LoaderAllocator *loaderAllocator,
        AllocMemTracker& amTracker);

public:
    static NativeImage *Open(
        PEFile *pPeFile,
        PEImage *pPeImage,
        LPCUTF8 nativeImageName,
        uint8_t nativeImageNameLength,
        LoaderAllocator *pLoaderAllocator);

    bool Matches(LPCUTF8 utf8SimpleName, uint32_t utf8Length, const AssemblyLoadContext *pLoadContext) const;

    bool EagerFixupsNeedToRun();

    uint32_t GetComponentAssemblyCount() const { return m_componentAssemblyCount; }
    ReadyToRunInfo *GetReadyToRunInfo() const { return m_pReadyToRunInfo; }
    IMDInternalImport *GetManifestMetadata() const { return m_pManifestMetadata; }

    AssemblyLoadContext *GetAssemblyLoadContext() const { return m_pLoadContext; }
    Assembly *LoadComponentAssembly(uint32_t rowid);
    
    PTR_READYTORUN_CORE_HEADER GetComponentAssemblyHeader(const SString& assemblySimpleName);
};

#endif
