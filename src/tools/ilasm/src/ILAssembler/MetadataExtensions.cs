// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace ILAssembler;

internal static class MetadataExtensions
{
    extension(MethodImplAttributes)
    {
        public static MethodImplAttributes UserMask =>
            MethodImplAttributes.ManagedMask |
            MethodImplAttributes.ForwardRef |
            MethodImplAttributes.PreserveSig |
            MethodImplAttributes.InternalCall |
            MethodImplAttributes.Synchronized |
            MethodImplAttributes.NoInlining |
            MethodImplAttributes.AggressiveInlining |
            MethodImplAttributes.NoOptimization |
            MethodImplAttributes.AggressiveOptimization |
            MethodImplAttributes.Async;
    }

    extension(TypeAttributes)
    {
        public static TypeAttributes ExtendedLayout => (TypeAttributes)0x18;
        public static TypeAttributes Forwarder => (TypeAttributes)0x00200000;
    }

    extension(UnmanagedType)
    {
        public static int ArraySizeParamIndexSpecified => 0x0001;
        public static UnmanagedType ByValStr => (UnmanagedType)0x22;
        public static UnmanagedType Max => (UnmanagedType)0x50;
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

    extension(ILOpCode)
    {
        public static ILOpCode Unused => (ILOpCode)0xFE22;
    }
}

internal static class ClassInterfaceTypeExtensions
{
    extension(ClassInterfaceType)
    {
        public static ClassInterfaceType Last => (ClassInterfaceType)3;
    }
}

internal static class ComInterfaceTypeExtensions
{
    extension(ComInterfaceType)
    {
        public static ComInterfaceType Last => (ComInterfaceType)4;
    }
}
