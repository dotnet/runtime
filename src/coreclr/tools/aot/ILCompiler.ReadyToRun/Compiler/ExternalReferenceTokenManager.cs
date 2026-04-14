// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Reflection.ReadyToRun;
using Internal.TypeSystem.Ecma;
using ILCompiler.ReadyToRun.TypeSystem;

namespace ILCompiler.ReadyToRun
{
    /// <summary>
    /// Handles the creation of IL tokens necessary for creating ReadyToRun signatures for types, methods, and fields not already present in the input modules within the version bubble.
    /// </summary>
    internal class ExternalReferenceTokenManager
    {
        private MutableModule _mutableModule;
        private ModuleTokenResolver _tokenResolver;

        public ExternalReferenceTokenManager(MutableModule mutableModule, ModuleTokenResolver tokenResolver)
        {
            _mutableModule = mutableModule;
            _tokenResolver = tokenResolver;
        }

        /// <summary>
        /// Ensures that all the tokens necessary for creating a ReadyToRun signature for the given entity are present in the MutableModule.
        /// Adds the necessary tokens for the given entity to the MutableModule if they are not already present.
        /// </summary>
        public void EnsureDefTokensAreAvailable(IEnumerable<TypeSystemEntity> entities, ModuleDesc moduleForNewReferences)
        {
            _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = moduleForNewReferences;
            foreach (var entity in entities)
            {
                EnsureDefTokensAreAvailableInternal(entity);
            }
            _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
        }

        /// <summary>
        /// Ensures that all the tokens necessary for creating a ReadyToRun signature for the given entity are present in the MutableModule.
        /// Adds the necessary tokens for the given entity to the MutableModule if they are not already present.
        /// </summary>
        public void EnsureDefTokensAreAvailable(TypeSystemEntity entity, ModuleDesc moduleForNewReferences)
        {

            _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = moduleForNewReferences;
            try
            {
                EnsureDefTokensAreAvailableInternal(entity);
            }
            finally
            {
                _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
            }
        }

        private void EnsureDefTokensAreAvailableInternal(TypeSystemEntity entity)
        {
            Debug.Assert(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences != null);
            switch (entity)
            {
                case TypeDesc typeDesc:
                    EnsureTypeDefTokensAreAvailableInVersionBubble(typeDesc);
                    break;
                case MethodDesc methodDesc:
                    EnsureMethodDefTokensAreAvailableInVersionBubble(methodDesc);
                    break;
                case FieldDesc fieldDesc:
                    EnsureFieldDefTokensAreAvailableInVersionBubble(fieldDesc);
                    break;
                default:
                    throw new NotSupportedException();
            };
        }

        private void AddTokenToMutableModule(TypeSystemEntity entity)
        {
            var existingToken = entity switch
            {
                TypeDesc typeDesc => _tokenResolver.GetModuleTokenForType(typeDesc, allowDynamicallyCreatedReference: true, throwIfNotFound: false),
                MethodDesc methodDesc => _tokenResolver.GetModuleTokenForMethod(methodDesc, allowDynamicallyCreatedReference: true, throwIfNotFound: false),
                FieldDesc fieldDesc => _tokenResolver.GetModuleTokenForField(fieldDesc, allowDynamicallyCreatedReference: true, throwIfNotFound: false),
                _ => throw new NotSupportedException()
            };

            if (!existingToken.IsNull)
                return;

            if (!_mutableModule.TryGetEntityHandle(entity).HasValue)
            {
                throw new InternalCompilerErrorException($"Unable to create token to {entity}");
            }
        }

        private void EnsureMethodDefTokensAreAvailableInVersionBubble(MethodDesc methodDesc)
        {
            if (!methodDesc.IsPrimaryMethodDesc())
            {
                EnsureMethodDefTokensAreAvailableInVersionBubble(methodDesc.GetPrimaryMethodDesc());
                return;
            }
            if (methodDesc is EcmaMethod ecmaMethod)
            {
                AddTokenToMutableModule(ecmaMethod);
                return;
            }
            if (methodDesc.HasInstantiation)
            {
                EnsureTypeDefTokensAreAvailableInVersionBubble(methodDesc.GetMethodDefinition().OwningType);
                foreach (TypeDesc instParam in methodDesc.Instantiation)
                {
                    EnsureTypeDefTokensAreAvailableInVersionBubble(instParam);
                }
                EnsureMethodDefTokensAreAvailableInVersionBubble(methodDesc.GetTypicalMethodDefinition());
            }
        }

        private void EnsureFieldDefTokensAreAvailableInVersionBubble(FieldDesc fieldDesc)
        {
            EnsureTypeDefTokensAreAvailableInVersionBubble(fieldDesc.OwningType);
            EnsureTypeDefTokensAreAvailableInVersionBubble(fieldDesc.FieldType);
            if (fieldDesc is EcmaField ecmaField)
            {
                AddTokenToMutableModule(ecmaField);
            }
            else
            {
                EnsureFieldDefTokensAreAvailableInVersionBubble(fieldDesc.GetTypicalFieldDefinition());
            }
        }

        private void EnsureTypeDefTokensAreAvailableInVersionBubble(TypeDesc type)
        {
            // Type represented by simple element type
            if (type.IsPrimitive || type.IsVoid || type.IsObject || type.IsString || type.IsTypedReference)
                return;

            if (type is EcmaType ecmaType)
            {
                AddTokenToMutableModule(ecmaType);
                return;
            }
            if (type is ParameterizedType parameterizedType)
            {
                EnsureTypeDefTokensAreAvailableInVersionBubble(parameterizedType.ParameterType);
                AddTokenToMutableModule(parameterizedType);
                return;
            }

            if (type.HasInstantiation)
            {
                EnsureTypeDefTokensAreAvailableInVersionBubble(type.GetTypeDefinition());

                foreach (TypeDesc instParam in type.Instantiation)
                {
                    EnsureTypeDefTokensAreAvailableInVersionBubble(instParam);
                }
            }
            else if (type.IsParameterizedType)
            {
                EnsureTypeDefTokensAreAvailableInVersionBubble(type.GetParameterType());
            }
        }
    }
}
