// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.AccessControl;

namespace System.IO.Pipes
{
    public static class PipesAclExtensions
    {
        public static PipeSecurity GetAccessControl(PipeStream stream)
        {
            return stream.GetAccessControl();
        }

        public static void SetAccessControl(PipeStream stream, PipeSecurity pipeSecurity)
        {
            stream.SetAccessControl(pipeSecurity);
        }
    }
}