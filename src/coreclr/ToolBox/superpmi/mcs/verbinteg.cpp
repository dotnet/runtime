// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbinteg.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"

int verbInteg::DoWork(const char* nameOfInput)
{
    LogVerbose("Checking the integrity of '%s'", nameOfInput);

    SimpleTimer st2;
    st2.Start();

    MethodContextIterator mci(true);
    if (!mci.Initialize(nameOfInput))
        return -1;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        // Nothing to do except load the current one.
    }

    st2.Stop();
    LogInfo("Checked the integrity of %d methodContexts at %d per second", mci.MethodContextNumber(),
            (int)((double)mci.MethodContextNumber() / st2.GetSeconds()));

    if (!mci.Destroy())
        return -1;

    return 0;
}
