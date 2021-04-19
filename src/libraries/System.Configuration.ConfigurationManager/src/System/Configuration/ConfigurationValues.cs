// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.Configuration
{
    internal sealed class ConfigurationValues : NameObjectCollectionBase
    {
        private static volatile IEnumerable s_emptyCollection;
        private BaseConfigurationRecord _configRecord;
        private volatile bool _containsElement;
        private volatile bool _containsInvalidValue;

        internal ConfigurationValues() : base(StringComparer.Ordinal) { }

        internal object this[string key]
        {
            get
            {
                ConfigurationValue configValue = GetConfigValue(key);
                return configValue?.Value;
            }
            set { SetValue(key, value, ConfigurationValueFlags.Modified, null); }
        }

        internal object this[int index]
        {
            get
            {
                ConfigurationValue configValue = GetConfigValue(index);
                return configValue?.Value;
            }
        }

        internal object SyncRoot => this;

        internal IEnumerable ConfigurationElements
            => _containsElement ? new ConfigurationElementsCollection(this) : EmptyCollectionInstance;

        internal IEnumerable InvalidValues
            => _containsInvalidValue ? new InvalidValuesCollection(this) : EmptyCollectionInstance;

        private static IEnumerable EmptyCollectionInstance
            => s_emptyCollection ?? (s_emptyCollection = new EmptyCollection());

        internal void AssociateContext(BaseConfigurationRecord configRecord)
        {
            _configRecord = configRecord;

            // Associate with children
            foreach (ConfigurationElement currentElement in ConfigurationElements)
                currentElement.AssociateContext(_configRecord);
        }

        internal bool Contains(string key)
        {
            return BaseGet(key) != null;
        }

        internal string GetKey(int index)
        {
            return BaseGetKey(index);
        }

        internal ConfigurationValue GetConfigValue(string key)
        {
            return (ConfigurationValue)BaseGet(key);
        }

        internal ConfigurationValue GetConfigValue(int index)
        {
            return (ConfigurationValue)BaseGet(index);
        }

        internal PropertySourceInfo GetSourceInfo(string key)
        {
            ConfigurationValue configurationValue = GetConfigValue(key);
            return configurationValue?.SourceInfo;
        }

        internal void ChangeSourceInfo(string key, PropertySourceInfo sourceInfo)
        {
            ConfigurationValue configurationValue = GetConfigValue(key);
            if (configurationValue != null) configurationValue.SourceInfo = sourceInfo;
        }

        private ConfigurationValue CreateConfigValue(object value, ConfigurationValueFlags valueFlags,
            PropertySourceInfo sourceInfo)
        {
            if (value != null)
            {
                if (value is ConfigurationElement)
                {
                    _containsElement = true;
                    ((ConfigurationElement)value).AssociateContext(_configRecord);
                }
                else
                {
                    if (value is InvalidPropValue) _containsInvalidValue = true;
                }
            }

            ConfigurationValue configValue = new ConfigurationValue(value, valueFlags, sourceInfo);
            return configValue;
        }

        internal void SetValue(string key, object value, ConfigurationValueFlags valueFlags,
            PropertySourceInfo sourceInfo)
        {
            ConfigurationValue configValue = CreateConfigValue(value, valueFlags, sourceInfo);
            BaseSet(key, configValue);
        }

        internal void Clear()
        {
            BaseClear();
        }

        internal ConfigurationValueFlags RetrieveFlags(string key)
        {
            ConfigurationValue configurationValue = (ConfigurationValue)BaseGet(key);
            return configurationValue?.ValueFlags ?? ConfigurationValueFlags.Default;
        }

        internal bool IsModified(string key)
        {
            ConfigurationValue configurationValue = (ConfigurationValue)BaseGet(key);
            if (configurationValue != null)
                return (configurationValue.ValueFlags & ConfigurationValueFlags.Modified) != 0;
            return false;
        }

        internal bool IsInherited(string key)
        {
            ConfigurationValue configurationValue = (ConfigurationValue)BaseGet(key);
            if (configurationValue != null)
                return (configurationValue.ValueFlags & ConfigurationValueFlags.Inherited) != 0;
            return false;
        }

        private sealed class EmptyCollection : IEnumerable
        {
            private readonly IEnumerator _emptyEnumerator;

            internal EmptyCollection()
            {
                _emptyEnumerator = new EmptyCollectionEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _emptyEnumerator;
            }

            private sealed class EmptyCollectionEnumerator : IEnumerator
            {
                bool IEnumerator.MoveNext()
                {
                    return false;
                }

                object IEnumerator.Current => null;

                void IEnumerator.Reset() { }
            }
        }

        private sealed class ConfigurationElementsCollection : IEnumerable
        {
            private readonly ConfigurationValues _values;

            internal ConfigurationElementsCollection(ConfigurationValues values)
            {
                _values = values;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_values._containsElement)
                {
                    for (int index = 0; index < _values.Count; index++)
                    {
                        object value = _values[index];
                        if (value is ConfigurationElement) yield return value;
                    }
                }
            }
        }

        private sealed class InvalidValuesCollection : IEnumerable
        {
            private readonly ConfigurationValues _values;

            internal InvalidValuesCollection(ConfigurationValues values)
            {
                _values = values;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_values._containsInvalidValue)
                {
                    for (int index = 0; index < _values.Count; index++)
                    {
                        object value = _values[index];
                        if (value is InvalidPropValue) yield return value;
                    }
                }
            }
        }
    }
}
