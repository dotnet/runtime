// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;

namespace Internal.TypeSystem
{
    public sealed partial class MethodSignature
    {
        public bool ReturnsTaskOrValueTask()
        {
            TypeDesc ret = this.ReturnType;

            if (ret is MetadataType md
                && md.Module == this.Context.SystemModule
                && md.Namespace.SequenceEqual("System.Threading.Tasks"u8))
            {
                ReadOnlySpan<byte> name = md.Name;
                if (name.SequenceEqual("Task"u8) || name.SequenceEqual("Task`1"u8)
                    || name.SequenceEqual("ValueTask"u8) || name.SequenceEqual("ValueTask`1"u8))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
