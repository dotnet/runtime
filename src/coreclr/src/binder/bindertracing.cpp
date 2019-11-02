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
            &activityId,
            &relatedActivityId);
#endif // FEATURE_EVENT_TRACE
    }

    void FireAssemblyLoadStop(const BinderTracing::AssemblyBindOperation::BindRequest &request, bool success, const WCHAR *resultName, const WCHAR *resultPath, bool cached)
    {
#ifdef FEATURE_EVENT_TRACE
        if (!EventEnabledAssemblyLoadStop())
            return;

        GUID activityId = GUID_NULL;
        ActivityTracker::Stop(&activityId);

        FireEtwAssemblyLoadStop(
            GetClrInstanceId(),
            request.AssemblyName,
            request.AssemblyPath,
            request.RequestingAssembly,
            request.AssemblyLoadContext,
            success,
            resultName,
            resultPath,
            cached,
            &activityId);
#endif // FEATURE_EVENT_TRACE
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
    AssemblyBindOperation::AssemblyBindOperation(AssemblySpec *assemblySpec)
        : m_bindRequest { assemblySpec }
        , m_success { false }
        , m_cached { false }
    {
        _ASSERTE(assemblySpec != nullptr);

        // ActivityTracker or EventSource may have triggered the system satellite load.
        // Don't track system satellite binding to avoid potential infinite recursion.
        m_trackingBind = BinderTracing::IsEnabled() && !m_bindRequest.AssemblySpec->IsMscorlibSatellite();
        if (m_trackingBind)
        {
            m_bindRequest.AssemblySpec->GetFileOrDisplayName(ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN, m_bindRequest.AssemblyName);
            FireAssemblyLoadStart(m_bindRequest);
        }
    }

    AssemblyBindOperation::~AssemblyBindOperation()
    {
        if (m_trackingBind)
            FireAssemblyLoadStop(m_bindRequest, m_success, m_resultName.GetUnicode(), m_resultPath.GetUnicode(), m_cached);
    }

    void AssemblyBindOperation::SetResult(PEAssembly *assembly)
    {
        m_success = assembly != nullptr;
    }
}