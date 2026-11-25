// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop;

/// <summary>
/// Properties that are common to many interop generators' diagnostics. These properties are used by code-fixers to provide a good user experience.
/// </summary>
public class GeneratorDiagnosticProperties
{
    /// <summary>
    /// The diagnostic can be resolved by adding the System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute to the assembly
    /// </summary>
    public const string AddDisableRuntimeMarshallingAttribute = nameof(AddDisableRuntimeMarshallingAttribute);

    /// <summary>
    /// The diagnostic can be resolved by adding the System.Runtime.InteropServices.MarshalAsAttribute to the element with one of the UnmanagedType values
    /// specified in the comma-separated list of values specified in the value of this property.
    /// </summary>
    public const string AddMarshalAsAttribute = nameof(AddMarshalAsAttribute);
}
