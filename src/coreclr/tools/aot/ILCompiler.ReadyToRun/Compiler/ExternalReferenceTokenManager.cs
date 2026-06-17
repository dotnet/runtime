// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.ReadyToRun.TypeSystem;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

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
        /// Ensures that all the tokens necessary for creating a ReadyToRun signature for the given entities are available.
        /// If a token for an entity is not already resolvable from a module known to the <see cref="ModuleTokenResolver"/>,
        /// a new token is added to the manifest <see cref="MutableModule"/>.
        /// </summary>
        public void EnsureDefTokensAreAvailable(IEnumerable<TypeSystemEntity> entities, ModuleDesc moduleForNewReferences, bool referencesAreForAsyncMethod)
        {
            Debug.Assert(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
            Debug.Assert(!_mutableModule.CreatingTokensForAsyncMethod);
            _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = moduleForNewReferences;
            _mutableModule.CreatingTokensForAsyncMethod = referencesAreForAsyncMethod;
            try
            {
                foreach (var entity in entities)
                {
                    EnsureDefTokensAreAvailableInternal(entity);
                }
            }
            finally
            {
                _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
                _mutableModule.CreatingTokensForAsyncMethod = false;
            }
        }

        /// <summary>
        /// Ensures that all the tokens necessary for creating a ReadyToRun signature for the given entity are available.
        /// If a token for the entity is not already resolvable from a module known to the <see cref="ModuleTokenResolver"/>,
        /// a new token is added to the manifest <see cref="MutableModule"/>.
        /// </summary>
        public void EnsureDefTokensAreAvailable(TypeSystemEntity entity, ModuleDesc moduleForNewReferences, bool referencesAreForAsyncMethod)
        {
            Debug.Assert(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
            Debug.Assert(!_mutableModule.CreatingTokensForAsyncMethod);
            _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = moduleForNewReferences;
            _mutableModule.CreatingTokensForAsyncMethod = referencesAreForAsyncMethod;
            try
            {
                EnsureDefTokensAreAvailableInternal(entity);
            }
            finally
            {
                _mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
                _mutableModule.CreatingTokensForAsyncMethod = false;
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
            }
            else if (type.IsParameterizedType)
            {
                var parameterizedType = (ParameterizedType)type;
                EnsureTypeDefTokensAreAvailableInVersionBubble(parameterizedType.ParameterType);
                AddTokenToMutableModule(parameterizedType);
            }
            else if (type.HasInstantiation)
            {
                EnsureTypeDefTokensAreAvailableInVersionBubble(type.GetTypeDefinition());

                foreach (TypeDesc instParam in type.Instantiation)
                {
                    EnsureTypeDefTokensAreAvailableInVersionBubble(instParam);
                }
            }
        }
    }
}
