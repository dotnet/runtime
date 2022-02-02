// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Provides a means to root types / methods at the compiler driver layer
    /// </summary>
    public interface IRootingServiceProvider
    {
        void AddCompilationRoot(MethodDesc method, string reason, string exportName = null);
        void AddCompilationRoot(TypeDesc type, string reason);
        void AddReflectionRoot(MethodDesc method, string reason);
        void RootThreadStaticBaseForType(TypeDesc type, string reason);
        void RootGCStaticBaseForType(TypeDesc type, string reason);
        void RootNonGCStaticBaseForType(TypeDesc type, string reason);
        void RootModuleMetadata(ModuleDesc module, string reason);
        void RootReadOnlyDataBlob(byte[] data, int alignment, string reason, string exportName);
        void RootDelegateMarshallingData(DefType type, string reason);
        void RootStructMarshallingData(DefType type, string reason);
    }
}
