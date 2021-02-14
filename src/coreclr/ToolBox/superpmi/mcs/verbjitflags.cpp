//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "verbjitflags.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "errorhandling.h"
#include "corjitflags.h"

int verbJitFlags::DoWork(const char* nameOfInput)
{
    MethodContextIterator mci;
    if (!mci.Initialize(nameOfInput))
        return -1;

    LightWeightMap<unsigned long long, unsigned> flagMap;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        CORJIT_FLAGS corJitFlags;
        mc->repGetJitFlags(&corJitFlags, sizeof(corJitFlags));
        unsigned long long rawFlags = corJitFlags.GetFlagsRaw();

        int index = flagMap.GetIndex(rawFlags);
        if (index == -1)
        {
            flagMap.Add(rawFlags, 1);
        }
        else
        {
            int oldVal = flagMap.GetItem(index);
            flagMap.Update(index, oldVal + 1);
        }
    }

    if (!mci.Destroy())
        return -1;

    printf("%16s,%8s, parsed\n", "bits", "count");

    const unsigned int count = flagMap.GetCount();
    unsigned long long* pFlag = flagMap.GetRawKeys();
    for (unsigned int i = 0; i < count; i++)
    {
        const unsigned long long flag = *pFlag++;
        const int index = flagMap.GetIndex(flag);

        printf("%016llx,%8u", flag, flagMap.GetItem(index));

        for (int flagBit = 63; flagBit >= 0; flagBit--)
        {
            if (((flag >> flagBit) & 1ull) == 1ull)
            {
                switch (flagBit)
                {
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_SPEED_OPT: printf(", SPEED_OPT"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_SIZE_OPT: printf(", SIZE_OPT"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_DEBUG_CODE: printf(", DEBUG_CODE"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_DEBUG_EnC: printf(", DEBUG_EnC"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_DEBUG_INFO: printf(", DEBUG_INFO"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_MIN_OPT: printf(", MIN_OPT"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_MCJIT_BACKGROUND: printf(", MCJIT_BACKGROUND"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_OSR: printf(", OSR"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_ALT_JIT: printf(", ALT_JIT"); break;

                    case 17: printf(", FEATURE_SIMD"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_MAKEFINALCODE: printf(", MAKEFINALCODE"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_READYTORUN: printf(", READYTORUN"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_PROF_ENTERLEAVE: printf(", PROF_ENTERLEAVE"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE: printf(", NO_PINVOKE_INLINE"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_SKIP_VERIFICATION: printf(", SKIP_VERIFICATION"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_PREJIT: printf(", PREJIT"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_RELOC: printf(", RELOC"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_IMPORT_ONLY: printf(", IMPORT_ONLY"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_IL_STUB: printf(", IL_STUB"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_PROCSPLIT: printf(", PROCSPLIT"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_BBINSTR: printf(", BBINSTR"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_BBOPT: printf(", BBOPT"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_FRAMED: printf(", FRAMED"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_PUBLISH_SECRET_PARAM: printf(", PUBLISH_SECRET_PARAM"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_SAMPLING_JIT_BACKGROUND: printf(", SAMPLING_JIT_BACKGROUND"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_USE_PINVOKE_HELPERS: printf(", USE_PINVOKE_HELPERS"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_REVERSE_PINVOKE: printf(", REVERSE_PINVOKE"); break;                    
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_TRACK_TRANSITIONS: printf(", TRACK_TRANSITIONS"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_TIER0: printf(", TIER0"); break;
                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_TIER1: printf(", TIER1"); break;

                    case CORJIT_FLAGS::CorJitFlag::CORJIT_FLAG_NO_INLINING: printf(", NO_INLINING"); break;
                    default:
                        printf(", ?_%02u_?", flagBit);
                        break;
                }
            }
        }

        printf("\n");
    }

    return 0;
}

