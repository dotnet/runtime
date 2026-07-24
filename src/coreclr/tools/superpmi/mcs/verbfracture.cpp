// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbfracture.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"
#include "logging.h"

int verbFracture::DoWork(
    const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes, bool stripCR)
{
    int rangeSize = indexes[0];

    LogVerbose("Reading from '%s' copying %d MethodContexts files into each output file of '%s'", nameOfInput,
               rangeSize, nameOfOutput);

    MethodContextIterator mci(true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int  fileCount = 0;
    char fileName[512];

    FILE* fpOut = NULL;
    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();

        if ((fpOut == NULL) || (((mci.MethodContextNumber() - 1) % rangeSize) == 0))
        {
            if (fpOut != NULL)
            {
                if (fclose(fpOut) != 0)
                {
                    LogError("1st CloseHandle failed. errno=%d", errno);
                    return -1;
                }
                fpOut = NULL;
            }
            sprintf_s(fileName, 512, "%s-%0*d.mch", nameOfOutput, 5, fileCount++);
            fpOut = fopen(fileName, "wb");
            if (fpOut == NULL)
            {
                LogError("Failed to open output file '%s'. errno=%d", fileName, errno);
                return -1;
            }
        }
        if (stripCR)
        {
            delete mc->cr;
            mc->cr = new CompileResult();
        }
        mc->saveToFile(fpOut);
    }

    if (fpOut != NULL)
    {
        if (fclose(fpOut) != 0)
        {
            LogError("2nd CloseHandle failed. errno=%d", errno);
            return -1;
        }
    }

    LogInfo("Output fileCount %d", fileCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
