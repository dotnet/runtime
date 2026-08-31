// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;

namespace ILCompiler
{
    /// <summary>
    /// Provides debug information by delegating to the <see cref="MethodIL"/>.
    /// </summary>
    public class DebugInformationProvider
    {
        public virtual MethodDebugInformation GetDebugInfo(MethodIL methodIL)
        {
            return methodIL.GetDebugInfo();
        }
    }

    /// <summary>
    /// Provides empty debug information.
    /// </summary>
    public sealed class NullDebugInformationProvider : DebugInformationProvider
    {
        public override MethodDebugInformation GetDebugInfo(MethodIL methodIL)
        {
            return MethodDebugInformation.None;
        }
    }
}
