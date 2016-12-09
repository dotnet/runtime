//------------------------------------------------------------------------------
// <copyright file="AuthTypes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
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
