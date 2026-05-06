// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;

namespace System.Configuration
{
    internal sealed class KeyValueInternalCollection : NameValueCollection
    {
        private readonly AppSettingsSection _root;

        public KeyValueInternalCollection(AppSettingsSection root)
        {
            _root = root;
            foreach (KeyValueConfigurationElement element in _root.Settings) base.Add(element.Key, element.Value);
        }

        public override void Add(string key, string value)
        {
            _root.Settings.Add(new KeyValueConfigurationElement(key, value));
            base.Add(key, value);
        }

        public override void Clear()
        {
            _root.Settings.Clear();
            base.Clear();
        }

        public override void Remove(string key)
        {
            _root.Settings.Remove(key);
            base.Remove(key);
        }
    }
}
