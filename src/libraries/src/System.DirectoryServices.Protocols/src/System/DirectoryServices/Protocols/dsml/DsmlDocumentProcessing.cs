// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    public enum DsmlDocumentProcessing
    {
        Sequential = 0,
        Parallel = 1
    }

    public enum DsmlResponseOrder
    {
        Sequential = 0,
        Unordered = 1
    }

    public enum DsmlErrorProcessing
    {
        Resume = 0,
        Exit = 1
    }

    public enum ErrorResponseCategory
    {
        NotAttempted = 0,
        CouldNotConnect = 1,
        ConnectionClosed = 2,
        MalformedRequest = 3,
        GatewayInternalError = 4,
        AuthenticationFailed = 5,
        UnresolvableUri = 6,
        Other = 7
    }
}
