// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    internal abstract class ConfigurationElement
    {
        protected internal Object this[String propertyName]
        {
            get { return null; }
            set { }
        }

        protected virtual ConfigurationPropertyCollection Properties
        {
            get { return null; }
        }

        protected virtual void InitializeDefault()
        {
        }
    }
}
