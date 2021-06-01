// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NETCore.Platforms.BuildTasks
{
    public partial class BuildTask
    {
        static BuildTask()
        {
            AssemblyResolver.Enable();
        }
    }
}
