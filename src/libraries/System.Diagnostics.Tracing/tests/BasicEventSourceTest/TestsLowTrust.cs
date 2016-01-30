// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;
using System;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace BasicEventSourceTests
{
    
    public class TestsLowTrust
    {
        /// <summary>
        /// Very simple low trust test that calls directly into a low trust method
        /// without creating a separate app domain
        /// </summary>
        [Fact]
        public void Test_EventSource_LowTrust()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            Console.WriteLine("Running Array.CreateInstance() in low trust...");
            RunLowTrustTest();
            Console.WriteLine("Success...");
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [SecurityPermission(SecurityAction.PermitOnly, Execution = true)]
        static void RunLowTrustTest()
        {
            System.Array arr = Array.CreateInstance(typeof(byte), 100L, 200L);
            using (var es = new LowTrustTestEventSource())
            {
                es.Event0();
            }
        }

        #region support event source
        class LowTrustTestEventSource : EventSource
        {
            [Event(1)]
            public void Event0()
            { WriteEvent(1); }
        }
        #endregion support event source

        /// <summary>
        /// Low trust test that creates separate app domains with a variety of permissions
        /// </summary>
        [Fact]
        public void Test_EventSource_LowTrustAppDomain()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            var scenarios =
                new[]
                {
                    new { Zone = SecurityZone.Internet, TrustEventSource = false, GrantUnmanagedCodePermission = false },
                    new { Zone = SecurityZone.Internet, TrustEventSource = false, GrantUnmanagedCodePermission = true },
                    new { Zone = SecurityZone.Internet, TrustEventSource = true, GrantUnmanagedCodePermission = false },
                    new { Zone = SecurityZone.Internet, TrustEventSource = true, GrantUnmanagedCodePermission = true },
                    new { Zone = SecurityZone.Intranet, TrustEventSource = false, GrantUnmanagedCodePermission = false },
                    new { Zone = SecurityZone.Intranet, TrustEventSource = false, GrantUnmanagedCodePermission = true },
                    new { Zone = SecurityZone.Intranet, TrustEventSource = true, GrantUnmanagedCodePermission = false },
                    new { Zone = SecurityZone.Intranet, TrustEventSource = true, GrantUnmanagedCodePermission = true },
                    new { Zone = SecurityZone.MyComputer, TrustEventSource = true, GrantUnmanagedCodePermission = true },
                };

            foreach (var scenario in scenarios)
            {
                RunScenario(scenario.Zone, scenario.TrustEventSource, scenario.GrantUnmanagedCodePermission);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        private static void RunScenario(SecurityZone zone, bool fullyTrustEventSource, bool grantUnmanagedCodePermission)
        {
            Console.Write("Running scenario for zone '{0}', fully trusted EventSource {1}, unmanaged permission {2}: ", zone, fullyTrustEventSource, grantUnmanagedCodePermission);

            var evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(zone));
            var permissionSet = SecurityManager.GetStandardSandbox(evidence);
            if (!permissionSet.IsUnrestricted() && grantUnmanagedCodePermission)
            {
                permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
            }

            var eventSourceAssemblyName = typeof(EventSource).Assembly.GetName();
            var fullyTrustedAssemblies =
                fullyTrustEventSource
                    ? new StrongName[]  
                    {
                        new StrongName(new StrongNamePublicKeyBlob(eventSourceAssemblyName.GetPublicKey()), eventSourceAssemblyName.Name, eventSourceAssemblyName.Version)
                    }
                    : new StrongName[0];

            var info = new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory };
            info.ApplicationTrust = new ApplicationTrust(permissionSet, fullyTrustedAssemblies);

            var appDomain =
                AppDomain.CreateDomain(
                    "partial trust",
                    evidence,
                    info);
            try
            {
                var tester = (LttADEventSourceTester)appDomain
                    .CreateInstanceAndUnwrap(
                        typeof(LttADEventSourceTester).Assembly.GetName().Name,
                        typeof(LttADEventSourceTester).FullName);
                tester.IsEventSourceAssmFullyTrusted = fullyTrustEventSource;
                tester.DoStuff(1);
                tester.DoStuff(2);
                tester.DoStuff(3);

                Assert.IsTrue(tester.IsStateValid, "EventSource ConstructionException as expected");
                Console.WriteLine("SUCCESS");
                Console.WriteLine();
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }

            Console.WriteLine("==================================================================");
            Console.WriteLine();
        }
    }

    public class LttADEventSourceTester : MarshalByRefObject
    {
        public void DoStuff(int p)
        {
            using (var eventListener = new DummyEventListener())
            {
                // when the MDT.EventSource assembly is not fully trusted EnableEvents raises a SecurityException
                if (IsEventSourceAssmFullyTrusted)
                {
                    eventListener.EnableEvents(LttADEventSource.Log, EventLevel.LogAlways);
                }

                LttADEventSource.Log.MyEvent("test", p);
            }
        }

        public bool IsEventSourceAssmFullyTrusted { get; set; }
        public bool IsStateValid { get { return LttADEventSource.Log.ConstructionException == null || !IsEventSourceAssmFullyTrusted; } }

        #region Support classes: event source and event listener

        public sealed class LttADEventSource : EventSource
        {
            public static readonly LttADEventSource Log = new LttADEventSource();

            [Event(1)]
            public void MyEvent(string p1, int p2)
            {
                this.WriteEvent(1, p1, p2);
            }
        }

        public class DummyEventListener : EventListener
        {
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (true)
                {
                    Console.WriteLine(
                        "Event received: {0} {1} \"{2}\"",
                        eventData.EventSource.Name,
                        eventData.EventName,
                        string.Join(", ", eventData.Payload.ToArray()));
                }
            }
        }

        #endregion Support classes: event source and event listener
    }
}
