// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        // This partial implementation contains members common to all HttpClientHandler implementations.
        private const string ManagedHandlerEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_USEMANAGEDHTTPCLIENTHANDLER";
        private const string ManagedHandlerAppCtxSettingName = "System.Net.Http.UseManagedHttpClientHandler";

        private static LocalDataStoreSlot s_useManagedHandlerSlot;

        private static bool UseManagedHandler
        {
            get
            {
                // First check for the AppContext switch, giving it priority over over the environment variable.
                if (AppContext.TryGetSwitch(ManagedHandlerAppCtxSettingName, out bool isManagedEnabled))
                {
                    return isManagedEnabled;
                }

                // AppContext switch wasn't used. Check the environment variable to see if it's been set to true.
                string envVar = Environment.GetEnvironmentVariable(ManagedHandlerEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
                {
                    return true;
                }

                // TODO #23166: Remove the following TLS check assuming the type is exposed publicly.  If it's not,
                // re-evaluate the priority ordering of this with regards to the AppContext and environment settings.

                // Then check whether a thread local has been set with the same name.
                // If it's been set to a Boolean true, also use the managed handler.
                LocalDataStoreSlot slot = LazyInitializer.EnsureInitialized(ref s_useManagedHandlerSlot, () =>
                {
                    LocalDataStoreSlot local = Thread.GetNamedDataSlot(ManagedHandlerEnvironmentVariableSettingName);
                    if (local == null)
                    {
                        try
                        {
                            local = Thread.AllocateNamedDataSlot(ManagedHandlerEnvironmentVariableSettingName);
                        }
                        catch (ArgumentException)
                        {
                            local = Thread.GetNamedDataSlot(ManagedHandlerEnvironmentVariableSettingName);
                        }
                    }
                    return local;
                });
                Debug.Assert(slot != null);
                return Thread.GetData(slot) is bool result && result;
            }
        }

        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator { get; } = delegate { return true; };
    }
}
