// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.CustomAttributes;

using Internal.LowLevelLinq;
using Internal.Reflection.Core.Execution;

using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos that represent type definitions (i.e. Foo or Foo<>, but not Foo<int> or arrays/pointers/byrefs.)
    // that not opted into pay-for-play metadata.
    //
    internal sealed partial class RuntimeNoMetadataNamedTypeInfo : RuntimeTypeDefinitionTypeInfo
    {
        private RuntimeNoMetadataNamedTypeInfo(RuntimeTypeHandle typeHandle, bool isGenericTypeDefinition)
        {
            _typeHandle = typeHandle;
            _isGenericTypeDefinition = isGenericTypeDefinition;
        }

        public sealed override Assembly Assembly
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                return _isGenericTypeDefinition;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override string FullName
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override Guid GUID
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override bool IsGenericTypeDefinition
        {
            get
            {
                return _isGenericTypeDefinition;
            }
        }

#if DEBUG
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => base.HasSameMetadataDefinitionAs(other);
#endif

        public sealed override string Namespace
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        public sealed override string ToString()
        {
            return _typeHandle.LastResortString();
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        protected sealed override TypeAttributes GetAttributeFlagsImpl()
        {
            throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
        }

        protected sealed override int InternalGetHashCode()
        {
            return _typeHandle.GetHashCode();
        }

        //
        // Returns the anchoring typedef that declares the members that this type wants returned by the Declared*** properties.
        // The Declared*** properties will project the anchoring typedef's members by overriding their DeclaringType property with "this"
        // and substituting the value of this.TypeContext into any generic parameters.
        //
        // Default implementation returns null which causes the Declared*** properties to return no members.
        //
        // Note that this does not apply to DeclaredNestedTypes. Nested types and their containers have completely separate generic instantiation environments
        // (despite what C# might lead you to think.) Constructed generic types return the exact same same nested types that its generic type definition does
        // - i.e. their DeclaringTypes refer back to the generic type definition, not the constructed generic type.)
        //
        // Note also that we cannot use this anchoring concept for base types because of generic parameters. Generic parameters return
        // baseclass and interfaces based on its constraints.
        //
        internal sealed override RuntimeNamedTypeInfo AnchoringTypeDefinitionForDeclaredMembers
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        internal sealed override bool CanBrowseWithoutMissingMetadataExceptions => false;

        internal sealed override Type InternalDeclaringType
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        internal sealed override string? InternalGetNameIfAvailable(ref Type? rootCauseForFailure)
        {
            rootCauseForFailure = this;
            return null;
        }

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable
        {
            get
            {
                return _typeHandle;
            }
        }

        internal sealed override RuntimeTypeInfo[] RuntimeGenericTypeParameters
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        //
        // Returns the *directly implemented* interfaces as typedefs, specs or refs. ImplementedInterfaces will take care of the transitive closure and
        // insertion of the TypeContext.
        //
        internal sealed override QTypeDefRefOrSpec[] TypeRefDefOrSpecsForDirectlyImplementedInterfaces
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        //
        // Returns the generic parameter substitutions to use when enumerating declared members, base class and implemented interfaces.
        //
        internal sealed override TypeContext TypeContext
        {
            get
            {
                throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(this);
            }
        }

        private readonly RuntimeTypeHandle _typeHandle;
        private readonly bool _isGenericTypeDefinition;
    }
}
