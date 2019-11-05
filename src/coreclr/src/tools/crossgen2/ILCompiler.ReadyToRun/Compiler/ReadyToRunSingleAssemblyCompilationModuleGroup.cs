// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class ReadyToRunSingleAssemblyCompilationModuleGroup : CompilationModuleGroup
    {
        private HashSet<ModuleDesc> _compilationModuleSet;
        private HashSet<ModuleDesc> _versionBubbleModuleSet;
        private ProfileDataManager _profileGuidedCompileRestriction;

        private bool _compileGenericDependenciesFromVersionBubbleModuleSet;

        public ReadyToRunSingleAssemblyCompilationModuleGroup(
            TypeSystemContext context, 
            IEnumerable<ModuleDesc> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet,
            ProfileDataManager profileGuidedCompileRestriction)
        {
            _compilationModuleSet = new HashSet<ModuleDesc>(compilationModuleSet);

            _versionBubbleModuleSet = new HashSet<ModuleDesc>(versionBubbleModuleSet);
            _versionBubbleModuleSet.UnionWith(_compilationModuleSet);

            _compileGenericDependenciesFromVersionBubbleModuleSet = compileGenericDependenciesFromVersionBubbleModuleSet;
            _profileGuidedCompileRestriction = profileGuidedCompileRestriction;
        }

        public sealed override bool ContainsType(TypeDesc type)
        {
            return type.GetTypeDefinition() is EcmaType ecmaType && IsModuleInCompilationGroup(ecmaType.EcmaModule);
        }

        public sealed override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            if (_profileGuidedCompileRestriction != null)
            {
                bool found = false;

                if (!method.HasInstantiation && !method.OwningType.HasInstantiation)
                {
                    // Check only the defining module for non-generics
                    MetadataType mdType = method.OwningType as MetadataType;
                    if (mdType != null)
                    {
                        if (_profileGuidedCompileRestriction.GetDataForModuleDesc(mdType.Module).GetMethodProfileData(method) != null)
                        {
                            found = true;
                        }
                    }
                }
                else
                {
                    // For generics look in the profile data of all modules being compiled
                    foreach (ModuleDesc module in _compilationModuleSet)
                    {
                        if (_profileGuidedCompileRestriction.GetDataForModuleDesc(module).GetMethodProfileData(method) != null)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found == false)
                    return false;
            }

            if (method is ArrayMethod)
            {
                // TODO-PERF: for now, we never emit native code for array methods as Crossgen ignores
                // them too. At some point we might be able to "exceed Crossgen CQ" by adding this support.
                return false;
            }

            return ContainsType(method.OwningType) || CompileVersionBubbleGenericsIntoCurrentModule(method);

        }

        private bool IsModuleInCompilationGroup(EcmaModule module)
        {
            return _compilationModuleSet.Contains(module);
        }

        private bool CompileVersionBubbleGenericsIntoCurrentModule(MethodDesc method)
        {
            if (!_compileGenericDependenciesFromVersionBubbleModuleSet)
                return false;

            if (!method.HasInstantiation && !method.OwningType.HasInstantiation)
                return false;

            return VersionsWithType(method.OwningType);
        }

        private ConcurrentDictionary<TypeDesc, bool> _containsTypeLayoutCache = new ConcurrentDictionary<TypeDesc, bool>();

        /// <summary>
        /// If true, the type is fully contained in the current compilation group.
        /// </summary>
        public override bool ContainsTypeLayout(TypeDesc type)
        {
            return _containsTypeLayoutCache.GetOrAdd(type, ContainsTypeLayoutUncached);
        }

        private bool ContainsTypeLayoutUncached(TypeDesc type)
        {
            if (type.IsObject || 
                type.IsPrimitive || 
                type.IsEnum || 
                type.IsPointer ||
                type.IsFunctionPointer ||
                type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
            {
                return true;
            }
            var defType = (MetadataType)type;
            if (!ContainsType(defType))
            {
                if (!defType.IsValueType)
                {
                    // Eventually, we may respect the non-versionable attribute for reference types too. For now, we are going
                    // to play is safe and ignore it.
                    return false;
                }

                // Valuetypes with non-versionable attribute are candidates for fixed layout. Reject the rest.
                if (!defType.HasCustomAttribute("System.Runtime.Versioning", "NonVersionableAttribute"))
                {
                    return false;
                }
            }
            if (!defType.IsValueType && !ContainsTypeLayout(defType.BaseType))
            {
                return false;
            }
            foreach (FieldDesc field in defType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                if (fieldType.IsValueType && 
                    !ContainsTypeLayout(fieldType))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool VersionsWithType(TypeDesc typeDesc)
        {
            return typeDesc.GetTypeDefinition() is EcmaType ecmaType &&
                _versionBubbleModuleSet.Contains(ecmaType.EcmaModule);
        }

        public override bool VersionsWithMethodBody(MethodDesc method)
        {
            return VersionsWithType(method.OwningType);
        }

        public override bool CanInline(MethodDesc callerMethod, MethodDesc calleeMethod)
        {
            // Allow inlining if the caller is within the current version bubble
            // (because otherwise we may not be able to encode its tokens)
            // and if the callee is either in the same version bubble or is marked as non-versionable.
            bool canInline = VersionsWithMethodBody(callerMethod) &&
                (VersionsWithMethodBody(calleeMethod) ||
                    calleeMethod.HasCustomAttribute("System.Runtime.Versioning", "NonVersionableAttribute"));

            return canInline;
        }

        public override bool GeneratesPInvoke(MethodDesc method)
        {
            // PInvokes depend on details of the core library.
            //
            // We allow compiling them if both the module of the pinvoke and the core library
            // are in the version bubble.
            Debug.Assert(method is EcmaMethod);
            return _versionBubbleModuleSet.Contains(((EcmaMethod)method).Module)
                && _versionBubbleModuleSet.Contains(method.Context.SystemModule);
        }
    }
}
