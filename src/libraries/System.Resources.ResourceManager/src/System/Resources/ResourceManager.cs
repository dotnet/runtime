// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Internal.Runtime.Augments;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources.Core;

namespace System.Resources
{
    public class ResourceManager
    {
        private readonly ResourceMap _resourceMap;
        private readonly string _resourcesSubtree;
        private readonly string _neutralResourcesCultureName;

        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            // Portable libraries resources are indexed under the the type full name 
            _resourcesSubtree = resourceSource.FullName;
            _resourceMap = GetResourceMap(_resourcesSubtree);
        }

        public ResourceManager(string baseName, Assembly assembly)
        {
            if (null == baseName)
            {
                throw new ArgumentNullException(nameof(baseName));
            }
            if (null == assembly)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _resourcesSubtree = baseName;
            _resourceMap = GetResourceMap(_resourcesSubtree);
            _neutralResourcesCultureName = GetNeutralLanguageForAssembly(assembly);
        }

        public ResourceManager(string resourcesName)
        {
            _resourcesSubtree = resourcesName;
            _resourceMap = GetResourceMap(_resourcesSubtree);
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
                throw new ArgumentNullException(nameof(name));
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

            return GetResourceString(name, culture == null ? null : culture.Name);
        }
        
        private static string GetNeutralLanguageForAssembly(Assembly assembly)
        {
            foreach (CustomAttributeData cad in assembly.CustomAttributes)
            {
                if (cad.AttributeType.FullName.Equals("System.Resources.NeutralResourcesLanguageAttribute"))
                {
                    foreach (CustomAttributeTypedArgument cata in cad.ConstructorArguments)
                    {
                        if (cata.ArgumentType.Equals(typeof(System.String)))
                        {
                            return (string) cata.Value;
                        }
                    }
                }
            }
            
            // The assembly is not tagged with NeutralResourcesLanguageAttribute
            return null;
        }
        
        //
        // WinRT Wrappers
        //

        private ResourceMap GetResourceMap(string subtreeName)
        {
            if (WinRTInterop.Callbacks.IsAppxModel())
                return Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree(subtreeName);

            return null;
        }

        private string GetResourceString(string resourceName, string languageName)
        {
            if (!WinRTInterop.Callbacks.IsAppxModel())
            {
                // on desktop we usually don't have resource strings. so we just return the key name
                return resourceName;
            }

            if (_resourceMap == null)
                return null;

            ResourceContext context;
            ResourceCandidate candidate;

            if (languageName == null && _neutralResourcesCultureName == null)
            {
                candidate = _resourceMap.GetValue(resourceName);
            }
            else
            {
                context = new ResourceContext();
                context.QualifierValues["language"] = (languageName != null ? languageName + ";" : "") + 
                                                      (_neutralResourcesCultureName != null ? _neutralResourcesCultureName : ""); 
                candidate = _resourceMap.GetValue(resourceName, context);
            }

            return candidate == null ? null : candidate.ValueAsString;
        }
    }
}
