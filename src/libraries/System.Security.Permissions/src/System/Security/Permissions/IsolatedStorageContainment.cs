// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum IsolatedStorageContainment
    {
        None = 0x00,
        DomainIsolationByUser = 0x10,
        ApplicationIsolationByUser = 0x15,
        AssemblyIsolationByUser = 0x20,
        DomainIsolationByMachine = 0x30,
        AssemblyIsolationByMachine = 0x40,
        ApplicationIsolationByMachine = 0x45,
        DomainIsolationByRoamingUser = 0x50,
        AssemblyIsolationByRoamingUser = 0x60,
        ApplicationIsolationByRoamingUser = 0x65,
        AdministerIsolatedStorageByUser = 0x70,
        //AdministerIsolatedStorageByMachine    = 0x80,
        UnrestrictedIsolatedStorage = 0xF0
    }
}
