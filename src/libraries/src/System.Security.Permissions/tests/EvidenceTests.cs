// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class EvidenceTests
    {
        [Fact]
        public static void EvidenceCallMethods()
        {
            Evidence e = new Evidence();
            e = new Evidence(new Evidence());
            e.Clear();
            Evidence e2 = e.Clone();
            System.Collections.IEnumerator ie = e.GetAssemblyEnumerator();
            ie = e.GetHostEnumerator();
            e.Merge(e2);
        }
    }
}
