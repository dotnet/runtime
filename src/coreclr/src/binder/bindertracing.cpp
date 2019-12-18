// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// bindertracing.cpp
//


//
// Implements helpers for binder tracing
//
// ============================================================

#include "common.h"
#include "bindertracing.h"

#include "activitytracker.h"

#ifdef FEATURE_EVENT_TRACE
#include "eventtracebase.h"
#endif // FEATURE_EVENT_TRACE

using namespace BINDER_SPACE;

namespace
{
    void FireAssemblyLoadStart(const BinderTracing::AssemblyBindOperation::BindRequest &request)
    {
#ifdef FEATURE_EVENT_TRACE
        if (!EventEnabledAssemblyLoadStart())
            return;

        GUID activityId = GUID_NULL;
        GUID relatedActivityId = GUID_NULL;
        ActivityTracker::Start(&activityId, &relatedActivityId);

        FireEtwAssemblyLoadStart(
            GetClrInstanceId(),
            request.AssemblyName,
            request.AssemblyPath,
            request.RequestingAssembly,
            request.AssemblyLoadContext,
            request.RequestingAssemblyLoadContext,
            &activityId,
            &relatedActivityId);
#endif // FEATURE_EVENT_TRACE
    }

    void FireAssemblyLoadStop(const BinderTracing::AssemblyBindOperation::BindRequest &request, PEAssembly *resultAssembly, bool cached)
    {
#ifdef FEATURE_EVENT_TRACE
        if (!EventEnabledAssemblyLoadStop())
            return;

        GUID activityId = GUID_NULL;
        ActivityTracker::Stop(&activityId);

        SString resultName;
        SString resultPath;
        bool success = resultAssembly != nullptr;
        if (success)
        {
            resultPath = resultAssembly->GetPath();
            resultAssembly->GetDisplayName(resultName);
        }

        FireEtwAssemblyLoadStop(
            GetClrInstanceId(),
            request.AssemblyName,
            request.AssemblyPath,
            request.RequestingAssembly,
            request.AssemblyLoadContext,
            request.RequestingAssemblyLoadContext,
            success,
            resultName,
            resultPath,
            cached,
            &activityId);
#endif // FEATURE_EVENT_TRACE
    }

    void GetAssemblyLoadContextNameFromBindContext(ICLRPrivBinder *bindContext, AppDomain *domain, /*out*/ SString &alcName)
    {
        _ASSERTE(bindContext != nullptr);

        UINT_PTR binderID = 0;
        HRESULT hr = bindContext->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        if (FAILED(hr))
            return;

        ICLRPrivBinder *binder = reinterpret_cast<ICLRPrivBinder *>(binderID);
#ifdef FEATURE_COMINTEROP
        if (AreSameBinderInstance(binder, domain->GetTPABinderContext()) || AreSameBinderInstance(binder, domain->GetWinRtBinder()))
#else
        if (AreSameBinderInstance(binder, domain->GetTPABinderContext()))
#endif // FEATURE_COMINTEROP
        {
            alcName.Set(W("Default"));
        }
        else
        {
#ifdef CROSSGEN_COMPILE
            alcName.Set(W("Custom"));
#else // CROSSGEN_COMPILE
            CLRPrivBinderAssemblyLoadContext * alcBinder = static_cast<CLRPrivBinderAssemblyLoadContext *>(binder);
            OBJECTREF *alc = reinterpret_cast<OBJECTREF *>(alcBinder->GetManagedAssemblyLoadContext());

            GCX_COOP();
            struct _gc {
                STRINGREF alcName;
            } gc;
            ZeroMemory(&gc, sizeof(gc));

            GCPROTECT_BEGIN(gc);

            PREPARE_VIRTUAL_CALLSITE(METHOD__OBJECT__TO_STRING, *alc);
            DECLARE_ARGHOLDER_ARRAY(args, 1);
            args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*alc);
            CALL_MANAGED_METHOD_RETREF(gc.alcName, STRINGREF, args);
            gc.alcName->GetSString(alcName);

            GCPROTECT_END();
#endif // CROSSGEN_COMPILE
        }
    }

    void GetAssemblyLoadContextNameFromSpec(AssemblySpec *spec, /*out*/ SString &alcName)
    {
        _ASSERTE(spec != nullptr);

        AppDomain *domain = spec->GetAppDomain();
        ICLRPrivBinder* bindContext = spec->GetBindingContext();
        if (bindContext == nullptr)
            bindContext = spec->GetBindingContextFromParentAssembly(domain);

        GetAssemblyLoadContextNameFromBindContext(bindContext, domain, alcName);
    }

    void PopulateBindRequest(/*inout*/ BinderTracing::AssemblyBindOperation::BindRequest &request)
    {
        AssemblySpec *spec = request.AssemblySpec;
        _ASSERTE(spec != nullptr);

        if (request.AssemblyPath.IsEmpty())
            request.AssemblyPath = spec->GetCodeBase();

        if (spec->GetName() != nullptr)
            spec->GetDisplayName(ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN, request.AssemblyName);

        DomainAssembly *parentAssembly = spec->GetParentAssembly();
        if (parentAssembly != nullptr)
        {
            PEAssembly *peAssembly = parentAssembly->GetFile();
            _ASSERTE(peAssembly != nullptr);
            peAssembly->GetDisplayName(request.RequestingAssembly);

            AppDomain *domain = parentAssembly->GetAppDomain();
            ICLRPrivBinder *bindContext = peAssembly->GetBindingContext();
            if (bindContext == nullptr)
                bindContext = domain->GetTPABinderContext(); // System.Private.CoreLib returns null

            GetAssemblyLoadContextNameFromBindContext(bindContext, domain, request.RequestingAssemblyLoadContext);
        }

        GetAssemblyLoadContextNameFromSpec(spec, request.AssemblyLoadContext);
    }
}

bool BinderTracing::IsEnabled()
{
#ifdef FEATURE_EVENT_TRACE
    // Just check for the AssemblyLoadStart event being enabled.
    return EventEnabledAssemblyLoadStart();
#endif // FEATURE_EVENT_TRACE
    return false;
}

namespace BinderTracing
{
    AssemblyBindOperation::AssemblyBindOperation(AssemblySpec *assemblySpec, const WCHAR *assemblyPath)
        : m_bindRequest { assemblySpec, nullptr, assemblyPath }
        , m_populatedBindRequest { false }
        , m_checkedIgnoreBind { false }
        , m_ignoreBind { false }
        , m_resultAssembly { nullptr }
        , m_cached { false }
    {
        _ASSERTE(assemblySpec != nullptr);

        if (!BinderTracing::IsEnabled() || ShouldIgnoreBind())
            return;

        PopulateBindRequest(m_bindRequest);
        m_populatedBindRequest = true;
        FireAssemblyLoadStart(m_bindRequest);
    }

    AssemblyBindOperation::~AssemblyBindOperation()
    {
        if (!BinderTracing::IsEnabled() || ShouldIgnoreBind())
            return;

        // Make sure the bind request is populated. Tracing may have been enabled mid-bind.
        if (!m_populatedBindRequest)
            PopulateBindRequest(m_bindRequest);

        FireAssemblyLoadStop(m_bindRequest, m_resultAssembly, m_cached);

        if (m_resultAssembly != nullptr)
            m_resultAssembly->Release();
    }

    void AssemblyBindOperation::SetResult(PEAssembly *assembly, bool cached)
    {
        _ASSERTE(m_resultAssembly == nullptr);
        m_resultAssembly = assembly;
        if (m_resultAssembly != nullptr)
            m_resultAssembly->AddRef();

        m_cached = cached;
    }

    bool AssemblyBindOperation::ShouldIgnoreBind()
    {
        if (m_checkedIgnoreBind)
            return m_ignoreBind;

        // ActivityTracker or EventSource may have triggered the system satellite load.
        // Don't track system satellite binding to avoid potential infinite recursion.
        m_ignoreBind = m_bindRequest.AssemblySpec->IsMscorlibSatellite();
        m_checkedIgnoreBind = true;
        return m_ignoreBind;
    }
}

void BinderTracing::PathProbed(const WCHAR *path, BinderTracing::PathSource source, HRESULT hr)
{
    FireEtwKnownPathProbed(GetClrInstanceId(), path, source, hr);
}