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
        private const string ManagedHandlerSettingName = "COMPlus_UseManagedHttpClientHandler";
        private const string AppCtxManagedHandlerSettingName = "System.Net.Http.UseManagedHttpClientHandler";

        private static LocalDataStoreSlot s_useManagedHandlerSlot;

        private static bool UseManagedHandler
        {
            get
            {
                // Check the environment variable to see if it's been set to true.  If it has, use the managed handler.
                if (Environment.GetEnvironmentVariable(ManagedHandlerSettingName) == "true")
                {
                    return true;
                }

                if (AppContext.TryGetSwitch(AppCtxManagedHandlerSettingName, out bool isManagedEnabled) && isManagedEnabled)
                {
                    return true;
                }

                // Then check whether a thread local has been set with the same name.
                // If it's been set to a Boolean true, also use the managed handler.
                LocalDataStoreSlot slot = LazyInitializer.EnsureInitialized(ref s_useManagedHandlerSlot, () =>
                {
                    LocalDataStoreSlot local = Thread.GetNamedDataSlot(ManagedHandlerSettingName);
                    if (local == null)
                    {
                        try
                        {
                            local = Thread.AllocateNamedDataSlot(ManagedHandlerSettingName);
                        }
                        catch (ArgumentException)
                        {
                            local = Thread.GetNamedDataSlot(ManagedHandlerSettingName);
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
