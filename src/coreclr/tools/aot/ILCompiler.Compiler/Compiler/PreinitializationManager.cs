// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Manages policies around static constructors (.cctors) and static data initialization.
    /// </summary>
    public class PreinitializationManager
    {
        private readonly bool _supportsLazyCctors;

        public PreinitializationManager(TypeSystemContext context, CompilationModuleGroup compilationGroup, ILProvider ilprovider, TypePreinit.TypePreinitializationPolicy policy)
        {
            _supportsLazyCctors = context.SystemModule.GetType("System.Runtime.CompilerServices", "ClassConstructorRunner", throwIfNotFound: false) != null;
            _preinitHashTable = new PreinitializationInfoHashtable(compilationGroup, ilprovider, policy);
        }

        /// <summary>
        /// Returns true if '<paramref name="type"/>' has a lazily executed static constructor.
        /// A lazy static constructor gets executed on first access to type's members.
        /// </summary>
        public bool HasLazyStaticConstructor(TypeDesc type)
        {
            if (!type.HasStaticConstructor)
                return false;

            // If the cctor runs eagerly at startup, it's not lazy
            if (HasEagerConstructorAttribute(type))
                return false;

            // If the class library doesn't support lazy cctors, everything is preinitialized before Main
            // either by interpretting the cctor at compile time, or by running the cctor eagerly at startup.
            if (!_supportsLazyCctors)
                return false;

            // Would be odd to see a type with a cctor that is not MetadataType
            Debug.Assert(type is MetadataType);
            var mdType = (MetadataType)type;

            // The cctor on the Module type is the module constructor. That's special.
            if (mdType.IsModuleType)
                return false;

            // If we can't interpret the cctor at compile time, the cctor is lazy.
            return !IsPreinitialized(mdType);
        }

        /// <summary>
        /// Returns true if '<paramref name="type"/>' has a static constructor that is eagerly
        /// executed at process startup time.
        /// </summary>
        public bool HasEagerStaticConstructor(TypeDesc type)
        {
            if (!type.HasStaticConstructor)
                return false;

            // Would be odd to see a type with a cctor that is not MetadataType
            Debug.Assert(type is MetadataType);
            var mdType = (MetadataType)type;

            // If the type is preinitialized at compile time, that's not eager.
            if (IsPreinitialized(mdType))
                return false;

            // If the type is marked as eager or classlib doesn't have a cctor runner, it's eager.
            return HasEagerConstructorAttribute(type) || !_supportsLazyCctors;
        }

        private static bool HasEagerConstructorAttribute(TypeDesc type)
        {
            MetadataType mdType = type as MetadataType;
            return mdType != null &&
                mdType.HasCustomAttribute("System.Runtime.CompilerServices", "EagerStaticClassConstructionAttribute");
        }

        public bool IsPreinitialized(MetadataType type)
        {
            if (!type.HasStaticConstructor)
                return false;

            // The cctor on the Module type is the module constructor. That's special.
            if (type.IsModuleType)
                return false;

            // Generic definitions cannot be preinitialized
            if (type.IsGenericDefinition)
                return false;

            return GetPreinitializationInfo(type).IsPreinitialized;
        }

        public void LogStatistics(Logger logger)
        {
            if (_preinitHashTable._policy is TypePreinit.DisabledPreinitializationPolicy)
                return;

            int totalEligibleTypes = 0;
            int totalPreinitializedTypes = 0;

            if (logger.IsVerbose)
            {
                foreach (var item in LockFreeReaderHashtable<MetadataType, TypePreinit.PreinitializationInfo>.Enumerator.Get(_preinitHashTable))
                {
                    // Canonical types are not actual types. They represent the pessimized version of all types that share the form.
                    if (item.Type.IsCanonicalSubtype(CanonicalFormKind.Any))
                        continue;

                    totalEligibleTypes++;
                    if (item.IsPreinitialized)
                    {
                        logger.LogMessage($"Preinitialized type '{item.Type}'");
                        totalPreinitializedTypes++;
                    }
                    else
                    {
                        logger.LogMessage($"Could not preinitialize '{item.Type}': {item.FailureReason}");
                    }
                }

                logger.LogMessage($"Preinitialized {totalPreinitializedTypes} types out of {totalEligibleTypes}.");
            }
        }

        public TypePreinit.PreinitializationInfo GetPreinitializationInfo(MetadataType type)
        {
            return _preinitHashTable.GetOrCreateValue(type);
        }

        private sealed class PreinitializationInfoHashtable : LockFreeReaderHashtable<MetadataType, TypePreinit.PreinitializationInfo>
        {
            private readonly CompilationModuleGroup _compilationGroup;
            private readonly ILProvider _ilProvider;
            internal readonly TypePreinit.TypePreinitializationPolicy _policy;

            public PreinitializationInfoHashtable(CompilationModuleGroup compilationGroup, ILProvider ilProvider, TypePreinit.TypePreinitializationPolicy policy)
            {
                _compilationGroup = compilationGroup;
                _ilProvider = ilProvider;
                _policy = policy;
            }

            protected override bool CompareKeyToValue(MetadataType key, TypePreinit.PreinitializationInfo value) => key == value.Type;
            protected override bool CompareValueToValue(TypePreinit.PreinitializationInfo value1, TypePreinit.PreinitializationInfo value2) => value1.Type == value2.Type;
            protected override int GetKeyHashCode(MetadataType key) => key.GetHashCode();
            protected override int GetValueHashCode(TypePreinit.PreinitializationInfo value) => value.Type.GetHashCode();

            protected override TypePreinit.PreinitializationInfo CreateValueFromKey(MetadataType key)
            {
                var info = TypePreinit.ScanType(_compilationGroup, _ilProvider, _policy, key);

                // We either successfully preinitialized or
                // the type doesn't have a canonical form or
                // the policy doesn't allow treating canonical forms of this type as preinitialized
                Debug.Assert(info.IsPreinitialized ||
                    (key.ConvertToCanonForm(CanonicalFormKind.Specific) is DefType canonType && (key == canonType || !_policy.CanPreinitializeAllConcreteFormsForCanonForm(canonType))));

                return info;
            }
        }
        private PreinitializationInfoHashtable _preinitHashTable;
    }
}
