// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Resources;

using ILCompiler.DependencyAnalysis;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class SubstitutionProvider
    {
        private readonly FeatureSwitchHashtable _hashtable;

        public SubstitutionProvider(Logger logger, IReadOnlyDictionary<string, bool> switchValues, BodyAndFieldSubstitutions globalSubstitutions)
        {
            _hashtable = new FeatureSwitchHashtable(logger, switchValues, globalSubstitutions);
        }

        public BodySubstitution GetSubstitution(MethodDesc method)
        {
            if (method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
            {
                AssemblyFeatureInfo info = _hashtable.GetOrCreateValue(ecmaMethod.Module);
                if (info.BodySubstitutions != null && info.BodySubstitutions.TryGetValue(ecmaMethod, out BodySubstitution result))
                    return result;

                // If there are no substitutions for the method, look for FeatureGuardAttribute on the property,
                // and substitute 'false' for guards for features that are known to be disabled.
                if (IsGuardForDisabledFeature (ecmaMethod))
                    return BodySubstitution.Create (0);
            }

            return null;
        }

        private bool IsGuardForDisabledFeature (EcmaMethod ecmaMethod)
        {
            if (!ecmaMethod.Signature.IsStatic)
                return false;

            if ((ecmaMethod.Attributes & MethodAttributes.SpecialName) == 0)
                return false;

            if (ecmaMethod.OwningType is not EcmaType declaringType)
                return false;

            MetadataReader reader = declaringType.MetadataReader;

            // Look for a property with a getter that matches the method.
            foreach (PropertyDefinitionHandle propertyHandle in reader.GetTypeDefinition(declaringType.Handle).GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                var accessors = property.GetAccessors();
                var getter = accessors.Getter;
                if (getter.IsNil)
                    continue;

                if (getter != ecmaMethod.Handle)
                    continue;

                // Found the matching property. Look for FeatureGuardAttribute on the property.
                PropertyPseudoDesc propertyDesc = new PropertyPseudoDesc(declaringType, propertyHandle);
                foreach (var attr in propertyDesc.GetDecodedCustomAttributes ("System.Diagnostics.CodeAnalysis", "FeatureGuardAttribute")) {
                    if (attr.FixedArguments.Length != 1)
                        continue; 

                    if (attr.FixedArguments[0].Value is not MetadataType featureType)
                        continue;


                    if (featureType.Namespace is not "System.Diagnostics.CodeAnalysis")
                        continue;

                    switch (featureType.Name) {
                    case "RequiresUnreferencedCodeAttribute":
                        return true;
                    case "RequiresDynamicCodeAttribute":
                        if (_hashtable._switchValues.TryGetValue(
                                "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported",
                                out bool isDynamicCodeSupported)
                            && !isDynamicCodeSupported)
                            return true;
                        break;
                    case "RequiresAssemblyFilesAttribute":
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        public object GetSubstitution(FieldDesc field)
        {
            if (field.GetTypicalFieldDefinition() is EcmaField ecmaField)
            {
                AssemblyFeatureInfo info = _hashtable.GetOrCreateValue(ecmaField.Module);
                if (info.BodySubstitutions != null && info.FieldSubstitutions.TryGetValue(ecmaField, out object result))
                    return result;
            }

            return null;
        }

        public bool HasSubstitutedBody(MethodDesc method)
        {
            return GetSubstitution(method) != null;
        }

        public bool HasSubstitutedValue(FieldDesc field)
        {
            return GetSubstitution(field) != null;
        }

        internal bool ShouldInlineResourceStrings => !_hashtable._switchValues.TryGetValue("System.Resources.UseSystemResourceKeys", out bool useResourceKeys) || !useResourceKeys;

        internal string GetResourceStringForAccessor(EcmaMethod method)
        {
            Debug.Assert(method.Name.StartsWith("get_", StringComparison.Ordinal));
            string resourceStringName = method.Name.Substring(4);

            Dictionary<string, string> dict = _hashtable.GetOrCreateValue(method.Module).InlineableResourceStrings;
            if (dict != null
                && dict.TryGetValue(resourceStringName, out string result))
            {
                return result;
            }

            return null;
        }

        private sealed class FeatureSwitchHashtable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            internal readonly IReadOnlyDictionary<string, bool> _switchValues;
            private readonly Logger _logger;
            private readonly BodyAndFieldSubstitutions _globalSubstitutions;

            public FeatureSwitchHashtable(Logger logger, IReadOnlyDictionary<string, bool> switchValues, BodyAndFieldSubstitutions globalSubstitutions)
            {
                _logger = logger;
                _switchValues = switchValues;
                _globalSubstitutions = globalSubstitutions;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _logger, _switchValues, _globalSubstitutions);
            }
        }

        private sealed class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public IReadOnlyDictionary<MethodDesc, BodySubstitution> BodySubstitutions { get; }
            public IReadOnlyDictionary<FieldDesc, object> FieldSubstitutions { get; }
            public Dictionary<string, string> InlineableResourceStrings { get; }

            public AssemblyFeatureInfo(EcmaModule module, Logger logger, IReadOnlyDictionary<string, bool> featureSwitchValues, BodyAndFieldSubstitutions globalSubstitutions)
            {
                Module = module;

                PEMemoryBlock resourceDirectory = module.PEReader.GetSectionData(module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                BodyAndFieldSubstitutions substitutions = default;

                foreach (var resourceHandle in module.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = module.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = module.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.Substitutions.xml")
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        substitutions = BodySubstitutionsParser.GetSubstitutions(logger, module.Context, ms, resource, module, "name", featureSwitchValues);
                    }
                    else if (InlineableStringsResourceNode.IsInlineableStringsResource(module, resourceName))
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        InlineableResourceStrings = new Dictionary<string, string>();

                        using var resReader = new ResourceReader(ms);
                        var enumerator = resReader.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Key is string key && enumerator.Value is string value)
                                InlineableResourceStrings[key] = value;
                        }
                    }
                }

                // Also apply any global substitutions
                // Note we allow these to overwrite substitutions in the assembly
                substitutions.AppendFrom(globalSubstitutions);

                (BodySubstitutions, FieldSubstitutions) = (substitutions.BodySubstitutions, substitutions.FieldSubstitutions);
            }
        }
    }
}
