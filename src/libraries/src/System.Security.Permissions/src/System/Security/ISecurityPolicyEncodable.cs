// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial interface ISecurityPolicyEncodable
    {
        void FromXml(SecurityElement e, System.Security.Policy.PolicyLevel level);
        SecurityElement ToXml(System.Security.Policy.PolicyLevel level);
    }
}
