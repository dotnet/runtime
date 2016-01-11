// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Internal.Runtime.Augments;

namespace System.Resources
{
    public class ResourceManager
    {
        private readonly object _resourceMap;
        private readonly string _resourcesSubtree;

        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
            {
                throw new ArgumentNullException("resourceSource");
            }

            // Portable libraries resources are indexed under the the type full name 
            _resourcesSubtree = resourceSource.FullName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public ResourceManager(string baseName, Assembly assembly)
        {
            if (null == baseName)
            {
                throw new ArgumentNullException("baseName");
            }
            if (null == assembly)
            {
                throw new ArgumentNullException("assembly");
            }

            _resourcesSubtree = baseName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public ResourceManager(string resourcesName)
        {
            _resourcesSubtree = resourcesName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public string GetString(string name)
        {
            return GetString(name, null);
        }

        // Looks up a resource value for a particular name.  Looks in the 
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        // 
        public virtual string GetString(string name, CultureInfo culture)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (WinRTInterop.Callbacks.IsAppxModel() && _resourceMap == null)
            {
                if (name.Equals("MissingManifestResource_ResWFileNotLoaded") &&
                    _resourcesSubtree.Equals("FxResources.System.Resources.ResourceManager.SR", StringComparison.OrdinalIgnoreCase))
                {
                    // If we get here, means we are missing the resources even for the message MissingManifestResource_ResWFileNotLoaded
                    // it is important to throw the exception using the hardcoded message to prevent the stack overflow from occurring.
                    throw new MissingManifestResourceException("Unable to load resources for resource file " + _resourcesSubtree + ".");
                }
                throw new MissingManifestResourceException(SR.Format(SR.MissingManifestResource_ResWFileNotLoaded, _resourcesSubtree));
            }

            return WinRTInterop.Callbacks.GetResourceString(_resourceMap, name, culture == null ? null : culture.Name);
        }
    }
}
