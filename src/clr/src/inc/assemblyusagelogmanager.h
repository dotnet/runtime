// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#ifndef ASSEMBLY_USAGE_LOG_MANAGER_H
#define ASSEMBLY_USAGE_LOG_MANAGER_H

#include "assemblyusagelog.h"
#include "daccess.h"

class AssemblyUsageLogManager
{
public:

    class Config
    {
    public:
        LPCWSTR wszLogDir;
        unsigned int cLogBufferSize;
#ifdef FEATURE_APPX
        unsigned int uiLogRefreshInterval;
#endif
    };

    enum GENERATE_LOG_FLAGS
    {
        GENERATE_LOG_FLAGS_NONE = 0,
    };

    // we depend on static PODs being initialized to 0 which is why ASSEMBLY_USAGE_LOG_FLAGS_NONE is 0
    enum ASSEMBLY_USAGE_LOG_FLAGS : DWORD
    {
        ASSEMBLY_USAGE_LOG_FLAGS_NONE = 0,
        ASSEMBLY_USAGE_LOG_FLAGS_INITTED = 1,
        ASSEMBLY_USAGE_LOG_FLAGS_APPLOCALNGENDISABLED = 2,
    };
                                                                                                                                    
    static HRESULT Init(const Config *pConfig);
    static HRESULT GenerateLog(GENERATE_LOG_FLAGS flags);
    static HRESULT GetUsageLogForContext(LPCWSTR binder, LPCWSTR binderParameter, IAssemblyUsageLog **ppUsageLog);
    static HRESULT RegisterBinderWithUsageLog(UINT_PTR binderId, IAssemblyUsageLog *pUsageLog);
    static HRESULT UnRegisterBinderFromUsageLog(UINT_PTR binderId);
    static IAssemblyUsageLog *GetUsageLogForBinder(UINT_PTR binderId);
    static ASSEMBLY_USAGE_LOG_FLAGS GetUsageLogFlags();
    static HRESULT SetUsageLogFlag(ASSEMBLY_USAGE_LOG_FLAGS flag, BOOL);

private:
    SVAL_DECL(ASSEMBLY_USAGE_LOG_FLAGS, s_UsageLogFlags);
};

#endif /*  ASSEMBLY_USAGE_LOG_MANAGER_H */

