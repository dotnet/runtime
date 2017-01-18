// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Configuration
{
    public sealed class ElementInformation
    {
        private readonly ConfigurationElement _thisElement;
        private ConfigurationException[] _errors;
        private PropertyInformationCollection _internalProperties;

        internal ElementInformation(ConfigurationElement thisElement)
        {
            _thisElement = thisElement;
        }

        public PropertyInformationCollection Properties
            => _internalProperties ?? (_internalProperties = new PropertyInformationCollection(_thisElement));

        public bool IsPresent => _thisElement.ElementPresent;

        public bool IsLocked => ((_thisElement.ItemLocked & ConfigurationValueFlags.Locked) != 0) &&
            ((_thisElement.ItemLocked & ConfigurationValueFlags.Inherited) != 0);

        public bool IsCollection
        {
            get
            {
                ConfigurationElementCollection collection = _thisElement as ConfigurationElementCollection;
                if ((collection == null) && (_thisElement.Properties.DefaultCollectionProperty != null))
                {
                    // this is not a collection but it may contain a default collection
                    collection =
                        _thisElement[_thisElement.Properties.DefaultCollectionProperty] as
                            ConfigurationElementCollection;
                }

                return collection != null;
            }
        }

        public string Source => _thisElement.Values.GetSourceInfo(_thisElement.ElementTagName)?.FileName;

        /// <summary>
        /// The line number or 0 if no source.
        /// </summary>
        public int LineNumber => _thisElement.Values.GetSourceInfo(_thisElement.ElementTagName)?.LineNumber ?? 0;

        public Type Type => _thisElement.GetType();

        public ConfigurationValidatorBase Validator => _thisElement.ElementProperty.Validator;

        public ICollection Errors => _errors ?? (_errors = GetReadOnlyErrorsList());

        // Internal method to fix SetRawXML defect...
        internal PropertySourceInfo PropertyInfoInternal()
        {
            return _thisElement.PropertyInfoInternal(_thisElement.ElementTagName);
        }

        internal void ChangeSourceAndLineNumber(PropertySourceInfo sourceInformation)
        {
            _thisElement.Values.ChangeSourceInfo(_thisElement.ElementTagName, sourceInformation);
        }

        private ConfigurationException[] GetReadOnlyErrorsList()
        {
            int count;

            ArrayList arrayList = _thisElement.GetErrorsList();
            count = arrayList.Count;

            // Create readonly array
            ConfigurationException[] exceptionList = new ConfigurationException[arrayList.Count];

            if (count != 0) arrayList.CopyTo(exceptionList, 0);

            return exceptionList;
        }
    }
}