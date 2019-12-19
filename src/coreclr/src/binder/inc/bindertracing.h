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
        AssemblyBindOperation(AssemblySpec *assemblySpec, const WCHAR *assemblyPath = nullptr);
        ~AssemblyBindOperation();

        void SetResult(PEAssembly *assembly, bool cached = false);

        struct BindRequest
        {
            AssemblySpec *AssemblySpec;
            SString AssemblyName;
            SString AssemblyPath;
            SString RequestingAssembly;
            SString AssemblyLoadContext;
            SString RequestingAssemblyLoadContext;
        };

    private:
        bool ShouldIgnoreBind();

    private:
        BindRequest m_bindRequest;
        bool m_populatedBindRequest;

        bool m_checkedIgnoreBind;
        bool m_ignoreBind;

        PEAssembly *m_resultAssembly;
        bool m_cached;
    };

    // This must match the BindingPathSource value map in ClrEtwAll.man
    enum PathSource
    {
        ApplicationAssemblies,
        AppNativeImagePaths,
        AppPaths,
        PlatformResourceRoots,
        SatelliteSubdirectory
    };

    void PathProbed(const WCHAR *path, PathSource source, HRESULT hr);
};

#endif // __BINDER_TRACING_H__
