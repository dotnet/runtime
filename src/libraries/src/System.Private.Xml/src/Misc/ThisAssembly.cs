// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <summary>
//      Declares assembly-level attributes.
// </summary>
//---------------------------------------------------------------------

[assembly: System.Security.SecurityCritical]

#if ASTORIA_LIGHT
// TODO: SQLBUDT 558791 use real silver light assembly version information, not using 3.5 so we can tell the difference more easily

//[assembly: System.Reflection.AssemblyVersion(ThisAssembly.Version)]
//[assembly: System.Reflection.AssemblyFileVersion(ThisAssembly.InformationalVersion)]
//[assembly: System.Reflection.AssemblyInformationalVersion(ThisAssembly.InformationalVersion)]
[assembly: System.Resources.SatelliteContractVersion(ThisAssembly.Version)]

internal static class ThisAssembly
{
    internal const string Version = "2.0.5.0";
    internal const string InformationalVersion = "2.0.40216.0";
}

#endif
