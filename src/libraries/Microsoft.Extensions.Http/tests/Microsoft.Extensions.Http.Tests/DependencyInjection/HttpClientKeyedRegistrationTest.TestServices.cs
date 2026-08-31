// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class HttpClientKeyedRegistrationTest
    {
        public const string Test = "test";
        public const string Other = "other";
        public const string Disabled = "disabled";
        public const string KeyedDefaults = "keyed-defaults";
        public const string Absent = "absent";
        public const string OtherAbsent = "other-absent";

        internal class ServiceWithTestClient : TestTypedClient
        {
            public ServiceWithTestClient([FromKeyedServices(Test)] HttpClient httpClient) : base(httpClient) { }
        }

        internal class ServiceWithOtherClient : TestTypedClient
        {
            public ServiceWithOtherClient([FromKeyedServices(Other)] HttpClient httpClient) : base(httpClient) { }
        }

        internal class ServiceWithDisabledClient : TestTypedClient
        {
            public ServiceWithDisabledClient([FromKeyedServices(Disabled)] HttpClient httpClient) : base(httpClient) { }
        }

        internal class ServiceWithKeyedDefaultsClient : TestTypedClient
        {
            public ServiceWithKeyedDefaultsClient([FromKeyedServices(KeyedDefaults)] HttpClient httpClient) : base(httpClient) { }
        }

        internal class ServiceWithAbsentClient : TestTypedClient
        {
            public ServiceWithAbsentClient([FromKeyedServices(Absent)] HttpClient httpClient) : base(httpClient) { }
        }

        internal class ServiceWithHandler
        {
            public HttpMessageHandler Handler { get; }
            public ServiceWithHandler(HttpMessageHandler handler)
            {
                Handler = handler;
            }
        }

        internal class ServiceWithTestHandler : ServiceWithHandler
        {
            public ServiceWithTestHandler([FromKeyedServices(Test)] HttpMessageHandler handler) : base(handler) { }
        }

        internal class ServiceWithOtherHandler : ServiceWithHandler
        {
            public ServiceWithOtherHandler([FromKeyedServices(Other)] HttpMessageHandler handler) : base(handler) { }
        }

        internal class ServiceWithDisabledHandler : ServiceWithHandler
        {
            public ServiceWithDisabledHandler([FromKeyedServices(Disabled)] HttpMessageHandler handler) : base(handler) { }
        }

        internal class ServiceWithKeyedDefaultsHandler : ServiceWithHandler
        {
            public ServiceWithKeyedDefaultsHandler([FromKeyedServices(KeyedDefaults)] HttpMessageHandler handler) : base(handler) { }
        }

        internal class ServiceWithAbsentHandler : ServiceWithHandler
        {
            public ServiceWithAbsentHandler([FromKeyedServices(Absent)] HttpMessageHandler handler) : base(handler) { }
        }
    }
}
