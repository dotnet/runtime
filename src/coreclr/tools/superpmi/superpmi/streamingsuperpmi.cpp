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

#if defined(_WIN32)
#define strtok_r strtok_s
#endif

static bool ParseJitOption(const char* optionString, WCHAR** key, WCHAR** value)
{
    char tempKey[1024];

    unsigned i;
    for (i = 0; (optionString[i] != '=') && (optionString[i] != '#'); i++)
    {
        if ((i >= 1023) || (optionString[i] == '\0'))
        {
            return false;
        }
        tempKey[i] = optionString[i];
    }
    tempKey[i] = '\0';

    const char* tempVal = &optionString[i + 1];

    const unsigned keyLen = i;
    WCHAR*       keyBuf = new WCHAR[keyLen + 1];
    MultiByteToWideChar(CP_UTF8, 0, tempKey, keyLen + 1, keyBuf, keyLen + 1);

    const unsigned valLen = (unsigned)strlen(tempVal);
    WCHAR*       valBuf = new WCHAR[valLen + 1];
    MultiByteToWideChar(CP_UTF8, 0, tempVal, valLen + 1, valBuf, valLen + 1);

    LogDebug("[streaming] option '%S=%S'", keyBuf, valBuf);

    *key   = keyBuf;
    *value = valBuf;
    return true;
}

static bool AddJitOption(LightWeightMap<DWORD,DWORD>* map, char* newOption)
{
    WCHAR* key;
    WCHAR* value;

    if (!ParseJitOption(newOption, &key, &value))
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

    char line[2048];
    const char* const seps = "!";
    char *next = nullptr;

    // Syntax is dddd { ! <jit-option>=value }*
    // Likes starting with '#' are ignored
    //
    while (fgets(line, sizeof(line), streamFile) != nullptr)
    {
        for (int i = 0; i < sizeof(line); i++)
        {
            if (line[i] == '\n' || line[i] == '\r')
            {
                line[i]= 0;
                break;
            }
        }

        size_t len = strlen(line);

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

        // launch this instance...
        //
        reader->Reset(&index, 1);
        MethodContextBuffer mcb = reader->GetNextMethodContext();

        if (mcb.Error())
        {
            return (int)SpmiResult::GeneralFailure;
        }

        MethodContext* mc = nullptr;
        if (!MethodContext::Initialize(index, mcb.buff, mcb.size, &mc))
        {
            return (int)SpmiResult::GeneralFailure;
        }

        if (jit == nullptr)
        {    
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

        bool collectThroughput = false;
        ReplayResults res = jit->CompileMethod(mc, reader->GetMethodContextIndex(), collectThroughput);

        // Protocol with clients is for them to read stdout. Let them know we're done.
        //
        printf("[streaming] Done.\n");
        fflush(stdout);

        if (res.Result == ReplayResult::Success)
        {
            if (Logger::IsLogLevelEnabled(LOGLEVEL_DEBUG))
            {
                mc->cr->dumpToConsole(); // Dump the compile results if doing debug logging
            }
        }
        else if (res.Result == ReplayResult::Error)
        {
            LogError("Method %d of size %d failed to load and compile correctly%s (%s).",
                reader->GetMethodContextIndex(), mc->methodSize,
                (o.nameOfJit2 == nullptr) ? "" : " by JIT1", o.nameOfJit);
            return (int)SpmiResult::Error;
        }

        delete forceJitOptions;
    }

    return 0;
}
