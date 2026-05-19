// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

using Microsoft.Quic;

namespace System.Net.Quic.Tests
{
    public class MsQuicInteropTests
    {
        private static MemberInfo[] GetMembers<T>()
        {
#pragma warning disable IL2090
            var members = typeof(T).FindMembers(MemberTypes.Field | MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, (mi, _) =>
#pragma warning restore IL2090
            {
                if (mi is PropertyInfo property && property.GetSetMethod() == null)
                {
                    return false;
                }

                return true;
            }, null);

            Assert.NotEmpty(members);

            return members;
        }

        private static void ResetMember(MemberInfo member, object instance)
        {
            switch (member)
            {
                case FieldInfo field:
#pragma warning disable IL2072
                    field.SetValue(instance, Activator.CreateInstance(field.FieldType));
#pragma warning restore IL2072
                    break;
                case PropertyInfo property:
#pragma warning disable IL2072
                    property.SetValue(instance, Activator.CreateInstance(property.PropertyType));
#pragma warning restore IL2072
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected member type: {member.MemberType}");
            }
        }

        [Fact]
        public void QuicSettings_Equals_RespectsAllMembers()
        {
            QUIC_SETTINGS settings = new QUIC_SETTINGS();

            // make sure the extension definition is included in compilation
            Assert.Contains(typeof(IEquatable<QUIC_SETTINGS>), typeof(QUIC_SETTINGS).GetInterfaces());

            var settingsSpan = MemoryMarshal.AsBytes(new Span<QUIC_SETTINGS>(ref settings));

            // Fill the memory with 1s,we will try to zero out each member and compare
            settingsSpan.Fill(0xFF);

            foreach (MemberInfo member in GetMembers<QUIC_SETTINGS>())
            {
                // copy and box the instance because reflection methods take a reference type arg
                object boxed = settings;
                ResetMember(member, boxed);
                Assert.False(settings.Equals((QUIC_SETTINGS)boxed), $"Member {member.Name} is not compared.");
            }
        }

        [Fact]
        public void MsQuicCipherSuites_MapTo_TlsCipherSuite()
        {
            foreach (QUIC_CIPHER_SUITE msQuicCipherSuite in Enum.GetValues<QUIC_CIPHER_SUITE>())
            {
                // both QUIC_CIPHER_SUITE and TlsCipherSuite members use the IANA identifiers of the TLS cipher suites, check that their values match
                TlsCipherSuite cipherSuite = (TlsCipherSuite)msQuicCipherSuite;
                // if same numerical value maps to the same enum member, the ToString() should be the same
                Assert.Equal(msQuicCipherSuite.ToString(), cipherSuite.ToString());
            }
        }
    }
}
