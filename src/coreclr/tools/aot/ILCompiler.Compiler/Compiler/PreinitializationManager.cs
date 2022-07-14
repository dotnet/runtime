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
        private readonly bool _enableInterpreter;

        public PreinitializationManager(TypeSystemContext context, CompilationModuleGroup compilationGroup, ILProvider ilprovider, bool enableInterpreter)
        {
            _supportsLazyCctors = context.SystemModule.GetType("System.Runtime.CompilerServices", "ClassConstructorRunner", throwIfNotFound: false) != null;
            _preinitHashTable = new PreinitializationInfoHashtable(compilationGroup, ilprovider);
            _enableInterpreter = enableInterpreter;
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
            // If the cctor interpreter is not enabled, no type is preinitialized.
            if (!_enableInterpreter)
                return false;

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
            if (!_enableInterpreter)
                return;

            int totalEligibleTypes = 0;
            int totalPreinitializedTypes = 0;

            if (logger.IsVerbose)
            {
                foreach (var item in LockFreeReaderHashtable<MetadataType, TypePreinit.PreinitializationInfo>.Enumerator.Get(_preinitHashTable))
                {
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

        class PreinitializationInfoHashtable : LockFreeReaderHashtable<MetadataType, TypePreinit.PreinitializationInfo>
        {
            private readonly CompilationModuleGroup _compilationGroup;
            private readonly ILProvider _ilProvider;

            public PreinitializationInfoHashtable(CompilationModuleGroup compilationGroup, ILProvider ilProvider)
            {
                _compilationGroup = compilationGroup;
                _ilProvider = ilProvider;
            }

            protected override bool CompareKeyToValue(MetadataType key, TypePreinit.PreinitializationInfo value) => key == value.Type;
            protected override bool CompareValueToValue(TypePreinit.PreinitializationInfo value1, TypePreinit.PreinitializationInfo value2) => value1.Type == value2.Type;
            protected override int GetKeyHashCode(MetadataType key) => key.GetHashCode();
            protected override int GetValueHashCode(TypePreinit.PreinitializationInfo value) => value.Type.GetHashCode();

            protected override TypePreinit.PreinitializationInfo CreateValueFromKey(MetadataType key)
            {
                return TypePreinit.ScanType(_compilationGroup, _ilProvider, key);
            }
        }
        private PreinitializationInfoHashtable _preinitHashTable;
    }
}
