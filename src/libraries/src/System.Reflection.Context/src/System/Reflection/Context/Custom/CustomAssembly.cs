// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Context.Projection;

namespace System.Reflection.Context.Custom
{
    internal class CustomAssembly : ProjectingAssembly
    {
        public CustomAssembly(Assembly template, CustomReflectionContext context)
            : base(template, context.Projector)
        {
            ReflectionContext = context;
        }

        public CustomReflectionContext ReflectionContext { get; }
    }
}
