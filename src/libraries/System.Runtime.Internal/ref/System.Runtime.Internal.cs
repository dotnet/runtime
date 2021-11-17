// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------
namespace Internal.Runtime.InteropServices
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static class ComActivator
    {
        //
        // Summary:
        //     Entry point for unmanaged COM register/unregister API from managed code
        //
        // Parameters:
        //   cxt:
        //     Reference to a Internal.Runtime.InteropServices.ComActivationContext instance
        //
        //   register:
        //     true if called for register or false to indicate unregister
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        public static void ClassRegistrationScenarioForType(ComActivationContext cxt, bool register) {}
        //
        // Summary:
        //     Entry point for unmanaged COM activation API from managed code
        //
        // Parameters:
        //   cxt:
        //     Reference to a Internal.Runtime.InteropServices.ComActivationContext instance
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        public static object GetClassFactoryForType(ComActivationContext cxt) => throw null;
    }
    public struct ComActivationContext
    {
        public System.Guid ClassId;
        public System.Guid InterfaceId;
        public string AssemblyPath;
        public string AssemblyName;
        public string TypeName;
    }
    public static class InMemoryAssemblyLoader
    {
        public static unsafe void LoadInMemoryAssembly(System.IntPtr moduleHandle, System.IntPtr assemblyPath) => throw null;
    }
}

namespace System.Runtime.CompilerServices
{
    public interface ICastable
    {
        bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Exception? castError);
        RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType);
    }
}
