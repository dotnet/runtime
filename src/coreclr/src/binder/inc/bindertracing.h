// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// bindertracing.h
//

#ifndef __BINDER_TRACING_H__
#define __BINDER_TRACING_H__

class AssemblySpec;
class PEAssembly;

namespace BinderTracing
{
    bool IsEnabled();

    // If tracing is enabled, this class fires an assembly bind start event on construction
    // and the corresponding stop event on destruction
    class AssemblyBindOperation
    {
    public:
        // This class assumes the assembly spec will have a longer lifetime than itself
        AssemblyBindOperation(AssemblySpec *assemblySpec);
        ~AssemblyBindOperation();

        void SetResult(PEAssembly *assembly);

        struct BindRequest
        {
            AssemblySpec *AssemblySpec;
            SString AssemblyName;
            SString AssemblyPath;
            SString RequestingAssembly;
            SString AssemblyLoadContext;
        };

    private:
        BindRequest m_bindRequest;

        bool m_trackingBind;

        bool m_success;
        SString m_resultName;
        SString m_resultPath;
        bool m_cached;
    };
};

#endif // __BINDER_TRACING_H__
