// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;

namespace System.Configuration.Provider
{
    public abstract class ProviderBase
    {
        private string _description;
        private bool _initialized;

        private string _name;

        public virtual string Name => _name;

        public virtual string Description => string.IsNullOrEmpty(_description) ? Name : _description;

        public virtual void Initialize(string name, NameValueCollection config)
        {
            lock (this)
            {
                if (_initialized)
                    throw new InvalidOperationException(SR.Provider_Already_Initialized);
                _initialized = true;
            }

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Config_provider_name_null_or_empty, nameof(name));

            _name = name;
            if (config != null)
            {
                _description = config["description"];
                config.Remove("description");
            }
        }
    }
}