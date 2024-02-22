// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ConcurrencyTests : ContainerTests
    {
        [Export, Shared]
        public class PausesDuringActivation
        {
            public bool IsActivationComplete { get; set; }

            [OnImportsSatisfied]
            public void OnImportsSatisfied()
            {
                Task.Delay(200).Wait();
                IsActivationComplete = true;
            }
        }

        // This does not test the desired behaviour deterministically,
        // but is close enough to be repeatable at least on my machine :)
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void SharedInstancesAreNotVisibleUntilActivationCompletes()
        {
            var c = CreateContainer(typeof(PausesDuringActivation));
            Task.Run(() => c.GetExport<PausesDuringActivation>());
            Task.Delay(50).Wait();
            var pda = c.GetExport<PausesDuringActivation>();
            Assert.True(pda.IsActivationComplete);
        }
    }
}
