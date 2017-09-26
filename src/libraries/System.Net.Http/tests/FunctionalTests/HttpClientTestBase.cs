// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientTestBase : RemoteExecutorTestBase
    {
        private const string ManagedHandlerEnvVar = "COMPlus_UseManagedHttpClientHandler";
        private static readonly LocalDataStoreSlot s_managedHandlerSlot;

        static HttpClientTestBase()
        {
            s_managedHandlerSlot = Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
            if (s_managedHandlerSlot == null)
            {
                try
                {
                    s_managedHandlerSlot = Thread.AllocateNamedDataSlot(ManagedHandlerEnvVar);
                }
                catch (ArgumentException)
                {
                    s_managedHandlerSlot = Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
                }
            }
            Debug.Assert(s_managedHandlerSlot != null);
        }

        protected virtual bool UseManagedHandler => false;

        protected HttpClient CreateHttpClient() => new HttpClient(CreateHttpClientHandler());

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseManagedHandler);

        protected static HttpClient CreateHttpClient(string useManagedHandlerBoolString) =>
            new HttpClient(CreateHttpClientHandler(useManagedHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(string useManagedHandlerBoolString) =>
            CreateHttpClientHandler(bool.Parse(useManagedHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(bool useManagedHandler) =>
            useManagedHandler ? CreateManagedHttpClientHandler() : new HttpClientHandler();

        private static HttpClientHandler CreateManagedHttpClientHandler()
        {
            try
            {
                Thread.SetData(s_managedHandlerSlot, true);
                return new HttpClientHandler();
            }
            finally
            {
                Thread.SetData(s_managedHandlerSlot, null);
            }
        }
    }
}
