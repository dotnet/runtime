// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Resources
{
    /// <summary>
    /// Specifies whether a <see cref="ResourceManager" /> object looks for the resources of the app's default culture in the main assembly or in a satellite assembly.
    /// </summary>
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly,
        Satellite
    }
}
