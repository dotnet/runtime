// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class ApplicationTrustTests
    {
        [Fact]
        public static void ApplicationTrustCollectionCallMethods()
        {
            ApplicationTrustCollection atc = (ApplicationTrustCollection)Activator.CreateInstance(typeof(ApplicationTrustCollection), true);
            ApplicationTrust at = new ApplicationTrust();
            int testint = atc.Add(at);
            ApplicationTrust[] atarray = new ApplicationTrust[1];
            atc.AddRange(atarray);
            atc.AddRange(atc);
            atc.Clear();
            atc.CopyTo(atarray, 0);
            ApplicationTrustEnumerator ate = atc.GetEnumerator();
            atc.Remove(at);
            atc.RemoveRange(atarray);
            atc.RemoveRange(atc);
        }

        [Fact]
        public static void ApplicationTrustEnumeratorCallMethods()
        {
            ApplicationTrustEnumerator ate = (ApplicationTrustEnumerator)Activator.CreateInstance(typeof(ApplicationTrustEnumerator), true);
            bool testbool = ate.MoveNext();
            ate.Reset();
        }
    }
}
