// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

/// <summary>Provides generated discovery and binding for an assembly's JavaScript exports.</summary>
/// <remarks>This API supports JavaScript interop infrastructure and is not intended to be used directly from your code.</remarks>
[SupportedOSPlatform("browser")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JSExportProvider
{
    /// <summary>Initializes a new instance of the <see cref="JSExportProvider"/> class.</summary>
    protected JSExportProvider() { }

    /// <summary>Gets the fully qualified export names, including signature hashes, in method ID order.</summary>
    /// <returns>The generated export catalog.</returns>
    [DebuggerNonUserCode]
    public abstract string[] GetExportNames();

    /// <summary>Binds the specified JavaScript export for the current context.</summary>
    /// <param name="methodId">The invariant decimal index of the export in <see cref="GetExportNames"/>.</param>
    /// <remarks>Method IDs are local to the generated assembly build and are not stable across builds.</remarks>
    [DebuggerNonUserCode]
    public abstract void Bind(string methodId);
}
