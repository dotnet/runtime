// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Dataflow
{
    public class ILProvider
    {
        public MethodIL GetMethodIL(MethodDesc method)
        {
            return EcmaMethodIL.Create((EcmaMethod)method.GetTypicalMethodDefinition());
        }
    }
}
