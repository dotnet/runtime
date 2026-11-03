// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

/// <summary>Provides generated binding for a JavaScript export.</summary>
/// <remarks>This API supports JavaScript interop infrastructure and is not intended to be used directly from your code.</remarks>
[SupportedOSPlatform("browser")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JSExportProvider
{
    /// <summary>Initializes a new instance of the <see cref="JSExportProvider"/> class.</summary>
    protected JSExportProvider() { }

    /// <summary>Binds the generated JavaScript export for the current context.</summary>
    [DebuggerNonUserCode]
    public abstract void Bind();
}
