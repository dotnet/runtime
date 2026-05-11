// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Used to indicate to the compiler that the <c>.locals init</c> flag should not be set in method headers.
/// </summary>
/// <remarks>
/// Downlevel polyfill of <see href="https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.skiplocalsinitattribute">System.Runtime.CompilerServices.SkipLocalsInitAttribute</see>
/// for target frameworks that do not provide it (.NET Standard 2.0, .NET Framework). The C# compiler recognizes the
/// attribute by full type name, so providing this internal definition is enough to enable the optimization.
/// </remarks>
[AttributeUsage(AttributeTargets.Module
    | AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Event, Inherited = false)]
internal sealed class SkipLocalsInitAttribute : Attribute
{
    public SkipLocalsInitAttribute()
    {
    }
}
