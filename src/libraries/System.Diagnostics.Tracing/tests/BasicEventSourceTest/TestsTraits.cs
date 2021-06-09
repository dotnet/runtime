// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
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
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51382", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
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
    }
}
