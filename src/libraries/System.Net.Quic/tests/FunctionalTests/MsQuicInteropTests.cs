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
            var members = typeof(T).FindMembers(MemberTypes.Field | MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, (mi, _) =>
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
                ResetMember(member, boxed);
                Assert.False(settings.Equals((QUIC_SETTINGS)boxed), $"Member {member.Name} is not compared.");
            }
        }
    }
}
