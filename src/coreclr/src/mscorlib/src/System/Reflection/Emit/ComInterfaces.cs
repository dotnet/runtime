// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;

namespace System.Runtime.InteropServices
{
    [GuidAttribute("BEBB2505-8B54-3443-AEAD-142A16DD9CC7")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.AssemblyBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _AssemblyBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("ED3E4384-D7E2-3FA7-8FFD-8940D330519A")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.ConstructorBuilder))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _ConstructorBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("BE9ACCE8-AAFF-3B91-81AE-8211663F5CAD")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.CustomAttributeBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _CustomAttributeBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("C7BD73DE-9F85-3290-88EE-090B8BDFE2DF")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.EnumBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _EnumBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("AADABA99-895D-3D65-9760-B1F12621FAE8")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.EventBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _EventBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("CE1A3BF5-975E-30CC-97C9-1EF70F8F3993")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.FieldBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _FieldBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("A4924B27-6E3B-37F7-9B83-A4501955E6A7")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.ILGenerator))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _ILGenerator
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("4E6350D1-A08B-3DEC-9A3E-C465F9AEEC0C")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.LocalBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _LocalBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("007D8A14-FDF3-363E-9A0B-FEC0618260A2")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.MethodBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

#if FEATURE_METHOD_RENTAL
    [GuidAttribute("C2323C25-F57F-3880-8A4D-12EBEA7A5852")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.MethodRental))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodRental
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }
#endif

    [GuidAttribute("D05FFA9A-04AF-3519-8EE1-8D93AD73430B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.ModuleBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _ModuleBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("36329EBA-F97A-3565-BC07-0ED5C6EF19FC")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.ParameterBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _ParameterBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("15F9A479-9397-3A63-ACBD-F51977FB0F02")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.PropertyBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _PropertyBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("7D13DD37-5A04-393C-BBCA-A5FEA802893D")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.SignatureHelper))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _SignatureHelper
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("7E5678EE-48B3-3F83-B076-C58543498A58")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Emit.TypeBuilder))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _TypeBuilder
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

}
