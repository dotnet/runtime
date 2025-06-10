// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern ICorJitHost* g_interpHost;

class CILInterp : public ICorJitCompiler
{
    CorJitResult compileMethod(ICorJitInfo*         comp,            /* IN */
                               CORINFO_METHOD_INFO* methodInfo,      /* IN */
                               unsigned             flags,           /* IN */
                               uint8_t**            nativeEntry,     /* OUT */
                               uint32_t*            nativeSizeOfCode /* OUT */
    );
    void ProcessShutdownWork(ICorStaticInfo* statInfo);
    void getVersionIdentifier(GUID* versionIdentifier /* OUT */ );
    void setTargetOS(CORINFO_OS os);
};
