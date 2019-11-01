//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbdumptoc.h"
#include "methodcontext.h"
#include "tocfile.h"
#include "runtimedetails.h"

int verbDumpToc::DoWork(const char* nameOfInput)
{
    TOCFile tf;

    tf.LoadToc(nameOfInput, false);

    for (size_t i = 0; i < tf.GetTocCount(); i++)
    {
        const TOCElement* te = tf.GetElementPtr(i);
        printf("%4u: %016llX ", te->Number, te->Offset);

        for (size_t j = 0; j < sizeof(te->Hash); j++)
        {
            printf("%02x ", te->Hash[j]);
        }

        printf("\n");
    }

    return 0;
}
