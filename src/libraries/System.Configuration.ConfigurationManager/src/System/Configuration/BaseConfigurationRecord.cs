// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Xml;

namespace System.Configuration
{
    // This object represents the configuration for a request path, and is cached per-path.
    [DebuggerDisplay("ConfigPath = {ConfigPath}")]
    internal abstract class BaseConfigurationRecord : IInternalConfigRecord
    {
        protected const string NewLine = "\r\n";

        internal const string KeywordTrue = "true";
        internal const string KeywordFalse = "false";

        protected const string ConfigurationTag = "configuration";
        protected const string XmlnsAttribute = "xmlns";
        protected const string ConfigurationNamespace = "http://schemas.microsoft.com/.NetConfiguration/v2.0";

        protected const string ConfigSectionsTag = "configSections";

        protected const string SectionTag = "section";
        protected const string SectionNameAttribute = "name";
        protected const string SectionTypeAttribute = "type";
        protected const string SectionAllowLocationAttribute = "allowLocation";
        protected const string SectionAllowDefinitionAttribute = "allowDefinition";
        protected const string AllowDefinitionEverywhere = "Everywhere";
        protected const string AllowDefinitionMachineOnly = "MachineOnly";
        protected const string AllowDefinitionMachineToApplication = "MachineToApplication";
        protected const string AllowDefinitionMachineToWebRoot = "MachineToWebRoot";
        protected const string SectionAllowExeDefinitionAttribute = "allowExeDefinition";
        protected const string AllowExeDefinitionMachineToRoaming = "MachineToRoamingUser";
        protected const string AllowExeDefinitionMachineToLocal = "MachineToLocalUser";
        protected const string SectionRestartonExternalChangesAttribute = "restartOnExternalChanges";
        protected const string SectionRequirePermissionAttribute = "requirePermission";
        internal const string SectionOverrideModeDefaultAttribute = "overrideModeDefault";

        internal const string OverrideModeInherit = "Inherit";
        internal const string OverrideModeAllow = "Allow";
        internal const string OverrideModeDeny = "Deny";

        protected const string SectionGroupTag = "sectionGroup";
        protected const string SectionGroupNameAttribute = "name";
        protected const string SectionGroupTypeAttribute = "type";

        protected const string RemoveTag = "remove";
        protected const string ClearTag = "clear";

        protected const string LocationTag = "location";
        protected const string LocationPathAttribute = "path";
        internal const string LocationAllowOverrideAttribute = "allowOverride";
        internal const string LocationOverrideModeAttribute = "overrideMode";
        protected const string LocationInheritInChildApplicationsAttribute = "inheritInChildApplications";

        protected const string ConfigSourceAttribute = "configSource";
        internal const string ProtectionProviderAttribute = "configProtectionProvider";

        protected const string FormatNewConfigFile = "<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n";
        protected const string FormatConfiguration = "<configuration>\r\n";
        protected const string FormatConfigurationNamespace = "<configuration xmlns=\"{0}\">\r\n";
        protected const string FormatConfigurationEndElement = "</configuration>";

        protected const string FormatLocationNoPath = "<location {0} inheritInChildApplications=\"{1}\">\r\n";
        protected const string FormatLocationPath = "<location path=\"{2}\" {0} inheritInChildApplications=\"{1}\">\r\n";
        protected const string FormatLocationEndElement = "</location>";
        internal const string KeywordLocationOverrideModeString = "{0}=\"{1}\"";
        protected const string FormatSectionConfigSource = "<{0} configSource=\"{1}\" />";
        protected const string FormatConfigSourceFile = "<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n";
        protected const string FormatSectionGroupEndElement = "</sectionGroup>";

        // Class flags should only be used with the ClassFlags property.
        protected const int ClassSupportsChangeNotifications = 0x00000001;
        protected const int ClassSupportsRefresh = 0x00000002;
        protected const int ClassSupportsImpersonation = 0x00000004;
        protected const int ClassSupportsRestrictedPermissions = 0x00000008;
        protected const int ClassSupportsKeepInputs = 0x00000010;
        protected const int ClassSupportsDelayedInit = 0x00000020;
        protected const int ClassIgnoreLocalErrors = 0x00000040;

        // Flags to use with the _flags field.
        protected const int ProtectedDataInitialized = 0x00000001;
        protected const int Closed = 0x00000002;
        protected const int PrefetchAll = 0x00000008;
        protected const int IsAboveApplication = 0x00000020;
        private const int ContextEvaluated = 0x00000080;
        private const int IsLocationListResolved = 0x00000100;
        protected const int NamespacePresentInFile = 0x00000200;
        protected const int IsTrusted = 0x00002000;
        protected const int SupportsChangeNotifications = 0x00010000;
        protected const int SupportsRefresh = 0x00020000;
        protected const int SupportsPath = 0x00040000;
        protected const int SupportsKeepInputs = 0x00080000;
        protected const int SupportsLocation = 0x00100000;

        // Flags for Mgmt Configuration Record
        protected const int ForceLocationWritten = 0x01000000;
        protected const int SuggestLocationRemoval = 0x02000000;
        protected const int NamespacePresentCurrent = 0x04000000;

        internal const char ConfigPathSeparatorChar = '/';
        internal const string ConfigPathSeparatorString = "/";

        // From http://www.w3.org/Addressing/
        //  reserved    = ';' | '/' | '?' | ':' | '@' | '&' | '=' | '+' | '$' | ','
        // From Platform SDK
        //  reserved    = '\' |  '/' | '|' | ':' |  '"' |  '<' | '>'

        // NOTE: If you change these strings, you must change the associated error message
        private const string InvalidFirstSubPathCharacters = @"\./";
        private const string InvalidLastSubPathCharacters = @"\./";
        private const string InvalidSubPathCharactersString = @"\?:*""<>|";

        private const string ProtectedConfigurationSectionTypeName = "System.Configuration.ProtectedConfigurationSection, " + TypeUtil.ConfigurationManagerAssemblyName;

        internal const string ReservedSectionProtectedConfiguration = "configProtectedData";
        internal static readonly char[] s_configPathSeparatorParams = { ConfigPathSeparatorChar };
        private static string s_appConfigPath;

        // Comparer used in sorting IndirectInputs.
        private static readonly IComparer<SectionInput> s_indirectInputsComparer = new IndirectLocationInputComparer();
        private static readonly char[] s_invalidSubPathCharactersArray = InvalidSubPathCharactersString.ToCharArray();
        protected Hashtable _children; // configName -> record

        private object _configContext; // Context for config level
        protected string _configName; // the last part of the config path
        protected string _configPath; // the full config path
        protected InternalConfigRoot _configRoot; // root of configuration

        private ConfigRecordStreamInfo _configStreamInfo; // stream info for the config record

        // Records information about <configSections> present in this web.config file.
        //      config key -> FactoryRecord
        protected Hashtable _factoryRecords;

        protected SafeBitVector32 _flags; // state
        private BaseConfigurationRecord _initDelayedRoot; // root of delayed initialization
        private ConfigurationSchemaErrors _initErrors; // errors encountered during the parse of the configuration file

        // Records information about sections in a <location> directive
        // that do not apply to this configPath (sections where path != ".")
        protected ArrayList _locationSections;
        protected string _locationSubPath; // subPath for the config record when editing a location configuration
        protected BaseConfigurationRecord _parent; // parent record
        private ProtectedConfigurationSection _protectedConfig; // section containing the encryption providers

        // Records information about sections that apply to this path,
        // which may be found in this web.config file, in a parent through
        // inheritance, or in a parent through <location>
        //      config key -> SectionRecord
        protected Hashtable _sectionRecords;

        internal BaseConfigurationRecord()
        {
            // not strictly necessary, but compiler spits out a warning without this initiailization
            _flags = default;
        }

        protected abstract SimpleBitVector32 ClassFlags { get; }

        internal bool HasStream => ConfigStreamInfo.HasStream;

        private bool IsInitDelayed => _initDelayedRoot != null;

        internal IInternalConfigHost Host => _configRoot.Host;

        internal BaseConfigurationRecord Parent => _parent;

        internal bool IsRootConfig => _parent == null;

        internal bool IsMachineConfig => _parent == _configRoot.RootConfigRecord;

        internal string LocationSubPath => _locationSubPath;

        internal bool IsLocationConfig => _locationSubPath != null;

        protected ConfigRecordStreamInfo ConfigStreamInfo
            => IsLocationConfig ? _parent._configStreamInfo : _configStreamInfo;

        internal string DefaultProviderName => ProtectedConfig.DefaultProvider;

        private ProtectedConfigurationSection ProtectedConfig
        {
            get
            {
                if (!_flags[ProtectedDataInitialized]) InitProtectedConfigurationSection();
                return _protectedConfig;
            }
        }

        private bool HasFactoryRecords => _factoryRecords != null;

        // Return true if there is no unique configuration information in this record.
        internal bool IsEmpty => (_parent != null)
            && !_initErrors.HasErrors(false)
            && ((_sectionRecords == null) || (_sectionRecords.Count == 0))
            && ((_factoryRecords == null) || (_factoryRecords.Count == 0))
            && ((_locationSections == null) || (_locationSections.Count == 0));

        internal object ConfigContext
        {
            get
            {
                if (!_flags[ContextEvaluated])
                {
                    // Retrieve context for Path
                    _configContext = Host.CreateConfigurationContext(ConfigPath, LocationSubPath);
                    _flags[ContextEvaluated] = true;
                }

                return _configContext;
            }
        }

        // Does it make sense to put use location tags in this file?
        // In the web case this is true at any level.  In the exe case
        // this is only true for machine.config (since machine.config
        // can really be used for any scenario)
        internal bool RecordSupportsLocation => _flags[SupportsLocation] || IsMachineConfig;

        internal Configuration CurrentConfiguration => _configRoot.CurrentConfiguration;

        internal bool TypeStringTransformerIsSet => CurrentConfiguration?.TypeStringTransformerIsSet ?? false;

        internal bool AssemblyStringTransformerIsSet => CurrentConfiguration?.AssemblyStringTransformerIsSet ?? false;

        internal Func<string, string> TypeStringTransformer => CurrentConfiguration?.TypeStringTransformer;

        internal Func<string, string> AssemblyStringTransformer => CurrentConfiguration?.AssemblyStringTransformer;

        internal FrameworkName TargetFramework => CurrentConfiguration?.TargetFramework;

        internal Stack SectionsStack => CurrentConfiguration == null ? new Stack() : CurrentConfiguration.SectionsStack;

        public string ConfigPath => _configPath;

        public string StreamName => ConfigStreamInfo.StreamName;

        public bool HasInitErrors => _initErrors.HasErrors(ClassFlags[ClassIgnoreLocalErrors]);

        public void ThrowIfInitErrors()
        {
            ThrowIfParseErrors(_initErrors);
        }

        public object GetSection(string configKey)
        {
            return GetSection(configKey, false, true);
        }

        public object GetLkgSection(string configKey)
        {
            return GetSection(configKey, true, true);
        }

        public void RefreshSection(string configKey)
        {
            _configRoot.ClearResult(this, configKey, true);
        }

        public void Remove()
        {
            _configRoot.RemoveConfigRecord(this);
        }

        // Create the factory that will evaluate configuration
        protected abstract object CreateSectionFactory(FactoryRecord factoryRecord);

        // Create the configuration object
        protected abstract object CreateSection(bool inputIsTrusted, FactoryRecord factoryRecord,
            SectionRecord sectionRecord, object parentConfig, ConfigXmlReader reader);

        // Use the parent result in creating the child
        protected abstract object UseParentResult(string configKey, object parentResult, SectionRecord sectionRecord);

        // Return the runtime object from GetSection
        protected abstract object GetRuntimeObject(object result);

        // Determine which sections should be prefetched during the first scan.
        private bool ShouldPrefetchRawXml(FactoryRecord factoryRecord)
        {
            if (_flags[PrefetchAll])
                return true;

            switch (factoryRecord.ConfigKey)
            {
                case ReservedSectionProtectedConfiguration:
                case "system.diagnostics":
                case "appSettings":
                case "connectionStrings":
                    return true;
            }

            return Host.PrefetchSection(factoryRecord.Group, factoryRecord.Name);
        }

        internal void Init(
            IInternalConfigRoot configRoot,
            BaseConfigurationRecord parent,
            string configPath,
            string locationSubPath)
        {
            _initErrors = new ConfigurationSchemaErrors();

            // try/catch here is only for unexpected exceptions due to errors in
            // our own code, as we always want the configuration record to be usable
            try
            {
                _configRoot = (InternalConfigRoot)configRoot;
                _parent = parent;
                _configPath = configPath;
                _locationSubPath = locationSubPath;
                _configName = ConfigPathUtility.GetName(configPath);

                _configStreamInfo = IsLocationConfig ? _parent.ConfigStreamInfo : new ConfigRecordStreamInfo();

                // no more initialization in case of root config
                if (IsRootConfig)
                    return;

                // determine what features we support
                _flags[SupportsChangeNotifications] = ClassFlags[ClassSupportsChangeNotifications] &&
                    Host.SupportsChangeNotifications;
                _flags[SupportsRefresh] = ClassFlags[ClassSupportsRefresh] && Host.SupportsRefresh;
                _flags[SupportsKeepInputs] = ClassFlags[ClassSupportsKeepInputs] || _flags[SupportsRefresh];
                _flags[SupportsPath] = Host.SupportsPath;
                _flags[SupportsLocation] = Host.SupportsLocation;

                // get static state based on the configPath
                if (_flags[SupportsLocation])
                    _flags[IsAboveApplication] = Host.IsAboveApplication(_configPath);

                _flags[IsTrusted] = true;

                ArrayList locationSubPathInputs = null;

                if (_flags[SupportsLocation])
                {
                    // Treat location inputs from parent record
                    // as though they were bonafide sections in this record.
                    if (IsLocationConfig && (_parent._locationSections != null))
                    {
                        // Resolve paths and check for errors in location sections.
                        _parent.ResolveLocationSections();

                        int i = 0;
                        while (i < _parent._locationSections.Count)
                        {
                            LocationSectionRecord locationSectionRecord =
                                (LocationSectionRecord)_parent._locationSections[i];

                            if (!StringUtil.EqualsIgnoreCase(locationSectionRecord.SectionXmlInfo.TargetConfigPath, ConfigPath))
                            {
                                i++;
                            }
                            else
                            {
                                // remove the LocationSectionRecord from the list
                                _parent._locationSections.RemoveAt(i);

                                if (locationSubPathInputs == null) locationSubPathInputs = new ArrayList();

                                locationSubPathInputs.Add(locationSectionRecord);
                            }
                        }
                    }

                    // Handle indirect inputs for location config record.
                    if (IsLocationConfig && Host.IsLocationApplicable(_configPath))
                    {
                        Dictionary<string, List<SectionInput>> indirectLocationInputs = null;
                        BaseConfigurationRecord current = _parent;

                        // Note that the code will also go thru all parents, just like what we did
                        // with the location inputs later.  But perf-wise it's okay not to merge the two loops
                        // because a Configuration object will contain at most one location config record.
                        while (!current.IsRootConfig)
                        {
                            if (current._locationSections != null)
                            {
                                current.ResolveLocationSections();

                                foreach (LocationSectionRecord locationSectionRecord in current._locationSections)
                                    if (
                                        // Check #1
                                        IsLocationConfig &&
                                        // Check #2.1
                                        UrlPath.IsSubpath(locationSectionRecord.SectionXmlInfo.TargetConfigPath, ConfigPath) &&
                                        // Check #2.2
                                        UrlPath.IsSubpath(parent.ConfigPath, locationSectionRecord.SectionXmlInfo.TargetConfigPath) &&
                                        // Check #3
                                        !ShouldSkipDueToInheritInChildApplications(
                                            locationSectionRecord.SectionXmlInfo.SkipInChildApps,
                                            locationSectionRecord.SectionXmlInfo.TargetConfigPath)
                                        )
                                    {
                                        // In order to separate these kinds of input from "file inputs" and "location inputs"
                                        // we introduce a new kind of input called the "indirect location inputs".

                                        // First add all indirect inputs per configKey to a local list.
                                        // We will sort all lists after the while loop.
                                        if (indirectLocationInputs == null)
                                            indirectLocationInputs = new Dictionary<string, List<SectionInput>>(1);

                                        string configKey = locationSectionRecord.SectionXmlInfo.ConfigKey;

                                        if (!((IDictionary)indirectLocationInputs).Contains(configKey))
                                            indirectLocationInputs.Add(configKey, new List<SectionInput>(1));

                                        indirectLocationInputs[configKey].Add(
                                            new SectionInput(locationSectionRecord.SectionXmlInfo,
                                                locationSectionRecord.ErrorsList));

                                        // copy the initialization errors to the record
                                        if (locationSectionRecord.HasErrors)
                                            _initErrors.AddSavedLocalErrors(locationSectionRecord.Errors);
                                    }
                            }

                            current = current._parent;
                        }

                        if (indirectLocationInputs != null)
                        {
                            // Add indirect inputs per configKey
                            foreach (KeyValuePair<string, List<SectionInput>> keyValuePair in indirectLocationInputs)
                            {
                                List<SectionInput> inputsPerConfigKey = keyValuePair.Value;
                                string configKey = keyValuePair.Key;

                                // We have to sort the indirect inputs
                                // 1. First by the location tag's target config path, and if they're the same,
                                // 2. Then by the location tag's definition config path.
                                inputsPerConfigKey.Sort(s_indirectInputsComparer);

                                // Add them to the section record.
                                // In the sorted list, the closest parent is at the beginning of the
                                // list, which is what we'll add first.
                                SectionRecord sectionRecord = EnsureSectionRecord(configKey, true);
                                Debug.Assert(inputsPerConfigKey.Count > 0, "We won't get here unless we have inputs.");
                                foreach (SectionInput sectionInput in inputsPerConfigKey)
                                    sectionRecord.AddIndirectLocationInput(sectionInput);

                                DebugValidateIndirectInputs(sectionRecord);
                            }
                        }
                    }

                    // Add location inputs that apply to this path, all the way up the parent hierarchy.
                    if (Host.IsLocationApplicable(_configPath))
                    {
                        BaseConfigurationRecord current = _parent;
                        while (!current.IsRootConfig)
                        {
                            if (current._locationSections != null)
                            {
                                current.ResolveLocationSections();
                                foreach (LocationSectionRecord locationSectionRecord in current._locationSections)
                                    if (StringUtil.EqualsIgnoreCase(locationSectionRecord.SectionXmlInfo.TargetConfigPath, _configPath) &&
                                        !ShouldSkipDueToInheritInChildApplications(locationSectionRecord.SectionXmlInfo.SkipInChildApps))
                                    {
                                        // add the location input for this section
                                        SectionRecord sectionRecord =
                                            EnsureSectionRecord(locationSectionRecord.ConfigKey, true);
                                        SectionInput sectionInput = new SectionInput(
                                            locationSectionRecord.SectionXmlInfo, locationSectionRecord.ErrorsList);

                                        sectionRecord.AddLocationInput(sectionInput);

                                        // copy the initialization errors to the record
                                        if (locationSectionRecord.HasErrors)
                                            _initErrors.AddSavedLocalErrors(locationSectionRecord.Errors);
                                    }
                            }

                            current = current._parent;
                        }
                    }
                }

                if (!IsLocationConfig)
                {
                    // If config file exists, open it and parse it once so we can
                    // find what to evaluate later.
                    InitConfigFromFile();
                }
                else
                {
                    // Add location sections for this record as bonafide sections.
                    if (locationSubPathInputs == null) return;
                    foreach (LocationSectionRecord locationSectionRecord in locationSubPathInputs)
                    {
                        // add the file input
                        SectionRecord sectionRecord = EnsureSectionRecord(locationSectionRecord.ConfigKey, true);
                        SectionInput sectionInput = new SectionInput(locationSectionRecord.SectionXmlInfo,
                            locationSectionRecord.ErrorsList);
                        sectionRecord.AddFileInput(sectionInput);

                        // copy the initialization errors to the record
                        if (locationSectionRecord.HasErrors)
                            _initErrors.AddSavedLocalErrors(locationSectionRecord.Errors);
                    }
                }
            }
            catch (Exception e)
            {
                // Capture all exceptions and include them in initialization errors.
                string streamname = ConfigStreamInfo?.StreamName;

                _initErrors.AddError(
                    ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e, streamname, 0),
                    ExceptionAction.Global);
            }
        }

        private void InitConfigFromFile()
        {
            bool implicitSectionsAdded = false;

            try
            {
                // If initialization should be delayed, do not read the file.
                if (ClassFlags[ClassSupportsDelayedInit] && Host.IsInitDelayed(this))
                {
                    // determine the root of delayed initialization
                    _initDelayedRoot = _parent._initDelayedRoot ?? this;
                }
                else
                {
                    // This parent of a record that is not initDelayed must also
                    // not be initDelayed.
                    Debug.Assert(!_parent.IsInitDelayed, "!_parent.IsInitDelayed");

                    // Get the stream name. Note that this may be an expensive operation
                    // for the client configuration host, which is why it comes after the
                    // check for delayed init.
                    ConfigStreamInfo.StreamName = Host.GetStreamName(_configPath);
                    if (!string.IsNullOrEmpty(ConfigStreamInfo.StreamName))
                    {
                        // Lets listen to the stream
                        ConfigStreamInfo.StreamVersion = MonitorStream(null, null, ConfigStreamInfo.StreamName);
                        using (Stream stream = Host.OpenStreamForRead(ConfigStreamInfo.StreamName))
                        {
                            if (stream == null)
                            {
                                // There is no stream to load from
                                return;
                            }

                            ConfigStreamInfo.HasStream = true;

                            // Determine whether or not to prefetch.
                            _flags[PrefetchAll] = Host.PrefetchAll(_configPath, ConfigStreamInfo.StreamName);

                            using (XmlUtil xmlUtil = new XmlUtil(stream, ConfigStreamInfo.StreamName, true, _initErrors))
                            {
                                ConfigStreamInfo.StreamEncoding = xmlUtil.Reader.Encoding;

                                // Read the factories
                                Hashtable factoryList = ScanFactories(xmlUtil);
                                _factoryRecords = factoryList;

                                // Add implicit sections before scanning sections
                                AddImplicitSections(null);
                                implicitSectionsAdded = true;

                                // Read the sections themselves
                                if (xmlUtil.Reader.Depth == 1) ScanSections(xmlUtil);
                            }
                        }
                    }
                }
            }
            catch (XmlException e)
            {
                // Capture all exceptions and include them in _initErrors so execution in Init can continue.

                // Treat invalid XML as unrecoverable by replacing all errors with the XML error.
                _initErrors.SetSingleGlobalError(
                    ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e,
                        ConfigStreamInfo.StreamName, 0));
            }
            catch (Exception e)
            {
                _initErrors.AddError(
                    ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e,
                        ConfigStreamInfo.StreamName, 0),
                    ExceptionAction.Global);
            }

            // If there are global errors that make all input invalid,
            // reset our state so that inherited configuration can still be used,
            // including location sections that apply to this file.
            if (_initErrors.HasGlobalErrors)
            {
                // Parsing of a section may have been in progress when the exception
                // was thrown, so clear any accumulated local errors.
                _initErrors.ResetLocalErrors();

                // Stop monitoring any configSource streams, but still continue
                // to monitor the main config file if it was successfully monitored.
                HybridDictionary streamInfos = null;
                lock (this)
                {
                    if (ConfigStreamInfo.HasStreamInfos)
                    {
                        streamInfos = ConfigStreamInfo.StreamInfos;
                        ConfigStreamInfo.ClearStreamInfos();

                        if (!string.IsNullOrEmpty(ConfigStreamInfo.StreamName))
                        {
                            StreamInfo fileStreamInfo = (StreamInfo)streamInfos[ConfigStreamInfo.StreamName];

                            // add this file's streaminfo to the now empty list
                            if (fileStreamInfo != null)
                            {
                                streamInfos.Remove(ConfigStreamInfo.StreamName);
                                ConfigStreamInfo.StreamInfos.Add(ConfigStreamInfo.StreamName, fileStreamInfo);
                            }
                        }
                    }
                }

                // Stop monitoring streams outside the lock, to prevent deadlock.
                if (streamInfos != null)
                {
                    foreach (StreamInfo streamInfo in streamInfos.Values)
                        if (streamInfo.IsMonitored)
                            Host.StopMonitoringStreamForChanges(streamInfo.StreamName, ConfigStreamInfo.CallbackDelegate);
                }

                // Remove file input
                if (_sectionRecords != null)
                {
                    List<SectionRecord> removes = null;
                    foreach (SectionRecord sectionRecord in _sectionRecords.Values)
                    {
                        Debug.Assert(!sectionRecord.HasIndirectLocationInputs,
                            "Don't expect to have any IndirectLocationInputs because location config record shouldn't call InitConfigFromFile");

                        if (sectionRecord.HasLocationInputs)
                        {
                            // Remove any file input
                            sectionRecord.RemoveFileInput();
                        }
                        else
                        {
                            // Remove the entire section record
                            if (removes == null) removes = new List<SectionRecord>();

                            removes.Add(sectionRecord);
                        }
                    }

                    if (removes != null)
                    {
                        foreach (SectionRecord sectionRecord in removes)
                            _sectionRecords.Remove(sectionRecord.ConfigKey);
                    }
                }

                // Remove all location section input defined here
                _locationSections?.Clear();

                // Remove all factory records
                _factoryRecords?.Clear();
            }

            if (!implicitSectionsAdded)
            {
                // Always add implicit sections no matter we have a file or not.
                AddImplicitSections(null);
            }
        }

        // Refresh the Factory Record for a particular section.
        private void RefreshFactoryRecord(string configKey)
        {
            Hashtable factoryList = null;
            FactoryRecord factoryRecord = null;

            ConfigurationSchemaErrors errors = new ConfigurationSchemaErrors();

            // Get Updated Factory List from File
            int lineNumber = 0;
            try
            {
                using (Stream stream = Host.OpenStreamForRead(ConfigStreamInfo.StreamName))
                {
                    if (stream != null)
                    {
                        ConfigStreamInfo.HasStream = true;

                        using (XmlUtil xmlUtil = new XmlUtil(stream, ConfigStreamInfo.StreamName, true, errors))
                        {
                            try
                            {
                                factoryList = ScanFactories(xmlUtil);
                                ThrowIfParseErrors(xmlUtil.SchemaErrors);
                            }
                            catch
                            {
                                lineNumber = xmlUtil.LineNumber;
                                throw;
                            }
                        }
                    }
                }

                // Add implicit sections to the factory list
                if (factoryList == null)
                {
                    // But if factoryList isn't found in this config, we still try to
                    // add implicit sections to an empty factoryList.
                    factoryList = new Hashtable();
                }

                AddImplicitSections(factoryList);
                factoryRecord = (FactoryRecord)factoryList[configKey];
            }

            // Guarantee that exceptions contain the name of the stream and an approximate line number if available.
            // And don't allow frames up the stack to run exception filters while impersonated.
            catch (Exception e)
            {
                errors.AddError(
                    ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e,
                        ConfigStreamInfo.StreamName, lineNumber),
                    ExceptionAction.Global);
            }

            // Set/Add/Remove Record
            // Note that the _factoryRecords hashtable is protected by the hierarchy lock.
            if ((factoryRecord != null) || HasFactoryRecords) EnsureFactories()[configKey] = factoryRecord;

            // Throw accumulated errors
            ThrowIfParseErrors(errors);
        }

        private object GetSection(string configKey, bool getLkg, bool checkPermission)
        {
            object resultRuntimeObject;

            // Note that GetSectionRecursive may invalidate this record,
            // so there should be no further references to 'this' after the call.
            GetSectionRecursive(
                configKey,
                getLkg,
                checkPermission,
                getRuntimeObject: true,
                requestIsHere: true,
                result: out _,
                resultRuntimeObject: out resultRuntimeObject);

            return resultRuntimeObject;
        }

        private void GetSectionRecursive(
            string configKey, bool getLkg, bool checkPermission, bool getRuntimeObject, bool requestIsHere,
            out object result, out object resultRuntimeObject)
        {
            result = null;
            resultRuntimeObject = null;

#if DEBUG
            Debug.Assert(requestIsHere || !checkPermission, "requestIsHere || !checkPermission");
            if (getLkg)
            {
                Debug.Assert(getRuntimeObject, "getRuntimeObject == true");
                Debug.Assert(requestIsHere, "requestIsHere == true");
            }
#endif

            object tmpResult = null;
            object tmpResultRuntimeObject = null;

            // Throw errors from initial parse, if any.
            if (!getLkg) ThrowIfInitErrors();

            // check for a cached result
            bool hasResult = false;
            SectionRecord sectionRecord = GetSectionRecord(configKey, getLkg);
            if ((sectionRecord != null) && sectionRecord.HasResult)
            {
                // Results should never be stored if the section has errors.
                Debug.Assert(!sectionRecord.HasErrors, "!sectionRecord.HasErrors");

                // Create the runtime object if requested and does not yet exist.
                if (getRuntimeObject && !sectionRecord.HasResultRuntimeObject)
                {
                    try
                    {
                        sectionRecord.ResultRuntimeObject = GetRuntimeObject(sectionRecord.Result);
                    }
                    catch
                    {
                        // Ignore the error if we are attempting to retrieve
                        // the last known good configuration.
                        if (!getLkg) throw;
                    }
                }

                // Get the cached result.
                if (!getRuntimeObject || sectionRecord.HasResultRuntimeObject)
                {
                    tmpResult = sectionRecord.Result;
                    if (getRuntimeObject) tmpResultRuntimeObject = sectionRecord.ResultRuntimeObject;

                    hasResult = true;
                }
            }

            // If there is no cached result, get the parent's section,
            // then merge it with our own input if we have any.
            if (!hasResult)
            {
                bool hasInput = (sectionRecord != null) && sectionRecord.HasInput;

                // We want to cache results in a section record if:
                // - The request is made at this level, and so is likely to be
                //   made here again.
                // OR
                // - The section has input, in which case we want to
                //   avoid evaluating the same input multiple times.
                bool cacheResults = requestIsHere || hasInput;

                try
                {
                    // We need to get a factory record to:
                    // - Check whether the caller has permission to access a section.
                    // - Determine if this is the root declaration of a config section,
                    //   and thus the termination point for recursion.
                    // - Get a factory that can create a configuration section.
                    //
                    // Since most factories will be declared in machine.config and not in
                    // child config files, we do not optimize for checking whether a
                    // factory record is the root declaration, as the calculation at
                    // machine.config is trivial.
                    //
                    // It WILL be common in web scenarios for there to be
                    // deep hierarchies of config files, most of which have
                    // sparse input. Therefore we do not want to retrieve a
                    // factory record if it is not necessary to do so, as
                    // it would always lead to an order N-squared operation,
                    // where N is the depth of the config hierarchy.
                    //
                    // We can skip the reteival of a factory record if:
                    // - This is the recursive call to GetSectionRecursive,
                    // AND
                    // - There is no section input at this level,
                    // AND
                    // - No factory is declared at this level.
                    //
                    // In this case, we'll simply continue the recursion to our parent.

                    FactoryRecord factoryRecord;
                    bool isRootDeclaration;

                    if (requestIsHere)
                    {
                        // Ensure that we have a valid factory record and a valid factory
                        // for creating sections when a request for a section is first
                        // made.
                        factoryRecord = FindAndEnsureFactoryRecord(configKey, out isRootDeclaration);

                        // If initialization is delayed, complete initialization if:
                        //  - We can't find the requested factory, and it therefore
                        //    may be in the file we haven't yet read,
                        //  OR
                        //  - The definition of that factory is allowed at
                        //    levels of the config hierarchy that have not
                        //    been initialized.
                        //
                        // This works for client config scenarios because the default
                        // for AllowExeDefinition is MachineToApplication. It would not
                        // be useful for Web scenarios, as most sections can be requested
                        // Everywhere.
                        //
                        // Note that configuration errors that may be present in the
                        // file where initialization is delayed will be ignored, and
                        // thus the order in which configuration sections are requested
                        // will affect results. This is considered OK as it is very
                        // expensive to determine configuration paths to
                        // client user configuration files, which aren't needed by
                        // most applications.
                        if (IsInitDelayed
                            && ((factoryRecord == null)
                            ||
                            _initDelayedRoot.IsDefinitionAllowed(factoryRecord.AllowDefinition,
                                factoryRecord.AllowExeDefinition)))
                        {
                            // We are going to remove this record, so get any data we need
                            // before the reference to 'this' becomes invalid.
                            string configPath = _configPath;
                            InternalConfigRoot configRoot = _configRoot;

                            // Tell the host to no longer permit delayed initialization.
                            Host.RequireCompleteInit(_initDelayedRoot);

                            // Removed config at the root of where initialization is delayed.
                            _initDelayedRoot.Remove();

                            // Get the config record for this config path
                            BaseConfigurationRecord newRecord =
                                (BaseConfigurationRecord)configRoot.GetConfigRecord(configPath);

                            // Repeat the call to GetSectionRecursive
                            newRecord.GetSectionRecursive(
                                configKey, getLkg, checkPermission,
                                getRuntimeObject, requestIsHere,
                                out result, out resultRuntimeObject);

                            // Return and make no more references to this record.
                            return;
                        }

                        // For compatibility with previous versions,
                        // return null if the section is not found
                        // or is a group.
                        if ((factoryRecord == null) || factoryRecord.IsGroup) return;

                        // Use the factory record's copy of the configKey,
                        // so that we don't store more than one instance
                        // of the same configKey.
                        configKey = factoryRecord.ConfigKey;
                    }
                    else
                    {
                        if (hasInput)
                        {
                            // We'll need a factory to evaluate the input.
                            factoryRecord = FindAndEnsureFactoryRecord(configKey, out isRootDeclaration);
                            Debug.Assert(factoryRecord != null, "factoryRecord != null");
                        }
                        else
                        {
                            // We don't need a factory record unless this is the root declaration.
                            // We know it is not the root declaration if there is no factory
                            // declared here. This is important to avoid a walk up the config
                            // hierachy when there is no input in this record.
                            factoryRecord = GetFactoryRecord(configKey, false);
                            if (factoryRecord == null) isRootDeclaration = false;
                            else
                            {
                                factoryRecord = FindAndEnsureFactoryRecord(configKey, out isRootDeclaration);
                                Debug.Assert(factoryRecord != null, "factoryRecord != null");
                            }
                        }
                    }

                    // We need a factory record to check permission.
                    Debug.Assert(!checkPermission || (factoryRecord != null),
                        "!checkPermission || factoryRecord != null");

                    // If this is the root declaration, then we always want to cache
                    // the result, in order to prevent the section default from being
                    // created multiple times.
                    if (isRootDeclaration) cacheResults = true;

                    // We'll need a section record to cache results,
                    // and maybe to use in creating the section default.
                    if ((sectionRecord == null) && cacheResults) sectionRecord = EnsureSectionRecord(configKey, true);

                    // Retrieve the parent's runtime object if the runtimeObject
                    // is requested, and we are not going to merge that input
                    // with input in this section.
                    bool getParentRuntimeObject = getRuntimeObject && !hasInput;

                    object parentResult;
                    object parentResultRuntimeObject;
                    if (isRootDeclaration)
                    {
                        // Create the default section.
                        //
                        // Use the existing section record to create it if there is no input,
                        // so that the cached result is attached to the correct record.
                        SectionRecord sectionRecordForDefault = hasInput ? null : sectionRecord;
                        CreateSectionDefault(configKey, getParentRuntimeObject, factoryRecord, sectionRecordForDefault,
                            out parentResult, out parentResultRuntimeObject);
                    }
                    else
                    {
                        // Get the parent section.
                        _parent.GetSectionRecursive(
                            configKey: configKey,
                            getLkg: false,
                            checkPermission: false,
                            getRuntimeObject: getParentRuntimeObject,
                            requestIsHere: false,
                            result: out parentResult,
                            resultRuntimeObject: out parentResultRuntimeObject);
                    }

                    if (hasInput)
                    {
                        // Evaluate the input.
                        //
                        // If Evaluate() encounters an error, it may not throw an exception
                        // when getLkg == true.
                        //
                        // The complete success of the evaluation is determined by the return value.
                        bool success = Evaluate(factoryRecord, sectionRecord, parentResult, getLkg, getRuntimeObject,
                            out tmpResult, out tmpResultRuntimeObject);

                        Debug.Assert(success || getLkg, "success || getLkg");

                        if (!success)
                        {
                            Debug.Assert(getLkg, "getLkg == true");
                            // Do not cache partial results if getLkg was specified.
                            cacheResults = false;
                        }
                    }
                    else
                    {
                        // If we are going to cache results here, we will need
                        // to create a copy in the case of MgmtConfigurationRecord -
                        // otherwise we could inadvertently return the parent to the user,
                        // which could then be modified.
                        if (sectionRecord != null)
                        {
                            tmpResult = UseParentResult(configKey, parentResult, sectionRecord);
                            if (getRuntimeObject)
                            {
                                // If the parent result is the same as the parent runtime object,
                                // then use the same copy of the parent result for our own runtime object.
                                if (ReferenceEquals(parentResult, parentResultRuntimeObject))
                                    tmpResultRuntimeObject = tmpResult;
                                else
                                {
                                    tmpResultRuntimeObject = UseParentResult(configKey, parentResultRuntimeObject,
                                        sectionRecord);
                                }
                            }
                        }
                        else
                        {
                            Debug.Assert(!requestIsHere, "!requestIsHere");

                            // We don't need to make a copy if we are not storing
                            // the result, and thus not returning the result to the
                            // caller of GetSection.
                            tmpResult = parentResult;
                            tmpResultRuntimeObject = parentResultRuntimeObject;
                        }
                    }

                    // Determine which permissions are required of the caller.
                    if (cacheResults || checkPermission)
                    {
                        bool requirePermission = factoryRecord.RequirePermission;

                        // Cache the results.
                        if (cacheResults)
                        {
                            if (sectionRecord == null) sectionRecord = EnsureSectionRecord(configKey, true);

                            sectionRecord.Result = tmpResult;
                            if (getRuntimeObject) sectionRecord.ResultRuntimeObject = tmpResultRuntimeObject;

                            sectionRecord.RequirePermission = requirePermission;
                        }
                    }

                    hasResult = true;
                }
                catch
                {
                    // Ignore the error if we are attempting to retrieve
                    // the last known good configuration.
                    if (!getLkg) throw;
                }

                // If we don't have a result, ask our parent for its
                // last known good result.
                if (!hasResult)
                {
                    Debug.Assert(getLkg, "getLkg == true");

                    _parent.GetSectionRecursive(
                        configKey,
                        getLkg: true,
                        checkPermission: checkPermission,
                        getRuntimeObject: true,
                        requestIsHere: true,
                        result: out result,
                        resultRuntimeObject: out resultRuntimeObject);

                    return;
                }
            }

            // Return the results.
            result = tmpResult;
            if (getRuntimeObject) resultRuntimeObject = tmpResultRuntimeObject;
        }

        protected void CreateSectionDefault(
            string configKey, bool getRuntimeObject, FactoryRecord factoryRecord, SectionRecord sectionRecord,
            out object result, out object resultRuntimeObject)
        {
            SectionRecord sectionRecordForDefault = sectionRecord ?? new SectionRecord(configKey);

            object tmpResult = CallCreateSection(true, factoryRecord, sectionRecordForDefault, null, null, null, -1);
            object tmpResultRuntimeObject = getRuntimeObject ? GetRuntimeObject(tmpResult) : null;

            result = tmpResult;
            resultRuntimeObject = tmpResultRuntimeObject;
        }

        // Check if an input should be skipped based on inheritInChildApplications setting.
        //
        // If skipInChildApps == true (it means inheritInChildApplications=="false" in the location tag):
        //
        // - If _flags[IsAboveApplication]==true, that means the app pointed to by _configPath is above of the
        //   current running app.  In another word, the running app is a child app of the app pointed to by _configPath.
        //   In this case, we should skip the input.
        //
        // - If _flags[IsAboveApplication]==false, that means the app pointed to by _configPath == current running app.
        //   In this case it's okay to use the input.
        private bool ShouldSkipDueToInheritInChildApplications(bool skipInChildApps)
        {
            return skipInChildApps && _flags[IsAboveApplication];
        }

        // configPath - The config path of the config record to which a location tag points to.
        private bool ShouldSkipDueToInheritInChildApplications(bool skipInChildApps, string configPath)
        {
            return skipInChildApps && Host.IsAboveApplication(configPath);
        }

        // Evaluate the input.
        //
        // If Evaluate() encounters an error, it may not throw an exception
        // when getLkg == true.
        //
        // The complete success of the evaluation is determined by the return value.
        private bool Evaluate(
            FactoryRecord factoryRecord, SectionRecord sectionRecord, object parentResult,
            bool getLkg, bool getRuntimeObject, out object result, out object resultRuntimeObject)
        {
            result = null;
            resultRuntimeObject = null;

            // Store results in temporary variables, because we don't want to return
            // results if an exception is thrown.
            object tmpResult = null;
            object tmpResultRuntimeObject = null;

            // Factory record should be error-free.
            Debug.Assert(!factoryRecord.HasErrors, "!factoryRecord.HasErrors");

            // We should have some input
            Debug.Assert(sectionRecord.HasInput, "sectionRecord.HasInput");

            // Grab inputs before checking result,
            // as inputs may be cleared once the result is set.
            List<SectionInput> locationInputs = sectionRecord.LocationInputs;
            List<SectionInput> indirectLocationInputs = sectionRecord.IndirectLocationInputs;
            SectionInput fileInput = sectionRecord.FileInput;

            Debug.Assert(!(IsLocationConfig && getLkg),
                "Don't expect getLkg to be true when we're dealing with a location config record");

            // Now that we have our inputs, lets check if there
            // is a result, because if there is, our inputs might
            // have been invalidated.
            bool success = false;
            if (sectionRecord.HasResult)
            {
                // Results should never be stored if the section has errors.
                Debug.Assert(!sectionRecord.HasErrors, "!sectionRecord.HasErrors");

                // Create the runtime object if requested and it does not yet exist.
                if (getRuntimeObject && !sectionRecord.HasResultRuntimeObject)
                {
                    try
                    {
                        sectionRecord.ResultRuntimeObject = GetRuntimeObject(sectionRecord.Result);
                    }
                    catch
                    {
                        // Ignore the error if we are attempting to retrieve
                        // the last known good configuration.
                        if (!getLkg) throw;
                    }
                }

                // Get the cached result.
                if (!getRuntimeObject || sectionRecord.HasResultRuntimeObject)
                {
                    tmpResult = sectionRecord.Result;
                    if (getRuntimeObject) tmpResultRuntimeObject = sectionRecord.ResultRuntimeObject;

                    success = true;
                }
            }

            if (!success)
            {
                Exception savedException = null;
                try
                {
                    string configKey = factoryRecord.ConfigKey;
                    string[] keys = configKey.Split(s_configPathSeparatorParams);
                    object currentResult = parentResult;

                    // Evaluate indirectLocationInputs.  Used only in location config record.
                    // See the comment for VSWhidbey 540184 in Init() for more details.
                    if (indirectLocationInputs != null)
                    {
                        Debug.Assert(IsLocationConfig,
                            "indirectLocationInputs is non-null only in location config record");
                        foreach (SectionInput input in indirectLocationInputs)
                        {
                            if (!input.HasResult)
                            {
                                input.ThrowOnErrors();
                                input.Result = EvaluateOne(keys, input, true, factoryRecord, sectionRecord,
                                    currentResult);
                            }

                            currentResult = input.Result;
                        }
                    }

                    // Evaluate location inputs
                    if (locationInputs != null)
                    {
                        foreach (SectionInput locationInput in locationInputs)
                        {
                            if (!locationInput.HasResult)
                            {
                                locationInput.ThrowOnErrors();
                                locationInput.Result = EvaluateOne(keys, locationInput, true, factoryRecord,
                                    sectionRecord, currentResult);
                            }

                            currentResult = locationInput.Result;
                        }
                    }

                    // Evaluate file input
                    if (fileInput != null)
                    {
                        if (!fileInput.HasResult)
                        {
                            fileInput.ThrowOnErrors();
                            bool isTrusted = _flags[IsTrusted];
                            fileInput.Result = EvaluateOne(keys, fileInput, isTrusted, factoryRecord, sectionRecord,
                                currentResult);
                        }

                        currentResult = fileInput.Result;
                    }
                    else
                    {
                        // The section needs its own copy of the result that is distinct
                        // from its location parent result.
                        //
                        // Please note that this is needed only in design-time because any
                        // change to the section shouldn't go to the locationInput.Result.
                        //
                        // In runtime, UseParentResult does nothing.
                        Debug.Assert((locationInputs != null) || (indirectLocationInputs != null),
                            "locationInputs != null || indirectLocationInputs != null");
                        currentResult = UseParentResult(configKey, currentResult, sectionRecord);
                    }

                    if (getRuntimeObject) tmpResultRuntimeObject = GetRuntimeObject(currentResult);

                    tmpResult = currentResult;
                    success = true;
                }
                catch (Exception e)
                {
                    // Catch the exception if LKG is requested and we have
                    // location input to fall back on.
                    if (getLkg && (locationInputs != null)) savedException = e;
                    else throw;
                }

                // If getLkg, then return a result from the last valid location input.
                if (!success)
                {
                    Debug.Assert(getLkg, "getLkg == true");

                    int i = locationInputs.Count;
                    while (--i >= 0)
                    {
                        SectionInput locationInput = locationInputs[i];
                        if (!locationInput.HasResult) continue;
                        if (getRuntimeObject && !locationInput.HasResultRuntimeObject)
                        {
                            try
                            {
                                locationInput.ResultRuntimeObject = GetRuntimeObject(locationInput.Result);
                            }
                            catch { }
                        }

                        if (getRuntimeObject && !locationInput.HasResultRuntimeObject) continue;
                        tmpResult = locationInput.Result;
                        if (getRuntimeObject) tmpResultRuntimeObject = locationInput.ResultRuntimeObject;

                        break;
                    }

                    if (i < 0) throw savedException;
                }
            }

            // If evaluation was successful, we can remove any saved rawXml.
            if (success && !_flags[SupportsKeepInputs]) sectionRecord.ClearRawXml();

            result = tmpResult;
            if (getRuntimeObject) resultRuntimeObject = tmpResultRuntimeObject;

            return success;
        }

        private object EvaluateOne(
            string[] keys, SectionInput input, bool isTrusted,
            FactoryRecord factoryRecord, SectionRecord sectionRecord, object parentResult)
        {
            object result;
            try
            {
                ConfigXmlReader reader = GetSectionXmlReader(keys, input);
                if (reader == null)
                {
                    // If section is not found in a file, use the parent result
                    result = UseParentResult(factoryRecord.ConfigKey, parentResult, sectionRecord);
                }
                else
                {
                    result = CallCreateSection(
                        isTrusted, factoryRecord, sectionRecord, parentResult,
                        reader, input.SectionXmlInfo.Filename, input.SectionXmlInfo.LineNumber);
                }
            }
            catch (Exception e)
            {
                throw ExceptionUtil.WrapAsConfigException(
                    SR.Format(SR.Config_exception_creating_section, factoryRecord.ConfigKey),
                    e, input.SectionXmlInfo);
            }

            return result;
        }

        private ConfigXmlReader FindSection(string[] keys, SectionXmlInfo sectionXmlInfo, out int lineNumber)
        {
            lineNumber = 0;
            ConfigXmlReader section = null;

            using (Stream stream = Host.OpenStreamForRead(sectionXmlInfo.Filename))
            {
                if (!_flags[SupportsRefresh]
                    &&
                    ((stream == null) || HasStreamChanged(sectionXmlInfo.Filename, sectionXmlInfo.StreamVersion)))
                {
                    throw new ConfigurationErrorsException(SR.Config_file_has_changed,
                        sectionXmlInfo.Filename, 0);
                }

                if (stream == null) return null;

                // CONSIDER: In refresh case, need to recheck validity of file - can't make assumptions
                using (XmlUtil xmlUtil = new XmlUtil(stream, sectionXmlInfo.Filename, true))
                {
                    if (sectionXmlInfo.SubPath == null)
                        section = FindSectionRecursive(keys, 0, xmlUtil, ref lineNumber);
                    else
                    {
                        // search children of <configuration> for <location>
                        xmlUtil.ReadToNextElement();
                        while (xmlUtil.Reader.Depth > 0)
                        {
                            if (xmlUtil.Reader.Name == LocationTag)
                            {
                                bool locationValid = false;
                                string locationSubPathAttribute =
                                    xmlUtil.Reader.GetAttribute(LocationPathAttribute);

                                try
                                {
                                    locationSubPathAttribute =
                                        NormalizeLocationSubPath(locationSubPathAttribute, xmlUtil);
                                    locationValid = true;
                                }
                                catch (ConfigurationException ce)
                                {
                                    xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.NonSpecific);
                                }

                                if (locationValid &&
                                    StringUtil.EqualsIgnoreCase(sectionXmlInfo.SubPath,
                                        locationSubPathAttribute))
                                {
                                    section = FindSectionRecursive(keys, 0, xmlUtil, ref lineNumber);
                                    if (section != null)
                                        break;
                                }
                            }

                            xmlUtil.SkipToNextElement();
                        }
                    }

                    // Throw accumulated errors
                    ThrowIfParseErrors(xmlUtil.SchemaErrors);
                }
            }

            return section;
        }

        private ConfigXmlReader FindSectionRecursive(string[] keys, int iKey, XmlUtil xmlUtil, ref int lineNumber)
        {
            string name = keys[iKey];
            ConfigXmlReader section = null;

            int depth = xmlUtil.Reader.Depth;
            xmlUtil.ReadToNextElement();

            while (xmlUtil.Reader.Depth > depth)
            {
                if (xmlUtil.Reader.Name == name)
                {
                    if (iKey < keys.Length - 1)
                    {
                        // We haven't reached the section yet, so keep evaluating
                        section = FindSectionRecursive(keys, iKey + 1, xmlUtil, ref lineNumber);
                        if (section != null) break;

                        continue; // don't call "Skip" -- FindSectionRecursive forwards the reader
                    }

                    // We've reached the section. Load the section into a string.
                    string filename = xmlUtil.Filename;
                    int lineOffset = xmlUtil.Reader.LineNumber;
                    string rawXml = xmlUtil.CopySection();
                    section = new ConfigXmlReader(rawXml, filename, lineOffset);
                    break;
                }

                if ((iKey == 0) && (xmlUtil.Reader.Name == LocationTag))
                {
                    string locationSubPath = xmlUtil.Reader.GetAttribute(LocationPathAttribute);
                    bool isValid = false;
                    try
                    {
                        locationSubPath = NormalizeLocationSubPath(locationSubPath, xmlUtil);
                        isValid = true;
                    }
                    catch (ConfigurationException ce)
                    {
                        xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.NonSpecific);
                    }

                    if (isValid && (locationSubPath == null))
                    {
                        // Location sections that don't have a subpath are treated
                        // as ordinary sections.
                        section = FindSectionRecursive(keys, iKey, xmlUtil, ref lineNumber);
                        if (section != null) break;

                        continue; // don't call "Skip" -- FindSectionRecursive forwards the reader
                    }
                }

                xmlUtil.SkipToNextElement();
            }

            return section;
        }

        private ConfigXmlReader LoadConfigSource(string name, SectionXmlInfo sectionXmlInfo)
        {
            string configSourceStreamName = sectionXmlInfo.ConfigSourceStreamName;

            using (Stream stream = Host.OpenStreamForRead(configSourceStreamName))
            {
                if (stream == null)
                {
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_cannot_open_config_source, sectionXmlInfo.ConfigSource),
                        sectionXmlInfo);
                }

                using (XmlUtil xmlUtil = new XmlUtil(stream, configSourceStreamName, true))
                {
                    if (xmlUtil.Reader.Name != name)
                        throw new ConfigurationErrorsException(SR.Config_source_file_format, xmlUtil);

                    // Check for protectionProvider
                    string protectionProviderAttribute = xmlUtil.Reader.GetAttribute(ProtectionProviderAttribute);
                    if (protectionProviderAttribute != null)
                    {
                        if (xmlUtil.Reader.AttributeCount != 1)
                        {
                            // Error: elements with protectionProvider should not have other attributes
                            throw new ConfigurationErrorsException(SR.Protection_provider_syntax_error, xmlUtil);
                        }

                        sectionXmlInfo.ProtectionProviderName =
                            ValidateProtectionProviderAttribute(protectionProviderAttribute, xmlUtil);
                    }

                    int lineOffset = xmlUtil.Reader.LineNumber;
                    string rawXml = xmlUtil.CopySection();

                    // Detect if there is any XML left over after the section
                    while (!xmlUtil.Reader.EOF)
                    {
                        XmlNodeType t = xmlUtil.Reader.NodeType;
                        if (t != XmlNodeType.Comment)
                            throw new ConfigurationErrorsException(SR.Config_source_file_format, xmlUtil);

                        xmlUtil.Reader.Read();
                    }

                    ConfigXmlReader section = new ConfigXmlReader(rawXml, configSourceStreamName, lineOffset);
                    return section;
                }
            }
        }

        protected ConfigXmlReader GetSectionXmlReader(string[] keys, SectionInput input)
        {
            ConfigXmlReader reader;
            string filename = input.SectionXmlInfo.Filename;
            int lineNumber = input.SectionXmlInfo.LineNumber;

            try
            {
                string name = keys[keys.Length - 1];
                string rawXml = input.SectionXmlInfo.RawXml;
                if (rawXml != null)
                {
                    // Use the stored raw xml to provide the content of the section.
                    reader = new ConfigXmlReader(rawXml, input.SectionXmlInfo.Filename, input.SectionXmlInfo.LineNumber);
                }
                else
                {
                    if (!string.IsNullOrEmpty(input.SectionXmlInfo.ConfigSource))
                    {
                        // Load the  config source to provide the content of the section.
                        filename = input.SectionXmlInfo.ConfigSourceStreamName;
                        lineNumber = 0;
                        reader = LoadConfigSource(name, input.SectionXmlInfo);
                    }
                    else
                    {
                        // Find the content of the section in the config file.
                        lineNumber = 0;
                        reader = FindSection(keys, input.SectionXmlInfo, out lineNumber);
                    }
                }

                if (reader != null)
                {
                    if (!input.IsProtectionProviderDetermined)
                    {
                        input.ProtectionProvider =
                            GetProtectionProviderFromName(input.SectionXmlInfo.ProtectionProviderName, false);
                    }

                    if (input.ProtectionProvider != null)
                        reader = DecryptConfigSection(reader, input.ProtectionProvider);
                }
            }
            catch (Exception e)
            {
                // Guarantee that exceptions contain the name of the stream and an approximate line number.
                throw ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e, filename,
                    lineNumber);
            }

            return reader;
        }

        internal ProtectedConfigurationProvider GetProtectionProviderFromName(string providerName, bool throwIfNotFound)
        {
            if (!string.IsNullOrEmpty(providerName)) return ProtectedConfig.GetProviderFromName(providerName);

            if (!throwIfNotFound) return null;

            throw new ConfigurationErrorsException(SR.Format(SR.ProtectedConfigurationProvider_not_found,
                providerName));
        }

        internal void InitProtectedConfigurationSection()
        {
            if (_flags[ProtectedDataInitialized]) return;

            _protectedConfig =
                GetSection(ReservedSectionProtectedConfiguration, false, false) as ProtectedConfigurationSection;

            Debug.Assert(_protectedConfig != null,
                "<configProtectedData> section should always be available because it's a built-in section");

            _flags[ProtectedDataInitialized] = true;
        }

        protected object CallCreateSection(bool inputIsTrusted, FactoryRecord factoryRecord, SectionRecord sectionRecord,
            object parentConfig, ConfigXmlReader reader, string filename, int line)
        {
            object config;

            // Call into config section while impersonating process or UNC identity
            // so that the section could read files from disk if needed
            try
            {
                config = CreateSection(inputIsTrusted, factoryRecord, sectionRecord, parentConfig, reader);
                if ((config == null) && (parentConfig != null))
                    throw new ConfigurationErrorsException(SR.Config_object_is_null, filename, line);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw ExceptionUtil.WrapAsConfigException(
                    SR.Format(SR.Config_exception_creating_section_handler, factoryRecord.ConfigKey), e, filename,
                    line);
            }

            return config;
        }

        // Is this the Root Record of where this configKey is Declared
        //
        // If parent is null, or there is not a factory record above
        // this one, then it is the root of where it can be declared
        //
        // Optionally consider whether implicit sections are to be considered rooted.
        internal bool IsRootDeclaration(string configKey, bool implicitIsRooted)
        {
            if (!implicitIsRooted && IsImplicitSection(configKey)) return false;

            return _parent.IsRootConfig || (_parent.FindFactoryRecord(configKey, true) == null);
        }

        // Search the config hierarchy for a FactoryRecord.
        // Note that callers should check whether the returned factory has errors.
        internal FactoryRecord FindFactoryRecord(string configKey, bool permitErrors,
            out BaseConfigurationRecord configRecord)
        {
            configRecord = null;
            BaseConfigurationRecord tConfigRecord = this;

            while (!tConfigRecord.IsRootConfig)
            {
                FactoryRecord factoryRecord = tConfigRecord.GetFactoryRecord(configKey, permitErrors);
                if (factoryRecord != null)
                {
#if DEBUG
                    if (IsImplicitSection(configKey) && !factoryRecord.HasErrors)
                    {
                        Debug.Assert(tConfigRecord._parent.IsRootConfig,
                            "Implicit section should be found only at the record beneath the root (e.g. machine.config)");
                    }
#endif

                    configRecord = tConfigRecord;
                    return factoryRecord;
                }

                tConfigRecord = tConfigRecord._parent;
            }

            return null;
        }


        internal FactoryRecord FindFactoryRecord(string configKey, bool permitErrors)
        {
            return FindFactoryRecord(configKey, permitErrors, out _);
        }

        // - Find the nearest factory record
        // - Determine if it is the root
        // - Create the factory in the root if it doesn't exist.
        private FactoryRecord FindAndEnsureFactoryRecord(string configKey, out bool isRootDeclaredHere)
        {
            isRootDeclaredHere = false;

            BaseConfigurationRecord configRecord;
            FactoryRecord factoryRecord = FindFactoryRecord(configKey, false, out configRecord);
            if ((factoryRecord == null) || factoryRecord.IsGroup) return factoryRecord;

            // Find the root declaration
            FactoryRecord rootFactoryRecord = factoryRecord;
            BaseConfigurationRecord rootConfigRecord = configRecord;

            BaseConfigurationRecord currentConfigRecord = configRecord._parent;
            while (!currentConfigRecord.IsRootConfig)
            {
                BaseConfigurationRecord tempConfigRecord;
                FactoryRecord tempFactoryRecord = currentConfigRecord.FindFactoryRecord(configKey, false,
                    out tempConfigRecord);
                if (tempFactoryRecord == null)
                    break;

                rootFactoryRecord = tempFactoryRecord;
                rootConfigRecord = tempConfigRecord;

                // continue the search from the parent of the configRecord we found
                currentConfigRecord = tempConfigRecord.Parent;
            }

            // A child factory record must be equivalent to its parent,
            // so if the child has no errors, the parent must also have no errors.
            Debug.Assert(!rootFactoryRecord.HasErrors, "!rootFactoryRecord.HasErrors");
            if (rootFactoryRecord.Factory == null)
            {
                try
                {
                    // Create the factory from the type string, and cache it
                    object factory = rootConfigRecord.CreateSectionFactory(rootFactoryRecord);
                    rootFactoryRecord.Factory = factory;
                }
                catch (Exception e)
                {
                    throw ExceptionUtil.WrapAsConfigException(
                        SR.Format(SR.Config_exception_creating_section_handler, factoryRecord.ConfigKey), e,
                        factoryRecord);
                }
            }

            if (factoryRecord.Factory == null)
            {
                factoryRecord.Factory = rootFactoryRecord.Factory;
            }

            isRootDeclaredHere = ReferenceEquals(this, rootConfigRecord);

            return factoryRecord;
        }

        private Hashtable ScanFactories(XmlUtil xmlUtil)
        {
            Hashtable factoryList = new Hashtable();

            // Check for a root <configuration>
            if ((xmlUtil.Reader.NodeType != XmlNodeType.Element) || (xmlUtil.Reader.Name != ConfigurationTag))
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_file_doesnt_have_root_configuration, xmlUtil.Filename), xmlUtil);

            // Look at the configuration attributes
            while (xmlUtil.Reader.MoveToNextAttribute())
                switch (xmlUtil.Reader.Name)
                {
                    case XmlnsAttribute: // xmlns
                        if (xmlUtil.Reader.Value == ConfigurationNamespace)
                        {
                            // http://schemas.microsoft.com/.NetConfiguration/v2.0
                            _flags[NamespacePresentInFile] = true;
                            _flags[NamespacePresentCurrent] = true;
                        }
                        else
                        {
                            // A configuration namespace was defined that we don't understand
                            ConfigurationErrorsException ce = new ConfigurationErrorsException(
                                SR.Format(SR.Config_namespace_invalid, xmlUtil.Reader.Value, ConfigurationNamespace),
                                xmlUtil);
                            xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Global);
                        }
                        break;
                    default:
                        xmlUtil.AddErrorUnrecognizedAttribute(ExceptionAction.NonSpecific);
                        break;
                }

            // If the first child of <configuration> isn't <configSections>, bail
            xmlUtil.StrictReadToNextElement(ExceptionAction.NonSpecific);
            if ((xmlUtil.Reader.Depth != 1) || (xmlUtil.Reader.Name != ConfigSectionsTag))
                return factoryList;

            // Log an error for any attributes <configSections> might have
            xmlUtil.VerifyNoUnrecognizedAttributes(ExceptionAction.NonSpecific);

            // Scan in the <configSections> data
            ScanFactoriesRecursive(xmlUtil, parentConfigKey: string.Empty, factoryList: factoryList);

            return factoryList;
        }


        // Scans the <configSections> section of a configuration file.  The function is recursive
        // to traverse arbitrarily nested config groups.
        //
        //     <sectionGroup name="foo">
        //         <sectionGroup name="bar">
        //             <section name="fooBarSection" type="..." />
        //     ...
        //
        // Note: This function valiates that the factory record has not been
        //       declared before in a parent record. (it does not check
        //       current record, which allows you to update list)
        private void ScanFactoriesRecursive(XmlUtil xmlUtil, string parentConfigKey, Hashtable factoryList)
        {
            // discard any accumulated local errors
            xmlUtil.SchemaErrors.ResetLocalErrors();

            int depth = xmlUtil.Reader.Depth;
            xmlUtil.StrictReadToNextElement(ExceptionAction.NonSpecific);

            while (xmlUtil.Reader.Depth == depth + 1)
            {
                bool positionedAtNextElement = false;

                switch (xmlUtil.Reader.Name)
                {
                    case SectionGroupTag: // <sectionGroup>
                        {
                            string tagName = null;
                            string typeName = null;

                            int lineNumber = xmlUtil.Reader.LineNumber;
                            while (xmlUtil.Reader.MoveToNextAttribute())
                                switch (xmlUtil.Reader.Name)
                                {
                                    case SectionGroupNameAttribute:
                                        tagName = xmlUtil.Reader.Value;
                                        VerifySectionName(tagName, xmlUtil, ExceptionAction.Local, false);
                                        break;
                                    case SectionGroupTypeAttribute:
                                        xmlUtil.VerifyAndGetNonEmptyStringAttribute(ExceptionAction.Local, out typeName);
                                        break;
                                    default:
                                        xmlUtil.AddErrorUnrecognizedAttribute(ExceptionAction.Local);
                                        break;
                                }

                            // Move back to the element
                            xmlUtil.Reader.MoveToElement();

                            if (!xmlUtil.VerifyRequiredAttribute(tagName, SectionGroupNameAttribute, ExceptionAction.NonSpecific))
                            {
                                // Without a name="", we cannot continue parsing the sections and groups within.
                                // Skip the entire section.
                                xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(true);
                                xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                            }
                            else
                            {
                                string configKey = CombineConfigKey(parentConfigKey, tagName);

                                FactoryRecord factoryRecord = (FactoryRecord)factoryList[configKey];
                                if (factoryRecord != null)
                                {
                                    // Error: duplicate <sectionGroup> declaration
                                    xmlUtil.SchemaErrors.AddError(
                                        new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined_at_this_level, tagName), xmlUtil),
                                        ExceptionAction.Local);
                                }
                                else
                                {
                                    // Look for a factory of the same name in the parent
                                    FactoryRecord parentFactoryRecord = _parent.FindFactoryRecord(configKey, true);
                                    if (parentFactoryRecord != null)
                                    {
                                        configKey = parentFactoryRecord.ConfigKey;

                                        // make sure that an ancestor has not defined a <section> with the same name as the <sectionGroup>
                                        if ((parentFactoryRecord != null) &&
                                            (!parentFactoryRecord.IsGroup || !parentFactoryRecord.IsEquivalentSectionGroupFactory(Host, typeName)))
                                        {
                                            xmlUtil.SchemaErrors.AddError(
                                                new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, tagName), xmlUtil),
                                                ExceptionAction.Local);
                                            parentFactoryRecord = null;
                                        }
                                    }

                                    factoryRecord = parentFactoryRecord?.CloneSectionGroup(typeName, xmlUtil.Filename, lineNumber)
                                        ?? new FactoryRecord(configKey, parentConfigKey, tagName, typeName, xmlUtil.Filename, lineNumber);

                                    factoryList[configKey] = factoryRecord;
                                }

                                // Add any errors we may have encountered
                                factoryRecord.AddErrors(xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(true));

                                // continue recursive scan
                                ScanFactoriesRecursive(xmlUtil, configKey, factoryList);
                            }

                            continue;
                        }

                    case SectionTag: // <section>
                        {
                            string tagName = null;
                            string typeName = null;
                            ConfigurationAllowDefinition allowDefinition = ConfigurationAllowDefinition.Everywhere;
                            ConfigurationAllowExeDefinition allowExeDefinition = ConfigurationAllowExeDefinition.MachineToApplication;
                            OverrideModeSetting overrideModeDefault = OverrideModeSetting.s_sectionDefault;
                            bool allowLocation = true;
                            bool restartOnExternalChanges = true;
                            bool requirePermission = true;
                            bool gotType = false;

                            // parse section attributes
                            int lineNumber = xmlUtil.Reader.LineNumber;
                            while (xmlUtil.Reader.MoveToNextAttribute())
                                switch (xmlUtil.Reader.Name)
                                {
                                    case SectionNameAttribute:
                                        tagName = xmlUtil.Reader.Value;
                                        VerifySectionName(tagName, xmlUtil, ExceptionAction.Local, false);
                                        break;
                                    case SectionTypeAttribute:
                                        xmlUtil.VerifyAndGetNonEmptyStringAttribute(ExceptionAction.Local, out typeName);
                                        gotType = true;
                                        break;
                                    case SectionAllowLocationAttribute:
                                        xmlUtil.VerifyAndGetBooleanAttribute(ExceptionAction.Local, true, out allowLocation);
                                        break;
                                    case SectionAllowExeDefinitionAttribute:
                                        try
                                        {
                                            allowExeDefinition = AllowExeDefinitionToEnum(xmlUtil.Reader.Value, xmlUtil);
                                        }
                                        catch (ConfigurationException ce)
                                        {
                                            xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                                        }
                                        break;
                                    case SectionAllowDefinitionAttribute:
                                        try
                                        {
                                            allowDefinition = AllowDefinitionToEnum(xmlUtil.Reader.Value, xmlUtil);
                                        }
                                        catch (ConfigurationException ce)
                                        {
                                            xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                                        }
                                        break;
                                    case SectionRestartonExternalChangesAttribute:
                                        xmlUtil.VerifyAndGetBooleanAttribute(ExceptionAction.Local, true, out restartOnExternalChanges);
                                        break;
                                    case SectionRequirePermissionAttribute:
                                        xmlUtil.VerifyAndGetBooleanAttribute(ExceptionAction.Local, true, out requirePermission);
                                        break;
                                    case SectionOverrideModeDefaultAttribute:
                                        try
                                        {
                                            overrideModeDefault = OverrideModeSetting.CreateFromXmlReadValue(
                                                OverrideModeSetting.ParseOverrideModeXmlValue(xmlUtil.Reader.Value, xmlUtil));

                                            // Inherit means Allow when comming from the default value
                                            if (overrideModeDefault.OverrideMode == OverrideMode.Inherit)
                                                overrideModeDefault.ChangeModeInternal(OverrideMode.Allow);
                                        }
                                        catch (ConfigurationException ce)
                                        {
                                            xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                                        }
                                        break;
                                    default:
                                        xmlUtil.AddErrorUnrecognizedAttribute(ExceptionAction.Local);
                                        break;
                                }

                            // if on an attribute move back to the element
                            xmlUtil.Reader.MoveToElement();

                            if (!xmlUtil.VerifyRequiredAttribute(
                                tagName, SectionNameAttribute, ExceptionAction.NonSpecific))
                            {
                                // Without a name, we cannot continue to create a factoryRecord.
                                xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(true);
                            }
                            else
                            {
                                // Verify that the Type attribute was present.
                                // Note that 'typeName' will be null if the attribute was present
                                // but specified as an empty string.
                                if (!gotType)
                                    xmlUtil.AddErrorRequiredAttribute(SectionTypeAttribute, ExceptionAction.Local);

                                string configKey = CombineConfigKey(parentConfigKey, tagName);

                                FactoryRecord factoryRecord = (FactoryRecord)factoryList[configKey];
                                if (factoryRecord != null)
                                {
                                    // Error: duplicate section declaration
                                    xmlUtil.SchemaErrors.AddError(
                                        new ConfigurationErrorsException(
                                            SR.Format(SR.Config_tag_name_already_defined_at_this_level, tagName),
                                            xmlUtil),
                                        ExceptionAction.Local);
                                }
                                else
                                {
                                    FactoryRecord parentFactoryRecord = _parent.FindFactoryRecord(configKey, true);
                                    if (parentFactoryRecord != null)
                                    {
                                        // We have a parent factory record with the same name
                                        configKey = parentFactoryRecord.ConfigKey;

                                        // Look for collisions
                                        if (parentFactoryRecord.IsGroup)
                                        {
                                            // Already a <sectionGroup> with this name
                                            xmlUtil.SchemaErrors.AddError(
                                                new ConfigurationErrorsException(
                                                    SR.Format(SR.Config_tag_name_already_defined, tagName), xmlUtil),
                                                ExceptionAction.Local);
                                            parentFactoryRecord = null;
                                        }
                                        else if (!parentFactoryRecord.IsEquivalentSectionFactory(
                                            Host, typeName, allowLocation, allowDefinition, allowExeDefinition, restartOnExternalChanges, requirePermission))
                                        {
                                            // Already a <section> with the same name
                                            xmlUtil.SchemaErrors.AddError(
                                                new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, tagName), xmlUtil),
                                                ExceptionAction.Local);
                                            parentFactoryRecord = null;
                                        }
                                    }

                                    // Note - Clone will propagate the IsFromTrustedConfigRecord bit,
                                    // which is what we want - if this record is a duplicate of an ancestor,
                                    // the ancestor may be from a trusted config record.
                                    factoryRecord = parentFactoryRecord?.CloneSection(xmlUtil.Filename, lineNumber)
                                        ?? new FactoryRecord(
                                            configKey,
                                            parentConfigKey,
                                            tagName,
                                            typeName,
                                            allowLocation,
                                            allowDefinition,
                                            allowExeDefinition,
                                            overrideModeDefault,
                                            restartOnExternalChanges,
                                            requirePermission,
                                            _flags[IsTrusted],
                                            isUndeclared: false,
                                            filename: xmlUtil.Filename,
                                            lineNumber: lineNumber);

                                    factoryList[configKey] = factoryRecord;
                                }

                                // Add any errors we may have encountered
                                factoryRecord.AddErrors(xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(true));
                            }
                            break;
                        }
                    case RemoveTag: // <remove>
                        // Find the name attribute
                        string name = null;
                        while (xmlUtil.Reader.MoveToNextAttribute())
                        {
                            if (xmlUtil.Reader.Name != SectionNameAttribute)
                                xmlUtil.AddErrorUnrecognizedAttribute(ExceptionAction.NonSpecific);
                            else
                                name = xmlUtil.Reader.Value;
                        }
                        xmlUtil.Reader.MoveToElement();
                        if (xmlUtil.VerifyRequiredAttribute(
                            name, SectionNameAttribute, ExceptionAction.NonSpecific))
                            VerifySectionName(name, xmlUtil, ExceptionAction.NonSpecific, false);
                        break;
                    case ClearTag: // <clear>
                        xmlUtil.VerifyNoUnrecognizedAttributes(ExceptionAction.NonSpecific);
                        break;
                    default:
                        // Unknown element, skip over it
                        xmlUtil.AddErrorUnrecognizedElement(ExceptionAction.NonSpecific);
                        xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                        positionedAtNextElement = true;
                        break;
                }

                if (positionedAtNextElement) continue;

                xmlUtil.StrictReadToNextElement(ExceptionAction.NonSpecific);

                if (xmlUtil.Reader.Depth > depth + 1)
                {
                    // Unrecognized children are not allowed in <configSections>
                    xmlUtil.AddErrorUnrecognizedElement(ExceptionAction.NonSpecific);

                    // Try to backup to where we are suppose to be
                    while (xmlUtil.Reader.Depth > depth + 1) xmlUtil.ReadToNextElement();
                }
            }
        }

        /// <summary>
        /// Translate an ExeDefinition string from the Declaration in a file
        /// to the appropriate enumeration
        /// </summary>
        /// <param name="allowExeDefinition">string representation of value</param>
        /// <param name="xmlUtil">[optional] - can provide better error</param>
        internal static ConfigurationAllowExeDefinition AllowExeDefinitionToEnum(string allowExeDefinition, XmlUtil xmlUtil) =>
            allowExeDefinition switch
            {
                AllowDefinitionMachineOnly => ConfigurationAllowExeDefinition.MachineOnly,
                AllowDefinitionMachineToApplication => ConfigurationAllowExeDefinition.MachineToApplication,
                AllowExeDefinitionMachineToRoaming => ConfigurationAllowExeDefinition.MachineToRoamingUser,
                AllowExeDefinitionMachineToLocal => ConfigurationAllowExeDefinition.MachineToLocalUser,
                _ => throw new ConfigurationErrorsException(
                        SR.Config_section_allow_exe_definition_attribute_invalid,
                        xmlUtil),
            };

        internal static ConfigurationAllowDefinition AllowDefinitionToEnum(string allowDefinition, XmlUtil xmlUtil) =>
            xmlUtil.Reader.Value switch
            {
                AllowDefinitionEverywhere => ConfigurationAllowDefinition.Everywhere,
                AllowDefinitionMachineOnly => ConfigurationAllowDefinition.MachineOnly,
                AllowDefinitionMachineToApplication => ConfigurationAllowDefinition.MachineToApplication,
                AllowDefinitionMachineToWebRoot => ConfigurationAllowDefinition.MachineToWebRoot,
                _ => throw new ConfigurationErrorsException(
                        SR.Config_section_allow_definition_attribute_invalid,
                        xmlUtil),
            };

        internal static string CombineConfigKey(string parentConfigKey, string tagName)
        {
            if (string.IsNullOrEmpty(parentConfigKey)) return tagName;

            if (string.IsNullOrEmpty(tagName)) return parentConfigKey;

            return parentConfigKey + "/" + tagName;
        }

        internal static void SplitConfigKey(string configKey, out string group, out string name)
        {
            int lastSlash = configKey.LastIndexOf('/');
            if (lastSlash == -1)
            {
                group = string.Empty;
                name = configKey;
            }
            else
            {
                group = configKey.Substring(0, lastSlash);
                name = configKey.Substring(lastSlash + 1);
            }
        }

        [Conditional("DEBUG")]
        private void DebugValidateIndirectInputs(SectionRecord sectionRecord)
        {
            if (_parent.IsRootConfig) return;

            // Verify that for each indirect input, its target config path is a child path of _parent.
            // That's the definition of indirect input.
            for (int i = sectionRecord.IndirectLocationInputs.Count - 1; i >= 0; i--)
            {
                SectionInput input = sectionRecord.IndirectLocationInputs[i];

                // Get the override mode starting from the closest input.
                Debug.Assert(UrlPath.IsSubpath(_parent.ConfigPath, input.SectionXmlInfo.TargetConfigPath));
            }
        }

        // Return the lock mode for a section as comming from parent config levels
        private OverrideMode ResolveOverrideModeFromParent(string configKey, out OverrideMode childLockMode)
        {
            // When the current record is a location config level we are a direct child of the config level of the actual
            // config file inside which the location tag is. For example we have a file d:\inetpub\wwwroot\web.config which
            // containts <location path="Sub"> then "this" will be the config level inside the location tag and this.Parent
            // is the config level of d:\inetpub\wwwroot\web.config.

            // What we will do to come up with the result is:
            // 1) Try to find an existing section record somewhere above us.
            //    If we find an existing section record then it will have the effective value of the lock mode
            //    that applies to us in it's LockChidlren. We dont need to go further up once we find a section record
            //    as it has the lock mode of all it's parents accumulated
            //
            //    There is one huge trick though - Location config records are different ( see beginning of the func for what a location config record is )
            //    A location config record is not locked if the config level of the web.config file in which it lives is not locked.
            //    I.e. when we are looking for the effective value for a location config we have two cases
            //      a) There is a section record in our immediate parent ( remember our immediate parent is the config file in which we /as a location tag/ are defined )
            //         In this case our lock mode is not the LockChildren of this section record because this lock mode applies to child config levels in child config files
            //         The real lock mode for us is the Locked mode of the section record in self.
            //      b) There is no section record in our immediate parent - in this case the locking is the same as for normal config - LockChildren value of any section
            //         record we may find above us.
            //
            // 2) If we can't find an existing section record we have two cases again:
            //      a)  We are at the section declaration level - at this level a section is always unlocked by definition
            //          If this wasnt so there would be no way to unlock a section that is locked by default
            //          A Location config is a bit wierd again in a sence that a location config is unlocked if its in the config file where the section is declared
            //          I.e. if "this" is a location record then a section is unconditionally unlocked if "this.Parent" is the section declaration level
            //      b) We are not at section declaration level - in this case the result is whatever the default lock mode for the section is ( remember
            //         that we fall back to the default since we couldn't find a section record with explicit lock mode nowhere above us)
            //
            // I sure hope that made some sense!

            OverrideMode mode = OverrideMode.Inherit;
            BaseConfigurationRecord parent = Parent;
            BaseConfigurationRecord immediateParent = Parent;

            childLockMode = OverrideMode.Inherit;

            // Walk the hierarchy until we find an explicit setting for lock state at a config level or we reach to root
            while (!parent.IsRootConfig && (mode == OverrideMode.Inherit))
            {
                SectionRecord sectionRecord = parent.GetSectionRecord(configKey, true);

                if (sectionRecord != null)
                {
                    // Check for 1a
                    if (IsLocationConfig && ReferenceEquals(immediateParent, parent))
                    {
                        // Apply case 1a
                        mode = sectionRecord.Locked ? OverrideMode.Deny : OverrideMode.Allow;

                        // In this specific case the lock mode for our children is whatever the children of our parent should inherit
                        // For example imagine a web.config which has a <location path="." overrideMode="Deny"> and we open "locationSubPath" from this web.config
                        // The lock for the section is not Deny and will be allow ( see the code line above ). However the chidlren of this location tag
                        // inherit the lock that applies to the children of the web.config file itself
                        childLockMode = sectionRecord.LockChildren ? OverrideMode.Deny : OverrideMode.Allow;
                    }
                    else
                    {
                        mode = sectionRecord.LockChildren ? OverrideMode.Deny : OverrideMode.Allow;

                        // When the lock mode is comming from a parent level the
                        // lock mode that applies to children of "this" is the same as what applies to "this"
                        childLockMode = mode;
                    }
                }

                parent = parent._parent;
            }

            // Case 2
            if (mode == OverrideMode.Inherit)
            {
                Debug.Assert(FindFactoryRecord(configKey, true) != null);

                bool atDeclarationLevel;
                OverrideMode defaultMode = FindFactoryRecord(configKey, true).OverrideModeDefault.OverrideMode;

                if (IsLocationConfig) atDeclarationLevel = Parent.GetFactoryRecord(configKey, true) != null;
                else atDeclarationLevel = GetFactoryRecord(configKey, true) != null;

                if (!atDeclarationLevel)
                {
                    // Case 2b
                    //
                    // Lock mode for children and self is the same since the default value is comming
                    // from a parent level and hence - applies to both
                    childLockMode = mode = defaultMode;

                    Debug.Assert(mode != OverrideMode.Inherit); // Remember that the default is never Inherit
                }
                else
                {
                    // Case 2a
                    //
                    // Self is always allow at section declaration level
                    // Child lock mode is the default value ( remember we are here because no explici mode was set anywhere above us )

                    mode = OverrideMode.Allow;
                    childLockMode = defaultMode;
                }
            }

            // This function must return Allow or Deny
            Debug.Assert(mode != OverrideMode.Inherit);

            return mode;
        }

        protected OverrideMode GetSectionLockedMode(string configKey)
        {
            return GetSectionLockedMode(configKey, out _);
        }

        // Return the current lock mode for a section
        protected OverrideMode GetSectionLockedMode(string configKey, out OverrideMode childLockMode)
        {
            OverrideMode result;

            SectionRecord sectionRecord = GetSectionRecord(configKey, true);

            // If there is a section record it has the effective locking settings resolved
            // There is no need to do ResolveOverrideModeFromParent because it was done in:
            // 1) In EnsureSectionRecord when the section record was creteted
            // 2) Right after the SectionRecord was created without initialization of the lock settings
            //    in this:ScanSectionsRecursive().
            // As long as nobody uses EnsureSectionRecordUnsafe this method will be returning the correct
            // lock value only by looking at the section record

            if (sectionRecord != null)
            {
                result = sectionRecord.Locked ? OverrideMode.Deny : OverrideMode.Allow;
                childLockMode = sectionRecord.LockChildren ? OverrideMode.Deny : OverrideMode.Allow;
            }
            else result = ResolveOverrideModeFromParent(configKey, out childLockMode);

            return result;
        }

        private void ScanSections(XmlUtil xmlUtil)
        {
            ScanSectionsRecursive(xmlUtil, string.Empty, false, null, OverrideModeSetting.s_locationDefault, false);
        }

        private void ScanSectionsRecursive(
            XmlUtil xmlUtil,
            string parentConfigKey,
            bool inLocation,
            string locationSubPath,
            OverrideModeSetting overrideMode,
            bool skipInChildApps)
        {
            // discard any accumulated local errors
            xmlUtil.SchemaErrors.ResetLocalErrors();

            int depth;

            // only move to child nodes when not on first level (we've already passed the first <configsections>)
            if ((parentConfigKey.Length == 0) && !inLocation)
            {
                depth = 0;
            }
            else
            {
                depth = xmlUtil.Reader.Depth;
                xmlUtil.StrictReadToNextElement(ExceptionAction.NonSpecific);
            }

            while (xmlUtil.Reader.Depth == depth + 1)
            {
                string tagName = xmlUtil.Reader.Name;

                // Check for reserved elements before looking up the factory,
                // which may have the same name if it is in error.
                switch (tagName)
                {
                    case ConfigSectionsTag: // <configSections>
                        // Either a duplicate or not the first tag under <configuration>
                        xmlUtil.SchemaErrors.AddError(
                            new ConfigurationErrorsException(SR.Format(SR.Config_client_config_too_many_configsections_elements, tagName), xmlUtil),
                            ExceptionAction.NonSpecific);
                        xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                        continue;
                    case LocationTag: // <location>
                        if ((parentConfigKey.Length > 0) || inLocation)
                        {
                            // The section isn't at the top level
                            xmlUtil.SchemaErrors.AddError(
                                new ConfigurationErrorsException(SR.Config_location_location_not_allowed, xmlUtil),
                                ExceptionAction.Global);
                            xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                        }
                        else
                        {
                            // Recurse into the location section
                            ScanLocationSection(xmlUtil);
                        }
                        continue;
                }

                string configKey = CombineConfigKey(parentConfigKey, tagName);

                // Find the relevant factory record
                FactoryRecord factoryRecord = FindFactoryRecord(configKey, true);

                if (factoryRecord == null)
                {
                    // Unregistered configuration section, add DefaultSection factory for it.
                    //
                    // At runtime, it is a local error to have an unrecognized section.
                    // By treating it as local we avoid throwing an error if the
                    // section is encountered within a location section, just as we treat
                    // other section errors in a location tag.
                    //
                    // At designtime, we do not consider it an error, so that programs
                    // that worked on version N config files can continue to work with
                    // version N+1 config files that may introduce new sections.
                    if (!ClassFlags[ClassIgnoreLocalErrors])
                    {
                        xmlUtil.SchemaErrors.AddError(
                            new ConfigurationErrorsException(
                                SR.Format(SR.Config_unrecognized_configuration_section, configKey), xmlUtil),
                            ExceptionAction.Local);
                    }

                    VerifySectionName(tagName, xmlUtil, ExceptionAction.Local, false);

                    factoryRecord = new FactoryRecord(
                        configKey,
                        parentConfigKey,
                        tagName,
                        typeof(DefaultSection).AssemblyQualifiedName,
                        allowLocation: true,
                        allowDefinition: ConfigurationAllowDefinition.Everywhere,
                        allowExeDefinition: ConfigurationAllowExeDefinition.MachineToRoamingUser,
                        overrideModeDefault: OverrideModeSetting.s_sectionDefault,
                        restartOnExternalChanges: true,
                        requirePermission: true,
                        isFromTrustedConfigRecord: _flags[IsTrusted],
                        isUndeclared: true,
                        filename: null,
                        lineNumber: -1);

                    // Add any errors we may have encountered to the factory record,
                    // so that child config that also refer to this unrecognized section
                    // get the error.
                    factoryRecord.AddErrors(xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(true));

                    // Add the factory to the list of factories
                    EnsureFactories()[configKey] = factoryRecord;
                }

                if (factoryRecord.IsGroup)
                {
                    // A section group
                    if (factoryRecord.HasErrors) xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                    else
                    {
                        if (xmlUtil.Reader.AttributeCount > 0)
                        {
                            // We allow unrecognized attributes for backward compatibility
                            // However, we will still throw if the unrecognized attribute is reserved.
                            while (xmlUtil.Reader.MoveToNextAttribute())
                                if (IsReservedAttributeName(xmlUtil.Reader.Name))
                                    xmlUtil.AddErrorReservedAttribute(ExceptionAction.NonSpecific);

                            xmlUtil.Reader.MoveToElement(); // if on an attribute move back to the element
                        }

                        // Recurse into group definition
                        ScanSectionsRecursive(xmlUtil, configKey, inLocation, locationSubPath, overrideMode,
                            skipInChildApps);
                    }
                }
                else
                {
                    // A section
                    configKey = factoryRecord.ConfigKey;
                    string fileName = xmlUtil.Filename;
                    int lineNumber = xmlUtil.LineNumber;
                    string rawXml = null;
                    string configSource = null;
                    string configSourceStreamName = null;
                    object configSourceStreamVersion = null;
                    string protectionProviderName = null;
                    OverrideMode sectionLockMode = OverrideMode.Inherit;
                    OverrideMode sectionChildLockMode = OverrideMode.Inherit;
                    bool positionedAtNextElement = false;
                    bool isFileInput = locationSubPath == null;

                    if (!factoryRecord.HasErrors)
                    {
                        // We have a valid factoryRecord for a section
                        if (inLocation && (factoryRecord.AllowLocation == false))
                        {
                            xmlUtil.SchemaErrors.AddError(
                                new ConfigurationErrorsException(
                                    SR.Config_section_cannot_be_used_in_location, xmlUtil),
                                ExceptionAction.Local);
                        }

                        // Verify correctness for file inputs.
                        if (isFileInput)
                        {
                            // Verify that the section is unique
                            SectionRecord sectionRecord = GetSectionRecord(configKey, true);
                            if ((sectionRecord != null) && sectionRecord.HasFileInput)
                            {
                                if (!factoryRecord.IsIgnorable())
                                {
                                    xmlUtil.SchemaErrors.AddError(
                                        new ConfigurationErrorsException(
                                            SR.Config_sections_must_be_unique, xmlUtil),
                                        ExceptionAction.Local);
                                }
                            }

                            // Verify that the definition is allowed.
                            try
                            {
                                VerifyDefinitionAllowed(factoryRecord, _configPath, xmlUtil);
                            }
                            catch (ConfigurationException ce)
                            {
                                xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                            }
                        }

                        // Verify that section is unlocked, both for file and location inputs.
                        sectionLockMode = GetSectionLockedMode(configKey, out sectionChildLockMode);

                        if (sectionLockMode == OverrideMode.Deny)
                        {
                            xmlUtil.SchemaErrors.AddError(
                                new ConfigurationErrorsException(SR.Config_section_locked, xmlUtil),
                                ExceptionAction.Local);
                        }

                        // check for configSource or protectionProvider
                        if (xmlUtil.Reader.AttributeCount >= 1)
                        {
                            // First do all the attributes reading without advancing the reader.

                            string configSourceAttribute = xmlUtil.Reader.GetAttribute(ConfigSourceAttribute);
                            if (configSourceAttribute != null)
                            {
                                try
                                {
                                    configSource = NormalizeConfigSource(configSourceAttribute, xmlUtil);
                                }
                                catch (ConfigurationException ce)
                                {
                                    xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                                }

                                if (xmlUtil.Reader.AttributeCount != 1)
                                {
                                    // Error: elements with configSource should not have other attributes
                                    xmlUtil.SchemaErrors.AddError(
                                        new ConfigurationErrorsException(SR.Config_source_syntax_error, xmlUtil),
                                        ExceptionAction.Local);
                                }
                            }

                            string protectionProviderAttribute = xmlUtil.Reader.GetAttribute(ProtectionProviderAttribute);
                            if (protectionProviderAttribute != null)
                            {
                                try
                                {
                                    protectionProviderName =
                                        ValidateProtectionProviderAttribute(protectionProviderAttribute, xmlUtil);
                                }
                                catch (ConfigurationException ce)
                                {
                                    xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Local);
                                }

                                if (xmlUtil.Reader.AttributeCount != 1)
                                {
                                    // Error: elements with protectionProvider should not have other attributes
                                    xmlUtil.SchemaErrors.AddError(
                                        new ConfigurationErrorsException(
                                            SR.Protection_provider_syntax_error, xmlUtil),
                                        ExceptionAction.Local);
                                }
                            }

                            // The 2nd part of the configSource check requires advancing the reader.
                            // Please note that this part should be done only AFTER all other attributes
                            // checking are done.
                            if (configSourceAttribute != null)
                            {
                                if (!xmlUtil.Reader.IsEmptyElement)
                                {
                                    while (xmlUtil.Reader.Read())
                                    {
                                        XmlNodeType t = xmlUtil.Reader.NodeType;
                                        if (t == XmlNodeType.EndElement)
                                            break;

                                        if (t == XmlNodeType.Comment) continue;

                                        // Error: elements with configSource should not subelements other than comments
                                        xmlUtil.SchemaErrors.AddError(
                                            new ConfigurationErrorsException(SR.Config_source_syntax_error, xmlUtil),
                                            ExceptionAction.Local);

                                        if (t == XmlNodeType.Element)
                                            xmlUtil.StrictSkipToOurParentsEndElement(ExceptionAction.NonSpecific);
                                        else xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);

                                        positionedAtNextElement = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (configSource != null)
                        {
                            try
                            {
                                try
                                {
                                    configSourceStreamName =
                                        Host.GetStreamNameForConfigSource(ConfigStreamInfo.StreamName, configSource);
                                }
                                catch (Exception e)
                                {
                                    throw ExceptionUtil.WrapAsConfigException(SR.Config_source_invalid, e, xmlUtil);
                                }
                                ValidateUniqueConfigSource(configKey, configSourceStreamName, configSource, xmlUtil);
                                configSourceStreamVersion = MonitorStream(configKey, configSource,
                                    configSourceStreamName);
                            }
                            catch (ConfigurationException ex)
                            {
                                xmlUtil.SchemaErrors.AddError(ex, ExceptionAction.Local);
                            }
                        }

                        // prefetch the raw xml
                        if (!xmlUtil.SchemaErrors.HasLocalErrors)
                        {
                            if ((configSource == null) && ShouldPrefetchRawXml(factoryRecord))
                            {
                                Debug.Assert(!positionedAtNextElement, "!positionedAtNextElement");

                                rawXml = xmlUtil.CopySection();
                                if (xmlUtil.Reader.NodeType != XmlNodeType.Element)
                                {
                                    xmlUtil.VerifyIgnorableNodeType(ExceptionAction.NonSpecific);
                                    xmlUtil.StrictReadToNextElement(ExceptionAction.NonSpecific);
                                }

                                positionedAtNextElement = true;
                            }
                        }
                    }

                    // Get the list of errors before advancing the reader
                    List<ConfigurationException> localErrors =
                        xmlUtil.SchemaErrors.RetrieveAndResetLocalErrors(isFileInput);

                    // advance the reader to the next element
                    if (!positionedAtNextElement) xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);

                    // Add the input either to:
                    // 1. The file input at the current config level, or
                    // 2. LocationSections, where it will be used in sub paths
                    bool addInput = true;

                    if (isFileInput)
                    {
                        // If isFileInput==true, Input added will be used against this config level.
                        // Need to check if we need to skip it due to inheritInChildApplications.

                        if (ShouldSkipDueToInheritInChildApplications(skipInChildApps)) addInput = false;
                    }
                    else
                    {
                        if (!_flags[SupportsLocation])
                        {
                            // Skip if we have a location input but we don't support location tag.
                            addInput = false;
                        }
                    }

                    if (addInput)
                    {
                        string targetConfigPath = locationSubPath == null ? _configPath : null;

                        SectionXmlInfo sectionXmlInfo = new SectionXmlInfo(
                            configKey, _configPath, targetConfigPath, locationSubPath,
                            fileName, lineNumber, ConfigStreamInfo.StreamVersion, rawXml,
                            configSource, configSourceStreamName, configSourceStreamVersion,
                            protectionProviderName, overrideMode, skipInChildApps);

                        if (locationSubPath == null)
                        {
                            // Add this file input to the section record

                            // We've already checked for locked above, so use skip the second check
                            // and set the locked bit.
                            SectionRecord sectionRecord = EnsureSectionRecordUnsafe(configKey, true);

                            // Since we called EnsureSectionRecordUnsafe the section record does not have its lock mode resolved
                            // but we have it in sectionLockMode and childLockMode. Apply it now
                            sectionRecord.ChangeLockSettings(sectionLockMode, sectionChildLockMode);

                            // Note that we first apply the lock mode comming from parent levels ( the line above ) and then
                            // add the file input since the file input takes precedence over whats comming from parent
                            SectionInput fileInput = new SectionInput(sectionXmlInfo, localErrors);
                            sectionRecord.AddFileInput(fileInput);
                        }
                        else
                        {
                            // Add this location input to this list of location sections
                            LocationSectionRecord locationSectionRecord = new LocationSectionRecord(sectionXmlInfo,
                                localErrors);
                            EnsureLocationSections().Add(locationSectionRecord);
                        }
                    }
                }
            }
        }

        private void ScanLocationSection(XmlUtil xmlUtil)
        {
            string locationSubPath = null;
            bool inheritInChildApp = true;
            int errorCountBeforeScan = xmlUtil.SchemaErrors.GlobalErrorCount;
            OverrideModeSetting overrideMode = OverrideModeSetting.s_locationDefault;
            bool overrideModeInit = false;

            // Get the location section attributes
            while (xmlUtil.Reader.MoveToNextAttribute())
                switch (xmlUtil.Reader.Name)
                {
                    case LocationPathAttribute:
                        locationSubPath = xmlUtil.Reader.Value;
                        break;
                    case LocationAllowOverrideAttribute:
                        // Check that allowOverride and OverrideMode werent specified at the same time
                        if (overrideModeInit)
                        {
                            xmlUtil.SchemaErrors.AddError(
                                new ConfigurationErrorsException(SR.Invalid_override_mode_declaration, xmlUtil),
                                ExceptionAction.Global);
                        }
                        else
                        {
                            bool value;

                            // Read the value
                            xmlUtil.VerifyAndGetBooleanAttribute(
                                ExceptionAction.Global, true, out value);

                            overrideMode = OverrideModeSetting.CreateFromXmlReadValue(value);
                            overrideModeInit = true;
                        }
                        break;
                    case LocationOverrideModeAttribute:
                        if (overrideModeInit)
                        {
                            xmlUtil.SchemaErrors.AddError(
                                new ConfigurationErrorsException(SR.Invalid_override_mode_declaration,
                                    xmlUtil), ExceptionAction.Global);
                        }
                        else
                        {
                            overrideMode = OverrideModeSetting.CreateFromXmlReadValue(
                                OverrideModeSetting.ParseOverrideModeXmlValue(xmlUtil.Reader.Value, xmlUtil));
                            overrideModeInit = true;
                        }
                        break;
                    case LocationInheritInChildApplicationsAttribute:
                        xmlUtil.VerifyAndGetBooleanAttribute(
                            ExceptionAction.Global, true, out inheritInChildApp);
                        break;
                    default:
                        xmlUtil.AddErrorUnrecognizedAttribute(ExceptionAction.Global);
                        break;
                }

            xmlUtil.Reader.MoveToElement(); // if on an attribute move back to the element

            try
            {
                locationSubPath = NormalizeLocationSubPath(locationSubPath, xmlUtil);

                // We throw if we see one of these in machine.config or root web.config:
                //
                //  <location path="." inheritInChildApplications="false" >
                //  <location inheritInChildApplications="false" >
                //
                // To detect whetherewe're machine.config or root web.config, the current fix is to use
                // Host.IsDefinitionAllowed.  Instead of this we should invent a new method in
                // IInternalConfigHost to return whether a configPath can be part of an app or not.
                // But since it's Whidbey RC "Ask Mode" I chose not to do it due to bigger code churn.
                if ((locationSubPath == null) &&
                    !inheritInChildApp &&
                    Host.IsDefinitionAllowed(_configPath, ConfigurationAllowDefinition.MachineToWebRoot,
                        ConfigurationAllowExeDefinition.MachineOnly))
                {
                    throw new ConfigurationErrorsException(
                        SR.Location_invalid_inheritInChildApplications_in_machine_or_root_web_config,
                        xmlUtil);
                }
            }
            catch (ConfigurationErrorsException ce)
            {
                xmlUtil.SchemaErrors.AddError(ce, ExceptionAction.Global);
            }

            // Skip over this location section if there are errors
            if (xmlUtil.SchemaErrors.GlobalErrorCount > errorCountBeforeScan)
            {
                xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                return;
            }

            // Scan elements of the location section if the path is the current path.
            // We do not add <location path="." /> to the _locationSections list.
            if (locationSubPath == null)
            {
                ScanSectionsRecursive(xmlUtil, string.Empty, true, null, overrideMode, !inheritInChildApp);
                return;
            }

            // Skip over location sections for client config
            if (!_flags[SupportsLocation])
            {
                xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                return;
            }

            // Skip over location sections that don't apply to this (application) host for runtime records
            IInternalConfigHost host = Host;
            if (this is RuntimeConfigurationRecord && (host != null) && (locationSubPath.Length != 0) &&
                (locationSubPath[0] != '.'))
            {
                // The application's config path is global to the AppDomain
                if (s_appConfigPath == null)
                {
                    object ctx = ConfigContext;
                    if (ctx != null)
                    {
                        string appConfigPath = ctx.ToString();
                        Interlocked.CompareExchange(ref s_appConfigPath, appConfigPath, null);
                    }
                }

                // If targetConfigPath is not upstream or downstream of the application's config path,
                // skip this location section.
                //
                // Example #1: <location path="Site1"> has a targetConfigPath of "machine/webroot/1".  This applies
                // to Site1, whose application config path is "machine/webroot/1", but it does not apply
                // to Site2, whose application config path is "machine/webroot/2"
                //
                // Example #2: <location path="subdir"> has a targetConfigPath of "machine/webroot/1/root/subdir".
                // This applies to an application with an application config path of "machine/webroot/1/root/subdir/app".
                string targetConfigPath = host.GetConfigPathFromLocationSubPath(_configPath, locationSubPath);
                if (!StringUtil.StartsWithOrdinalIgnoreCase(s_appConfigPath, targetConfigPath)
                    && !StringUtil.StartsWithOrdinalIgnoreCase(targetConfigPath, s_appConfigPath))
                {
                    xmlUtil.StrictSkipToNextElement(ExceptionAction.NonSpecific);
                    return;
                }
            }

            AddLocation(locationSubPath);
            ScanSectionsRecursive(xmlUtil, string.Empty, true, locationSubPath, overrideMode, !inheritInChildApp);
        }

        // If you wish to keep track of the Location Fields, then use this
        protected virtual void AddLocation(string locationSubPath) { }

        // Resolve information about a location section at the time that the location section
        // is being used by child configuration records. This allows us to:
        //      * Delay determining the configuration path for the location record until the sites section is available.
        //      * Delay reporting bad location paths until the location record has to be used.
        private void ResolveLocationSections()
        {
            if (_flags[IsLocationListResolved]) return;

            // Resolve outside of any lock
            if (!_parent.IsRootConfig) _parent.ResolveLocationSections();

            lock (this)
            {
                if (!_flags[IsLocationListResolved])
                {
                    if (_locationSections != null)
                    {
                        // Create dictionary that maps configPaths to (dictionary that maps sectionNames to locationSectionRecords)
                        HybridDictionary locationConfigPaths = new HybridDictionary(true);
                        foreach (LocationSectionRecord locationSectionRecord in _locationSections)
                        {
                            // Resolve the target config path
                            string targetConfigPath = Host.GetConfigPathFromLocationSubPath(_configPath,
                                locationSectionRecord.SectionXmlInfo.SubPath);
                            locationSectionRecord.SectionXmlInfo.TargetConfigPath = targetConfigPath;

                            // Check uniqueness
                            HybridDictionary locationSectionRecordDictionary =
                                (HybridDictionary)locationConfigPaths[targetConfigPath];
                            if (locationSectionRecordDictionary == null)
                            {
                                locationSectionRecordDictionary = new HybridDictionary(false);
                                locationConfigPaths.Add(targetConfigPath, locationSectionRecordDictionary);
                            }

                            LocationSectionRecord duplicateRecord =
                                (LocationSectionRecord)
                                locationSectionRecordDictionary[locationSectionRecord.ConfigKey];
                            FactoryRecord factoryRecord = null;
                            if (duplicateRecord == null)
                            {
                                locationSectionRecordDictionary.Add(locationSectionRecord.ConfigKey,
                                    locationSectionRecord);
                            }
                            else
                            {
                                factoryRecord = FindFactoryRecord(locationSectionRecord.ConfigKey, true);
                                if ((factoryRecord == null) || !factoryRecord.IsIgnorable())
                                {
                                    if (!duplicateRecord.HasErrors)
                                    {
                                        duplicateRecord.AddError(
                                            new ConfigurationErrorsException(
                                                SR.Config_sections_must_be_unique,
                                                duplicateRecord.SectionXmlInfo));
                                    }

                                    locationSectionRecord.AddError(
                                        new ConfigurationErrorsException(
                                            SR.Config_sections_must_be_unique,
                                            locationSectionRecord.SectionXmlInfo));
                                }
                            }

                            // Check if the definition is allowed
                            if (factoryRecord == null)
                                factoryRecord = FindFactoryRecord(locationSectionRecord.ConfigKey, true);

                            if (factoryRecord.HasErrors) continue;

                            try
                            {
                                VerifyDefinitionAllowed(factoryRecord, targetConfigPath,
                                    locationSectionRecord.SectionXmlInfo);
                            }
                            catch (ConfigurationException e)
                            {
                                locationSectionRecord.AddError(e);
                            }
                        }

                        // Check location section for being locked.
                        BaseConfigurationRecord parent = _parent;
                        while (!parent.IsRootConfig)
                        {
                            foreach (LocationSectionRecord locationSectionRecord in _locationSections)
                            {
                                bool locked = false;

                                // It is an error if a parent section with the same configKey is locked.
                                SectionRecord sectionRecord =
                                    parent.GetSectionRecord(locationSectionRecord.ConfigKey, true);
                                if ((sectionRecord != null) &&
                                    (sectionRecord.LockChildren || sectionRecord.Locked))
                                    locked = true;
                                else
                                {
                                    // It is an error if a parent configuration file locks a section for the
                                    // locationConfigPath or any sub-path of the locationConfigPath.
                                    if (parent._locationSections != null)
                                    {
                                        string targetConfigPath =
                                            locationSectionRecord.SectionXmlInfo.TargetConfigPath;

                                        foreach (
                                            LocationSectionRecord parentLocationSectionRecord in
                                            parent._locationSections)
                                        {
                                            string parentTargetConfigPath =
                                                parentLocationSectionRecord.SectionXmlInfo.TargetConfigPath;

                                            if (
                                                parentLocationSectionRecord.SectionXmlInfo.OverrideModeSetting.IsLocked &&
                                                (locationSectionRecord.ConfigKey ==
                                                parentLocationSectionRecord.ConfigKey) &&
                                                UrlPath.IsEqualOrSubpath(targetConfigPath, parentTargetConfigPath))
                                            {
                                                locked = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (locked)
                                {
                                    locationSectionRecord.AddError(new ConfigurationErrorsException(
                                        SR.Config_section_locked,
                                        locationSectionRecord.SectionXmlInfo));
                                }
                            }

                            parent = parent._parent;
                        }
                    }
                }

                _flags[IsLocationListResolved] = true;
            }
        }


        // Verify that the Definition is allowed at this place.
        //
        // For example, if this config record is an application then make sure the section
        // says it can be defined in an application
        private void VerifyDefinitionAllowed(FactoryRecord factoryRecord, string configPath, IConfigErrorInfo errorInfo)
        {
            Host.VerifyDefinitionAllowed(configPath, factoryRecord.AllowDefinition, factoryRecord.AllowExeDefinition,
                errorInfo);
        }

        internal bool IsDefinitionAllowed(ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition)
        {
            return Host.IsDefinitionAllowed(_configPath, allowDefinition, allowExeDefinition);
        }

        protected static void VerifySectionName(string name, XmlUtil xmlUtil, ExceptionAction action, bool allowImplicit)
        {
            try
            {
                VerifySectionName(name, xmlUtil, allowImplicit);
            }
            catch (ConfigurationErrorsException ce)
            {
                xmlUtil.SchemaErrors.AddError(ce, action);
            }
        }

        // Check if the section name contains reserved words from the config system,
        // and is a valid name for an XML Element.
        protected static void VerifySectionName(string name, IConfigErrorInfo errorInfo, bool allowImplicit)
        {
            if (string.IsNullOrEmpty(name))
                throw new ConfigurationErrorsException(SR.Config_tag_name_invalid, errorInfo);

            // must be a valid name in xml, so that it can be used as an element
            // n.b. - it also excludes forward slash '/'
            try
            {
                XmlConvert.VerifyName(name);
            }
            catch (Exception e)
            {
                // Do not let the exception propagate as an XML exception,
                // for we want errors in the section name to be treated as local errors,
                // not global ones.
                throw ExceptionUtil.WrapAsConfigException(SR.Config_tag_name_invalid, e, errorInfo);
            }

            if (IsImplicitSection(name))
            {
                if (allowImplicit)
                {
                    // avoid test below for strings starting with "config"
                    return;
                }

                throw new ConfigurationErrorsException(
                    SR.Format(SR.Cannot_declare_or_remove_implicit_section, name), errorInfo);
            }

            if (StringUtil.StartsWithOrdinal(name, "config"))
                throw new ConfigurationErrorsException(SR.Config_tag_name_cannot_begin_with_config, errorInfo);

            if (name == LocationTag)
                throw new ConfigurationErrorsException(SR.Config_tag_name_cannot_be_location, errorInfo);
        }

        // Return null if the subPath represents the current directory, for example:
        //      path=""
        //      path="   "
        //      path="."
        //      path="./"
        internal static string NormalizeLocationSubPath(string subPath, IConfigErrorInfo errorInfo)
        {
            // if subPath is null or empty, it is the current dir
            if (string.IsNullOrEmpty(subPath))
                return null;

            // if subPath=".", it is the current dir
            if (subPath == ".")
                return null;

            // do not allow whitespace in front of subPath, as the OS
            // handles beginning and trailing whitespace inconsistently
            string trimmedSubPath = subPath.TrimStart();
            if (trimmedSubPath.Length != subPath.Length)
                throw new ConfigurationErrorsException(SR.Config_location_path_invalid_first_character, errorInfo);

            // do not allow problematic starting characters
            if (InvalidFirstSubPathCharacters.IndexOf(subPath[0]) != -1)
                throw new ConfigurationErrorsException(SR.Config_location_path_invalid_first_character, errorInfo);

            // do not allow whitespace at end of subPath, as the OS
            // handles beginning and trailing whitespace inconsistently
            trimmedSubPath = subPath.TrimEnd();
            if (trimmedSubPath.Length != subPath.Length)
                throw new ConfigurationErrorsException(SR.Config_location_path_invalid_last_character, errorInfo);

            // the file system ignores trailing '.', '\', or '/', so do not allow it in a location subpath specification
            if (InvalidLastSubPathCharacters.IndexOf(subPath[subPath.Length - 1]) != -1)
                throw new ConfigurationErrorsException(SR.Config_location_path_invalid_last_character, errorInfo);

            // combination of URI reserved characters and OS invalid filename characters, minus / (allowed reserved character)
            if (subPath.IndexOfAny(s_invalidSubPathCharactersArray) != -1)
                throw new ConfigurationErrorsException(SR.Config_location_path_invalid_character, errorInfo);

            return subPath;
        }

        // Return the SectionRecord for a section.
        // If the record does not exist, return null.
        // Throw cached errors if the section is in error and permitErrors == false.
        protected SectionRecord GetSectionRecord(string configKey, bool permitErrors)
        {
            SectionRecord sectionRecord;

            if (_sectionRecords != null) sectionRecord = (SectionRecord)_sectionRecords[configKey];
            else sectionRecord = null;

            if ((sectionRecord != null) && !permitErrors) sectionRecord.ThrowOnErrors();

            return sectionRecord;
        }

        // Return an existing SectionRecord, or create one if one does not exist.
        // Propagate the Locked bit from parent
        protected SectionRecord EnsureSectionRecord(string configKey, bool permitErrors)
        {
            return EnsureSectionRecordImpl(configKey, permitErrors, true);
        }

        // Return an existing SectionRecord, or create one if one does not exist.
        // Do not propagate the Locked bit from parent, because caller will check
        // himself later.
        protected SectionRecord EnsureSectionRecordUnsafe(string configKey, bool permitErrors)
        {
            return EnsureSectionRecordImpl(configKey, permitErrors, false);
        }

        // Return an existing SectionRecord, or create one if one does not exist.
        // If desired, set the lock settings based on parent configs.
        private SectionRecord EnsureSectionRecordImpl(string configKey, bool permitErrors, bool setLockSettings)
        {
            SectionRecord sectionRecord = GetSectionRecord(configKey, permitErrors);
            if (sectionRecord != null) return sectionRecord;

            lock (this)
            {
                if (_sectionRecords == null) _sectionRecords = new Hashtable();
                else sectionRecord = GetSectionRecord(configKey, permitErrors);

                if (sectionRecord == null)
                {
                    sectionRecord = new SectionRecord(configKey);

                    _sectionRecords.Add(configKey, sectionRecord);
                }
            }

            if (setLockSettings)
            {
                // Get the lock mode from parent configs
                OverrideMode childLockMode;
                OverrideMode parentMode = ResolveOverrideModeFromParent(configKey, out childLockMode);

                sectionRecord.ChangeLockSettings(parentMode, childLockMode);
            }

            return sectionRecord;
        }

        internal FactoryRecord GetFactoryRecord(string configKey, bool permitErrors)
        {
            if (_factoryRecords == null) return null;

            FactoryRecord factoryRecord = (FactoryRecord)_factoryRecords[configKey];
            if ((factoryRecord != null) && !permitErrors) factoryRecord.ThrowOnErrors();

            return factoryRecord;
        }

        // Only create a _factories hashtable when necessary.
        // Most config records won't have factories, so we can save 120 bytes
        // per record by creating the table on demand.
        protected Hashtable EnsureFactories()
        {
            return _factoryRecords ??= new Hashtable();
        }

        private ArrayList EnsureLocationSections()
        {
            return _locationSections ??= new ArrayList();
        }

        internal static string NormalizeConfigSource(string configSource, IConfigErrorInfo errorInfo)
        {
            if (string.IsNullOrEmpty(configSource))
                throw new ConfigurationErrorsException(SR.Config_source_invalid_format, errorInfo);

            string trimmedConfigSource = configSource.Trim();
            if (trimmedConfigSource.Length != configSource.Length)
                throw new ConfigurationErrorsException(SR.Config_source_invalid_format, errorInfo);

            if (string.IsNullOrEmpty(configSource) || Path.IsPathRooted(configSource))
                throw new ConfigurationErrorsException(SR.Config_source_invalid_format, errorInfo);

            if (configSource.IndexOf('\\') != -1 || configSource.IndexOf('/') != -1) // string.Contains(char) is .NetCore2.1+ specific
            {
                string newConfigSource = configSource.Replace('\\', '/');
                if (!ConfigPathUtility.IsValid(newConfigSource))
                    throw new ConfigurationErrorsException(SR.Config_source_invalid_format, errorInfo);
            }

            return configSource;
        }

        protected object MonitorStream(string configKey, string configSource, string streamname)
        {
            lock (this)
            {
                if (_flags[Closed]) return null;

                StreamInfo streamInfo = (StreamInfo)ConfigStreamInfo.StreamInfos[streamname];
                if (streamInfo != null)
                {
                    if (streamInfo.SectionName != configKey)
                    {
                        throw new ConfigurationErrorsException(SR.Format(SR.Config_source_cannot_be_shared,
                            streamname));
                    }

                    if (streamInfo.IsMonitored) return streamInfo.Version;
                }
                else
                {
                    streamInfo = new StreamInfo(configKey, configSource, streamname);
                    ConfigStreamInfo.StreamInfos.Add(streamname, streamInfo);
                }
            }

            // Call the host outside the lock to avoid deadlock.
            object version = Host.GetStreamVersion(streamname);

            StreamChangeCallback callbackDelegate = null;

            lock (this)
            {
                if (_flags[Closed]) return null;

                StreamInfo streamInfo = (StreamInfo)ConfigStreamInfo.StreamInfos[streamname];
                if (streamInfo.IsMonitored) return streamInfo.Version;

                streamInfo.IsMonitored = true;
                streamInfo.Version = version;

                if (_flags[SupportsChangeNotifications])
                {
                    if (ConfigStreamInfo.CallbackDelegate == null)
                        ConfigStreamInfo.CallbackDelegate = OnStreamChanged;

                    callbackDelegate = ConfigStreamInfo.CallbackDelegate;
                }
            }

            if (_flags[SupportsChangeNotifications]) Host.StartMonitoringStreamForChanges(streamname, callbackDelegate);

            return version;
        }

        private void OnStreamChanged(string streamname)
        {
            bool notifyChanged;
            string sectionName;

            lock (this)
            {
                if (_flags[Closed])
                    return;

                StreamInfo streamInfo = (StreamInfo)ConfigStreamInfo.StreamInfos[streamname];
                if ((streamInfo == null) || !streamInfo.IsMonitored)
                    return;

                sectionName = streamInfo.SectionName;
            }

            if (sectionName == null) notifyChanged = true;
            else
            {
                FactoryRecord factoryRecord = FindFactoryRecord(sectionName, false);
                notifyChanged = factoryRecord.RestartOnExternalChanges;
            }

            if (notifyChanged) _configRoot.FireConfigChanged(_configPath);
            else _configRoot.ClearResult(this, sectionName, false);
        }

        // ValidateUniqueConfigSource
        //
        // Validate that the configSource is unique for this particular
        // configKey.  This looks up at the parents and makes sure it is
        // unique.  It if is in a child, then it's check will find this
        // one.  If it is in a peer, then we don't care as much, since it
        // will not affect Merge and UnMerge
        private void ValidateUniqueConfigSource(
            string configKey, string configSourceStreamName, string configSourceArg, IConfigErrorInfo errorInfo)
        {
            // Detect if another section in this file is using the same configSource
            // with has a different section name.
            lock (this)
            {
                if (ConfigStreamInfo.HasStreamInfos)
                {
                    StreamInfo streamInfo = (StreamInfo)ConfigStreamInfo.StreamInfos[configSourceStreamName];
                    if ((streamInfo != null) && (streamInfo.SectionName != configKey))
                    {
                        throw new ConfigurationErrorsException(
                            SR.Format(SR.Config_source_cannot_be_shared, configSourceArg),
                            errorInfo);
                    }
                }
            }

            ValidateUniqueChildConfigSource(configKey, configSourceStreamName, configSourceArg, errorInfo);
        }

        protected void ValidateUniqueChildConfigSource(
            string configKey, string configSourceStreamName, string configSourceArg, IConfigErrorInfo errorInfo)
        {
            // Detect if a parent config file is using the same config source stream.
            BaseConfigurationRecord current = IsLocationConfig ? _parent._parent : _parent;

            while (!current.IsRootConfig)
            {
                lock (current)
                {
                    if (current.ConfigStreamInfo.HasStreamInfos)
                    {
                        StreamInfo streamInfo =
                            (StreamInfo)current.ConfigStreamInfo.StreamInfos[configSourceStreamName];
                        if (streamInfo != null)
                        {
                            throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_source_parent_conflict, configSourceArg),
                                errorInfo);
                        }
                    }
                }

                current = current.Parent;
            }
        }

        // Recursively clear the result.
        // If forceEvaluation == true, force a rescan of the config file to find
        // the section.
        // Requires the hierarchy lock to be acquired (hl)
        internal void HlClearResultRecursive(string configKey, bool forceEvaluatation)
        {
            // Refresh it's factory Record
            RefreshFactoryRecord(configKey);

            // Clear any stored result in the section
            SectionRecord sectionRecord = GetSectionRecord(configKey, false);
            if (sectionRecord != null)
            {
                sectionRecord.ClearResult();

                // Need to clear all RawXml so that when GetSectionXmlReader is called later it will reload the file.
                sectionRecord.ClearRawXml();
            }

            // If we need to reevaluate, add a dummy file input so
            // that we open the file on the next evaluation
            if (forceEvaluatation && !IsInitDelayed && !string.IsNullOrEmpty(ConfigStreamInfo.StreamName))
            {
                if (_flags[SupportsPath])
                    throw ExceptionUtil.UnexpectedError("BaseConfigurationRecord::hlClearResultRecursive");

                FactoryRecord factoryRecord = FindFactoryRecord(configKey, false);
                if ((factoryRecord != null) && !factoryRecord.IsGroup)
                {
                    configKey = factoryRecord.ConfigKey;
                    sectionRecord = EnsureSectionRecord(configKey, false);
                    if (!sectionRecord.HasFileInput)
                    {
                        SectionXmlInfo sectionXmlInfo = new SectionXmlInfo(
                            configKey, _configPath, _configPath, null,
                            ConfigStreamInfo.StreamName, 0, null, null,
                            null, null, null,
                            null, OverrideModeSetting.s_locationDefault, false);

                        SectionInput fileInput = new SectionInput(sectionXmlInfo, null);
                        sectionRecord.AddFileInput(fileInput);
                    }
                }
            }

            // Recurse
            if (_children != null)
            {
                IEnumerable children = _children.Values;
                foreach (BaseConfigurationRecord child in children)
                    child.HlClearResultRecursive(configKey, forceEvaluatation);
            }
        }

        // Returns a child record.
        // Requires the hierarchy lock to be acquired (hl)
        internal BaseConfigurationRecord HlGetChild(string configName)
        {
            return (BaseConfigurationRecord)_children?[configName];
        }

        // Adds a child record.
        // Requires the hierarchy lock to be acquired (hl)
        internal void HlAddChild(string configName, BaseConfigurationRecord child)
        {
            if (_children == null) _children = new Hashtable(StringComparer.OrdinalIgnoreCase);

            _children.Add(configName, child);
        }

        // Removes a child record.
        // Requires the hierarchy lock to be acquired (hl)
        internal void HlRemoveChild(string configName)
        {
            _children?.Remove(configName);
        }

        // Removes true if a child record is needed for a
        // child config path.
        // Requires the hierarchy lock to be acquired (hl)
        internal bool HlNeedsChildFor(string configName)
        {
            // Always return true for root config record
            if (IsRootConfig)
                return true;

            // Never create a child record when the parent has an exception.
            if (HasInitErrors) return false;

            string childConfigPath = ConfigPathUtility.Combine(_configPath, configName);

            // check host if required
            if (Host.IsConfigRecordRequired(childConfigPath)) return true;

            // see if there's a location
            if (!_flags[SupportsLocation]) return false;
            BaseConfigurationRecord configRecord = this;

            while (!configRecord.IsRootConfig)
            {
                if (configRecord._locationSections != null)
                {
                    configRecord.ResolveLocationSections();
                    foreach (LocationSectionRecord locationSectionRecord in configRecord._locationSections)
                        if (UrlPath.IsEqualOrSubpath(childConfigPath,
                            locationSectionRecord.SectionXmlInfo.TargetConfigPath))
                            return true;
                }

                configRecord = configRecord._parent;
            }

            return false;
        }

        // Close the record. An explicit close is needed
        // in order to stop monitoring streams used by
        // this record. Stream monitors cause this record
        // to be rooted in the GC heap.
        //
        // Note that we purposely do not cleanup the child/parent
        // hierarchy. This is so that a config system which has
        // a pointer to this record can still call GetSection on
        // it while another thread closes it.
        internal void CloseRecursive()
        {
            if (_flags[Closed]) return;
            bool doClose = false;
            HybridDictionary streamInfos = null;
            StreamChangeCallback callbackDelegate = null;

            lock (this)
            {
                if (!_flags[Closed])
                {
                    _flags[Closed] = true;
                    doClose = true;

                    if (!IsLocationConfig && ConfigStreamInfo.HasStreamInfos)
                    {
                        callbackDelegate = ConfigStreamInfo.CallbackDelegate;
                        streamInfos = ConfigStreamInfo.StreamInfos;

                        ConfigStreamInfo.CallbackDelegate = null;
                        ConfigStreamInfo.ClearStreamInfos();
                    }
                }
            }

            if (!doClose) return;

            // no hierarchy lock is needed to access _children here,
            // as it has already been detached from the hierarchy tree
            if (_children != null)
                foreach (BaseConfigurationRecord child in _children.Values) child.CloseRecursive();

            if (streamInfos != null)
            {
                foreach (StreamInfo streamInfo in streamInfos.Values)
                    if (streamInfo.IsMonitored)
                    {
                        Host.StopMonitoringStreamForChanges(streamInfo.StreamName, callbackDelegate);
                        streamInfo.IsMonitored = false;
                    }
            }
        }

        internal string FindChangedConfigurationStream()
        {
            BaseConfigurationRecord configRecord = this;
            while (!configRecord.IsRootConfig)
            {
                lock (configRecord)
                {
                    if (configRecord.ConfigStreamInfo.HasStreamInfos)
                    {
                        foreach (StreamInfo streamInfo in configRecord.ConfigStreamInfo.StreamInfos.Values)
                            if (streamInfo.IsMonitored && HasStreamChanged(streamInfo.StreamName, streamInfo.Version))
                                return streamInfo.StreamName;
                    }
                }

                configRecord = configRecord._parent;
            }

            return null;
        }

        private bool HasStreamChanged(string streamname, object lastVersion)
        {
            object currentVersion = Host.GetStreamVersion(streamname);

            if (lastVersion != null) return (currentVersion == null) || !lastVersion.Equals(currentVersion);

            return currentVersion != null;
        }

        protected virtual string CallHostDecryptSection(string encryptedXml,
            ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfig)
        {
            return Host.DecryptSection(encryptedXml, protectionProvider, protectedConfig);
        }

        internal static string ValidateProtectionProviderAttribute(string protectionProvider, IConfigErrorInfo errorInfo)
        {
            if (string.IsNullOrEmpty(protectionProvider))
                throw new ConfigurationErrorsException(SR.Protection_provider_invalid_format, errorInfo);

            return protectionProvider;
        }

        private ConfigXmlReader DecryptConfigSection(ConfigXmlReader reader,
            ProtectedConfigurationProvider protectionProvider)
        {
            ConfigXmlReader clone = reader.Clone();
            IConfigErrorInfo err = clone;
            string clearTextXml;
            XmlNodeType nodeType;

            clone.Read();

            // Save the file and line at the top of the section
            string filename = err.Filename;
            int lineNumber = err.LineNumber;
            int sectionLineNumber = lineNumber;

            if (clone.IsEmptyElement)
                throw new ConfigurationErrorsException(SR.EncryptedNode_not_found, filename, lineNumber);

            // Find the <EncryptedData> node
            while (true)
            {
                clone.Read(); // Keep reading till we find a relavant node

                nodeType = clone.NodeType;

                if ((nodeType == XmlNodeType.Element) && (clone.Name == "EncryptedData"))
                {
                    // Found it!
                    break;
                }

                if (nodeType == XmlNodeType.EndElement)
                    throw new ConfigurationErrorsException(SR.EncryptedNode_not_found, filename, lineNumber);

                if ((nodeType != XmlNodeType.Comment) && (nodeType != XmlNodeType.Whitespace))
                {
                    // some other unexpected content
                    throw new ConfigurationErrorsException(SR.EncryptedNode_is_in_invalid_format, filename, lineNumber);
                }
            }

            // Do the decryption

            // Save the line at the top of the <EncryptedData> node
            lineNumber = err.LineNumber;

            string encryptedXml = clone.ReadOuterXml();
            try
            {
                clearTextXml = CallHostDecryptSection(encryptedXml, protectionProvider, ProtectedConfig);
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Decryption_failed, protectionProvider.Name, e.Message), e, filename, lineNumber);
            }

            // Detect if there is any XML left over after <EncryptedData>
            do
            {
                nodeType = clone.NodeType;

                if (nodeType == XmlNodeType.EndElement) break;

                if ((nodeType != XmlNodeType.Comment) && (nodeType != XmlNodeType.Whitespace))
                {
                    // Got other unexpected content
                    throw new ConfigurationErrorsException(SR.EncryptedNode_is_in_invalid_format, filename, lineNumber);
                }
            } while (clone.Read());

            // Create a new reader, using the position of the original reader
            return new ConfigXmlReader(clearTextXml, filename, sectionLineNumber, true);
        }

        private void ThrowIfParseErrors(ConfigurationSchemaErrors schemaErrors)
        {
            schemaErrors.ThrowIfErrors(ClassFlags[ClassIgnoreLocalErrors]);
        }

        internal static bool IsImplicitSection(string configKey)
        {
            return string.Equals(configKey, ReservedSectionProtectedConfiguration, StringComparison.Ordinal);
        }

        /// <summary>
        /// Add implicit sections to the specified factory list.
        /// </summary>
        /// <param name="factoryList">The factory list to add to. If null, adds to the current record's factory list.</param>
        private void AddImplicitSections(Hashtable factoryList)
        {
            // Only add implicit sections to the factoryList if we're under the root
            // (e.g. if we're in machine.config)
            if (!_parent.IsRootConfig) return;

            if (factoryList == null) factoryList = EnsureFactories();

            // Look to see if we already have a factory for "configProtectedData"
            FactoryRecord factoryRecord = (FactoryRecord)factoryList[ReservedSectionProtectedConfiguration];

            if (factoryRecord != null)
            {
                // If the user has mistakenly declared an implicit section, we should leave the factoryRecord
                // alone because it contains the error and the error will be thrown later.

                Debug.Assert(factoryRecord.HasErrors,
                    "If the user has mistakenly declared an implicit section, we should have recorded an error.");
                return;
            }

            // Add our implicit "configProtectedData" for ProtectedConfigurationSection
            factoryList[ReservedSectionProtectedConfiguration] =
                new FactoryRecord(
                    configKey: ReservedSectionProtectedConfiguration,
                    group: string.Empty,
                    name: ReservedSectionProtectedConfiguration,
                    factoryTypeName: ProtectedConfigurationSectionTypeName,
                    allowLocation: true,
                    allowDefinition: ConfigurationAllowDefinition.Everywhere,
                    allowExeDefinition: ConfigurationAllowExeDefinition.MachineToApplication,
                    overrideModeDefault: OverrideModeSetting.s_sectionDefault,
                    restartOnExternalChanges: true,
                    requirePermission: true,
                    isFromTrustedConfigRecord: true,
                    isUndeclared: true,
                    filename: null,
                    lineNumber: -1);
        }

        // We reserve all attribute names starting with config or lock
        internal static bool IsReservedAttributeName(string name)
        {
            return StringUtil.StartsWithOrdinal(name, "config") ||
                StringUtil.StartsWithOrdinal(name, "lock");
        }

        protected sealed class ConfigRecordStreamInfo
        {
            private HybridDictionary _streamInfos;

            internal ConfigRecordStreamInfo()
            {
                StreamEncoding = Encoding.UTF8;
            }

            internal bool HasStream { get; set; }

            internal string StreamName { get; set; }

            internal object StreamVersion { get; set; }

            internal Encoding StreamEncoding { get; set; }

            internal StreamChangeCallback CallbackDelegate { get; set; }

            internal HybridDictionary StreamInfos => _streamInfos ??= new HybridDictionary(true);

            internal bool HasStreamInfos => _streamInfos != null;

#if DEBUG
            internal string[] Keys
            {
                get
                {
                    string[] keys = new string[StreamInfos.Count];
                    StreamInfos.Keys.CopyTo(keys, 0);
                    return keys;
                }
            }
#endif

            internal void ClearStreamInfos()
            {
                _streamInfos = null;
            }
        }

        private sealed class IndirectLocationInputComparer : IComparer<SectionInput>
        {
            public int Compare(SectionInput x, SectionInput y)
            {
                // We have to sort the indirect inputs
                // 1. First by the location tag's target config path, and if they're the same,
                // 2. Then by the location tag's definition config path.
                //
                // In the final sorted list, a child will be smaller than a parent.

                Debug.Assert(x.SectionXmlInfo.ConfigKey == y.SectionXmlInfo.ConfigKey);

                if (ReferenceEquals(x, y))
                {
                    // Check if they're the same object.
                    return 0;
                }

                string xTargetConfigPath = x.SectionXmlInfo.TargetConfigPath;
                string yTargetConfigPath = y.SectionXmlInfo.TargetConfigPath;

                // First compare using location tag's target config path:
                if (UrlPath.IsSubpath(xTargetConfigPath, yTargetConfigPath))
                {
                    // yTargetConfigPath is a child path of xTargetConfigPath, so y < x
                    return 1;
                }

                if (UrlPath.IsSubpath(yTargetConfigPath, xTargetConfigPath))
                {
                    // xTargetConfigPath is a child path of yTargetConfigPath, so x < y
                    return -1;
                }

                // Because all indirect inputs must be pointing to nodes along a
                // single branch of config hierarchy, so if the above two cases
                // aren't true, then the two target config path must be equal;
                // in another word, they should not be siblings.
                Debug.Assert(StringUtil.EqualsIgnoreCase(yTargetConfigPath, xTargetConfigPath));

                string xDefinitionConfigPath = x.SectionXmlInfo.DefinitionConfigPath;
                string yDefinitionConfigPath = y.SectionXmlInfo.DefinitionConfigPath;

                // Then compare using where the location tag is defined.
                if (UrlPath.IsSubpath(xDefinitionConfigPath, yDefinitionConfigPath))
                {
                    // yDefinitionConfigPath is a child path of xDefinitionConfigPath, so y < x
                    return 1;
                }

                if (UrlPath.IsSubpath(yDefinitionConfigPath, xDefinitionConfigPath))
                {
                    // xDefinitionConfigPath is a child path of yDefinitionConfigPath, so x < y
                    return -1;
                }

                Debug.Fail("It's not possible for two location input to come from the same config file and point to the same target");
                return 0;
            }
        }
    }
}
