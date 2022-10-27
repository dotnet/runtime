// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// Asserts that the given documentation signature string does not resolve
    /// to the member with this attribute.
    public class ExpectUnresolvedDocumentationSignatureAttribute : BaseMemberAssertionAttribute
    {
        public ExpectUnresolvedDocumentationSignatureAttribute(string expected)
        {
        }
    }
}
