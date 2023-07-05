// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    // Pluggable file that adds PDB handling functionality to EcmaMethodIL
    public partial class EcmaMethodIL
    {
        public override MethodDebugInformation GetDebugInfo()
        {
            return new EcmaMethodDebugInformation(_method);
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
            _method = method;
        }

        public override bool IsStateMachineMoveNextMethod
        {
            get
            {
                PdbSymbolReader reader = _method.Module.PdbReader;
                return reader != null
                    ? reader.GetStateMachineKickoffMethod(MetadataTokens.GetToken(_method.Handle)) != 0
                    : false;
            }
        }

        public override IEnumerable<ILSequencePoint> GetSequencePoints()
        {
            PdbSymbolReader reader = _method.Module.PdbReader;
            return reader != null
                ? reader.GetSequencePointsForMethod(MetadataTokens.GetToken(_method.Handle))
                : Array.Empty<ILSequencePoint>();
        }

        public override IEnumerable<ILLocalVariable> GetLocalVariables()
        {
            PdbSymbolReader reader = _method.Module.PdbReader;
            return reader != null
                ? reader.GetLocalVariableNamesForMethod(MetadataTokens.GetToken(_method.Handle))
                : Array.Empty<ILLocalVariable>();
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
