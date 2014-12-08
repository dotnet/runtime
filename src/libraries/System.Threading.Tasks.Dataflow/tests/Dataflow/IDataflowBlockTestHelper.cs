// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    internal class IDataflowBlockTestHelper
    {
        internal static bool TestToString(Func<string, IDataflowBlock> blockFactory)
        {
            bool passed = true;
            // Test default
            {
                const string nameFormat = "{0} Id={1}";
                bool localPassed = true;
                var block = blockFactory(null);
                var toString = block.ToString();

                var expected = string.Format(nameFormat, block.GetType().Name, block.Completion.Id);
                localPassed &= toString.Equals(expected);
                string diagnostics = localPassed ? string.Empty : string.Format("(actual='{0}' expected='{1}')", toString, expected);

                Assert.True(localPassed, string.Format("{0}: default {1}", localPassed ? "Success" : "Failure", diagnostics));
                passed &= localPassed;
            }

            // Test with no args
            {
                const string nameFormat = "none";
                bool localPassed = true;
                var block = blockFactory(nameFormat);
                var toString = block.ToString();

                var expected = nameFormat;
                localPassed &= toString.Equals(expected);
                string diagnostics = localPassed ? string.Empty : string.Format("(actual='{0}' expected='{1}')", toString, expected);

                Assert.True(localPassed, string.Format("{0}: no args {1}", localPassed ? "Success" : "Failure", diagnostics));
                passed &= localPassed;
            }

            // Test with 1 arg
            {
                const string nameFormat = "foo {0}";
                bool localPassed = true;
                var block = blockFactory(nameFormat);
                var toString = block.ToString();

                var expected = string.Format(nameFormat, block.GetType().Name);
                localPassed &= toString.Equals(expected);
                string diagnostics = localPassed ? string.Empty : string.Format("(actual='{0}' expected='{1}')", toString, expected);

                Assert.True(localPassed, string.Format("{0}: 1 arg {1}", localPassed ? "Success" : "Failure", diagnostics));
                passed &= localPassed;
            }

            // Test with 2 args
            {
                const string nameFormat = "foo {0} bar {1}";
                bool localPassed = true;
                var block = blockFactory(nameFormat);
                var toString = block.ToString();

                var expected = string.Format(nameFormat, block.GetType().Name, block.Completion.Id);
                localPassed &= toString.Equals(expected);
                string diagnostics = localPassed ? string.Empty : string.Format("(actual='{0}' expected='{1}')", toString, expected);

                Assert.True(localPassed, string.Format("{0}: 2 args {1}", localPassed ? "Success" : "Failure", diagnostics));
                passed &= localPassed;
            }

            return passed;
        }
    }
}
