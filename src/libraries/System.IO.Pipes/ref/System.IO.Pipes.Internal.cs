// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Pipes
{
    // The following types are incomplete, meant only to be the target of type forwards
    public enum PipeAccessRights { }
    public sealed class PipeAccessRule { }
    public sealed class PipeAuditRule { }
    public static class PipesAclExtensions { }
    public class PipeSecurity { }

#if NET5_0_OR_GREATER
    public static class AnonymousPipeServerStreamAcl { }
    public static class NamedPipeServerStreamAcl { }
#endif
}