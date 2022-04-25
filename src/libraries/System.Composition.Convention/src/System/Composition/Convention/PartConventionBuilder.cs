// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Diagnostics;
using System.Linq;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Configures a type as a MEF part.
    /// </summary>
    public class PartConventionBuilder
    {
        private readonly Type[] _emptyTypeArray = Type.EmptyTypes;
        private static List<Attribute> s_onImportsSatisfiedAttributeList;
        private static readonly List<Attribute> s_importingConstructorList = new List<Attribute>() { new ImportingConstructorAttribute() };
        private static readonly Type s_exportAttributeType = typeof(ExportAttribute);
        private readonly List<ExportConventionBuilder> _typeExportBuilders;
        private bool _isShared;
        private string _sharingBoundary;

        // Metadata selection
        private List<Tuple<string, object>> _metadataItems;
        private List<Tuple<string, Func<Type, object>>> _metadataItemFuncs;

        // Constructor selector / configuration
        private Func<IEnumerable<ConstructorInfo>, ConstructorInfo> _constructorFilter;
        private Action<ParameterInfo, ImportConventionBuilder> _configureConstuctorImports;

        //Property Import/Export selection and configuration
        private readonly List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportConventionBuilder>, Type>> _propertyExports;
        private readonly List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportConventionBuilder>>> _propertyImports;
        private readonly List<Tuple<Predicate<Type>, Action<Type, ExportConventionBuilder>>> _interfaceExports;
        private readonly List<Predicate<MethodInfo>> _methodImportsSatisfiedNotifications;

        internal Predicate<Type> SelectType { get; private set; }

        internal PartConventionBuilder(Predicate<Type> selectType)
        {
            SelectType = selectType;
            _typeExportBuilders = new List<ExportConventionBuilder>();
            _propertyExports = new List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportConventionBuilder>, Type>>();
            _propertyImports = new List<Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportConventionBuilder>>>();
            _interfaceExports = new List<Tuple<Predicate<Type>, Action<Type, ExportConventionBuilder>>>();
            _methodImportsSatisfiedNotifications = new List<Predicate<MethodInfo>>();
        }

        /// <summary>
        /// Export the part using its own concrete type as the contract.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Export()
        {
            var exportBuilder = new ExportConventionBuilder();
            _typeExportBuilders.Add(exportBuilder);
            return this;
        }

        /// <summary>
        /// Export the part.
        /// </summary>
        /// <param name="exportConfiguration">Configuration action for the export.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Export(Action<ExportConventionBuilder> exportConfiguration)
        {
            if (exportConfiguration is null)
            {
                throw new ArgumentNullException(nameof(exportConfiguration));
            }

            var exportBuilder = new ExportConventionBuilder();
            exportConfiguration(exportBuilder);
            _typeExportBuilders.Add(exportBuilder);
            return this;
        }

        /// <summary>
        /// Export the part using <typeparamref name="T"/> as the contract.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Export<T>()
        {
            ExportConventionBuilder exportBuilder = new ExportConventionBuilder().AsContractType<T>();
            _typeExportBuilders.Add(exportBuilder);
            return this;
        }

        /// <summary>
        /// Export the class using <typeparamref name="T"/> as the contract.
        /// </summary>
        /// <param name="exportConfiguration">Configuration action for the export.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Export<T>(Action<ExportConventionBuilder> exportConfiguration)
        {
            if (exportConfiguration is null)
            {
                throw new ArgumentNullException(nameof(exportConfiguration));
            }

            ExportConventionBuilder exportBuilder = new ExportConventionBuilder().AsContractType<T>();
            exportConfiguration(exportBuilder);
            _typeExportBuilders.Add(exportBuilder);
            return this;
        }

        /// <summary>
        /// Select which of the available constructors will be used to instantiate the part.
        /// </summary>
        /// <param name="constructorSelector">Filter that selects a single constructor.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder SelectConstructor(Func<IEnumerable<ConstructorInfo>, ConstructorInfo> constructorSelector)
        {
            if (constructorSelector is null)
            {
                throw new ArgumentNullException(nameof(constructorSelector));
            }

            _constructorFilter = constructorSelector;
            return this;
        }

        /// <summary>
        /// Select which of the available constructors will be used to instantiate the part.
        /// </summary>
        /// <param name="constructorSelector">Filter that selects a single constructor.</param>
        /// <param name="importConfiguration">Action configuring the parameters of the selected constructor.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder SelectConstructor(Func<IEnumerable<ConstructorInfo>, ConstructorInfo> constructorSelector,
            Action<ParameterInfo, ImportConventionBuilder> importConfiguration)
        {
            if (importConfiguration is null)
            {
                throw new ArgumentNullException(nameof(importConfiguration));
            }

            _configureConstuctorImports = importConfiguration;
            SelectConstructor(constructorSelector);
            return this;
        }

        /// <summary>
        /// Select the interfaces on the part type that will be exported.
        /// </summary>
        /// <param name="interfaceFilter">Filter for interfaces.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportInterfaces(Predicate<Type> interfaceFilter)
        {
            if (interfaceFilter is null)
            {
                throw new ArgumentNullException(nameof(interfaceFilter));
            }

            return ExportInterfacesImpl(interfaceFilter, null);
        }

        /// <summary>
        /// Export all interfaces implemented by the part.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportInterfaces()
        {
            return ExportInterfaces(t => true);
        }

        /// <summary>
        /// Select the interfaces on the part type that will be exported.
        /// </summary>
        /// <param name="interfaceFilter">Filter for interfaces.</param>
        /// <param name="exportConfiguration">Action to configure selected interfaces.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportInterfaces(Predicate<Type> interfaceFilter,
            Action<Type, ExportConventionBuilder> exportConfiguration)
        {
            if (interfaceFilter is null)
            {
                throw new ArgumentNullException(nameof(interfaceFilter));
            }
            if (exportConfiguration is null)
            {
                throw new ArgumentNullException(nameof(exportConfiguration));
            }

            return ExportInterfacesImpl(interfaceFilter, exportConfiguration);
        }

        private PartConventionBuilder ExportInterfacesImpl(Predicate<Type> interfaceFilter,
            Action<Type, ExportConventionBuilder> exportConfiguration)
        {
            _interfaceExports.Add(Tuple.Create(interfaceFilter, exportConfiguration));
            return this;
        }

        /// <summary>
        /// Select properties on the part to export.
        /// </summary>
        /// <param name="propertyFilter">Selector for exported properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportProperties(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ExportPropertiesImpl(propertyFilter, null);
        }

        /// <summary>
        /// Select properties on the part to export.
        /// </summary>
        /// <param name="propertyFilter">Selector for exported properties.</param>
        /// <param name="exportConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportProperties(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportConventionBuilder> exportConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }
            if (exportConfiguration is null)
            {
                throw new ArgumentNullException(nameof(exportConfiguration));
            }

            return ExportPropertiesImpl(propertyFilter, exportConfiguration);
        }

        private PartConventionBuilder ExportPropertiesImpl(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportConventionBuilder> exportConfiguration)
        {
            _propertyExports.Add(Tuple.Create(propertyFilter, exportConfiguration, default(Type)));
            return this;
        }

        /// <summary>
        /// Select properties to export from the part.
        /// </summary>
        /// <typeparam name="T">Contract type to export.</typeparam>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportProperties<T>(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ExportPropertiesImpl<T>(propertyFilter, null);
        }

        /// <summary>
        /// Select properties to export from the part.
        /// </summary>
        /// <typeparam name="T">Contract type to export.</typeparam>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <param name="exportConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ExportProperties<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportConventionBuilder> exportConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }
            if (exportConfiguration is null)
            {
                throw new ArgumentNullException(nameof(exportConfiguration));
            }

            return ExportPropertiesImpl<T>(propertyFilter, exportConfiguration);
        }

        private PartConventionBuilder ExportPropertiesImpl<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ExportConventionBuilder> exportConfiguration)
        {
            _propertyExports.Add(Tuple.Create(propertyFilter, exportConfiguration, typeof(T)));
            return this;
        }

        /// <summary>
        /// Select properties to import into the part.
        /// </summary>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ImportProperties(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ImportPropertiesImpl(propertyFilter, null);
        }

        /// <summary>
        /// Select properties to import into the part.
        /// </summary>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <param name="importConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ImportProperties(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportConventionBuilder> importConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }
            if (importConfiguration is null)
            {
                throw new ArgumentNullException(nameof(importConfiguration));
            }

            return ImportPropertiesImpl(propertyFilter, importConfiguration);
        }

        private PartConventionBuilder ImportPropertiesImpl(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportConventionBuilder> importConfiguration)
        {
            _propertyImports.Add(Tuple.Create(propertyFilter, importConfiguration));
            return this;
        }

        /// <summary>
        /// Select properties to import into the part.
        /// </summary>
        /// <typeparam name="T">Property type to import.</typeparam>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ImportProperties<T>(Predicate<PropertyInfo> propertyFilter)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ImportPropertiesImpl<T>(propertyFilter, null);
        }

        /// <summary>
        /// Select properties to import into the part.
        /// </summary>
        /// <typeparam name="T">Property type to import.</typeparam>
        /// <param name="propertyFilter">Filter to select matching properties.</param>
        /// <param name="importConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder ImportProperties<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportConventionBuilder> importConfiguration)
        {
            if (propertyFilter is null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }
            if (importConfiguration is null)
            {
                throw new ArgumentNullException(nameof(importConfiguration));
            }

            return ImportPropertiesImpl<T>(propertyFilter, importConfiguration);
        }

        private PartConventionBuilder ImportPropertiesImpl<T>(Predicate<PropertyInfo> propertyFilter,
            Action<PropertyInfo, ImportConventionBuilder> importConfiguration)
        {
            Predicate<PropertyInfo> typedFilter = pi => pi.PropertyType.Equals(typeof(T)) && (propertyFilter == null || propertyFilter(pi));
            _propertyImports.Add(Tuple.Create(typedFilter, importConfiguration));
            return this;
        }

        /// <summary>
        /// Mark the part as being shared within the entire composition.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder NotifyImportsSatisfied(Predicate<MethodInfo> methodFilter)
        {
            _methodImportsSatisfiedNotifications.Add(methodFilter);
            return this;
        }

        /// <summary>
        /// Mark the part as being shared within the entire composition.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Shared()
        {
            return SharedImpl(null);
        }

        /// <summary>
        /// Mark the part as being shared within the specified boundary.
        /// </summary>
        /// <param name="sharingBoundary">Name of the sharing boundary.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder Shared(string sharingBoundary)
        {
            if (sharingBoundary is null)
            {
                throw new ArgumentNullException(nameof(sharingBoundary));
            }

            if (sharingBoundary.Length == 0)
            {
                throw new ArgumentException(SR.ArgumentException_EmptyString);
            }

            return SharedImpl(sharingBoundary);
        }

        private PartConventionBuilder SharedImpl(string sharingBoundary)
        {
            _isShared = true;
            _sharingBoundary = sharingBoundary;
            return this;
        }

        /// <summary>
        /// Add the specified metadata to the part.
        /// </summary>
        /// <param name="name">The metadata name.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder AddPartMetadata(string name, object value)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }

            if (_metadataItems == null)
            {
                _metadataItems = new List<Tuple<string, object>>();
            }
            _metadataItems.Add(Tuple.Create(name, value));
            return this;
        }

        /// <summary>
        /// Add the specified metadata to the part.
        /// </summary>
        /// <param name="name">The metadata name.</param>
        /// <param name="getValueFromPartType">A function mapping the part type to the metadata value.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder AddPartMetadata(string name, Func<Type, object> getValueFromPartType)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (getValueFromPartType is null)
            {
                throw new ArgumentNullException(nameof(getValueFromPartType));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }

            if (_metadataItemFuncs == null)
            {
                _metadataItemFuncs = new List<Tuple<string, Func<Type, object>>>();
            }
            _metadataItemFuncs.Add(Tuple.Create(name, getValueFromPartType));
            return this;
        }

        private static bool MemberHasExportMetadata(MemberInfo member)
        {
            foreach (Attribute attr in member.GetCustomAttributes<Attribute>(false))
            {
                if (attr is ExportMetadataAttribute)
                {
                    return true;
                }
                else
                {
                    Type attrType = attr.GetType();
                    // Perf optimization, relies on short circuit evaluation, often a property attribute is an ExportAttribute
                    if (attrType != s_exportAttributeType && attrType.GetTypeInfo().IsDefined(typeof(MetadataAttributeAttribute), true))
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
                bool isConfigured = type.GetTypeInfo().GetCustomAttributes<ExportAttribute>(false).FirstOrDefault() != null || MemberHasExportMetadata(type.GetTypeInfo());
                if (isConfigured)
                {
                    CompositionTrace.Registration_TypeExportConventionOverridden(type);
                }
                else
                {
                    foreach (ExportConventionBuilder export in _typeExportBuilders)
                    {
                        export.BuildAttributes(type, ref attributes);
                    }
                }
            }

            if (_isShared)
            {
                // Check if there is already a SharedAttribute.  If found Trace a warning and do not add this Shared
                // otherwise add new one
                bool isConfigured = type.GetTypeInfo().GetCustomAttributes<SharedAttribute>(false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_PartCreationConventionOverridden(type);
                }
                else
                {
                    attributes.Add(_sharingBoundary == null ?
                        new SharedAttribute() :
                        new SharedAttribute(_sharingBoundary));
                }
            }

            //Add metadata attributes from direct specification
            if (_metadataItems != null)
            {
                bool isConfigured = type.GetTypeInfo().GetCustomAttributes<PartMetadataAttribute>(false).FirstOrDefault() != null;
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
                bool isConfigured = type.GetTypeInfo().GetCustomAttributes<PartMetadataAttribute>(false).FirstOrDefault() != null;
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
                    bool isConfigured = type.GetTypeInfo().GetCustomAttributes<ExportAttribute>(false).FirstOrDefault() != null || MemberHasExportMetadata(type.GetTypeInfo());
                    if (isConfigured)
                    {
                        CompositionTrace.Registration_TypeExportConventionOverridden(type);
                    }
                    else
                    {
                        foreach (Type iface in type.GetTypeInfo().ImplementedInterfaces)
                        {
                            if (iface == typeof(IDisposable))
                            {
                                continue;
                            }

                            // Run through the export specifications see if any match
                            foreach (Tuple<Predicate<Type>, Action<Type, ExportConventionBuilder>> exportSpecification in _interfaceExports)
                            {
                                if (exportSpecification.Item1 != null && exportSpecification.Item1(iface))
                                {
                                    ExportConventionBuilder exportBuilder = new ExportConventionBuilder();
                                    exportBuilder.AsContractType(iface);
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
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            // First see if any of these constructors have the ImportingConstructorAttribute if so then we are already done
            foreach (ConstructorInfo ci in constructors)
            {
                // We have a constructor configuration we must log a warning then not bother with ConstructorAttributes
                Attribute[] attributes = Attribute.GetCustomAttributes(ci, typeof(ImportingConstructorAttribute), false);
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
                    ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, _configureConstuctorImports);
                }
                return true;
            }
            else if (_configureConstuctorImports != null)
            {
                bool configured = false;
                foreach (ConstructorInfo constructorInfo in FindLongestConstructors(constructors))
                {
                    ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, _configureConstuctorImports);
                    configured = true;
                }
                return configured;
            }
            return false;
        }

        internal static void BuildDefaultConstructorAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (ConstructorInfo constructorInfo in FindLongestConstructors(constructors))
            {
                ConfigureConstructorAttributes(constructorInfo, ref configuredMembers, null);
            }
        }

        private static void ConfigureConstructorAttributes(ConstructorInfo constructorInfo, ref List<Tuple<object, List<Attribute>>> configuredMembers, Action<ParameterInfo, ImportConventionBuilder> configureConstuctorImports)
        {
            if (configuredMembers == null)
            {
                configuredMembers = new List<Tuple<object, List<Attribute>>>();
            }

            // Make its attribute
            configuredMembers.Add(Tuple.Create((object)constructorInfo, s_importingConstructorList));

            //Okay we have the constructor now we can configure the ImportBuilders
            ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
            foreach (ParameterInfo pi in parameterInfos)
            {
                bool isConfigured = pi.GetCustomAttributes<ImportAttribute>(false).FirstOrDefault() != null || pi.GetCustomAttributes<ImportManyAttribute>(false).FirstOrDefault() != null;
                if (isConfigured)
                {
                    CompositionTrace.Registration_ParameterImportConventionOverridden(pi, constructorInfo);
                }
                else
                {
                    var importBuilder = new ImportConventionBuilder();

                    // Let the developer alter them if they specified to do so
                    configureConstuctorImports?.Invoke(pi, importBuilder);

                    // Generate the attributes
                    List<Attribute> attributes = null;
                    importBuilder.BuildAttributes(pi.ParameterType, ref attributes);
                    configuredMembers.Add(Tuple.Create((object)pi, attributes));
                }
            }
        }

        internal void BuildOnImportsSatisfiedNotification(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            //Add OnImportsSatisfiedAttribute where specified
            if (_methodImportsSatisfiedNotifications != null)
            {
                foreach (MethodInfo mi in type.GetRuntimeMethods())
                {
                    //We are only interested in void methods with no arguments
                    if (mi.ReturnParameter.ParameterType == typeof(void)
                     && mi.GetParameters().Length == 0)
                    {
                        MethodInfo underlyingMi = mi.DeclaringType.GetRuntimeMethod(mi.Name, _emptyTypeArray);
                        if (underlyingMi != null)
                        {
                            bool checkedIfConfigured = false;
                            bool isConfigured = false;
                            foreach (Predicate<MethodInfo> notification in _methodImportsSatisfiedNotifications)
                            {
                                if (notification(underlyingMi))
                                {
                                    if (!checkedIfConfigured)
                                    {
                                        isConfigured = mi.GetCustomAttributes<OnImportsSatisfiedAttribute>(false).FirstOrDefault() != null;
                                        checkedIfConfigured = true;
                                    }

                                    if (isConfigured)
                                    {
                                        CompositionTrace.Registration_OnSatisfiedImportNotificationOverridden(type, mi);
                                        break;
                                    }
                                    else
                                    {
                                        // We really only need to create this list once and then cache it, it never goes back to null
                                        // Its perfectly okay if we make a list a few times on different threads, effectively though once we have
                                        // cached one we will never make another.
                                        if (s_onImportsSatisfiedAttributeList == null)
                                        {
                                            var onImportsSatisfiedAttributeList = new List<Attribute>
                                            {
                                                new OnImportsSatisfiedAttribute()
                                            };
                                            s_onImportsSatisfiedAttributeList = onImportsSatisfiedAttributeList;
                                        }
                                        configuredMembers.Add(new Tuple<object, List<Attribute>>(mi, s_onImportsSatisfiedAttributeList));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void BuildPropertyAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            if (_propertyImports.Any() || _propertyExports.Any())
            {
                foreach (PropertyInfo pi in type.GetRuntimeProperties())
                {
                    List<Attribute> attributes = null;
                    int importsBuilt = 0;
                    bool checkedIfConfigured = false;
                    bool isConfigured = false;

                    PropertyInfo underlyingPi = null;

                    // Run through the import specifications see if any match
                    foreach (Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ImportConventionBuilder>> importSpecification in _propertyImports)
                    {
                        if (underlyingPi == null)
                        {
                            underlyingPi = pi.DeclaringType.GetRuntimeProperty(pi.Name);
                        }
                        if (importSpecification.Item1 != null && importSpecification.Item1(underlyingPi))
                        {
                            var importBuilder = new ImportConventionBuilder();

                            importSpecification.Item2?.Invoke(pi, importBuilder);

                            if (!checkedIfConfigured)
                            {
                                isConfigured = pi.GetCustomAttributes<ImportAttribute>(false).FirstOrDefault() != null || pi.GetCustomAttributes<ImportManyAttribute>(false).FirstOrDefault() != null;
                                checkedIfConfigured = true;
                            }

                            if (isConfigured)
                            {
                                CompositionTrace.Registration_MemberImportConventionOverridden(type, pi);
                                break;
                            }
                            else
                            {
                                importBuilder.BuildAttributes(pi.PropertyType, ref attributes);
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
                    foreach (Tuple<Predicate<PropertyInfo>, Action<PropertyInfo, ExportConventionBuilder>, Type> exportSpecification in _propertyExports)
                    {
                        if (underlyingPi == null)
                        {
                            underlyingPi = pi.DeclaringType.GetRuntimeProperty(pi.Name);
                        }

                        if (exportSpecification.Item1 != null && exportSpecification.Item1(underlyingPi))
                        {
                            var exportBuilder = new ExportConventionBuilder();

                            if (exportSpecification.Item3 != null)
                            {
                                exportBuilder.AsContractType(exportSpecification.Item3);
                            }

                            exportSpecification.Item2?.Invoke(pi, exportBuilder);

                            if (!checkedIfConfigured)
                            {
                                isConfigured = pi.GetCustomAttributes<ExportAttribute>(false).FirstOrDefault() != null || MemberHasExportMetadata(pi);
                                checkedIfConfigured = true;
                            }

                            if (isConfigured)
                            {
                                CompositionTrace.Registration_MemberExportConventionOverridden(type, pi);
                                break;
                            }
                            else
                            {
                                exportBuilder.BuildAttributes(pi.PropertyType, ref attributes);
                            }
                        }
                    }

                    if (attributes != null)
                    {
                        if (configuredMembers == null)
                        {
                            configuredMembers = new List<Tuple<object, List<Attribute>>>();
                        }

                        configuredMembers.Add(Tuple.Create((object)pi, attributes));
                    }
                }
            }
            return;
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
