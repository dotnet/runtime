// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Configuration
{
    public sealed class PropertyInformation
    {
        private const string LockAll = "*";
        private ConfigurationProperty _prop;

        private readonly ConfigurationElement _thisElement;

        internal PropertyInformation(ConfigurationElement thisElement, string propertyName)
        {
            Name = propertyName;
            _thisElement = thisElement;
        }

        private ConfigurationProperty Prop => _prop ?? (_prop = _thisElement.Properties[Name]);


        public string Name { get; }


        internal string ProvidedName => Prop.ProvidedName;


        public object Value
        {
            get { return _thisElement[Name]; }
            set { _thisElement[Name] = value; }
        }

        public object DefaultValue => Prop.DefaultValue;

        public PropertyValueOrigin ValueOrigin
        {
            get
            {
                if (_thisElement.Values[Name] == null) return PropertyValueOrigin.Default;
                return _thisElement.Values.IsInherited(Name) ? PropertyValueOrigin.Inherited : PropertyValueOrigin.SetHere;
            }
        }

        public bool IsModified => _thisElement.Values[Name] != null && _thisElement.Values.IsModified(Name);

        public bool IsKey => Prop.IsKey;

        public bool IsRequired => Prop.IsRequired;

        public bool IsLocked =>
            ((_thisElement.LockedAllExceptAttributesList != null) &&
            !_thisElement.LockedAllExceptAttributesList.DefinedInParent(Name)) ||
            ((_thisElement.LockedAttributesList != null) &&
            (_thisElement.LockedAttributesList.DefinedInParent(Name) ||
            _thisElement.LockedAttributesList.DefinedInParent(LockAll))) ||
            (((_thisElement.ItemLocked & ConfigurationValueFlags.Locked) != 0) &&
            ((_thisElement.ItemLocked & ConfigurationValueFlags.Inherited) != 0));


        public string Source
        {
            get
            {
                PropertySourceInfo psi = _thisElement.Values.GetSourceInfo(Name) ??
                    _thisElement.Values.GetSourceInfo(string.Empty);
                return psi == null ? string.Empty : psi.FileName;
            }
        }

        /// <summary>
        /// Line number or 0 if there is no source.
        /// </summary>
        public int LineNumber
        {
            get
            {
                PropertySourceInfo psi = _thisElement.Values.GetSourceInfo(Name) ??
                    _thisElement.Values.GetSourceInfo(string.Empty);
                return psi?.LineNumber ?? 0;
            }
        }

        public Type Type => Prop.Type;

        public ConfigurationValidatorBase Validator => Prop.Validator;

        public TypeConverter Converter => Prop.Converter;

        public string Description => Prop.Description;
    }
}