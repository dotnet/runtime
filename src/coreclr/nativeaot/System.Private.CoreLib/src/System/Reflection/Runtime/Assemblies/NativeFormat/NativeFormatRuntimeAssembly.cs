// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.Modules.NativeFormat;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.Assemblies.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeAssembly : RuntimeAssemblyInfo
    {
        private NativeFormatRuntimeAssembly(MetadataReader reader, ScopeDefinitionHandle scope)
        {
            Scope = new QScopeDefinition(reader, scope);
        }

        internal sealed override MetadataReader GetMetadataReader() => Scope.Reader;

        internal sealed override CustomAttributeHandleCollection GetCustomAttributeHandles() => Scope.ScopeDefinition.CustomAttributes;

        public sealed override IEnumerable<TypeInfo> DefinedTypes
        {
            [RequiresUnreferencedCode("Types might be removed")]
            get
            {
                MetadataReader reader = Scope.Reader;
                IEnumerable<TypeDefinitionHandle> allTopLevelTypes = Scope.ScopeDefinition.TypeDefinitions.AsEnumerable();
                IEnumerable<TypeDefinitionHandle> allTypes = reader.GetTransitiveTypes(allTopLevelTypes, publicOnly: false);
                foreach (TypeDefinitionHandle typeDefinitionHandle in allTypes)
                    yield return (TypeInfo)typeDefinitionHandle.GetNamedType(reader).ToType();
            }
        }

        public sealed override IEnumerable<Type> ExportedTypes
        {
            [RequiresUnreferencedCode("Types might be removed")]
            get
            {
                MetadataReader reader = Scope.Reader;
                IEnumerable<TypeDefinitionHandle> allTopLevelTypes = Scope.ScopeDefinition.TypeDefinitions.AsEnumerable();
                IEnumerable<TypeDefinitionHandle> allTypes = reader.GetTransitiveTypes(allTopLevelTypes, publicOnly: true);
                foreach (TypeDefinitionHandle typeDefinitionHandle in allTypes)
                    yield return typeDefinitionHandle.ResolveTypeDefinition(reader).ToType();
            }
        }

        public sealed override MethodInfo EntryPoint
        {
            get
            {
                MetadataReader reader = Scope.Reader;

                QualifiedMethodHandle entrypointHandle = Scope.ScopeDefinition.EntryPoint;
                if (!entrypointHandle.IsNil)
                {
                    QualifiedMethod entrypointMethod = entrypointHandle.GetQualifiedMethod(reader);
                    TypeDefinitionHandle declaringTypeHandle = entrypointMethod.EnclosingType;
                    MethodHandle methodHandle = entrypointMethod.Method;
                    NativeFormatRuntimeNamedTypeInfo containingType = NativeFormatRuntimeNamedTypeInfo.GetRuntimeNamedTypeInfo(reader, declaringTypeHandle, default(RuntimeTypeHandle));
                    return RuntimeNamedMethodInfo<NativeFormatMethodCommon>.GetRuntimeNamedMethodInfo(new NativeFormatMethodCommon(methodHandle, containingType, containingType), containingType);
                }

                return null;
            }
        }

        protected sealed override IEnumerable<TypeForwardInfo> TypeForwardInfos
        {
            get
            {
                MetadataReader reader = Scope.Reader;
                foreach (TypeForwarderHandle typeForwarderHandle in Scope.ScopeDefinition.TypeForwarders)
                {
                    TypeForwarder typeForwarder = typeForwarderHandle.GetTypeForwarder(reader);
                    NamespaceReference namespaceReference = typeForwarder.Namespace.GetNamespaceReference(reader);
                    string namespaceName = namespaceReference.Name.GetStringOrNull(reader) ?? string.Empty;
                    string typeName = typeForwarder.Name.GetString(reader);
                    RuntimeAssemblyName redirectedAssemblyName = namespaceReference.Scope.ToRuntimeAssemblyName(reader);

                    yield return new TypeForwardInfo(redirectedAssemblyName, namespaceName, typeName);
                }
            }
        }

        public sealed override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
        {
            ArgumentNullException.ThrowIfNull(resourceName);
            return ReflectionCoreExecution.ExecutionEnvironment.GetManifestResourceInfo(this, resourceName);
        }

        public sealed override string[] GetManifestResourceNames()
        {
            return ReflectionCoreExecution.ExecutionEnvironment.GetManifestResourceNames(this);
        }

        public sealed override Stream GetManifestResourceStream(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return ReflectionCoreExecution.ExecutionEnvironment.GetManifestResourceStream(this, name);
        }

        public sealed override string ImageRuntimeVersion
        {
            get
            {
                // Needed to make RuntimeEnvironment.GetSystemVersion() work. Will not be correct always but anticipating most callers are not making
                // actual decisions based on the value.
                return "v4.0.30319";
            }
        }

        public sealed override Module ManifestModule
        {
            get
            {
                return NativeFormatRuntimeModule.GetRuntimeModule(this);
            }
        }

        internal sealed override RuntimeAssemblyName RuntimeAssemblyName
        {
            get
            {
                return Scope.Handle.ToRuntimeAssemblyName(Scope.Reader);
            }
        }

        public sealed override bool Equals(object obj)
        {
            NativeFormatRuntimeAssembly? other = obj as NativeFormatRuntimeAssembly;
            return Equals(other);
        }

        public bool Equals(NativeFormatRuntimeAssembly other)
        {
            if (other == null)
                return false;
            if (!(this.Scope.Reader == other.Scope.Reader))
                return false;
            if (!(this.Scope.Handle.Equals(other.Scope.Handle)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return Scope.Handle.GetHashCode();
        }

        internal QScopeDefinition Scope { get; }
    }
}
