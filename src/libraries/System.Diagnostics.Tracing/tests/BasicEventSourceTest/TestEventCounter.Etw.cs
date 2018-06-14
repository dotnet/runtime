// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace BasicEventSourceTests
{
    public partial class TestEventCounter
    {
        // Specifies whether the process is elevated or not.
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(AdminHelpers.IsProcessElevated);
        private static bool IsProcessElevated => s_isElevated.Value;

        [ConditionalFact(nameof(IsProcessElevated))]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/27106")]
        public void Test_Write_Metric_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_Write_Metric(listener);
            }
        }
    }
}
