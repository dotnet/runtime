// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    // Pluggable file that adds PDB handling functionality to EcmaMethodIL
    public partial class EcmaMethodIL
    {
        public override MethodDebugInformation GetDebugInfo()
        {
            if (_method.Module.PdbReader != null)
            {
                return new EcmaMethodDebugInformation(_method);
            }

            return MethodDebugInformation.None;
        }
    }

    /// <summary>
    /// Represents debugging information about a method backed by ECMA-335 metadata.
    /// </summary>
    public sealed class EcmaMethodDebugInformation : MethodDebugInformation
    {
        private EcmaMethod _method;

        public EcmaMethodDebugInformation(EcmaMethod method)
        {
            Debug.Assert(method.Module.PdbReader != null);
            _method = method;
        }

        public override bool IsStateMachineMoveNextMethod => _method.Module.PdbReader.GetStateMachineKickoffMethod(MetadataTokens.GetToken(_method.Handle)) != 0;

        public override IEnumerable<ILSequencePoint> GetSequencePoints()
        {
            return _method.Module.PdbReader.GetSequencePointsForMethod(MetadataTokens.GetToken(_method.Handle));
        }

        public override IEnumerable<ILLocalVariable> GetLocalVariables()
        {
            return _method.Module.PdbReader.GetLocalVariableNamesForMethod(MetadataTokens.GetToken(_method.Handle));
        }

        public override IEnumerable<string> GetParameterNames()
        {
            ParameterHandleCollection parameters = _method.MetadataReader.GetMethodDefinition(_method.Handle).GetParameters();

            if (!_method.Signature.IsStatic)
            {
                // TODO: this name might conflict with a parameter name or a local name. We need something unique.
                yield return "___this";
            }

            // TODO: The Params table is allowed to have holes in it. This expect all parameters to be present.
            foreach (var parameterHandle in parameters)
            {
                Parameter p = _method.MetadataReader.GetParameter(parameterHandle);

                // Parameter with sequence number 0 refers to the return parameter
                if (p.SequenceNumber == 0)
                    continue;

                yield return _method.MetadataReader.GetString(p.Name);
            }
        }
    }
}
