// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "simpletimer.h"
#include "mclist.h"
#include "lightweightmap.h"
#include "commandline.h"
#include "errorhandling.h"
#include "methodcontext.h"
#include "methodcontextreader.h"
#include "spmiutil.h"
#include "fileio.h"
#include "commandline.h"

#if defined(_WIN32)
#define strtok_r strtok_s
#endif

static bool AddJitOption(LightWeightMap<DWORD,DWORD>* map, char* newOption)
{
    WCHAR* key;
    WCHAR* value;

    if (!CommandLine::ParseJitOption(newOption, &key, &value))
    {
        return false;
    }

    DWORD keyIndex =
        (DWORD)map->AddBuffer((unsigned char*)key, sizeof(WCHAR) * ((unsigned int)u16_strlen(key) + 1));
    DWORD valueIndex =
        (DWORD)map->AddBuffer((unsigned char*)value, sizeof(WCHAR) * ((unsigned int)u16_strlen(value) + 1));
    map->Add(keyIndex, valueIndex);

    delete[] key;
    delete[] value;
    
    return true;
}

struct CacheEntry
{
    MethodContext* mc;
    int age;
};

static MethodContext* getMethodContext(int index, MethodContextReader* reader)
{
    enum { CACHE_SIZE = 100 };
    static CacheEntry cache[CACHE_SIZE] = {};
    static int count = 0;
    static int age = 0;
    int i = 0;

    // Search the cache
    //
    for (; i < count; i++)
    {
        if (cache[i].mc->index == index)
        {
            break;
        }
    }

    if (i == count)
    {
        // Method not found in cache
        //
        LogDebug("[streaming] loading MC %i from file", index);
        if (i == CACHE_SIZE)
        {
            // Cache is full, evict oldest entry
            //
            int oldestAge = age;
            int oldestEntry = -1;
            for (int j = 0; j < CACHE_SIZE; j++)
            {
                if (cache[j].age < oldestAge)
                {
                    oldestEntry = j;
                    oldestAge = cache[j].age;
                }
            }

            LogDebug("[streaming] evicting MC %i from cache", cache[oldestEntry].mc->index);
            delete cache[oldestEntry].mc;
            cache[oldestEntry].mc = nullptr;
            i = oldestEntry;
        }
        else
        {
            count++;
        }

        reader->Reset(&index, 1);
        MethodContextBuffer mcb = reader->GetNextMethodContext();

        if (mcb.Error())
        {
            return nullptr;
        }

        MethodContext* mc = nullptr;
        if (!MethodContext::Initialize(index, mcb.buff, mcb.size, &mc))
        {
            return nullptr;
        }

        cache[i].mc = mc;
    }
    else
    {
        LogDebug("[streaming] found MC %i in cache", index);
    }

    // Move to front...
    //
    if (i != 0)
    {
        CacheEntry temp = cache[0];
        cache[0] = cache[i];
        cache[i] = temp;
    }

    cache[0].age = age++;
    return cache[0].mc;
}

int doStreamingSuperPMI(CommandLine::Options& o)
{
    HRESULT     hr = E_FAIL;
    SimpleTimer st;
    st.Start();

    FILE* streamFile = nullptr;
    if (_stricmp(o.streamFile, "stdin") == 0)
    {
        streamFile = stdin;
    }
    else 
    {
        streamFile = fopen(o.streamFile, "r");
    }

    if (streamFile == nullptr)
    {
        LogError("Failed to open file '%s'. GetLastError()=%u", o.streamFile, GetLastError());
        return 1;
    }

    // Just one worker for now... all method selection done via stream file
    //
    o.workerCount = 1;
    o.indexes = nullptr;
    o.indexCount = -1;
    o.hash = nullptr;
    o.offset = -1;
    o.increment = -1;

    // The method context reader handles skipping any unrequested method contexts
    // Used in conjunction with an MCI file, it does a lot less work...
    MethodContextReader* reader =
        new MethodContextReader(o.nameOfInputMethodContextFile, o.indexes, o.indexCount, o.hash, o.offset, o.increment);
    if (!reader->isValid())
    {
        return (int)SpmiResult::GeneralFailure;
    }

    JitInstance* jit = nullptr;

    enum { BUFFER_SIZE = 2048 };

    char line[BUFFER_SIZE];
    const char* const seps = "!";
    char *next = nullptr;

    // Syntax is dddd { ! <jit-option>=value }*
    // Likes starting with '#' are ignored
    //
    while (fgets(line, BUFFER_SIZE, streamFile) != nullptr)
    {
        for (int i = 0; i < BUFFER_SIZE; i++)
        {
            if (line[i] == '\n' || line[i] == '\r')
            {
                line[i]= 0;
                break;
            }
        }
        line[BUFFER_SIZE - 1] = '0';

        LogDebug("[streaming] Request: '%s'", line);

        if (line[0] == '#')
        {
            continue;
        }

        if (strncmp(line, "quit", 4) == 0)
        {
            LogDebug("[streaming] Quitting");
            break;
        }

        char* tok = strtok_r(line, seps, &next);
        const int index = atoi(tok);

        if (index == 0)
        {
            LogDebug("[streaming] Stopping");
            break;
        }

        LogDebug("[streaming] Method %d", index);

        LightWeightMap<DWORD,DWORD>* baseForceJitOptions = o.forceJitOptions;
        LightWeightMap<DWORD,DWORD>* forceJitOptions = nullptr;
        bool skip = false;

        while ((tok = strtok_r(nullptr, seps, &next)))
        {
            if (forceJitOptions == nullptr)
            {
                if (baseForceJitOptions == nullptr)
                {
                    forceJitOptions = new LightWeightMap<DWORD, DWORD>();
                }
                else
                {
                    forceJitOptions = new LightWeightMap<DWORD, DWORD>(*baseForceJitOptions);
                }
            }

            bool added = AddJitOption(forceJitOptions, tok);

            if (!added)
            {
                LogInfo("[streaming] unable to parse option '%s'", tok);
                skip = true;
                break;
            }
        }

        if (skip)
        {
            continue;
        }

        LogDebug("[streaming] Launching...");
        MethodContext* const mc = getMethodContext(index, reader);

        if (mc == nullptr)
        {
            return (int)SpmiResult::GeneralFailure;
        }

        if (mc->index != index)
        {
            LogDebug("MC cache lookup failure, wanted index %d, got index %d\n", index, mc->index);
            return (int)SpmiResult::GeneralFailure;
        }

        if (jit == nullptr)
        {    
            LogDebug("[streaming] loading jit %s", o.nameOfJit);
            SimpleTimer stInitJit;
            jit = JitInstance::InitJit(o.nameOfJit, o.breakOnAssert, &stInitJit, mc, forceJitOptions, o.jitOptions);
            
            if (jit == nullptr)
            {
                // InitJit already printed a failure message
                return (int)SpmiResult::JitFailedToInit;
            }
        }
        else
        {
            jit->updateForceOptions(forceJitOptions);
            jit->resetConfig(mc);
        }

        LogDebug("[streaming] invoking jit");
        fflush(stdout);

        bool collectThroughput = false;
        ReplayResults res = jit->CompileMethod(mc, reader->GetMethodContextIndex(), collectThroughput);

        if (res.Result == ReplayResult::Success)
        {
            if (Logger::IsLogLevelEnabled(LOGLEVEL_DEBUG))
            {
                mc->cr->dumpToConsole(); // Dump the compile results if doing debug logging
            }
        }
        else if (res.Result == ReplayResult::Error)
        {
            LogDebug("[streaming] jit compilation failed");

            LogError("Method %d of size %d failed to load and compile correctly%s (%s).",
                reader->GetMethodContextIndex(), mc->methodSize,
                (o.nameOfJit2 == nullptr) ? "" : " by JIT1", o.nameOfJit);
        }

        // Protocol with clients is for them to read stdout. Let them know we're done.
        //
        printf("[streaming] Done. Status=%d\n", (int) res.Result);
        fflush(stdout);

        // Cleanup
        //
        delete forceJitOptions;
        mc->Reset();
    }

    return (int)SpmiResult::Success;
}
