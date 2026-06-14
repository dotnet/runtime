// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//-----------------------------------------------------------------------------
// MethodStatsEmitter.cpp - Emits useful method stats for compiled methods for analysis
//-----------------------------------------------------------------------------

#include "standardpch.h"
#include "methodstatsemitter.h"
#include "logging.h"

MethodStatsEmitter::MethodStatsEmitter(char* nameOfInput)
{
    char filename[MAX_PATH + 1];
    sprintf_s(filename, MAX_PATH + 1, "%s.stats", nameOfInput);

    fpStatsFile = fopen(filename, "w");
    if (fpStatsFile == NULL)
    {
        LogError("Failed to open output file '%s'. errno=%d", filename, errno);
    }
}

MethodStatsEmitter::~MethodStatsEmitter()
{
    if (fpStatsFile != NULL)
    {
        if (fclose(fpStatsFile) != 0)
        {
            LogError("fclose failed. errno=%d", errno);
        }
    }
}

void MethodStatsEmitter::Emit(int methodNumber, MethodContext* mc, ULONGLONG firstTime, ULONGLONG secondTime)
{
    if (fpStatsFile != NULL)
    {
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'h') != NULL || strchr(statsTypes, 'H') != NULL)
        {
            // Obtain the method Hash
            char md5Hash[MM3_HASH_BUFFER_SIZE];
            if (mc->dumpMethodHashToBuffer(md5Hash, MM3_HASH_BUFFER_SIZE) != MM3_HASH_BUFFER_SIZE)
                md5Hash[0] = 0;

            fprintf(fpStatsFile, "%s,", md5Hash);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'n') != NULL || strchr(statsTypes, 'N') != NULL)
        {
            fprintf(fpStatsFile, "%d,", methodNumber);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'i') != NULL || strchr(statsTypes, 'I') != NULL)
        {
            // Obtain the IL code size for this method
            CORINFO_METHOD_INFO info;
            unsigned            flags = 0;
            CORINFO_OS          os    = CORINFO_WINNT;
            mc->repCompileMethod(&info, &flags, &os);

            fprintf(fpStatsFile, "%d,", info.ILCodeSize);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'a') != NULL || strchr(statsTypes, 'A') != NULL)
        {
            // Obtain the compiled method ASM size
            BYTE*        temp;
            DWORD        codeSize;
            CorJitResult result;
            if (mc->cr->CompileMethod != nullptr)
                mc->cr->repCompileMethod(&temp, &codeSize, &result);
            else
                codeSize = 0; // this is likely a thin mc

            fprintf(fpStatsFile, "%d,", codeSize);
        }
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 't') != NULL || strchr(statsTypes, 'T') != NULL)
        {
            fprintf(fpStatsFile, "%llu,%llu,", (unsigned long long)firstTime, (unsigned long long)secondTime);
        }

        fprintf(fpStatsFile, "\n");
    }
}

void MethodStatsEmitter::SetStatsTypes(char* types)
{
    statsTypes = types;

    if (fpStatsFile != INVALID_HANDLE_VALUE)
    {
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'h') != NULL || strchr(statsTypes, 'H') != NULL)
            fprintf(fpStatsFile, "HASH,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'n') != NULL || strchr(statsTypes, 'N') != NULL)
            fprintf(fpStatsFile, "METHOD_NUMBER,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'i') != NULL || strchr(statsTypes, 'I') != NULL)
            fprintf(fpStatsFile, "IL_CODE_SIZE,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 'a') != NULL || strchr(statsTypes, 'A') != NULL)
            fprintf(fpStatsFile, "ASM_CODE_SIZE,");
        if (strchr(statsTypes, '*') != NULL || strchr(statsTypes, 't') != NULL || strchr(statsTypes, 'T') != NULL)
            fprintf(fpStatsFile, "Time1,Time2,");

        fprintf(fpStatsFile, "\n");
    }
}
