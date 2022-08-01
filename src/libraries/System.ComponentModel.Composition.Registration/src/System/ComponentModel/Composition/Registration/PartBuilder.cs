// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Diagnostics;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.Composition.Registration
{
    public class PartBuilder
    {
        private static readonly List<Attribute> s_importingConstructorList = new List<Attribute>() { new ImportingConstructorAttribute() };
        private static readonly Type s_exportAttributeType = typeof(ExportAttribute);
        private readonly List<ExportBuilder> _typeExportBuilders;
        private bool _setCreationPolicy;
        private CreationPolicy _creationPolicy;

        // Metadata selection
        private List<Tuple<string, object>> _metadataItems;
        private List<Tuple<string, Func<Type, object>>> _metadataItemFuncs;

        // Constructor selector / configuration
        private Func<ConstructorInfo[], ConstructorInfo> _constructorFilter;
        private Action<ParameterInfo, ImportBuilder> _configureConstructorImports;

        //Property Import/Export selection and configuration
        private readonly List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportBuilder>, Type>> _propertyExports;
        private readonly List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportBuilder>, Type>> _propertyImports;
        private readonly List<Tuple<Predicate<Type>, Action<Type, ExportBuilder>>> _interfaceExports;

        internal Predicate<Type> SelectType { get; private set; }

        internal PartBuilder(Predicate<Type> selectType)
        {
            SelectType = selectType;
            _setCreationPolicy = false;
            _creationPolicy = CreationPolicy.Any;
            _typeExportBuilders = new List<ExportBuilder>();
            _propertyExports = new List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportBuilder>, Type>>();
            _propertyImports = new List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportBuilder>, Type>>();
            _interfaceExports = new List<Tuple<Predicate<Type>, Action<Type, ExportBuilder>>>();
        }

        public PartBuilder Export()
        {
            return Export(null);
        }

        public PartBuilder Export(Action<ExportBuilder> exportConfiguration)
        {
            var exportBuilder = new ExportBuilder();
            exportConfiguration?.Invoke(exportBuilder);
            _typeExportBuilders.Add(exportBuilder);

            return this;
        }

        public PartBuilder Export<T>()
        {
            return Export<T>(null);
        }

        public PartBuilder Export<T>(Action<ExportBuilder> exportConfiguration)
        {
            ExportBuilder exportBuilder = new ExportBuilder().AsContractType<T>();
            exportConfiguration?.Invoke(exportBuilder);
            _typeExportBuilders.Add(exportBuilder);

            return this;
        }

        // Choose a constructor from all of the available constructors, then configure them
        public PartBuilder SelectConstructor(Func<ConstructorInfo[], ConstructorInfo> constructorFilter)
        {
            return SelectConstructor(constructorFilter, null);
        }

        public PartBuilder SelectConstructor(Func<ConstructorInfo[], ConstructorInfo> constructorFilter,
            Action<ParameterInfo, ImportBuilder> importConfiguration)
        {
            _constructorFilter = constructorFilter;
            _configureConstructorImports = importConfiguration;

            return this;
        }

        // Choose an interface to export then configure it
        public PartBuilder ExportInterfaces(Predicate<Type> interfaceFilter)
        {
            return ExportInterfaces(interfaceFilter, null);
        }

        public PartBuilder ExportInterfaces()
        {
            return ExportInterfaces(t => true, null);
        }

        public PartBuilder ExportInterfaces(Predicate<Type> interfaceFilter,
            Action<Type, ExportBuilder> exportConfiguration)
        {
            if (interfaceFilter is null)
            {
                throw new ArgumentNullException(nameof(interfaceFilter));
            }

            _interfaceExports.Add(Tuple.Create(interfaceFilter, exportConfiguration));

            return this;
        }

        // Choose a property to export then configure it
        public PartBuilder ExportProperties(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ExportProperties(propertyFilter, null);
        }

        public PartBuilder ExportProperties(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportBuilder> exportConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            _propertyExports.Add(Tuple.Create(propertyFilter, exportConfiguration, default(Type)));

            return this;
        }

        // Choose a property to export then configure it
        public PartBuilder ExportProperties<T>(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ExportProperties<T>(propertyFilter, null);
        }

        public PartBuilder ExportProperties<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportBuilder> exportConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            _propertyExports.Add(Tuple.Create(propertyFilter, exportConfiguration, typeof(T)));

            return this;
        }

        // Choose a property to export then configure it
        public PartBuilder ImportProperties(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ImportProperties(propertyFilter, null);
        }

        public PartBuilder ImportProperties(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportBuilder> importConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            _propertyImports.Add(Tuple.Create(propertyFilter, importConfiguration, default(Type)));
            return this;
        }

        // Choose a property to export then configure it
        public PartBuilder ImportProperties<T>(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ImportProperties<T>(propertyFilter, null);
        }

        public PartBuilder ImportProperties<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportBuilder> importConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            _propertyImports.Add(Tuple.Create(propertyFilter, importConfiguration, typeof(T)));
            return this;
        }

        public PartBuilder SetCreationPolicy(CreationPolicy creationPolicy)
        {
            _setCreationPolicy = true;
            _creationPolicy = creationPolicy;
            return this;
        }

        public PartBuilder AddMetadata(string name, object value)
        {
            _metadataItems ??= new List<Tuple<string, object>>();
            _metadataItems.Add(Tuple.Create(name, value));

            return this;
        }

        public PartBuilder AddMetadata(string name, Func<Type, object> itemFunc)
        {
            _metadataItemFuncs ??= new List<Tuple<string, Func<Type, object>>>();
            _metadataItemFuncs.Add(Tuple.Create(name, itemFunc));

            return this;
        }

        private static bool MemberHasExportMetadata(MemberInfo member)
        {
            foreach (Attribute attr in member.GetCustomAttributes(typeof(Attribute), false))
            {
                if (attr is ExportMetadataAttribute)
                {
                    return true;
                }
                else
                {
                    Type attrType = attr.GetType();
                    // Perf optimization, relies on short circuit evaluation, often a property attribute is an ExportAttribute
                    if (attrType != s_exportAttributeType && attrType.IsDefined(typeof(MetadataAttributeAttribute), true))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal IEnumerable<Attribute> BuildTypeAttributes(Type type)
        {
            var attributes = new List<Attribute>();

            if (_typeExportBuilders != null)
            {
                bool isConfigured = type.GetCustomAttributes(typeof(ExportAttribute), false).FirstOrDefault() != null || MemberHasExportMetadata(type);
                if (isConfigured)
                {
                    CompositionTrace.Registration_TypeExportConventionOverridden(type);
                }
                else
                {
                    foreach (ExportBuilder export in _typeExportBuilders)
                    {
                        export.BuildAttributes(type, ref attributes);
                    }
                }
            }

            if (_setCreationPolicy)
            {
                // Check if there is already a PartCreationPolicyAttribute
                // If found Trace a warning and do not add the registered part creationpolicy
                // otherwise add new one
                bool isConfigured = type.GetCustomAttributes(typeof(PartCreationPolicyAttribute), false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_PartCreationConventionOverridden(type);
                }
                else
                {
                    attributes.Add(new PartCreationPolicyAttribute(_creationPolicy));
                }
            }

            //Add metadata attributes from direct specification
            if (_metadataItems != null)
            {
                bool isConfigured = type.GetCustomAttributes(typeof(PartMetadataAttribute), false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_PartMetadataConventionOverridden(type);
                }
                else
                {
                    foreach (Tuple<string, object> item in _metadataItems)
                    {
                        attributes.Add(new PartMetadataAttribute(item.Item1, item.Item2));
                    }
                }
            }

            //Add metadata attributes from func specification
            if (_metadataItemFuncs != null)
            {
                bool isConfigured = type.GetCustomAttributes(typeof(PartMetadataAttribute), false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_PartMetadataConventionOverridden(type);
                }
                else
                {
                    foreach (Tuple<string, Func<Type, object>> item in _metadataItemFuncs)
                    {
                        var name = item.Item1;
                        var value = (item.Item2 != null) ? item.Item2(type) : null;
                        attributes.Add(new PartMetadataAttribute(name, value));
                    }
                }
            }

            if (_interfaceExports.Any())
            {
                if (_typeExportBuilders != null)
                {
                    bool isConfigured = type.GetCustomAttributes(typeof(ExportAttribute), false).FirstOrDefault() != null || MemberHasExportMetadata(type);
                    if (isConfigured)
                    {
                        CompositionTrace.Registration_TypeExportConventionOverridden(type);
                    }
                    else
                    {
                        foreach (Type iface in type.GetInterfaces())
                        {
                            Type underlyingType = iface.UnderlyingSystemType;

                            if (underlyingType == typeof(IDisposable) || underlyingType == typeof(IPartImportsSatisfiedNotification))
                            {
                                continue;
                            }

                            // Run through the export specifications see if any match
                            foreach (Tuple<Predicate<Type>, Action<Type, ExportBuilder>> exportSpecification in _interfaceExports)
                            {
                                if (exportSpecification.Item1 != null && exportSpecification.Item1(underlyingType))
                                {
                                    ExportBuilder exportBuilder = new ExportBuilder();
                                    exportBuilder.AsContractType((Type)iface);
                                    exportSpecification.Item2?.Invoke(iface, exportBuilder);
                                    exportBuilder.BuildAttributes(iface, ref attributes);
                                }
                            }
                        }
                    }

                }
            }

            return attributes;
        }

        internal bool BuildConstructorAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            ConstructorInfo[] constructors = type.GetConstructors();

            // First see if any of these constructors have the ImportingConstructorAttribute if so then we are already done
            foreach (ConstructorInfo ci in constructors)
            {
                // We have a constructor configuration we must log a warning then not bother with ConstructorAttributes
                object[] attributes = ci.GetCustomAttributes(typeof(ImportingConstructorAttribute), false);
                if (attributes.Length != 0)
                {
                    CompositionTrace.Registration_ConstructorConventionOverridden(type);
                    return true;
                }
            }

            if (_constructorFilter != null)
            {
                ConstructorInfo constructorInfo = _constructorFilter(constructors);
                if (constructorInfo != null)
                {
                    ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, _configureConstructorImports);
                }

                return true;
            }
            else if (_configureConstructorImports != null)
            {
                bool configured = false;
                foreach (ConstructorInfo constructorInfo in FindLongestConstructors(constructors))
                {
                    ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, _configureConstructorImports);
                    configured = true;
                }

                return configured;
            }

            return false;
        }

        internal static void BuildDefaultConstructorAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            ConstructorInfo[] constructors = type.GetConstructors();

            foreach (ConstructorInfo constructorInfo in FindLongestConstructors(constructors))
            {
                ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, null);
            }
        }

        private static void ConfigureConstructorAttributes(ConstructorInfo constructorInfo, ref List<Tuple<object, List<Attribute>>> configuredMembers, Action<ParameterInfo, ImportBuilder> configureConstructorImports)
        {
            configuredMembers ??= new List<Tuple<object, List<Attribute>>>();

            // Make its attribute
            configuredMembers.Add(Tuple.Create((object)constructorInfo, s_importingConstructorList));

            //Okay we have the constructor now we can configure the ImportBuilders
            ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
            foreach (ParameterInfo pi in parameterInfos)
            {
                bool isConfigured = pi.GetCustomAttributes(typeof(ImportAttribute), false).FirstOrDefault() != null || pi.GetCustomAttributes(typeof(ImportManyAttribute), false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_ParameterImportConventionOverridden(pi, constructorInfo);
                }
                else
                {
                    var importBuilder = new ImportBuilder();

                    // Let the developer alter them if they specified to do so
                    configureConstructorImports?.Invoke(pi, importBuilder);

                    // Generate the attributes
                    List<Attribute> attributes = null;
                    importBuilder.BuildAttributes(pi.ParameterType, ref attributes);
                    configuredMembers.Add(Tuple.Create((object)pi, attributes));
                }
            }
        }

        internal void BuildPropertyAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            if (_propertyImports.Any() || _propertyExports.Any())
            {
                foreach (PropertyInfo pi in type.GetProperties())
                {
                    List<Attribute> attributes = null;
                    PropertyInfo declaredPi = pi.DeclaringType.UnderlyingSystemType.GetProperty(pi.Name, pi.PropertyType);
                    int importsBuilt = 0;
                    bool checkedIfConfigured = false;
                    bool isConfigured = false;

                    // Run through the import specifications see if any match
                    foreach (Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportBuilder>, Type> importSpecification in _propertyImports)
                    {
                        if (importSpecification.Item1 != null && importSpecification.Item1(declaredPi))
                        {
                            var importBuilder = new ImportBuilder();

                            if (importSpecification.Item3 != null)
                            {
                                importBuilder.AsContractType(importSpecification.Item3);
                            }

                            importSpecification.Item2?.Invoke(declaredPi, importBuilder);

                            if (!checkedIfConfigured)
                            {
                                isConfigured = pi.GetCustomAttributes(typeof(ImportAttribute), false).FirstOrDefault() != null || pi.GetCustomAttributes(typeof(ImportManyAttribute), false).FirstOrDefault() != null;
                                checkedIfConfigured = true;
                            }

                            if (isConfigured)
                            {
                                CompositionTrace.Registration_MemberImportConventionOverridden(type, pi);
                                break;
                            }
                            else
                            {
                                importBuilder.BuildAttributes(declaredPi.PropertyType, ref attributes);
                                ++importsBuilt;
                            }
                        }
                        if (importsBuilt > 1)
                        {
                            CompositionTrace.Registration_MemberImportConventionMatchedTwice(type, pi);
                        }
                    }

                    checkedIfConfigured = false;
                    isConfigured = false;

                    // Run through the export specifications see if any match
                    foreach (Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportBuilder>, Type> exportSpecification in _propertyExports)
                    {
                        if (exportSpecification.Item1 != null && exportSpecification.Item1(declaredPi))
                        {
                            var exportBuilder = new ExportBuilder();

                            if (exportSpecification.Item3 != null)
                            {
                                exportBuilder.AsContractType(exportSpecification.Item3);
                            }

                            exportSpecification.Item2?.Invoke(declaredPi, exportBuilder);

                            if (!checkedIfConfigured)
                            {
                                isConfigured = pi.GetCustomAttributes(typeof(ExportAttribute), false).FirstOrDefault() != null || MemberHasExportMetadata(pi);
                                checkedIfConfigured = true;
                            }

                            if (isConfigured)
                            {
                                CompositionTrace.Registration_MemberExportConventionOverridden(type, pi);
                                break;
                            }
                            else
                            {
                                exportBuilder.BuildAttributes(declaredPi.PropertyType, ref attributes);
                            }
                        }
                    }

                    if (attributes != null)
                    {
                        configuredMembers ??= new List<Tuple<object, List<Attribute>>>();

                        configuredMembers.Add(Tuple.Create((object)declaredPi, attributes));
                    }
                }
            }
        }

        private static IEnumerable<ConstructorInfo> FindLongestConstructors(ConstructorInfo[] constructors)
        {
            ConstructorInfo longestConstructor = null;
            int argumentsCount = 0;
            int constructorsFound = 0;

            foreach (ConstructorInfo candidateConstructor in constructors)
            {
                int length = candidateConstructor.GetParameters().Length;
                if (length != 0)
                {
                    if (length > argumentsCount)
                    {
                        longestConstructor = candidateConstructor;
                        argumentsCount = length;
                        constructorsFound = 1;
                    }
                    else if (length == argumentsCount)
                    {
                        ++constructorsFound;
                    }
                }
            }
            if (constructorsFound > 1)
            {
                foreach (ConstructorInfo candidateConstructor in constructors)
                {
                    int length = candidateConstructor.GetParameters().Length;
                    if (length == argumentsCount)
                    {
                        yield return candidateConstructor;
                    }
                }
            }
            else if (constructorsFound == 1)
            {
                yield return longestConstructor;
            }

            yield break;
        }
    }
}
