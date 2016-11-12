// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class ClientCertificateScenarioTest
    {
        public static object[][] ValidClientCertificates
        {
            get
            {
                var helper = new ClientCertificateHelper();
                return helper.ValidClientCertificates;
            }
        }

        public static object[][] InvalidClientCertificates
        {
            get
            {
                var helper = new ClientCertificateHelper();
                return helper.InvalidClientCertificates;
            }
        }

        public ClientCertificateScenarioTest()
        {
            TestControl.ResetAll();
        }

        [Fact]
        public void NonSecureRequest_AddNoCertificates_CertificateContextNotSet()
        {
            using (var handler = new WinHttpHandler())
            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                () => { },
                TestServer.FakeServerEndpoint))
            {
                Assert.Equal(0, APICallHistory.WinHttpOptionClientCertContext.Count);
            }
        }

        [Theory, MemberData(nameof(ValidClientCertificates))]
        public void NonSecureRequest_AddValidCertificate_CertificateContextNotSet(X509Certificate2 certificate)
        {
            using (var handler = new WinHttpHandler())
            {
                handler.ClientCertificates.Add(certificate);
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeServerEndpoint))
                {
                    Assert.Equal(0, APICallHistory.WinHttpOptionClientCertContext.Count);
                }
            }
        }

        [Fact]
        public void SecureRequest_AddNoCertificates_NullCertificateContextSet()
        {
            using (var handler = new WinHttpHandler())
            using (HttpResponseMessage response = SendRequestHelper.Send(
                handler,
                () => { },
                TestServer.FakeSecureServerEndpoint))
            {
                Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                Assert.Equal(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
            }
        }

        [Theory, MemberData(nameof(ValidClientCertificates))]
        public void SecureRequest_AddValidCertificate_ValidCertificateContextSet(X509Certificate2 certificate)
        {
            using (var handler = new WinHttpHandler())
            {
                handler.ClientCertificates.Add(certificate);
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.NotEqual(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }

        [Theory, MemberData(nameof(InvalidClientCertificates))]
        public void SecureRequest_AddInvalidCertificate_NullCertificateContextSet(X509Certificate2 certificate)
        {
            using (var handler = new WinHttpHandler())
            {
                handler.ClientCertificates.Add(certificate);
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.Equal(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }
        
        [Fact]
        public void SecureRequest_ClientCertificateOptionAutomatic_CertStoreEmpty_NullCertificateContextSet()
        {
            using (var handler = new WinHttpHandler())
            {
                handler.ClientCertificateOption = ClientCertificateOption.Automatic;
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.Equal(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }
        
        [Fact]
        public void SecureRequest_ClientCertificateOptionAutomatic_CertStoreHasInvalidCerts_NullCertificateContextSet()
        {
            using (var handler = new WinHttpHandler())
            {
                var helper = new ClientCertificateHelper();
                TestControl.CurrentUserCertificateStore = helper.InvalidClientCertificateCollection;
                handler.ClientCertificateOption = ClientCertificateOption.Automatic;
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.Equal(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }
        
        [Fact]
        public void SecureRequest_ClientCertificateOptionAutomatic_CertStoreHasValidCerts_ValidCertificateContextSet()
        {
            using (var handler = new WinHttpHandler())
            {
                var helper = new ClientCertificateHelper();
                TestControl.CurrentUserCertificateStore = helper.ValidClientCertificateCollection;
                handler.ClientCertificateOption = ClientCertificateOption.Automatic;
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.NotEqual(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }
        
        
        [Fact]
        public void SecureRequest_ClientCertificateOptionAutomatic_CertStoreHasValidAndInvalidCerts_ValidCertificateContextSet()
        {
            using (var handler = new WinHttpHandler())
            {
                var helper = new ClientCertificateHelper();
                TestControl.CurrentUserCertificateStore = helper.ValidAndInvalidClientCertificateCollection;
                handler.ClientCertificateOption = ClientCertificateOption.Automatic;
                using (HttpResponseMessage response = SendRequestHelper.Send(
                    handler,
                    () => { },
                    TestServer.FakeSecureServerEndpoint))
                {
                    Assert.Equal(1, APICallHistory.WinHttpOptionClientCertContext.Count);
                    Assert.NotEqual(IntPtr.Zero, APICallHistory.WinHttpOptionClientCertContext[0]);
                }
            }
        }        
    }
}
