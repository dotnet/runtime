// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    internal abstract class ConfigurationElementCollection : ConfigurationElement
    {
        protected abstract ConfigurationElement CreateNewElement();
        protected abstract Object GetElementKey(ConfigurationElement element);

        protected virtual void BaseAdd(ConfigurationElement element)
        {
        }

        protected virtual void BaseAdd(int index, ConfigurationElement element)
        {
        }

        protected int BaseIndexOf(ConfigurationElement element)
        {
            return -1;
        }

        protected internal void BaseRemove(Object key)
        {
        }

        protected internal void BaseRemoveAt(int index)
        {
        }

        protected internal void BaseClear()
        {
        }
        protected internal ConfigurationElement BaseGet(Object key)
        {
            return null;
        }
    }
}
