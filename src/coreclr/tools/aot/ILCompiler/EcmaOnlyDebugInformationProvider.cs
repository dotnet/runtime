// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;

namespace ILCompiler
{
    /// <summary>
    /// Provides debug information for ECMA-based <see cref="MethodIL"/> only.
    /// </summary>
    public class EcmaOnlyDebugInformationProvider : DebugInformationProvider
    {
        public override MethodDebugInformation GetDebugInfo(MethodIL methodIL)
        {
            MethodIL definitionIL = methodIL.GetMethodILDefinition();
            if (definitionIL is EcmaMethodIL)
                return methodIL.GetDebugInfo();

            return MethodDebugInformation.None;
        }
    }
}
