// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Configuration
{
    public abstract class SettingsBase
    {
        private SettingsPropertyCollection _properties = null;
        private SettingsProviderCollection _providers = null;
        private SettingsPropertyValueCollection _propertyValues = null;
        private SettingsContext _context = null;
        private bool _isSynchronized = false;

        protected SettingsBase()
        {
            _propertyValues = new SettingsPropertyValueCollection();
        }

        public virtual object this[string propertyName]
        {
            get
            {
                if (IsSynchronized)
                {
                    lock (this)
                    {
                        return GetPropertyValueByName(propertyName);
                    }
                }
                else
                {
                    return GetPropertyValueByName(propertyName);
                }
            }
            set
            {
                if (IsSynchronized)
                {
                    lock (this)
                    {
                        SetPropertyValueByName(propertyName, value);
                    }
                }
                else
                {
                    SetPropertyValueByName(propertyName, value);
                }
            }
        }

        private object GetPropertyValueByName(string propertyName)
        {
            if (Properties == null || _propertyValues == null || Properties.Count == 0)
                throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));
            SettingsProperty pp = Properties[propertyName];
            if (pp == null)
                throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));
            SettingsPropertyValue p = _propertyValues[propertyName];
            if (p == null)
            {
                GetPropertiesFromProvider(pp.Provider);
                p = _propertyValues[propertyName];
                if (p == null)
                    throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));
            }
            return p.PropertyValue;
        }

        private void SetPropertyValueByName(string propertyName, object propertyValue)
        {
            if (Properties == null || _propertyValues == null || Properties.Count == 0)
                throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));

            SettingsProperty pp = Properties[propertyName];
            if (pp == null)
                throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));

            if (pp.IsReadOnly)
                throw new SettingsPropertyIsReadOnlyException(SR.Format(SR.SettingsPropertyReadOnly, propertyName));

            if (propertyValue != null && !pp.PropertyType.IsInstanceOfType(propertyValue))
                throw new SettingsPropertyWrongTypeException(SR.Format(SR.SettingsPropertyWrongType, propertyName));

            SettingsPropertyValue p = _propertyValues[propertyName];
            if (p == null)
            {
                GetPropertiesFromProvider(pp.Provider);
                p = _propertyValues[propertyName];
                if (p == null)
                    throw new SettingsPropertyNotFoundException(SR.Format(SR.SettingsPropertyNotFound, propertyName));
            }

            p.PropertyValue = propertyValue;
        }

        public void Initialize(
                SettingsContext context,
                SettingsPropertyCollection properties,
                SettingsProviderCollection providers)
        {
            _context = context;
            _properties = properties;
            _providers = providers;
        }

        public virtual void Save()
        {
            if (IsSynchronized)
            {
                lock (this)
                {
                    SaveCore();
                }
            }
            else
            {
                SaveCore();
            }
        }

        private void SaveCore()
        {
            if (Properties == null || _propertyValues == null || Properties.Count == 0)
                return;

            foreach (SettingsProvider prov in Providers)
            {
                SettingsPropertyValueCollection ppcv = new SettingsPropertyValueCollection();
                foreach (SettingsPropertyValue pp in PropertyValues)
                {
                    if (pp.Property.Provider == prov)
                    {
                        ppcv.Add(pp);
                    }
                }
                if (ppcv.Count > 0)
                {
                    prov.SetPropertyValues(Context, ppcv);
                }
            }
            foreach (SettingsPropertyValue pp in PropertyValues)
                pp.IsDirty = false;
        }

        virtual public SettingsPropertyCollection Properties { get { return _properties; } }
        virtual public SettingsProviderCollection Providers { get { return _providers; } }
        virtual public SettingsPropertyValueCollection PropertyValues { get { return _propertyValues; } }
        virtual public SettingsContext Context { get { return _context; } }

        private void GetPropertiesFromProvider(SettingsProvider provider)
        {
            SettingsPropertyCollection ppc = new SettingsPropertyCollection();
            foreach (SettingsProperty pp in Properties)
            {
                if (pp.Provider == provider)
                {
                    ppc.Add(pp);
                }
            }

            if (ppc.Count > 0)
            {
                SettingsPropertyValueCollection ppcv = provider.GetPropertyValues(Context, ppc);
                foreach (SettingsPropertyValue p in ppcv)
                {
                    if (_propertyValues[p.Name] == null)
                        _propertyValues.Add(p);
                }
            }
        }

        public static SettingsBase Synchronized(SettingsBase settingsBase)
        {
            settingsBase._isSynchronized = true;
            return settingsBase;
        }

        [Browsable(false)]
        public bool IsSynchronized { get { return _isSynchronized; } }
    }
}
