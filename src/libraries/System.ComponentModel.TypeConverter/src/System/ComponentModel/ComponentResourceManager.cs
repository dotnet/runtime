// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// The ComponentResourceManager is a resource manager object that
    /// provides simple functionality for enumerating resources for
    /// a component or object.
    /// </summary>
    public class ComponentResourceManager : ResourceManager
    {
        private Hashtable _resourceSets;
        private CultureInfo _neutralResourcesCulture;

        public ComponentResourceManager() : base() {
        }

        public ComponentResourceManager(Type t) : base(t)
        {
        }


        /// <summary>
        ///     The culture of the main assembly's neutral resources. If someone is asking for this culture's resources,
        ///     we don't need to walk up the parent chain.
        /// </summary>
        private CultureInfo NeutralResourcesCulture
        {
            get
            {
                if (_neutralResourcesCulture == null && MainAssembly != null)
                {
                    _neutralResourcesCulture = GetNeutralResourcesLanguage(MainAssembly);
                }

                return _neutralResourcesCulture;
            }
        }

        /// <summary>
        ///     This method examines all the resources for the current culture.
        ///     When it finds a resource with a key in the format of 
        ///     &quot;[objectName].[property name]&quot; it will apply that resource's value
        ///     to the corresponding property on the object.  If there is no matching
        ///     property the resource will be ignored.
        /// </summary>
        public void ApplyResources(object value, string objectName)
        {
            ApplyResources(value, objectName, null);
        }

        /// <summary>
        ///     This method examines all the resources for the provided culture.
        ///     When it finds a resource with a key in the format of 
        ///     &quot[objectName].[property name]&quot; it will apply that resource's value
        ///     to the corresponding property on the object.  If there is no matching
        ///     property the resource will be ignored.
        /// </summary>
        public virtual void ApplyResources(object value, string objectName, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (objectName == null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }
            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            // The general case here will be to always use the same culture, so optimize for
            // that.  The resourceSets hashtable uses culture as a key.  It's value is
            // a sorted dictionary that contains ALL the culture values (so it traverses up
            // the parent culture chain) for that culture.  This means that if ApplyResources
            // is called with different cultures there could be some redundancy in the
            // table, but it allows the normal case of calling with a single culture to 
            // be much faster.
            //

            // The reason we use a SortedDictionary here is to ensure the resources are applied
            // in an order consistent with codedom deserialization. 
            SortedList<string, object> resources;

            if (_resourceSets == null)
            {
                ResourceSet dummy;
                _resourceSets = new Hashtable();
                resources = FillResources(culture, out dummy);
                _resourceSets[culture] = resources;
            }
            else
            {
                resources = (SortedList<string, object>)_resourceSets[culture];
                if (resources == null || (resources.Comparer.Equals(StringComparer.OrdinalIgnoreCase) != IgnoreCase))
                {
                    ResourceSet dummy;
                    resources = FillResources(culture, out dummy);
                    _resourceSets[culture] = resources;
                }
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance;
            if (IgnoreCase)
            {
                flags |= BindingFlags.IgnoreCase;
            }

            bool componentReflect = false;
            if (value is IComponent)
            {
                ISite site = ((IComponent)value).Site;
                if (site != null && site.DesignMode)
                {
                    componentReflect = true;
                }
            }

            foreach (KeyValuePair<string, object> kvp in resources)
            {
                // See if this key matches our object.
                //
                string key = kvp.Key;
                if (key == null)
                {
                    continue;
                }

                if (IgnoreCase)
                {
                    if (string.Compare(key, 0, objectName, 0, objectName.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (string.CompareOrdinal(key, 0, objectName, 0, objectName.Length) != 0)
                    {
                        continue;
                    }
                }

                // Character after objectName.Length should be a ".", or else we should continue.
                //
                int idx = objectName.Length;
                if (key.Length <= idx || key[idx] != '.')
                {
                    continue;
                }

                // Bypass type descriptor if we are not in design mode.  TypeDescriptor does an attribute
                // scan which is quite expensive.
                //
                string propName = key.Substring(idx + 1);

                if (componentReflect)
                {
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(value).Find(propName, IgnoreCase);

                    if (prop != null && !prop.IsReadOnly && (kvp.Value == null || prop.PropertyType.IsInstanceOfType(kvp.Value)))
                    {
                        prop.SetValue(value, kvp.Value);
                    }
                }
                else
                {
                    PropertyInfo prop = null;

                    try
                    {
                        prop = value.GetType().GetProperty(propName, flags);
                    }
                    catch (AmbiguousMatchException)
                    {
                        // Looks like we ran into a conflict between a declared property and an inherited one.
                        // In such cases, we choose the most declared one.
                        Type t = value.GetType();
                        do
                        {
                            prop = t.GetProperty(propName, flags | BindingFlags.DeclaredOnly);
                            t = t.BaseType;
                        } while (prop == null && t != null && t != typeof(object));
                    }

                    if (prop != null && prop.CanWrite && (kvp.Value == null || prop.PropertyType.IsInstanceOfType(kvp.Value)))
                    {
                        prop.SetValue(value, kvp.Value, null);
                    }
                }
            }
        }

        /// <summary>
        ///     Recursive routine that creates a resource hashtable
        ///     populated with resources for culture and all parent
        ///     cultures.
        /// </summary>
        private SortedList<string, object> FillResources(CultureInfo culture, out ResourceSet resourceSet)
        {
            SortedList<string, object> sd;
            ResourceSet parentResourceSet = null;

            // Traverse parents first, so we always replace more
            // specific culture values with less specific.
            //
            if (!culture.Equals(CultureInfo.InvariantCulture) && !culture.Equals(NeutralResourcesCulture))
            {
                sd = FillResources(culture.Parent, out parentResourceSet);
            }
            else
            {
                // We're at the bottom, so create the sorted dictionary
                // 
                if (IgnoreCase)
                {
                    sd = new SortedList<string, object>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    sd = new SortedList<string, object>(StringComparer.Ordinal);
                }
            }

            // Now walk culture's resource set.  Another thing we
            // do here is ask ResourceManager to traverse up the 
            // parent chain.  We do NOT want to do this because
            // we are trawling up the parent chain ourselves, but by
            // passing in true for the second parameter the resource
            // manager will cache the culture it did find, so when we 
            // do recurse all missing resources will be filled in
            // so we are very fast.  That's why we remember what our
            // parent resource set's instance was -- if they are the
            // same, we're looking at a cache we've already applied.
            //
            resourceSet = GetResourceSet(culture, true, true);
            if (resourceSet != null && !object.ReferenceEquals(resourceSet, parentResourceSet))
            {
                foreach (DictionaryEntry de in resourceSet)
                {
                    sd[(string)de.Key] = de.Value;
                }
            }

            return sd;
        }
    }
}
