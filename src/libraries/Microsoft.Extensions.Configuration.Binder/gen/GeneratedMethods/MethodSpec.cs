// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record MethodSpec
    {
        public MethodSpec(SourceGenSpec spec) => SourceGenSpec = spec;

        public SourceGenSpec SourceGenSpec { get; }

        public abstract void Emit(Emitter emitter);

        public abstract void RegisterInvocation(Parser parser, BinderInvocation invocation);
    }
}
