// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public partial interface IMembershipCondition : ISecurityEncodable, ISecurityPolicyEncodable
    {
        bool Check(Evidence evidence);
        IMembershipCondition Copy();
        bool Equals(object obj);
        string ToString();
    }
}
