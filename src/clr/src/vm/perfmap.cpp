// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: perfmap.cpp
//

#include "common.h"

#if defined(FEATURE_PERFMAP) && !defined(DACCESS_COMPILE)
#include "perfmap.h"
#include "pal.h"

PerfMap * PerfMap::s_Current = NULL;

// Initialize the map for the process - called from EEStartupHelper.
void PerfMap::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    // Only enable the map if requested.
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapEnabled))
    {
        // Get the current process id.
        int currentPid = GetCurrentProcessId();

        // Create the map.
        s_Current = new PerfMap(currentPid);
    }
}

// Destroy the map for the process - called from EEShutdownHelper.
void PerfMap::Destroy()
{
    LIMITED_METHOD_CONTRACT;

    if (s_Current != NULL)
    {
        delete s_Current;
        s_Current = NULL;
    }
}

// Construct a new map for the process.
PerfMap::PerfMap(int pid)
{
    LIMITED_METHOD_CONTRACT;

    // Initialize with no failures.
    m_ErrorEncountered = false;

    // Build the path to the map file on disk.
    WCHAR tempPath[MAX_LONGPATH+1];
    if(!GetTempPathW(MAX_LONGPATH, tempPath))
    {
        return;
    }
    
    SString path;
    path.Printf("%Sperf-%d.map", &tempPath, pid);

    // Open the map file for writing.
    OpenFile(path);
}

// Construct a new map without a specified file name.
// Used for offline creation of NGEN map files.
PerfMap::PerfMap()
{
    LIMITED_METHOD_CONTRACT;
}

// Clean-up resources.
PerfMap::~PerfMap()
{
    LIMITED_METHOD_CONTRACT;

    delete m_FileStream;
    m_FileStream = NULL;
}

// Open the specified destination map file.
void PerfMap::OpenFile(SString& path)
{
    STANDARD_VM_CONTRACT;

    // Open the file stream.
    m_FileStream = new (nothrow) CFileStream();
    if(m_FileStream != NULL)
    {
        HRESULT hr = m_FileStream->OpenForWrite(path.GetUnicode());
        if(FAILED(hr))
        {
            delete m_FileStream;
            m_FileStream = NULL;
        }
    }
}

// Write a line to the map file.
void PerfMap::WriteLine(SString& line)
{
    STANDARD_VM_CONTRACT;

    EX_TRY
    {
        // Write the line.
        // The PAL already takes a lock when writing, so we don't need to do so here.
        StackScratchBuffer scratch;
        const char * strLine = line.GetANSI(scratch);
        ULONG inCount = line.GetCount();
        ULONG outCount;
        m_FileStream->Write(strLine, inCount, &outCount);

        if (inCount != outCount)
        {
            // This will cause us to stop writing to the file.
            // The file will still remain open until shutdown so that we don't have to take a lock at this level when we touch the file stream.
            m_ErrorEncountered = true;
        }

    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

// Log a method to the map.
void PerfMap::LogMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pMethod != NULL);
        PRECONDITION(pCode != NULL);
        PRECONDITION(codeSize > 0);
    } CONTRACTL_END;

    if (m_FileStream == NULL || m_ErrorEncountered)
    {
        // A failure occurred, do not log.
        return;
    }

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        // Get the full method signature.
        SString fullMethodSignature;
        pMethod->GetFullMethodInfo(fullMethodSignature);

        // Build the map file line.
        StackScratchBuffer scratch;
        SString line;
        line.Printf("%p %x %s\n", pCode, codeSize, fullMethodSignature.GetANSI(scratch));

        // Write the line.
        WriteLine(line);
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

// Log a native image to the map.
void PerfMap::LogNativeImage(PEFile * pFile)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pFile != NULL);
    } CONTRACTL_END;

    if (m_FileStream == NULL || m_ErrorEncountered)
    {
        // A failure occurred, do not log.
        return;
    }

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        // Get the native image name.
        LPCUTF8 lpcSimpleName = pFile->GetSimpleName();

        // Get the native image signature.
        WCHAR wszSignature[39];
        GetNativeImageSignature(pFile, wszSignature, lengthof(wszSignature));

        SString strNativeImageSymbol;
        strNativeImageSymbol.Printf("%s.ni.%S", lpcSimpleName, wszSignature);

        // Get the base addess of the native image.
        SIZE_T baseAddress = (SIZE_T)pFile->GetLoaded()->GetBase();

        // Get the image size
        COUNT_T imageSize = pFile->GetLoaded()->GetVirtualSize();

        // Log baseAddress imageSize strNativeImageSymbol
        StackScratchBuffer scratch;
        SString line;
        line.Printf("%p %x %s\n", baseAddress, imageSize, strNativeImageSymbol.GetANSI(scratch));

        // Write the line.
        WriteLine(line);
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

// Log a native image load to the map.
void PerfMap::LogNativeImageLoad(PEFile * pFile)
{
    STANDARD_VM_CONTRACT;

    if (s_Current != NULL)
    {
        s_Current->LogNativeImage(pFile);
    }
}

// Log a method to the map.
void PerfMap::LogJITCompiledMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize)
{
    LIMITED_METHOD_CONTRACT;

    if (s_Current != NULL)
    {
        s_Current->LogMethod(pMethod, pCode, codeSize);
    }
}

void PerfMap::GetNativeImageSignature(PEFile * pFile, WCHAR * pwszSig, unsigned int nSigSize)
{
    CONTRACTL{
        PRECONDITION(pFile != NULL);
        PRECONDITION(pwszSig != NULL);
        PRECONDITION(nSigSize >= 39);
    } CONTRACTL_END;

    // We use the MVID as the signature, since ready to run images
    // don't have a native image signature.
    GUID mvid;
    pFile->GetMVID(&mvid);
    if(!StringFromGUID2(mvid, pwszSig, nSigSize))
    {
        pwszSig[0] = '\0';
    }
}

// Create a new native image perf map.
NativeImagePerfMap::NativeImagePerfMap(Assembly * pAssembly, BSTR pDestPath)
  : PerfMap()
{
    STANDARD_VM_CONTRACT;

    // Generate perfmap path.

    // Get the assembly simple name.
    LPCUTF8 lpcSimpleName = pAssembly->GetSimpleName();

    // Get the native image signature (GUID).
    // Used to ensure that we match symbols to the correct NGEN image.
    WCHAR wszSignature[39];
    GetNativeImageSignature(pAssembly->GetManifestFile(), wszSignature, lengthof(wszSignature));

    // Build the path to the perfmap file, which consists of <inputpath><imagesimplename>.ni.<signature>.map.
    // Example: /tmp/mscorlib.ni.{GUID}.map
    SString sDestPerfMapPath;
    sDestPerfMapPath.Printf("%S%s.ni.%S.map", pDestPath, lpcSimpleName, wszSignature);

    // Open the perf map file.
    OpenFile(sDestPerfMapPath);
}

// Log data to the perfmap for the specified module.
void NativeImagePerfMap::LogDataForModule(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    PEImageLayout * pLoadedLayout = pModule->GetFile()->GetLoaded();
    _ASSERTE(pLoadedLayout != NULL);

    SIZE_T baseAddr = (SIZE_T)pLoadedLayout->GetBase();

#ifdef FEATURE_READYTORUN_COMPILER
    if (pLoadedLayout->HasReadyToRunHeader())
    {
        ReadyToRunInfo::MethodIterator mi(pModule->GetReadyToRunInfo());
        while (mi.Next())
        {
            MethodDesc *hotDesc = mi.GetMethodDesc();

            LogPreCompiledMethod(hotDesc, mi.GetMethodStartAddress(), baseAddr);
        }
    }
    else
#endif // FEATURE_READYTORUN_COMPILER
    {
        MethodIterator mi((PTR_Module)pModule);
        while (mi.Next())
        {
            MethodDesc *hotDesc = mi.GetMethodDesc();
            hotDesc->CheckRestore();

            LogPreCompiledMethod(hotDesc, mi.GetMethodStartAddress(), baseAddr);
        }
    }
}

// Log a pre-compiled method to the perfmap.
void NativeImagePerfMap::LogPreCompiledMethod(MethodDesc * pMethod, PCODE pCode, SIZE_T baseAddr)
{
    STANDARD_VM_CONTRACT;

    // Get information about the NGEN'd method code.
    EECodeInfo codeInfo(pCode);
    _ASSERTE(codeInfo.IsValid());

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    // NGEN can split code between hot and cold sections which are separate in memory.
    // Emit an entry for each section if it is used.
    if (methodRegionInfo.hotSize > 0)
    {
        LogMethod(pMethod, (PCODE)methodRegionInfo.hotStartAddress - baseAddr, methodRegionInfo.hotSize);
    }

    if (methodRegionInfo.coldSize > 0)
    {
        LogMethod(pMethod, (PCODE)methodRegionInfo.coldStartAddress - baseAddr, methodRegionInfo.coldSize);
    }
}

#endif // FEATURE_PERFMAP && !DACCESS_COMPILE
