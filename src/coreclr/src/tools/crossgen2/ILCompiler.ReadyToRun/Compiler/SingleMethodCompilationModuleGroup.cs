// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// A compilation group that only contains a single method. Useful for development purposes when investigating
    /// code generation issues.
    /// </summary>
    public class SingleMethodCompilationModuleGroup : CompilationModuleGroup
    {
        private MethodDesc _method;

        public SingleMethodCompilationModuleGroup(MethodDesc method)
        {
            _method = method;
        }

        public override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            return method == _method;
        }

        public override bool ContainsType(TypeDesc type)
        {
            return type == _method.OwningType;
        }

        public override bool VersionsWithModule(ModuleDesc module)
        {
            return ((EcmaMethod)_method.GetTypicalMethodDefinition()).Module == module;
        }

        public override bool GeneratesPInvoke(MethodDesc method)
        {
            return true;
        }

        public override bool TryGetModuleTokenForExternalType(TypeDesc type, out ModuleToken token)
        {
            // TODO: implement if needed (needed if not compiling with large version bubble)
            throw new NotImplementedException();
        }

        public override bool TryGetModuleTokenForExternalMethod(MethodDesc method, out ModuleToken token)
        {
            // TODO: implement if needed (needed if not compiling with large version bubble)
            throw new NotImplementedException();
        }

        public override bool TryGetModuleTokenForExternalField(FieldDesc field, out ModuleToken token)
        {
            // TODO: implement if needed (needed if not compiling with large version bubble)
            throw new NotImplementedException();
        }
    }
}
