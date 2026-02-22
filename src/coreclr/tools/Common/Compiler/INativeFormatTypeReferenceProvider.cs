// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface INativeFormatTypeReferenceProvider
    {
        internal Vertex EncodeReferenceToType(NativeWriter writer, TypeDesc type);
        internal Vertex EncodeReferenceToMethod(NativeWriter writer, MethodDesc method);
    }
}
