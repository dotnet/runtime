// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Security.Principal.Windows.Tests
{
    public class NTAccountTest
    {
        [Fact]
        public void Translate_Fail()
        {
            var nta = new NTAccount(Guid.NewGuid().ToString("N"));

            try
            {
                nta.Translate(typeof(SecurityIdentifier));
            }
            catch (Exception e)
            {
                Asserts(e);
            }

            try
            {
                nta.Translate(typeof(SecurityIdentifier));
            }
            catch (Exception e)
            {
                Asserts(e);
            }

            return;

            // Test assertions
            void Asserts(Exception exception)
            {
                // If machine is in a domain but off line throws Win32Exception
                Assert.True(exception is IdentityNotMappedException || exception is Win32Exception);
                if (exception is Win32Exception win32Exception)
                {
                    // ERROR_TRUSTED_RELATIONSHIP_FAILURE: The trust relationship between this workstation and the primary domain failed.
                    Assert.Equal(1789, win32Exception.NativeErrorCode);
                }
            }
        }
    }
}
