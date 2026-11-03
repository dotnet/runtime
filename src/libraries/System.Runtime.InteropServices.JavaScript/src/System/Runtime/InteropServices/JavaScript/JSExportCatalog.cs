// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

/// <summary>Provides generated discovery metadata for the JavaScript exports of an assembly.</summary>
/// <remarks>This API supports JavaScript interop infrastructure and is not intended to be used directly from your code.</remarks>
[SupportedOSPlatform("browser")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JSExportCatalog
{
    /// <summary>Initializes a new instance of the <see cref="JSExportCatalog"/> class.</summary>
    protected JSExportCatalog() { }

    /// <summary>Gets the fully qualified, hash-qualified provider keys of the assembly's exports.</summary>
    /// <returns>The generated export provider keys.</returns>
    [DebuggerNonUserCode]
    public abstract string[] GetExportNames();
}
