// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCEVENTSTATUS_H__
#define __GCEVENTSTATUS_H__


/*
 * gceventstatus.h - Eventing status for a standalone GC
 *
 * In order for a local GC to determine what events are enabled
 * in an efficient manner, the GC maintains some local state about
 * keywords and levels that are enabled for each eventing provider.
 *
 * The GC fires events from two providers: the "main" provider
 * and the "private" provider. This file tracks keyword and level
 * information for each provider separately.
 *
 * It is the responsibility of the EE to inform the GC of changes
 * to eventing state. This is accomplished by invoking the
 * `IGCHeap::ControlEvents` and `IGCHeap::ControlPrivateEvents` callbacks
 * on the EE's heap instance, which ultimately will enable and disable keywords
 * and levels within this file.
 */

#include "common.h"
#include "gcenv.h"
#include "gc.h"

// Uncomment this define to print out event state changes to standard error.
// #define TRACE_GC_EVENT_STATE 1

/*
 * GCEventProvider represents one of the two providers that the GC can
 * fire events from: the default and private providers.
 */
enum GCEventProvider
{
    GCEventProvider_Default = 0,
    GCEventProvider_Private = 1
};

/*
 * GCEventStatus maintains all eventing state for the GC. It consists
 * of a keyword bitmask and level for each provider that the GC can use
 * to fire events.
 *
 * A level and event pair are considered to be "enabled" on a given provider
 * if the given level is less than or equal to the current enabled level
 * and if the keyword is present in the enabled keyword bitmask for that
 * provider.
 */
class GCEventStatus
{
private:
    /*
     * The enabled level for each provider.
     */
    static Volatile<GCEventLevel> enabledLevels[2];

    /*
     * The bitmap of enabled keywords for each provider.
     */
    static Volatile<GCEventKeyword> enabledKeywords[2];

public:
    /*
     * IsEnabled queries whether or not the given level and keyword are
     * enabled on the given provider, returning true if they are.
     */
    __forceinline static bool IsEnabled(GCEventProvider provider, GCEventKeyword keyword, GCEventLevel level)
    {
        assert(level >= GCEventLevel_None && level < GCEventLevel_Max);

        size_t index = static_cast<size_t>(provider);
        return (enabledLevels[index].LoadWithoutBarrier() >= level)
          && (enabledKeywords[index].LoadWithoutBarrier() & keyword);
    }

    /*
     * Set sets the eventing state (level and keyword bitmap) for a given
     * provider to the provided values.
     */
    static void Set(GCEventProvider provider, GCEventKeyword keywords, GCEventLevel level)
    {
        assert(level >= GCEventLevel_None && level < GCEventLevel_Max);

        size_t index = static_cast<size_t>(provider);

        enabledLevels[index] = level;
        enabledKeywords[index] = keywords;

#if TRACE_GC_EVENT_STATE
        fprintf(stderr, "event state change:\n");
        DebugDumpState(provider);
#endif // TRACE_GC_EVENT_STATE
    }

#if TRACE_GC_EVENT_STATE
private:
    static void DebugDumpState(GCEventProvider provider)
    {
        size_t index = static_cast<size_t>(provider);
        GCEventLevel level = enabledLevels[index];
        GCEventKeyword keyword = enabledKeywords[index];
        if (provider == GCEventProvider_Default)
        {
            fprintf(stderr, "provider: default\n");
        }
        else
        {
            fprintf(stderr, "provider: private\n");
        }

        switch (level)
        {
        case GCEventLevel_None:
            fprintf(stderr, "  level: None\n");
            break;
        case GCEventLevel_Fatal:
            fprintf(stderr, "  level: Fatal\n");
            break;
        case GCEventLevel_Error:
            fprintf(stderr, "  level: Error\n");
            break;
        case GCEventLevel_Warning:
            fprintf(stderr, "  level: Warning\n");
            break;
        case GCEventLevel_Information:
            fprintf(stderr, "  level: Information\n");
            break;
        case GCEventLevel_Verbose:
            fprintf(stderr, "  level: Verbose\n");
            break;
        default:
            fprintf(stderr, "  level: %d?\n", level);
            break;
        }

        fprintf(stderr, "  keywords: ");
        if (keyword & GCEventKeyword_GC)
        {
            fprintf(stderr, "GC ");
        }

        if (keyword & GCEventKeyword_GCHandle)
        {
            fprintf(stderr, "GCHandle ");
        }

        if (keyword & GCEventKeyword_GCHeapDump)
        {
            fprintf(stderr, "GCHeapDump ");
        }

        if (keyword & GCEventKeyword_GCSampledObjectAllocationHigh)
        {
            fprintf(stderr, "GCSampledObjectAllocationHigh ");
        }

        if (keyword & GCEventKeyword_GCHeapSurvivalAndMovement)
        {
            fprintf(stderr, "GCHeapSurvivalAndMovement ");
        }

        if (keyword & GCEventKeyword_GCHeapCollect)
        {
            fprintf(stderr, "GCHeapCollect ");
        }

        if (keyword & GCEventKeyword_GCHeapAndTypeNames)
        {
            fprintf(stderr, "GCHeapAndTypeNames ");
        }

        if (keyword & GCEventKeyword_GCSampledObjectAllocationLow)
        {
            fprintf(stderr, "GCSampledObjectAllocationLow ");
        }

        fprintf(stderr, "\n");
    }
#endif // TRACE_GC_EVENT_STATUS

    // This class is a singleton and can't be instantiated.
    GCEventStatus() = delete;
};

class GCDynamicEvent
{
    /* TODO(segilles) - Not Yet Implemented */
};

#if FEATURE_EVENT_TRACE
#define KNOWN_EVENT(name, _provider, _level, _keyword)   \
  inline bool GCEventEnabled##name() { return GCEventStatus::IsEnabled(_provider, _level, _keyword); }
#include "gcevents.h"

#define EVENT_ENABLED(name) GCEventEnabled##name()
#define FIRE_EVENT(name, ...) \
  do {                                                      \
    IGCToCLREventSink* sink = GCToEEInterface::EventSink(); \
    assert(sink != nullptr);                                \
    sink->Fire##name(__VA_ARGS__);                          \
  } while(0)
#else
#define EVENT_ENABLED(name) false
#define FIRE_EVENT(name, ...) 0
#endif // FEATURE_EVENT_TRACE

#endif // __GCEVENTSTATUS_H__
