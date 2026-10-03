// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class ReferralCallbackTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var callback = new ReferralCallback();
            Assert.Null(callback.DereferenceConnection);
            Assert.Null(callback.NotifyNewConnection);
            Assert.Null(callback.QueryForConnection);
        }

        [Fact]
        public void DereferenceConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { DereferenceConnection = DereferenceConnection };
            Assert.Equal(DereferenceConnection, callback.DereferenceConnection);
        }

        [Fact]
        public void NotifyNewConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { NotifyNewConnection = NotifyNewConnection };
            Assert.Equal(NotifyNewConnection, callback.NotifyNewConnection);
        }

        [Fact]
        public void QueryForConnection_Set_GetReturnsExpected()
        {
            var callback = new ReferralCallback { QueryForConnection = QueryForConnection };
            Assert.Equal(QueryForConnection, callback.QueryForConnection);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "This test validates implementation details of the repository implementation.")]
        public void ProcessQueryConnection_MarshalsHostNameAsAnsi()
        {
            const string HostName = "server01";
            const int PortNumber = 389;
            string[] servers = null;

            using (var connection = new LdapConnection("server"))
            {
                connection.SessionOptions.ReferralCallback = new ReferralCallback
                {
                    QueryForConnection = (primaryConnection, referralFromConnection, newDistinguishedName, identifier, credential, currentUserToken) =>
                    {
                        servers = identifier.Servers;
                        return null;
                    }
                };

                InvokeProcessQueryConnection(connection.SessionOptions, HostName, PortNumber);
            }

            Assert.Equal(new[] { $"{HostName}:{PortNumber}" }, servers);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "This test validates implementation details of the repository implementation.")]
        public void ProcessNotifyConnection_MarshalsHostNameAsAnsi()
        {
            const string HostName = "server01";
            const int PortNumber = 389;
            string[] servers = null;

            using (var connection = new LdapConnection("server"))
            using (var newConnection = new LdapConnection("server"))
            {
                connection.SessionOptions.ReferralCallback = new ReferralCallback
                {
                    NotifyNewConnection = (primaryConnection, referralFromConnection, newDistinguishedName, identifier, notifiedConnection, credential, currentUserToken, errorCodeFromBind) =>
                    {
                        servers = identifier.Servers;
                        return false;
                    }
                };

                InvokeProcessNotifyConnection(connection.SessionOptions, HostName, GetConnectionHandle(newConnection), PortNumber);
            }

            Assert.Equal(new[] { $"{HostName}:{PortNumber}" }, servers);
        }

        private static unsafe void InvokeProcessQueryConnection(LdapSessionOptions options, string hostName, int portNumber)
        {
            MethodInfo method = GetCallbackMethod("ProcessQueryConnection");
            ParameterInfo[] parameters = method.GetParameters();
            IntPtr hostNamePointer = AllocateAnsiString(hostName);
            IntPtr currentUserPointer = Marshal.AllocHGlobal(sizeof(long));
            IntPtr connectionToUsePointer = Marshal.AllocHGlobal(IntPtr.Size);

            try
            {
                Marshal.WriteInt64(currentUserPointer, 0);
                Marshal.WriteIntPtr(connectionToUsePointer, IntPtr.Zero);

                method.Invoke(options, new object[]
                {
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    hostNamePointer,
                    portNumber,
                    null,
                    Pointer.Box(currentUserPointer.ToPointer(), parameters[6].ParameterType),
                    Pointer.Box(connectionToUsePointer.ToPointer(), parameters[7].ParameterType)
                });
            }
            finally
            {
                Marshal.FreeHGlobal(connectionToUsePointer);
                Marshal.FreeHGlobal(currentUserPointer);
                Marshal.FreeHGlobal(hostNamePointer);
            }
        }

        private static unsafe void InvokeProcessNotifyConnection(LdapSessionOptions options, string hostName, IntPtr newConnection, int portNumber)
        {
            MethodInfo method = GetCallbackMethod("ProcessNotifyConnection");
            ParameterInfo[] parameters = method.GetParameters();
            IntPtr hostNamePointer = AllocateAnsiString(hostName);
            IntPtr currentUserPointer = Marshal.AllocHGlobal(sizeof(long));

            try
            {
                Marshal.WriteInt64(currentUserPointer, 0);

                method.Invoke(options, new object[]
                {
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    hostNamePointer,
                    newConnection,
                    portNumber,
                    null,
                    Pointer.Box(currentUserPointer.ToPointer(), parameters[7].ParameterType),
                    0
                });
            }
            finally
            {
                Marshal.FreeHGlobal(currentUserPointer);
                Marshal.FreeHGlobal(hostNamePointer);
            }
        }

        private static IntPtr AllocateAnsiString(string value)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 2);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            Marshal.WriteByte(pointer, bytes.Length + 1, 0);
            return pointer;
        }

        private static MethodInfo GetCallbackMethod(string name) =>
            typeof(LdapSessionOptions).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private static IntPtr GetConnectionHandle(LdapConnection connection) =>
            ((SafeHandle)typeof(LdapConnection).GetField("_ldapHandle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection)).DangerousGetHandle();

        internal static void DereferenceConnection(LdapConnection primaryConnection, LdapConnection connectionToDereference) { }
        internal static bool NotifyNewConnection(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, LdapConnection newConnection, NetworkCredential credential, long currentUserToken, int errorCodeFromBind) => true;
        internal static LdapConnection QueryForConnection(LdapConnection primaryConnection, LdapConnection referralFromConnection, string newDistinguishedName, LdapDirectoryIdentifier identifier, NetworkCredential credential, long currentUserToken) => null;
    }
}
