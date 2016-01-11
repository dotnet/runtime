// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using Internal.Runtime.Augments;
using System.Diagnostics;

namespace System.Resources
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ResourceManager
    {
        private Object _resourceMap;
        private string _resourcesSubtree;

        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
                throw new ArgumentNullException("resourceSource");
            Contract.EndContractBlock();

            // Portable libraries resources are indexed under the the type full name 
            _resourcesSubtree = resourceSource.FullName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public ResourceManager(String baseName, Assembly assembly)
        {
            if (null == baseName)
                throw new ArgumentNullException("baseName");

            if (null == assembly)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();

            _resourcesSubtree = baseName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public ResourceManager(string resourcesName)
        {
            _resourcesSubtree = resourcesName;
            _resourceMap = WinRTInterop.Callbacks.GetResourceMap(_resourcesSubtree);
        }

        public String GetString(String name)
        {
            return GetString(name, null);
        }

        // Looks up a resource value for a particular name.  Looks in the 
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        // 
        public virtual String GetString(String name, CultureInfo culture)
        {
            if (null == name)
                throw new ArgumentNullException("name");

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
            Contract.EndContractBlock();

            return WinRTInterop.Callbacks.GetResourceString(_resourceMap, name, culture == null ? null : culture.Name);
        }
    }
}
