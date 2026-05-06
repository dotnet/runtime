// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Context.Projection;

namespace System.Reflection.Context.Custom
{
    internal sealed class CustomModule : ProjectingModule
    {
        public CustomModule(Module template, CustomReflectionContext context)
            : base(template, context.Projector)
        {
            ReflectionContext = context;
        }

        public CustomReflectionContext ReflectionContext { get; }
    }
}
