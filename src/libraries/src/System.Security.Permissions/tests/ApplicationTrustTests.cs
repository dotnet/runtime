// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class ApplicationTrustTests
    {
        [Fact]
        public static void ApplicationTrustCollectionCallMethods()
        {
            Policy.ApplicationTrustCollection atc = (Policy.ApplicationTrustCollection)Activator.CreateInstance(typeof(Policy.ApplicationTrustCollection), true);
            Policy.ApplicationTrust at = new Policy.ApplicationTrust();
            int testint = atc.Add(at);
            Policy.ApplicationTrust[] atarray = new Policy.ApplicationTrust[1];
            atc.AddRange(atarray);
            atc.AddRange(atc);
            atc.Clear();
            atc.CopyTo(atarray, 0);
            Policy.ApplicationTrustEnumerator ate = atc.GetEnumerator();
            atc.Remove(at);
            atc.RemoveRange(atarray);
            atc.RemoveRange(atc);
        }
        [Fact]
        public static void ApplicationTrustEnumeratorCallMethods()
        {
            Policy.ApplicationTrustEnumerator ate = (Policy.ApplicationTrustEnumerator)Activator.CreateInstance(typeof(Policy.ApplicationTrustEnumerator), true);
            bool testbool = ate.MoveNext();
            ate.Reset();
        }
    }
}
