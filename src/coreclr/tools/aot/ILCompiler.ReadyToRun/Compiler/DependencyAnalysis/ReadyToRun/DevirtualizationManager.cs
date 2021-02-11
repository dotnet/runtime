// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class DevirtualizationManager : ILCompiler.DevirtualizationManager
    {
        private readonly CompilationModuleGroup _compilationModuleGroup;

        public DevirtualizationManager(CompilationModuleGroup compilationModuleGroup)
        {
            _compilationModuleGroup = compilationModuleGroup;
        }

        public override bool IsEffectivelySealed(TypeDesc type)
        {
            return _compilationModuleGroup.VersionsWithType(type) && base.IsEffectivelySealed(type);
        }

        public override bool IsEffectivelySealed(MethodDesc method)
        {
            return _compilationModuleGroup.VersionsWithMethodBody(method) && base.IsEffectivelySealed(method);
        }

        protected override MethodDesc ResolveVirtualMethod(MethodDesc declMethod, DefType implType)
        {
            if (_compilationModuleGroup.VersionsWithMethodBody(declMethod) &&
                _compilationModuleGroup.VersionsWithType(implType))
            {
                /**
                 * It is possible for us to hit a scenario where a type implements
                 * the same interface more than once due to generic instantiations.
                 *
                 * In some instances of those cases, the VirtualMethodAlgorithm
                 * does not produce identical output as CoreCLR would, leading to
                 * behavioral differences in compiled outputs.
                 *
                 * Instead of fixing the algorithm (in which the work to fix it is
                 * tracked in https://github.com/dotnet/corert/issues/208), the
                 * following duplication detection algorithm will detect the case and
                 * refuse to devirtualize for those scenarios.
                 */
                if (declMethod.OwningType.IsInterface)
                {
                    DefType[] implTypeRuntimeInterfaces = implType.RuntimeInterfaces;
                    for (int i = 0; i < implTypeRuntimeInterfaces.Length; i++)
                    {
                        for (int j = i + 1; j < implTypeRuntimeInterfaces.Length; j++)
                        {
                            if (implTypeRuntimeInterfaces[i] == implTypeRuntimeInterfaces[j])
                            {
                                return null;
                            }
                        }
                    }
                }

                return base.ResolveVirtualMethod(declMethod, implType);
            }

            // Cannot devirtualize across version bubble boundary
            return null;
        }
    }
}
