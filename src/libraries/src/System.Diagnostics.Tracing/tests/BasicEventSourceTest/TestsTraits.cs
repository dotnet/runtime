// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using Xunit;
using System;

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
        [ActiveIssue(4871, PlatformID.AnyUnix)]
        [Fact]
        public void Test_EventSource_Traits_Contract()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new ContractEventSourceWithTraits())
            {
                // By default we are self-describing.  
                Assert.Equal(mySource.Settings, EventSourceSettings.EtwSelfDescribingEventFormat);
                Assert.Equal(mySource.GetTrait("MyTrait"), "MyTraitValue");
                Assert.Equal(mySource.GetTrait("ETW_GROUP"), "{4f50731a-89cf-4782-b3e0-dce8c90476ba}");
                Assert.Equal(mySource.GetTrait("ETW_2"), "#01 02 03 04");
                Assert.Equal(mySource.GetTrait("ETW_3"), "@Hello");
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        [ActiveIssue(4871, PlatformID.AnyUnix)]
        [Fact]
        public void Test_EventSource_Traits_Dynamic()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            using (var mySource = new EventSource("DynamicEventSourceWithTraits", EventSourceSettings.Default,
                "MyTrait", "MyTraitValue",
                "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}"))
            {
                // By default we are self-describing.  
                Assert.Equal(mySource.Settings, EventSourceSettings.EtwSelfDescribingEventFormat);
                Assert.Equal(mySource.GetTrait("MyTrait"), "MyTraitValue");
                Assert.Equal(mySource.GetTrait("ETW_GROUP"), "{4f50731a-89cf-4782-b3e0-dce8c90476ba}");
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
