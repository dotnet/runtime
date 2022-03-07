// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.CustomAttributes;
using System.Collections.Generic;

using Internal.Reflection.Core;
using Internal.Metadata.NativeFormat;
using System.Reflection.Runtime.Assemblies.NativeFormat;

namespace System.Reflection.Runtime.Modules.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeModule : RuntimeModule
    {
        private NativeFormatRuntimeModule(NativeFormatRuntimeAssembly assembly)
            : base()
        {
            _assembly = assembly;
        }

        public sealed override Assembly Assembly => _assembly;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                QScopeDefinition scope = _assembly.Scope;
                return RuntimeCustomAttributeData.GetCustomAttributes(scope.Reader, scope.ScopeDefinition.ModuleCustomAttributes);
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        public sealed override Guid ModuleVersionId
        {
            get
            {
                byte[] mvid = _assembly.Scope.ScopeDefinition.Mvid.ToArray();
                if (mvid.Length == 0)
                    return default(Guid); // Workaround for TFS 441076 - Module data not emitted for facade assemblies.
                return new Guid(mvid);
            }
        }

        public sealed override string ScopeName
        {
            get
            {
                QScopeDefinition scope = _assembly.Scope;
                return scope.Reader.GetString(scope.ScopeDefinition.ModuleName);
            }
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public sealed override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            QScopeDefinition scope = _assembly.Scope;
            MetadataReader reader = scope.Reader;
            return scope.ScopeDefinition.GlobalModuleType.GetNamedType(reader).GetField(name, bindingAttr);
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public sealed override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            QScopeDefinition scope = _assembly.Scope;
            MetadataReader reader = scope.Reader;
            return scope.ScopeDefinition.GlobalModuleType.GetNamedType(reader).GetFields(bindingFlags);
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        protected sealed override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            QScopeDefinition scope = _assembly.Scope;
            MetadataReader reader = scope.Reader;
            TypeInfos.RuntimeTypeDefinitionTypeInfo runtimeType = scope.ScopeDefinition.GlobalModuleType.GetNamedType(reader);

            if (types == null)
            {
                return runtimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return runtimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        public sealed override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            QScopeDefinition scope = _assembly.Scope;
            MetadataReader reader = scope.Reader;
            return scope.ScopeDefinition.GlobalModuleType.GetNamedType(reader).GetMethods(bindingFlags);
        }

        private readonly NativeFormatRuntimeAssembly _assembly;
    }
}
