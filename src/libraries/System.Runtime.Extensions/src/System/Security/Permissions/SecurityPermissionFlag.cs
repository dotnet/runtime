// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    [Flags]
    public enum SecurityPermissionFlag
    {
        AllFlags = 16383,
        Assertion = 1,
        BindingRedirects = 8192,
        ControlAppDomain = 1024,
        ControlDomainPolicy = 256,
        ControlEvidence = 32,
        ControlPolicy = 64,
        ControlPrincipal = 512,
        ControlThread = 16,
        Execution = 8,
        Infrastructure = 4096,
        NoFlags = 0,
        RemotingConfiguration = 2048,
        SerializationFormatter = 128,
        SkipVerification = 4,
        UnmanagedCode = 2,
    }
}
