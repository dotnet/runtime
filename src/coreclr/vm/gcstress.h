// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// #Overview
//
// This file provides convenient wrappers for the GC stress functionality.
//
// Exposed APIs:
//  GCStressPolicy::InhibitHolder
//  GCStressPolicy::GlobalEnable()
//  GCStressPolicy::GlobalDisable()
//  GCStressPolicy::IsEnabled()
//
//  GCStress<> template classes with its IsEnabled() & MaybeTrigger members.
//
//  Use GCStress<> to abstract away the GC stress related decissions. The
//  template definitions will resolve to nothing when STRESS_HEAP is not
//  defined, and will inline the function body at the call site otherwise.
//
// Examples:
//  GCStress<cfg_any>::IsEnabled()
//  GCStress<cfg_any, EeconfigFastGcSPolicy, CoopGcModePolicy>::MaybeTrigger()
//

#ifndef _GC_STRESS_
#define _GC_STRESS_

#include "mpl/type_list"


struct alloc_context;


enum gcs_trigger_points {
    // generic handling based on EEConfig settings
    cfg_any,                        // any bit set in EEConfig::iGCStress
    cfg_alloc,                      // trigger on GC allocations
    cfg_transition,                 // trigger on transitions
    cfg_instr_jit,                  // trigger on JITted instructions
    cfg_instr_ngen,                 // trigger on NGENed instructions
    cfg_easy,                       // trigger on allocs or transitions
    cfg_instr,                      // trigger on managed instructions (JITted or NGENed)
    cfg_last,                       // boundary

    // special handling at particular trigger points
    jit_on_create_jump_stub,
    jit_on_create_il_stub,
    gc_on_alloc,
    vsd_on_resolve
};


namespace GCStressPolicy
{

#ifdef STRESS_HEAP

#ifdef __GNUC__
#define UNUSED_ATTR __attribute__ ((unused))
#else  // __GNUC__
#define UNUSED_ATTR
#endif // __GNUC__

#ifndef __UNUSED
#define __UNUSED(x) ((void)(x))
#endif // __UNUSED

    class InhibitHolder
    {
    private:
        // This static controls whether GC stress may induce GCs. EEConfig::GetGCStressLevel() still
        // controls when GCs may occur.
        static Volatile<DWORD> s_nGcStressDisabled;

        bool m_bAcquired;

    public:
        InhibitHolder()
        { LIMITED_METHOD_CONTRACT; ++s_nGcStressDisabled; m_bAcquired = true; }

        ~InhibitHolder()
        { LIMITED_METHOD_CONTRACT; Release(); }

        void Release()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_bAcquired)
            {
                --s_nGcStressDisabled;
                m_bAcquired = false;
            }
        }

        friend bool IsEnabled();
        friend void GlobalDisable();
        friend void GlobalEnable();
    } UNUSED_ATTR;

    FORCEINLINE bool IsEnabled()
    { return InhibitHolder::s_nGcStressDisabled == 0U; }

    FORCEINLINE void GlobalDisable()
    { ++InhibitHolder::s_nGcStressDisabled; }

    FORCEINLINE void GlobalEnable()
    { --InhibitHolder::s_nGcStressDisabled; }

#else // STRESS_HEAP

    class InhibitHolder
    { void Release() {} };

    FORCEINLINE bool IsEnabled()
    { return false; }

    FORCEINLINE void GlobalDisable()
    {}

    FORCEINLINE void GlobalEnable()
    {}

#endif // STRESS_HEAP
}



namespace _GCStress
{

#ifdef STRESS_HEAP

    // Support classes to allow easy customization of GC Stress policies
    namespace detail
    {
        using namespace mpl;

        // Selecting a policy from a type list and a fallback/default policy
        // GetPolicy<>:type will represent either a type in ListT with the same "tag" as DefPolicy
        // or DefPolicy, based on the Traits passed in.
        template <
            typename ListT,
            typename DefPolicy,
            template <typename> class Traits
        >
        struct GetPolicy;

        // Common case: recurse over the type list
        template <
            typename HeadT,
            typename TailT,
            typename DefPolicy,
            template<typename> class Traits
        >
        struct GetPolicy<type_list<HeadT, TailT>, DefPolicy, Traits>
        {
            // is true if HeadT and DefPolicy evaluate to the same tag,
            // through Traits<>
            static const bool sameTag = std::is_same<
                        typename Traits<HeadT>::tag,
                        typename Traits<DefPolicy>::tag
                    >::value;

            typedef typename std::conditional<
                        sameTag,
                        HeadT,
                        typename GetPolicy<TailT, DefPolicy, Traits>::type
                    >::type type;
        };

        // Termination case.
        template <
            typename DefPolicy,
            template<typename> class Traits
        >
        struct GetPolicy <null_type, DefPolicy, Traits>
        {
            typedef DefPolicy type;
        };
    }


    // GC stress specific EEConfig accessors
    namespace detail
    {
        // no definition provided so that absence of concrete implementations cause compiler errors
        template <enum gcs_trigger_points>
        bool IsEnabled();

        template<> FORCEINLINE
        bool IsEnabled<cfg_any>()
        {
            // Most correct would be to test for each specific bits, but we've
            // always only tested against 0...
            return g_pConfig->GetGCStressLevel() != 0;
            // return (g_pConfig->GetGCStressLevel() &
            //       (EEConfig::GCSTRESS_ALLOC|EEConfig::GCSTRESS_TRANSITION|
            //        EEConfig::GCSTRESS_INSTR_JIT|EEConfig::GCSTRESS_INSTR_NGEN) != 0);
        }

        #define DefineIsEnabled(cfg_enum, eeconfig_bits)                    \
        template<> FORCEINLINE                                              \
        bool IsEnabled<cfg_enum>()                                   \
        {                                                                   \
            return (g_pConfig->GetGCStressLevel() & (eeconfig_bits)) != 0;  \
        }

        DefineIsEnabled(cfg_alloc,      EEConfig::GCSTRESS_ALLOC);
        DefineIsEnabled(cfg_transition, EEConfig::GCSTRESS_TRANSITION);
        DefineIsEnabled(cfg_instr_jit,  EEConfig::GCSTRESS_INSTR_JIT);
        DefineIsEnabled(cfg_instr_ngen, EEConfig::GCSTRESS_INSTR_NGEN);
        DefineIsEnabled(cfg_easy, EEConfig::GCSTRESS_ALLOC|EEConfig::GCSTRESS_TRANSITION);
        DefineIsEnabled(cfg_instr, EEConfig::GCSTRESS_INSTR_JIT|EEConfig::GCSTRESS_INSTR_NGEN);

        #undef DefineIsEnabled

    }


    //
    // GC stress policy classes used by GCSBase and GCStress template classes
    //

    // Fast GS stress policies that dictate whether GCStress<>::MaybeTrigger()
    // will consider g_pConfig->FastGCStressLevel() when deciding whether
    // to trigger a GC or not.

    // This is the default Fast GC stress policy that ignores the EEConfig
    // setting
    class IgnoreFastGcSPolicy
    {
    public:
        FORCEINLINE
        static bool FastGcSEnabled(DWORD minValue = 0)
        { return false; }
    };

    // This is the overriding Fast GC stress policy that considers the
    // EEConfig setting on checked/debug builds
    class EeconfigFastGcSPolicy
    {
    public:
        FORCEINLINE
        static bool FastGcSEnabled(DWORD minValue = 0)
        {
        #ifdef _DEBUG
            return g_pConfig->FastGCStressLevel() > minValue;
        #else // _DEBUG
            return false;
        #endif // _DEBUG
        }
    };

    // GC Mode policies that determines whether to switch the GC mode before
    // triggering the GC.

    // This is the default GC Mode stress policy that does not switch GC modes
    class AnyGcModePolicy
    {
    };

    // This is the overriding GC Mode stress policy that forces a switch to
    // cooperative mode before MaybeTrigger() will trigger a GC
    class CoopGcModePolicy
    {
#ifndef DACCESS_COMPILE
        // implicit constructor an destructor will do the right thing
        GCCoop m_coop;
#endif // DACCESS_COMPILE

    public:
        FORCEINLINE CoopGcModePolicy()
        { WRAPPER_NO_CONTRACT; }
        FORCEINLINE ~CoopGcModePolicy()
        { WRAPPER_NO_CONTRACT; }
    } UNUSED_ATTR;

    // GC Trigger policy classes define how a garbage collection is triggered

    // This is the default GC Trigger policy that simply calls
    // IGCHeap::StressHeap
    class StressGcTriggerPolicy
    {
    public:
        FORCEINLINE
        static void Trigger()
        {
            // BUG(github #10318) - when not using allocation contexts, the alloc lock
            // must be acquired here. Until fixed, this assert prevents random heap corruption.
            _ASSERTE(GCHeapUtilities::UseThreadAllocationContexts());
            GCHeapUtilities::GetGCHeap()->StressHeap(GetThread()->GetAllocContext());
        }

        FORCEINLINE
        static void Trigger(::gc_alloc_context* acontext)
        { GCHeapUtilities::GetGCHeap()->StressHeap(acontext); }
    };

    // This is an overriding GC Trigger policy that triggers a GC by calling
    // PulseGCMode
    class PulseGcTriggerPolicy
    {
    public:
        FORCEINLINE
        static void Trigger()
        {
            DEBUG_ONLY_REGION();
            GetThread()->PulseGCMode();
        }
    };


    // GC stress policy tags
    struct fast_gcs_policy_tag {};
    struct gc_mode_policy_tag {};
    struct gc_trigger_policy_tag {};


    template <class GCSPolicy>
    struct GcStressTraits
    { typedef mpl::null_type tag; };

    #define DefineGCStressTraits(Policy, policy_tag)                        \
    template <> struct GcStressTraits<Policy>                               \
    { typedef policy_tag tag; }

    DefineGCStressTraits(IgnoreFastGcSPolicy, fast_gcs_policy_tag);
    DefineGCStressTraits(EeconfigFastGcSPolicy, fast_gcs_policy_tag);

    DefineGCStressTraits(AnyGcModePolicy, gc_mode_policy_tag);
    DefineGCStressTraits(CoopGcModePolicy, gc_mode_policy_tag);

    DefineGCStressTraits(StressGcTriggerPolicy, gc_trigger_policy_tag);
    DefineGCStressTraits(PulseGcTriggerPolicy, gc_trigger_policy_tag);

    #undef DefineGCStressTraits

    // Special handling for GC stress policies
    template <class GCPolicies, class DefPolicy>
    struct GetPolicy:
        public detail::GetPolicy<GCPolicies, DefPolicy, GcStressTraits>
    {};


    //
    // The base for any customization GCStress class. It accepts an identifying
    // GC stress trigger point and at most three overriding policies.
    //
    // It defines FastGcSPolicy, GcModePolicy, and GcTriggerPolicy as either
    // the overriding policy or the default policy, if no corresponding
    // overriding policy is specified in the list. These names can then be
    // accessed from the derived GCStress class.
    //
    // Additionally it defines the static methods IsEnabled and MaybeTrigger and
    // how the policy classes influence their behavior.
    //
    template <
        enum gcs_trigger_points tp,
        class Policy1 = mpl::null_type,
        class Policy2 = mpl::null_type,
        class Policy3 = mpl::null_type
    >
    class GCSBase
    {
    public:
        typedef typename mpl::make_type_list<Policy1, Policy2, Policy3>::type Policies;

        typedef typename GetPolicy<Policies, IgnoreFastGcSPolicy>::type FastGcSPolicy;
        typedef typename GetPolicy<Policies, AnyGcModePolicy>::type GcModePolicy;
        typedef typename GetPolicy<Policies, StressGcTriggerPolicy>::type GcTriggerPolicy;

        typedef GCSBase<tp, FastGcSPolicy, GcModePolicy, GcTriggerPolicy> GcStressBase;

        // Returns true iff:
        //   . the bitflag in EEConfig::GetStressLevel() corresponding to the
        //     gc stress trigger point is set AND
        //   . when a Fast GC Stress policy is specified, if the minFastGC argument
        //     is below the EEConfig::FastGCStressLevel
        FORCEINLINE
        static bool IsEnabled(DWORD minFastGc = 0)
        {
            static_assert(tp < cfg_last, "GCSBase only supports cfg_ trigger points.");
            return detail::IsEnabled<tp>() && !FastGcSPolicy::FastGcSEnabled(minFastGc);
        }

        // Triggers a GC iff
        //   . the GC stress is not disabled globally (thru GCStressPolicy::GlobalDisable)
        //     AND
        //   . IsEnabled() returns true.
        // Additionally it switches the GC mode as specified by GcModePolicy, and it
        // uses GcTriggerPolicy::Trigger() to actually trigger the GC
        FORCEINLINE
        static void MaybeTrigger(DWORD minFastGc = 0)
        {
            if (IsEnabled(minFastGc) && GCStressPolicy::IsEnabled())
            {
                GcModePolicy gcModeObj; __UNUSED(gcModeObj);
                GcTriggerPolicy::Trigger();
            }
        }

        // Triggers a GC iff
        //   . the GC stress is not disabled globally (thru GCStressPolicy::GlobalDisable)
        //     AND
        //   . IsEnabled() returns true.
        // Additionally it switches the GC mode as specified by GcModePolicy, and it
        // uses GcTriggerPolicy::Trigger(alloc_context*) to actually trigger the GC
        FORCEINLINE
        static void MaybeTrigger(::gc_alloc_context* acontext, DWORD minFastGc = 0)
        {
            if (IsEnabled(minFastGc) && GCStressPolicy::IsEnabled())
            {
                GcModePolicy gcModeObj; __UNUSED(gcModeObj);
                GcTriggerPolicy::Trigger(acontext);
            }
        }
    };

    template <
        enum gcs_trigger_points tp,
        class Policy1 = mpl::null_type,
        class Policy2 = mpl::null_type,
        class Policy3 = mpl::null_type
    >
    class GCStress
        : public GCSBase<tp, Policy1, Policy2, Policy3>
    {
    };


    //
    // Partial specializations of GCStress for trigger points requiring non-default
    // handling.
    //

    template <>
    class GCStress<jit_on_create_jump_stub>
        : public GCSBase<cfg_any>
    {
    public:

        FORCEINLINE
        static void MaybeTrigger(DWORD minFastGc = 0)
        {
            if ((GetThreadNULLOk() != NULL) && (GetThreadNULLOk()->PreemptiveGCDisabled()))
            {
                // Force a GC if the stress level is high enough
                GcStressBase::MaybeTrigger(minFastGc);
            }
        }

    };

    template <>
    class GCStress<gc_on_alloc>
        : public GCSBase<cfg_alloc>
    {
    public:

        FORCEINLINE
        static void MaybeTrigger(::gc_alloc_context* acontext)
        {
            GcStressBase::MaybeTrigger(acontext);

#ifdef _DEBUG
            Thread *pThread = GetThreadNULLOk();
            if (pThread)
            {
                pThread->EnableStressHeap();
            }
#endif //_DEBUG
        }
    };

    template <>
    class GCStress<vsd_on_resolve>
        : public GCSBase<cfg_any>
    {
    public:

        // Triggers a GC iff
        //   . the GC stress is not disabled globally (thru GCStressPolicy::GlobalDisable)
        //     AND
        //   . IsEnabled() returns true.
        // Additionally it protects the passed in OBJECTREF&, and it uses
        // GcTriggerPolicy::Trigger() to actually trigger the GC
        //
        // Note: the OBJECTREF must be passed by reference so MaybeTrigger can protect
        // the calling function's stack slot.
        FORCEINLINE
        static void MaybeTriggerAndProtect(OBJECTREF& objref)
        {
            if (GcStressBase::IsEnabled() && GCStressPolicy::IsEnabled())
            {
                GCPROTECT_BEGIN(objref);
                GcTriggerPolicy::Trigger();
                GCPROTECT_END();
            }
        }
    };

#else // STRESS_HEAP

    class IgnoreFastGcSPolicy {};
    class EeconfigFastGcSPolicy {};
    class AnyGcModePolicy {};
    class CoopGcModePolicy {};
    class StressGcTriggerPolicy {};
    class PulseGcTriggerPolicy {};

    // Everything here should resolve to inlined empty blocks or "false"
    template <
        enum gcs_trigger_points tp,
        class Policy1 = mpl::null_type,
        class Policy2 = mpl::null_type,
        class Policy3 = mpl::null_type
    >
    class GCStress
    {
    public:
        FORCEINLINE
        static bool IsEnabled(DWORD minFastGc = 0)
        {
            static_assert(tp < cfg_last, "GCStress::IsEnabled only supports cfg_ trigger points.");
            return false;
        }

        FORCEINLINE
        static void MaybeTrigger(DWORD minFastGc = 0)
        {}

        FORCEINLINE
        static void MaybeTrigger(::alloc_context* acontext, DWORD minFastGc = 0)
        {}

        template<typename T>
        FORCEINLINE
        static void MaybeTrigger(T arg)
        {}
    };

#endif // STRESS_HEAP

}

using _GCStress::IgnoreFastGcSPolicy;
using _GCStress::EeconfigFastGcSPolicy;
using _GCStress::AnyGcModePolicy;
using _GCStress::CoopGcModePolicy;
using _GCStress::StressGcTriggerPolicy;
using _GCStress::PulseGcTriggerPolicy;

using _GCStress::GCStress;



#endif
