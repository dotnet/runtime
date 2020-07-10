// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "gcevent_serializers.h"

// Uncomment this define to print out event state changes to standard error.
// #define TRACE_GC_EVENT_STATE 1


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
        assert((level >= GCEventLevel_None && level < GCEventLevel_Max) || level == GCEventLevel_LogAlways);

        size_t index = static_cast<size_t>(provider);

        enabledLevels[index] = level;
        enabledKeywords[index] = keywords;

#if TRACE_GC_EVENT_STATE
        fprintf(stderr, "event state change:\n");
        DebugDumpState(provider);
#endif // TRACE_GC_EVENT_STATE
    }

    /*
     * Returns currently enabled levels
     */
    static inline GCEventLevel GetEnabledLevel(GCEventProvider provider)
    {
        return enabledLevels[static_cast<size_t>(provider)].LoadWithoutBarrier();
    }

    /*
     * Returns currently enabled keywords in GCPublic
     */
    static inline GCEventKeyword GetEnabledKeywords(GCEventProvider provider)
    {
        return enabledKeywords[static_cast<size_t>(provider)].LoadWithoutBarrier();
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
        case GCEventLevel_LogAlways:
            fprintf(stderr, "  level: LogAlways");
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

/*
 * FireDynamicEvent is a variadic function that fires a dynamic event with the
 * given name and event payload. This function serializes the arguments into
 * a binary payload that is then passed to IGCToCLREventSink::FireDynamicEvent.
 */
template<typename... EventArgument>
void FireDynamicEvent(const char* name, EventArgument... arguments)
{
    size_t size = gc_event::SerializedSize(arguments...);
    if (size > UINT32_MAX)
    {
        // ETW can't handle anything this big.
        // we shouldn't be firing events that big anyway.
        return;
    }

    uint8_t* buf = new (nothrow) uint8_t[size];
    if (!buf)
    {
        // best effort - if we're OOM, don't bother with the event.
        return;
    }

    memset(buf, 0, size);
    uint8_t* cursor = buf;
    gc_event::Serialize(&cursor, arguments...);
    IGCToCLREventSink* sink = GCToEEInterface::EventSink();
    assert(sink != nullptr);
    sink->FireDynamicEvent(name, buf, static_cast<uint32_t>(size));
    delete[] buf;
};

/*
 * In order to provide a consistent interface between known and dynamic events,
 * two wrapper functions are generated for each known and dynamic event:
 *   GCEventEnabled##name() - Returns true if the event is enabled, false otherwise.
 *   GCEventFire##name(...) - Fires the event, with the event payload consisting of
 *                            the arguments to the function.
 *
 * Because the schema of dynamic events comes from the DYNAMIC_EVENT xmacro, we use
 * the arguments vector as the argument list to `FireDynamicEvent`, which will traverse
 * the list of arguments and call `IGCToCLREventSink::FireDynamicEvent` with a serialized
 * payload. Known events will delegate to IGCToCLREventSink::Fire##name.
 */
#if FEATURE_EVENT_TRACE

#define KNOWN_EVENT(name, provider, level, keyword)               \
  inline bool GCEventEnabled##name() { return GCEventStatus::IsEnabled(provider, keyword, level); } \
  template<typename... EventActualArgument>                       \
  inline void GCEventFire##name(EventActualArgument... arguments) \
  {                                                               \
      if (GCEventEnabled##name())                                 \
      {                                                           \
          IGCToCLREventSink* sink = GCToEEInterface::EventSink(); \
          assert(sink != nullptr);                                \
          sink->Fire##name(arguments...);                         \
      }                                                           \
  }

#define DYNAMIC_EVENT(name, level, keyword, ...)                                                                   \
  inline bool GCEventEnabled##name() { return GCEventStatus::IsEnabled(GCEventProvider_Default, keyword, level); } \
  template<typename... EventActualArgument>                                                                        \
  inline void GCEventFire##name(EventActualArgument... arguments) { FireDynamicEvent<__VA_ARGS__>(#name, arguments...); }

#include "gcevents.h"

#define EVENT_ENABLED(name) GCEventEnabled##name()
#define FIRE_EVENT(name, ...) GCEventFire##name(__VA_ARGS__)

#else // FEATURE_EVENT_TRACE
#define EVENT_ENABLED(name) false
#define FIRE_EVENT(name, ...) 0
#endif // FEATURE_EVENT_TRACE

#endif // __GCEVENTSTATUS_H__
