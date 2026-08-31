// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace ILAssembler;

internal static class MetadataExtensions
{
    extension(TypeAttributes)
    {
        public static TypeAttributes ExtendedLayout => (TypeAttributes)0x18;
        public static TypeAttributes Forwarder => (TypeAttributes)0x00200000;
    }

    extension(DeclarativeSecurityAction)
    {
        public static DeclarativeSecurityAction Request => (DeclarativeSecurityAction)1;
        public static DeclarativeSecurityAction PrejitGrant => (DeclarativeSecurityAction)0xB;
        public static DeclarativeSecurityAction PrejitDeny => (DeclarativeSecurityAction)0xC;
        public static DeclarativeSecurityAction NonCasDemand => (DeclarativeSecurityAction)0xD;
        public static DeclarativeSecurityAction NonCasLinkDemand => (DeclarativeSecurityAction)0xE;
        public static DeclarativeSecurityAction NonCasInheritanceDemand => (DeclarativeSecurityAction)0xF;
    }

    extension(AssemblyFlags)
    {
        public static AssemblyFlags NoPlatform => (AssemblyFlags)0x70;
        public static AssemblyFlags ArchitectureMask => (AssemblyFlags)0xF0;
    }
}
