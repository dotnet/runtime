// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection
{
    //
    // Common logic for computing the effective set of custom attributes on a reflection element of type E where
    // E is a MemberInfo, ParameterInfo, Assembly or Module.
    //
    // This class is only used by the CustomAttributeExtensions class - hence, we bake in the CustomAttributeExtensions behavior of
    // filtering out WinRT attributes.
    //
    internal abstract class CustomAttributeSearcher<E>
        where E : class
    {
        //
        // Returns the effective set of custom attributes on a reflection element.
        //
        public IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(E element, Type optionalAttributeTypeFilter, bool inherit, bool skipTypeValidation = false)
        {
            // Do all parameter validation here before we enter the iterator function (so that exceptions from validations
            // show up immediately rather than on the first MoveNext()).
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            bool typeFilterKnownToBeSealed = false;
            if (!skipTypeValidation)
            {
                if (optionalAttributeTypeFilter == null)
                    throw new ArgumentNullException("type");
                TypeInfo attributeTypeFilterInfo = optionalAttributeTypeFilter.GetTypeInfo();
                if (!(optionalAttributeTypeFilter.Equals(CommonRuntimeTypes.Attribute) ||
                      attributeTypeFilterInfo.IsSubclassOf(CommonRuntimeTypes.Attribute)))
                    throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

                try
                {
                    typeFilterKnownToBeSealed = attributeTypeFilterInfo.IsSealed;
                }
                catch (MissingMetadataException)
                {
                    // If we got here, the custom attribute type itself was not opted into metadata. This can and does happen in the real world when an app
                    // contains a check for custom attributes that never actually appear on any entity within the app.
                    //
                    // Since "typeFilterKnownToBeSealed" is only used to enable an optimization, it's always safe to leave it "false".
                    //
                    // Because the Project N toolchain removes any custom attribute that refuses to opt into metadata so at this point,
                    // we could simply return an empty enumeration and "be correct." However, the code paths following this already do that naturally.
                    // (i.e. the "passFilter" will never return true, thus we will never attempt to query the custom attribute type for its
                    // own AttributeUsage custom attribute.) If the toolchain behavior changes in the future, it's preferable that 
                    // this shows up as new MissingMetadataExceptions rather than incorrect results from the api so we will not put 
                    // in an explicit return here. 
                }
            }

            Func<Type, bool> passesFilter;
            if (optionalAttributeTypeFilter == null)
            {
                passesFilter =
                    delegate (Type actualType)
                    {
                        return true;
                    };
            }
            else
            {
                passesFilter =
                    delegate (Type actualType)
                    {
                        if (optionalAttributeTypeFilter.Equals(actualType))
                            return true;
                        if (typeFilterKnownToBeSealed)
                            return false;
                        return actualType.GetTypeInfo().IsSubclassOf(optionalAttributeTypeFilter);
                    };
            }

            return GetMatchingCustomAttributesIterator(element, passesFilter, inherit);
        }

        //
        // Subclasses should override this to compute the "parent" of the element for the purpose of finding "inherited" custom attributes.
        // Return null if no parent.
        //
        public virtual E GetParent(E e)
        {
            return null;
        }


        //
        // Main iterator.
        //
        private IEnumerable<CustomAttributeData> GetMatchingCustomAttributesIterator(E element, Func<Type, bool> rawPassesFilter, bool inherit)
        {
            Func<Type, bool> passesFilter =
                delegate (Type attributeType)
                {
                    // Windows prohibits instantiating WinRT custom attributes. Filter them from the search as the desktop CLR does.
                    TypeAttributes typeAttributes = attributeType.GetTypeInfo().Attributes;
                    if (0 != (typeAttributes & TypeAttributes.WindowsRuntime))
                        return false;
                    return rawPassesFilter(attributeType);
                };

            LowLevelList<CustomAttributeData> immediateResults = new LowLevelList<CustomAttributeData>();
            foreach (CustomAttributeData cad in GetDeclaredCustomAttributes(element))
            {
                if (passesFilter(cad.AttributeType))
                {
                    yield return cad;
                    immediateResults.Add(cad);
                }
            }
            if (inherit)
            {
                // Because the "inherit" parameter defaults to "true", we probably get here for a lot of elements that
                // don't actually have any inheritance chains. Try to avoid doing any unnecessary setup for the inheritance walk
                // unless we have to.
                element = GetParent(element);
                if (element != null)
                {
                    // This dictionary serves two purposes:
                    //   - Let us know which attribute types we've encountered at lower levels so we can block them from appearing twice in the results
                    //     if appropriate.
                    //
                    //   - Cache the results of retrieving the usage attribute.
                    //
                    LowLevelDictionary<Type, AttributeUsageAttribute> encounteredTypes = new LowLevelDictionary<Type, AttributeUsageAttribute>(11);

                    for (int i = 0; i < immediateResults.Count; i++)
                    {
                        Type attributeType = immediateResults[i].AttributeType;
                        AttributeUsageAttribute usage;
                        if (!encounteredTypes.TryGetValue(attributeType, out usage))
                            encounteredTypes.Add(attributeType, null);
                    }

                    do
                    {
                        foreach (CustomAttributeData cad in GetDeclaredCustomAttributes(element))
                        {
                            Type attributeType = cad.AttributeType;
                            if (!passesFilter(attributeType))
                                continue;
                            AttributeUsageAttribute usage;
                            if (!encounteredTypes.TryGetValue(attributeType, out usage))
                            {
                                // Type was not encountered before. Only include it if it is inheritable.
                                usage = GetAttributeUsage(attributeType);
                                encounteredTypes.Add(attributeType, usage);
                                if (usage.Inherited)
                                    yield return cad;
                            }
                            else
                            {
                                if (usage == null)
                                    usage = GetAttributeUsage(attributeType);
                                encounteredTypes[attributeType] = usage;
                                // Type was encountered at a lower level. Only include it if its inheritable AND allowMultiple.
                                if (usage.Inherited && usage.AllowMultiple)
                                    yield return cad;
                            }
                        }
                    }
                    while ((element = GetParent(element)) != null);
                }
            }
        }

        //
        // Internal helper to compute the AttributeUsage. This must be coded specially to avoid an infinite recursion.
        //
        private AttributeUsageAttribute GetAttributeUsage(Type attributeType)
        {
            // This is only invoked when the seacher is called with "inherit: true", thus calling the searcher again
            // with "inherit: false" will not cause infinite recursion.
            //
            // Legacy: Why aren't we checking the parent types? Answer: Although AttributeUsageAttribute is itself marked inheritable, desktop Reflection
            // treats it as *non*-inheritable for the purpose of deciding whether another attribute class is inheritable.
            // This behavior goes all the way back to at least 3.5 (and perhaps earlier). For compat reasons,
            // we won't-fixed this in 4.5 and we won't-fix this in Project N.
            //
            AttributeUsageAttribute usage = attributeType.GetTypeInfo().GetCustomAttribute<AttributeUsageAttribute>(inherit: false);
            if (usage == null)
                return new AttributeUsageAttribute(AttributeTargets.All) { AllowMultiple = false, Inherited = true };
            return usage;
        }

        //
        // Subclasses must implement this to call the element-specific CustomAttributes property.
        //
        protected abstract IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(E element);
    }
}
