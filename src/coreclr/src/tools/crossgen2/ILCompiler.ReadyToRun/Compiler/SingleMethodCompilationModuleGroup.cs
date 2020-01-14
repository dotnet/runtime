// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysis.ReadyToRun;

namespace ILCompiler
{
    /// <summary>
    /// A compilation group that only contains a single method. Useful for development purposes when investigating
    /// code generation issues.
    /// </summary>
    public class SingleMethodCompilationModuleGroup : CompilationModuleGroup
    {
        private MethodDesc _method;
        private Dictionary<TypeDesc, ModuleToken> _typeRefsInCompilationModuleSet;

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
            Debug.Assert(!VersionsWithType(type));

            if (_typeRefsInCompilationModuleSet == null)
            {
                _typeRefsInCompilationModuleSet = new Dictionary<TypeDesc, ModuleToken>();

                EcmaModule ecmaModule = ((EcmaMethod)_method.GetTypicalMethodDefinition()).Module;
                foreach (var typeRefHandle in ecmaModule.MetadataReader.TypeReferences)
                {
                    try
                    {
                        TypeDesc typeFromTypeRef = ecmaModule.GetType(typeRefHandle);
                        _typeRefsInCompilationModuleSet[typeFromTypeRef] = new ModuleToken(ecmaModule, typeRefHandle);
                    }
                    catch (TypeSystemException) { }
                }
            }

            return _typeRefsInCompilationModuleSet.TryGetValue(type, out token);
        }
    }
}
