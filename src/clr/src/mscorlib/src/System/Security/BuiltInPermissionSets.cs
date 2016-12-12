// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Permissions;
using Microsoft.Win32;

namespace System.Security
{
    internal static class BuiltInPermissionSets
    {
        //
        // Raw PermissionSet XML - the built in permission sets are expressed in XML form since they contain
        // permissions from assemblies other than mscorlib.
        //

        private static readonly string s_everythingXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""Everything""
                             Description = """ + Environment.GetResourceString("Policy_PS_Everything") + @"""
                  <IPermission class = ""System.Data.OleDb.OleDbPermission, " + AssemblyRef.SystemData + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Data.SqlClient.SqlClientPermission, " + AssemblyRef.SystemData + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Diagnostics.PerformanceCounterPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Net.DnsPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Net.SocketPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Net.WebPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.DataProtectionPermission, " + AssemblyRef.SystemSecurity + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.EnvironmentPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Diagnostics.EventLogPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.FileDialogPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.FileIOPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" /> 
                  <IPermission class = ""System.Security.Permissions.IsolatedStorageFilePermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.KeyContainerPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Drawing.Printing.PrintingPermission, " + AssemblyRef.SystemDrawing + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.ReflectionPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.RegistryPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.SecurityPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""Assertion, UnmanagedCode, Execution, ControlThread, ControlEvidence, ControlPolicy, ControlAppDomain, SerializationFormatter, ControlDomainPolicy, ControlPrincipal, RemotingConfiguration, Infrastructure, BindingRedirects"" />
                  <IPermission class = ""System.Security.Permissions.UIPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.StorePermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.TypeDescriptorPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
               </PermissionSet>";

        private static readonly string s_executionXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""Execution""
                             Description = """ + Environment.GetResourceString("Policy_PS_Execution") + @""">
                  <IPermission class = ""System.Security.Permissions.SecurityPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""Execution"" />
               </PermissionSet>";

        private static readonly string s_fullTrustXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet"" 
                             version = ""1"" 
                             Unrestricted = ""true"" 
                             Name = ""FullTrust"" 
                             Description = """ + Environment.GetResourceString("Policy_PS_FullTrust") + @""" />";

        private static readonly string s_internetXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""Internet""
                             Description = """ + Environment.GetResourceString("Policy_PS_Internet") + @""">
                  <IPermission class = ""System.Drawing.Printing.PrintingPermission, " + AssemblyRef.SystemDrawing + @"""
                               version = ""1""
                               Level = ""SafePrinting"" />
                  <IPermission class = ""System.Security.Permissions.FileDialogPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Access = ""Open"" />
                  <IPermission class = ""System.Security.Permissions.IsolatedStorageFilePermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               UserQuota = ""1024000""
                               Allowed = ""ApplicationIsolationByUser"" />
                  <IPermission class = ""System.Security.Permissions.SecurityPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""Execution"" />
                  <IPermission class = ""System.Security.Permissions.UIPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Window = ""SafeTopLevelWindows""
                               Clipboard = ""OwnClipboard"" />
               </PermissionSet>";

        private static readonly string s_localIntranetXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""LocalIntranet""
                             Description = """ + Environment.GetResourceString("Policy_PS_LocalIntranet") + @""" >
                  <IPermission class = ""System.Drawing.Printing.PrintingPermission, " + AssemblyRef.SystemDrawing + @"""
                              version = ""1""
                              Level = ""DefaultPrinting"" />
                  <IPermission class = ""System.Net.DnsPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.EnvironmentPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Read = ""USERNAME"" />
                  <IPermission class = ""System.Security.Permissions.FileDialogPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
                  <IPermission class = ""System.Security.Permissions.IsolatedStorageFilePermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Allowed = ""AssemblyIsolationByUser""
                               UserQuota = ""9223372036854775807""
                               Expiry = ""9223372036854775807""
                               Permanent = ""true"" />
                  <IPermission class = ""System.Security.Permissions.ReflectionPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""ReflectionEmit, RestrictedMemberAccess"" />
                  <IPermission class = ""System.Security.Permissions.SecurityPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""Execution, Assertion, BindingRedirects "" />
                  <IPermission class = ""System.Security.Permissions.TypeDescriptorPermission, " + AssemblyRef.System + @"""
                               version = ""1""
                               Flags = ""RestrictedRegistrationAccess"" />
                  <IPermission class = ""System.Security.Permissions.UIPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Unrestricted = ""true"" />
               </PermissionSet>";

        private static readonly string s_nothingXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""Nothing""
                             Description = """ + Environment.GetResourceString("Policy_PS_Nothing") + @""" />";

        private static readonly string s_skipVerificationXml =
            @"<PermissionSet class = ""System.Security.NamedPermissionSet""
                             version = ""1""
                             Name = ""SkipVerification""
                             Description = """ + Environment.GetResourceString("Policy_PS_SkipVerification") + @""">
                  <IPermission class = ""System.Security.Permissions.SecurityPermission, " + AssemblyRef.Mscorlib + @"""
                               version = ""1""
                               Flags = ""SkipVerification"" />
               </PermissionSet>";

        //
        // Built in permission set objects
        // 

        private static NamedPermissionSet s_everything;
        private static NamedPermissionSet s_execution;
        private static NamedPermissionSet s_fullTrust;
        private static NamedPermissionSet s_internet;
        private static NamedPermissionSet s_localIntranet;
        private static NamedPermissionSet s_nothing;
        private static NamedPermissionSet s_skipVerification;

        //
        // Standard permission sets
        //

        internal static NamedPermissionSet Everything
        {
            get { return GetOrDeserializeExtendablePermissionSet(ref s_everything, s_everythingXml); }
        }

        internal static NamedPermissionSet Execution
        {
            get { return GetOrDeserializePermissionSet(ref s_execution, s_executionXml); }
        }

        internal static NamedPermissionSet FullTrust
        {
            get { return GetOrDeserializePermissionSet(ref s_fullTrust, s_fullTrustXml); }
        }

        internal static NamedPermissionSet Internet
        {
            get { return GetOrDeserializeExtendablePermissionSet(ref s_internet, s_internetXml); }
        }

        internal static NamedPermissionSet LocalIntranet
        {
            get { return GetOrDeserializeExtendablePermissionSet(ref s_localIntranet, s_localIntranetXml); }
        }

        internal static NamedPermissionSet Nothing
        {
            get { return GetOrDeserializePermissionSet(ref s_nothing, s_nothingXml); }
        }

        internal static NamedPermissionSet SkipVerification
        {
            get { return GetOrDeserializePermissionSet(ref s_skipVerification, s_skipVerificationXml); }
        }

        //
        // Utility methods to construct the permission set objects from the well known XML and any permission
        // set extensions if necessary
        //

        private static NamedPermissionSet GetOrDeserializeExtendablePermissionSet(
            ref NamedPermissionSet permissionSet,
            string permissionSetXml)
        {
            Contract.Requires(!String.IsNullOrEmpty(permissionSetXml));
            return permissionSet.Copy() as NamedPermissionSet;
        }

        private static NamedPermissionSet GetOrDeserializePermissionSet(ref NamedPermissionSet permissionSet,
                                                                        string permissionSetXml)
        {
            Debug.Assert(!String.IsNullOrEmpty(permissionSetXml));
            return permissionSet.Copy() as NamedPermissionSet;
        }
    }
}
