// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using Xunit;

namespace BasicEventSourceTests
{
    internal class ContractEventSourceWithTraits : EventSource
    {
        public ContractEventSourceWithTraits() : base(EventSourceSettings.Default,
            "MyTrait", "MyTraitValue",
            "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}",
            "ETW_2", "#01 02 03 04",    // New binary trait
            "ETW_3", "@Hello"           // New string trait
            )
        { }
    }


    public class TestsTraits
    {
        /// <summary>
        /// Tests EventSource Traits.
        /// </summary>
        [Fact]
        public void Test_EventSource_Traits_Contract()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new ContractEventSourceWithTraits())
            {
                // By default we are self-describing.
                Assert.Equal(EventSourceSettings.EtwSelfDescribingEventFormat, mySource.Settings);
                Assert.Equal("MyTraitValue", mySource.GetTrait("MyTrait"));
                Assert.Equal("{4f50731a-89cf-4782-b3e0-dce8c90476ba}", mySource.GetTrait("ETW_GROUP"));
                Assert.Equal("#01 02 03 04", mySource.GetTrait("ETW_2"));
                Assert.Equal("@Hello", mySource.GetTrait("ETW_3"));
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        public void Test_EventSource_Traits_Dynamic()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new EventSource("DynamicEventSourceWithTraits", EventSourceSettings.Default,
                "MyTrait", "MyTraitValue",
                "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}"))
            {
                // By default we are self-describing.
                Assert.Equal(EventSourceSettings.EtwSelfDescribingEventFormat, mySource.Settings);
                Assert.Equal("MyTraitValue", mySource.GetTrait("MyTrait"));
                Assert.Equal("{4f50731a-89cf-4782-b3e0-dce8c90476ba}", mySource.GetTrait("ETW_GROUP"));
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        public void Test_EventSource_Traits_Contract_Guid()
        {
            var guid = new Guid("{EB2FA63A-C72F-4D58-B6AC-ED6F82E9BF38}");
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new EventSource("MyEventSource", guid))
            {
                Assert.Equal(EventSourceSettings.EtwManifestEventFormat, mySource.Settings);
                Assert.Equal(guid, mySource.Guid);
                Assert.Equal("MyEventSource", mySource.Name);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        public void Test_EventSource_Traits_Contract_Guid_Manifest()
        {
            var guid = new Guid("{B8CE9801-6828-4311-AF29-72DB02EA9D8B}");
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new EventSource("MyEventSource", guid, EventSourceSettings.EtwSelfDescribingEventFormat))
            {
                Assert.Equal(EventSourceSettings.EtwSelfDescribingEventFormat, mySource.Settings);
                Assert.Equal(guid, mySource.Guid);
                Assert.Equal("MyEventSource", mySource.Name);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [Fact]
        public void Test_EventSource_Traits_Dynamic_Guid()
        {
            var guid = new Guid("{C96ADE53-54EE-4AEA-B5E4-369E97AD7DD2}");
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new EventSource("DynamicEventSourceWithTraits",
                       guid, EventSourceSettings.Default,
                       ["MyTrait", "MyTraitValue",
                       "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}"]))
            {
                Assert.Equal(EventSourceSettings.EtwSelfDescribingEventFormat, mySource.Settings);
                Assert.Equal(guid, mySource.Guid);
                Assert.Equal("MyTraitValue", mySource.GetTrait("MyTrait"));
                Assert.Equal("{4f50731a-89cf-4782-b3e0-dce8c90476ba}", mySource.GetTrait("ETW_GROUP"));
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
