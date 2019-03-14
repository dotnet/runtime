// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Configuration
{
    /// <summary>
    /// Base settings class for client applications.
    /// </summary>
    public abstract class ApplicationSettingsBase : SettingsBase, INotifyPropertyChanged
    {
        private bool _explicitSerializeOnClass = false;
        private object[] _classAttributes;
        private IComponent _owner;
        private PropertyChangedEventHandler _onPropertyChanged;
        private SettingsContext _context;
        private SettingsProperty _init;
        private SettingsPropertyCollection _settings;
        private SettingsProviderCollection _providers;
        private SettingChangingEventHandler _onSettingChanging;
        private SettingsLoadedEventHandler _onSettingsLoaded;
        private SettingsSavingEventHandler _onSettingsSaving;
        private string _settingsKey = string.Empty;
        private bool _firstLoad = true;
        private bool _initialized = false;

        /// <summary>
        /// Default constructor without a concept of "owner" component.
        /// </summary>
        protected ApplicationSettingsBase() : base()
        {
        }

        /// <summary>
        /// Constructor that takes an IComponent. The IComponent acts as the "owner" of this settings class. One
        /// of the things we do is query the component's site to see if it has a SettingsProvider service. If it 
        /// does, we allow it to override the providers specified in the metadata.
        /// </summary>
        protected ApplicationSettingsBase(IComponent owner) : this(owner, string.Empty)
        {
        }

        /// <summary>
        /// Convenience overload that takes the settings key
        /// </summary>
        protected ApplicationSettingsBase(string settingsKey)
        {
            _settingsKey = settingsKey;
        }

        /// <summary>
        /// Convenience overload that takes the owner component and settings key.
        /// </summary>
        protected ApplicationSettingsBase(IComponent owner, string settingsKey) : this(settingsKey)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            _owner = owner;

            if (owner.Site != null)
            {
                ISettingsProviderService providerService = owner.Site.GetService(typeof(ISettingsProviderService)) as ISettingsProviderService;
                if (providerService != null)
                {
                    // The component's site has a settings provider service. We pass each SettingsProperty to it
                    // to see if it wants to override the current provider.
                    foreach (SettingsProperty property in Properties)
                    {
                        SettingsProvider provider = providerService.GetSettingsProvider(property);
                        if (provider != null)
                        {
                            property.Provider = provider;
                        }
                    }

                    ResetProviders();
                }
            }
        }

        /// <summary>
        /// The Context to pass on to the provider. Currently, this will just contain the settings group name.
        /// </summary>
        [Browsable(false)]
        public override SettingsContext Context
        {
            get
            {
                if (_context == null)
                {
                    if (IsSynchronized)
                    {
                        lock (this)
                        {
                            if (_context == null)
                            {
                                _context = new SettingsContext();
                                EnsureInitialized();
                            }
                        }
                    }
                    else
                    {
                        _context = new SettingsContext();
                        EnsureInitialized();
                    }

                }

                return _context;
            }
        }

        /// <summary>
        /// The SettingsBase class queries this to get the collection of SettingsProperty objects. We reflect over 
        /// the properties defined on the current object's type and use the metadata on those properties to form 
        /// this collection.
        /// </summary>
        [Browsable(false)]
        public override SettingsPropertyCollection Properties
        {
            get
            {
                if (_settings == null)
                {
                    if (IsSynchronized)
                    {
                        lock (this)
                        {
                            if (_settings == null)
                            {
                                _settings = new SettingsPropertyCollection();
                                EnsureInitialized();
                            }
                        }
                    }
                    else
                    {
                        _settings = new SettingsPropertyCollection();
                        EnsureInitialized();
                    }

                }

                return _settings;
            }
        }

        /// <summary>
        /// Just overriding to add attributes.
        /// </summary>
        [Browsable(false)]
        public override SettingsPropertyValueCollection PropertyValues
        {
            get
            {
                return base.PropertyValues;
            }
        }

        /// <summary>
        /// Provider collection
        /// </summary>
        [Browsable(false)]
        public override SettingsProviderCollection Providers
        {
            get
            {
                if (_providers == null)
                {
                    if (IsSynchronized)
                    {
                        lock (this)
                        {
                            if (_providers == null)
                            {
                                _providers = new SettingsProviderCollection();
                                EnsureInitialized();
                            }
                        }
                    }
                    else
                    {
                        _providers = new SettingsProviderCollection();
                        EnsureInitialized();
                    }
                }

                return _providers;
            }
        }

        /// <summary>
        /// Derived classes should use this to uniquely identify separate instances of settings classes.
        /// </summary>
        [Browsable(false)]
        public string SettingsKey
        {
            get
            {
                return _settingsKey;
            }
            set
            {
                _settingsKey = value;
                Context["SettingsKey"] = _settingsKey;
            }
        }

        /// <summary>
        /// Fires when the value of a setting is changed. (INotifyPropertyChanged implementation.)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _onPropertyChanged += value;
            }
            remove
            {
                _onPropertyChanged -= value;
            }

        }

        /// <summary>
        /// Fires when the value of a setting is about to change. This is a cancellable event.
        /// </summary>
        public event SettingChangingEventHandler SettingChanging
        {
            add
            {
                _onSettingChanging += value;
            }
            remove
            {
                _onSettingChanging -= value;
            }
        }

        /// <summary>
        /// Fires when settings are retrieved from a provider. It fires once for each provider.
        /// </summary>
        public event SettingsLoadedEventHandler SettingsLoaded
        {
            add
            {
                _onSettingsLoaded += value;
            }
            remove
            {
                _onSettingsLoaded -= value;
            }
        }

        /// <summary>
        /// Fires when Save() is called. This is a cancellable event.
        /// </summary>
        public event SettingsSavingEventHandler SettingsSaving
        {
            add
            {
                _onSettingsSaving += value;
            }
            remove
            {
                _onSettingsSaving -= value;
            }
        }

        /// <summary>
        /// Used in conjunction with Upgrade - retrieves the previous value of a setting from the provider.
        /// Provider must implement IApplicationSettingsProvider to support this.
        /// </summary>
        public object GetPreviousVersion(string propertyName)
        {
            if (Properties.Count == 0)
                throw new SettingsPropertyNotFoundException();

            SettingsProperty sp = Properties[propertyName];
            SettingsPropertyValue value = null;

            if (sp == null)
                throw new SettingsPropertyNotFoundException();

            IApplicationSettingsProvider clientProv = sp.Provider as IApplicationSettingsProvider;

            if (clientProv != null)
            {
                value = clientProv.GetPreviousVersion(Context, sp);
            }

            if (value != null)
            {
                return value.PropertyValue;
            }

            return null;
        }

        /// <summary>
        /// Fires the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _onPropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Fires the SettingChanging event.
        /// </summary>
        protected virtual void OnSettingChanging(object sender, SettingChangingEventArgs e)
        {
            _onSettingChanging?.Invoke(this, e);
        }

        /// <summary>
        /// Fires the SettingsLoaded event.
        /// </summary>
        protected virtual void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            _onSettingsLoaded?.Invoke(this, e);
        }

        /// <summary>
        /// Fires the SettingsSaving event.
        /// </summary>
        protected virtual void OnSettingsSaving(object sender, CancelEventArgs e)
        {
            _onSettingsSaving?.Invoke(this, e);
        }

        /// <summary>
        /// Causes a reload to happen on next setting access, by clearing the cached values.
        /// </summary>
        public void Reload()
        {
            if (PropertyValues != null)
            {
                PropertyValues.Clear();
            }

            foreach (SettingsProperty sp in Properties)
            {
                PropertyChangedEventArgs pe = new PropertyChangedEventArgs(sp.Name);
                OnPropertyChanged(this, pe);
            }
        }

        /// <summary>
        /// Calls Reset on the providers.
        /// Providers must implement IApplicationSettingsProvider to support this.
        /// </summary>
        public void Reset()
        {
            if (Properties != null)
            {
                foreach (SettingsProvider provider in Providers)
                {
                    IApplicationSettingsProvider clientProv = provider as IApplicationSettingsProvider;
                    if (clientProv != null)
                    {
                        clientProv.Reset(Context);
                    }
                }
            }

            Reload();
        }

        /// <summary>
        /// Overridden from SettingsBase to support validation event.
        /// </summary>
        public override void Save()
        {
            CancelEventArgs e = new CancelEventArgs(false);
            OnSettingsSaving(this, e);

            if (!e.Cancel)
            {
                base.Save();
            }
        }

        /// <summary>
        /// Overridden from SettingsBase to support validation event.
        /// </summary>
        public override object this[string propertyName]
        {
            get
            {
                if (IsSynchronized)
                {
                    lock (this)
                    {
                        return GetPropertyValue(propertyName);
                    }
                }
                else
                {
                    return GetPropertyValue(propertyName);
                }

            }
            set
            {
                SettingChangingEventArgs e = new SettingChangingEventArgs(propertyName, this.GetType().FullName, SettingsKey, value, false);
                OnSettingChanging(this, e);

                if (!e.Cancel)
                {
                    base[propertyName] = value;

                    // CONSIDER: Should we call this even if canceled?
                    PropertyChangedEventArgs pe = new PropertyChangedEventArgs(propertyName);
                    OnPropertyChanged(this, pe);
                }
            }
        }

        /// <summary>
        /// Called when the app is upgraded so that we can instruct the providers to upgrade their settings.
        /// Providers must implement IApplicationSettingsProvider to support this.
        /// </summary>
        public virtual void Upgrade()
        {
            if (Properties != null)
            {
                foreach (SettingsProvider provider in Providers)
                {
                    IApplicationSettingsProvider clientProv = provider as IApplicationSettingsProvider;
                    if (clientProv != null)
                    {
                        clientProv.Upgrade(Context, GetPropertiesForProvider(provider));
                    }
                }
            }

            Reload();
        }

        /// <summary>
        /// Creates a SettingsProperty object using the metadata on the given property 
        /// and returns it.
        /// </summary>
        private SettingsProperty CreateSetting(PropertyInfo propertyInfo)
        {
            // Initialization method -
            // be careful not to access properties here to prevent stack overflow.

            object[] attributes = propertyInfo.GetCustomAttributes(false);
            SettingsProperty settingsProperty = new SettingsProperty(Initializer);
            bool explicitSerialize = _explicitSerializeOnClass;

            settingsProperty.Name = propertyInfo.Name;
            settingsProperty.PropertyType = propertyInfo.PropertyType;

            for (int i = 0; i < attributes.Length; i++)
            {
                Attribute attribute = attributes[i] as Attribute;
                if (attribute == null)
                    continue;

                if (attribute is DefaultSettingValueAttribute)
                {
                    settingsProperty.DefaultValue = ((DefaultSettingValueAttribute)attribute).Value;
                }
                else if (attribute is ReadOnlyAttribute)
                {
                    settingsProperty.IsReadOnly = true;
                }
                else if (attribute is SettingsProviderAttribute)
                {
                    string providerTypeName = ((SettingsProviderAttribute)attribute).ProviderTypeName;
                    Type providerType = Type.GetType(providerTypeName);
                    if (providerType == null)
                    {
                        throw new ConfigurationErrorsException(SR.Format(SR.ProviderTypeLoadFailed, providerTypeName));
                    }

                    SettingsProvider settingsProvider = TypeUtil.CreateInstance(providerType) as SettingsProvider;

                    if (settingsProvider == null)
                    {
                        throw new ConfigurationErrorsException(SR.Format(SR.ProviderInstantiationFailed, providerTypeName));
                    }

                    settingsProvider.Initialize(null, null);
                    settingsProvider.ApplicationName = ConfigurationManagerInternalFactory.Instance.ExeProductName;

                    // See if we already have a provider of the same name in our collection. If so,
                    // re-use the existing instance, since we cannot have multiple providers of the same name.
                    SettingsProvider existing = _providers[settingsProvider.Name];
                    if (existing != null)
                    {
                        settingsProvider = existing;
                    }

                    settingsProperty.Provider = settingsProvider;
                }
                else if (attribute is SettingsSerializeAsAttribute)
                {
                    settingsProperty.SerializeAs = ((SettingsSerializeAsAttribute)attribute).SerializeAs;
                    explicitSerialize = true;
                }
                else
                {
                    // This isn't an attribute we care about, so simply pass it on
                    // to the SettingsProvider.
                    //
                    // NOTE: The key is the type. So if an attribute was found at class
                    //       level and also property level, the latter overrides the former
                    //       for a given setting. This is exactly the behavior we want.

                    settingsProperty.Attributes.Add(attribute.GetType(), attribute);
                }
            }

            if (!explicitSerialize)
            {
                // Serialization method was not explicitly attributed.

                TypeConverter tc = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                if (tc.CanConvertTo(typeof(string)) && tc.CanConvertFrom(typeof(string)))
                {
                    // We can use string
                    settingsProperty.SerializeAs = SettingsSerializeAs.String;
                }
                else
                {
                    // Fallback is Xml
                    settingsProperty.SerializeAs = SettingsSerializeAs.Xml;
                }
            }

            return settingsProperty;
        }

        /// <summary>
        /// Ensures this class is initialized. Initialization involves reflecting over properties and building
        /// a list of SettingsProperty's.
        /// </summary>
        private void EnsureInitialized()
        {
            // Initialization method -
            // be careful not to access properties here to prevent stack overflow.

            if (!_initialized)
            {
                _initialized = true;

                Type type = GetType();

                if (_context == null)
                {
                    _context = new SettingsContext();
                }
                _context["GroupName"] = type.FullName;
                _context["SettingsKey"] = SettingsKey;
                _context["SettingsClassType"] = type;

                PropertyInfo[] properties = SettingsFilter(type.GetProperties(BindingFlags.Instance | BindingFlags.Public));
                _classAttributes = type.GetCustomAttributes(false);

                if (_settings == null)
                {
                    _settings = new SettingsPropertyCollection();
                }

                if (_providers == null)
                {
                    _providers = new SettingsProviderCollection();
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    SettingsProperty sp = CreateSetting(properties[i]);
                    if (sp != null)
                    {
                        _settings.Add(sp);

                        if (sp.Provider != null && _providers[sp.Provider.Name] == null)
                        {
                            _providers.Add(sp.Provider);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a SettingsProperty used to initialize settings. We initialize a setting with values
        /// derived from class level attributes, if present. Otherwise, we initialize to
        /// reasonable defaults.
        /// </summary>
        private SettingsProperty Initializer
        {
            // Initialization method -
            // be careful not to access properties here to prevent stack overflow.

            get
            {
                if (_init == null)
                {
                    _init = new SettingsProperty("");
                    _init.DefaultValue = null;
                    _init.IsReadOnly = false;
                    _init.PropertyType = null;

                    SettingsProvider provider = new LocalFileSettingsProvider();

                    if (_classAttributes != null)
                    {
                        for (int i = 0; i < _classAttributes.Length; i++)
                        {
                            Attribute attr = _classAttributes[i] as Attribute;
                            if (attr != null)
                            {
                                if (attr is ReadOnlyAttribute)
                                {
                                    _init.IsReadOnly = true;
                                }
                                else if (attr is SettingsGroupNameAttribute)
                                {
                                    if (_context == null)
                                    {
                                        _context = new SettingsContext();
                                    }
                                    _context["GroupName"] = ((SettingsGroupNameAttribute)attr).GroupName;
                                }
                                else if (attr is SettingsProviderAttribute)
                                {
                                    string providerTypeName = ((SettingsProviderAttribute)attr).ProviderTypeName;
                                    Type providerType = Type.GetType(providerTypeName);
                                    if (providerType != null)
                                    {
                                        SettingsProvider spdr = TypeUtil.CreateInstance(providerType) as SettingsProvider;
                                        if (spdr != null)
                                        {
                                            provider = spdr;
                                        }
                                        else
                                        {
                                            throw new ConfigurationErrorsException(SR.Format(SR.ProviderInstantiationFailed, providerTypeName));
                                        }
                                    }
                                    else
                                    {
                                        throw new ConfigurationErrorsException(SR.Format(SR.ProviderTypeLoadFailed, providerTypeName));
                                    }
                                }
                                else if (attr is SettingsSerializeAsAttribute)
                                {
                                    _init.SerializeAs = ((SettingsSerializeAsAttribute)attr).SerializeAs;
                                    _explicitSerializeOnClass = true;
                                }
                                else
                                {
                                    // This isn't an attribute we care about, so simply pass it on
                                    // to the SettingsProvider.
                                    // NOTE: The key is the type. So if an attribute was found at class
                                    //       level and also property level, the latter overrides the former
                                    //       for a given setting. This is exactly the behavior we want.
                                    _init.Attributes.Add(attr.GetType(), attr);
                                }
                            }
                        }
                    }

                    //Initialize the SettingsProvider
                    provider.Initialize(null, null);
                    provider.ApplicationName = ConfigurationManagerInternalFactory.Instance.ExeProductName;
                    _init.Provider = provider;

                }

                return _init;
            }
        }

        /// <summary>
        /// Gets all the settings properties for this provider.
        /// </summary>
        private SettingsPropertyCollection GetPropertiesForProvider(SettingsProvider provider)
        {
            SettingsPropertyCollection properties = new SettingsPropertyCollection();
            foreach (SettingsProperty sp in Properties)
            {
                if (sp.Provider == provider)
                {
                    properties.Add(sp);
                }
            }

            return properties;
        }

        /// <summary>
        /// Retrieves the value of a setting. We need this method so we can fire the SettingsLoaded event 
        /// when settings are loaded from the providers.Ideally, this should be fired from SettingsBase, 
        /// but unfortunately that will not happen in Whidbey. Instead, we check to see if the value has already 
        /// been retrieved. If not, we fire the load event, since we expect SettingsBase to load all the settings 
        /// from this setting's provider.
        /// </summary>
        private object GetPropertyValue(string propertyName)
        {
            if (PropertyValues[propertyName] == null)
            {

                // If this is our first load and we are part of a Clickonce app, call Upgrade.
                if (_firstLoad)
                {
                    _firstLoad = false;

                    if (IsFirstRunOfClickOnceApp())
                    {
                        Upgrade();
                    }
                }

                object temp = base[propertyName];
                SettingsProperty setting = Properties[propertyName];
                SettingsProvider provider = setting != null ? setting.Provider : null;

                Debug.Assert(provider != null, "Could not determine provider from which settings were loaded");

                SettingsLoadedEventArgs e = new SettingsLoadedEventArgs(provider);
                OnSettingsLoaded(this, e);

                // Note: we need to requery the value here in case someone changed it while
                // handling SettingsLoaded.
                return base[propertyName];
            }
            else
            {
                return base[propertyName];
            }
        }

        /// <summary>
        /// Returns true if this is a clickonce deployed app and this is the first run of the app
        /// since deployment or last upgrade.
        /// </summary>
        private bool IsFirstRunOfClickOnceApp()
        {
            // Never ClickOnce app in CoreFX
            return false;
        }

        /// <summary>
        /// Returns true if this is a clickonce deployed app.
        /// </summary>
        internal static bool IsClickOnceDeployed(AppDomain appDomain)
        {
            // Never ClickOnce app in CoreFX
            return false;
        }

        /// <summary>
        /// Only those settings class properties that have a SettingAttribute on them are 
        /// treated as settings. This routine filters out other properties.
        /// </summary>
        private PropertyInfo[] SettingsFilter(PropertyInfo[] allProps)
        {
            ArrayList settingProps = new ArrayList();
            object[] attributes;
            Attribute attr;

            for (int i = 0; i < allProps.Length; i++)
            {
                attributes = allProps[i].GetCustomAttributes(false);
                for (int j = 0; j < attributes.Length; j++)
                {
                    attr = attributes[j] as Attribute;
                    if (attr is SettingAttribute)
                    {
                        settingProps.Add(allProps[i]);
                        break;
                    }
                }
            }

            return (PropertyInfo[])settingProps.ToArray(typeof(PropertyInfo));
        }

        /// <summary>
        /// Resets the provider collection. This needs to be called when providers change after
        /// first being set.
        /// </summary>
        private void ResetProviders()
        {
            Providers.Clear();

            foreach (SettingsProperty sp in Properties)
            {
                if (Providers[sp.Provider.Name] == null)
                {
                    Providers.Add(sp.Provider);
                }
            }
        }
    }
}
