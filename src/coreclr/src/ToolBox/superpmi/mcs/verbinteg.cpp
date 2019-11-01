//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
