// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using ILCompiler.Logging;
using ILLink.Shared.TrimAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisMethodCallPattern
    {
        public readonly MethodIL MethodBody;
        public readonly ILOpcode Operation;
        public readonly int Offset;
        public readonly MethodDesc CalledMethod;
        public readonly MultiValue Instance;
        public readonly ImmutableArray<MultiValue> Arguments;
        public readonly MessageOrigin Origin;

        public TrimAnalysisMethodCallPattern(
            MethodIL methodBody,
            ILOpcode operation,
            int offset,
            MethodDesc calledMethod,
            MultiValue instance,
            ImmutableArray<MultiValue> arguments,
            MessageOrigin origin)
        {
            Debug.Assert(origin.MemberDefinition is MethodDesc);
            MethodBody = methodBody;
            Operation = operation;
            Offset = offset;
            CalledMethod = calledMethod;
            Instance = instance.Clone();
            if (arguments.IsEmpty)
            {
                Arguments = ImmutableArray<MultiValue>.Empty;
            }
            else
            {
                var builder = ImmutableArray.CreateBuilder<MultiValue>();
                foreach (var argument in arguments)
                    builder.Add(argument.Clone());
                Arguments = builder.ToImmutableArray();
            }
            Origin = origin;
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            var diagnosticContext = new DiagnosticContext(
                Origin,
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                logger);
            ReflectionMethodBodyScanner.HandleCall(MethodBody, CalledMethod, Operation, Offset, Instance, Arguments,
                diagnosticContext,
                reflectionMarker,
                out MultiValue _);
        }
    }
}
