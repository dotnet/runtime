// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using Microsoft.Quic;

namespace System.Net.Quic.Tests
{
    public class MsQuicInteropTests
    {
        private const DynamicallyAccessedMemberTypes FieldsAndProperties =
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;
        private const BindingFlags InstanceMembers = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private static MemberInfo[] GetMembers<
            [DynamicallyAccessedMembers(FieldsAndProperties)] T>()
        {
            var members = typeof(T).GetFields(InstanceMembers).Cast<MemberInfo>()
                .Concat(typeof(T).GetProperties(InstanceMembers).Where(property => property.GetSetMethod() is not null))
                .ToArray();

            Assert.NotEmpty(members);

            return members;
        }

        [RequiresUnreferencedCode("Resets members using reflection")]
        private static void ResetMember(MemberInfo member, object instance)
        {
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(instance, Activator.CreateInstance(field.FieldType));
                    break;
                case PropertyInfo property:
                    property.SetValue(instance, Activator.CreateInstance(property.PropertyType));
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
#pragma warning disable IL2026 // https://github.com/dotnet/runtime/issues/126862
                ResetMember(member, boxed);
#pragma warning restore IL2026
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
