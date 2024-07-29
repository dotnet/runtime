// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

//==================================================================================================================
// Dependency note:
//   This class must depend only on the CustomAttribute properties that return IEnumerable<CustomAttributeData>.
//   All of the other custom attribute api route back here so calls to them will cause an infinite recursion.
//==================================================================================================================

namespace Internal.Reflection.Extensions.NonPortable
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
            ArgumentNullException.ThrowIfNull(element);

            bool typeFilterKnownToBeSealed = false;
            if (!skipTypeValidation)
            {
                ArgumentNullException.ThrowIfNull(optionalAttributeTypeFilter, "type");
                if (!(optionalAttributeTypeFilter == typeof(Attribute) ||
                      optionalAttributeTypeFilter.IsSubclassOf(typeof(Attribute))))
                    throw new ArgumentException(SR.Argument_MustHaveAttributeBaseClass);

                typeFilterKnownToBeSealed = optionalAttributeTypeFilter.IsSealed;
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
            else if (optionalAttributeTypeFilter.IsGenericTypeDefinition)
            {
                passesFilter =
                    delegate (Type actualType)
                    {
                        if (actualType.IsConstructedGenericType && actualType.GetGenericTypeDefinition() == optionalAttributeTypeFilter)
                        {
                            return true;
                        }

                        if (!typeFilterKnownToBeSealed)
                        {
                            for (Type? type = actualType.BaseType; type != null; type = type.BaseType)
                            {
                                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == optionalAttributeTypeFilter)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
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
                        return optionalAttributeTypeFilter.IsAssignableFrom(actualType);
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
                    TypeAttributes typeAttributes = attributeType.Attributes;
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
                    LowLevelDictionary<TypeUnificationKey, AttributeUsageAttribute> encounteredTypes = new LowLevelDictionary<TypeUnificationKey, AttributeUsageAttribute>(11);

                    for (int i = 0; i < immediateResults.Count; i++)
                    {
                        Type attributeType = immediateResults[i].AttributeType;
                        TypeUnificationKey attributeTypeKey = new TypeUnificationKey(attributeType);
                        if (!encounteredTypes.TryGetValue(attributeTypeKey, out _))
                            encounteredTypes.Add(attributeTypeKey, null);
                    }

                    do
                    {
                        foreach (CustomAttributeData cad in GetDeclaredCustomAttributes(element))
                        {
                            Type attributeType = cad.AttributeType;
                            if (!passesFilter(attributeType))
                                continue;
                            AttributeUsageAttribute? usage;
                            TypeUnificationKey attributeTypeKey = new TypeUnificationKey(attributeType);
                            if (!encounteredTypes.TryGetValue(attributeTypeKey, out usage))
                            {
                                // Type was not encountered before. Only include it if it is inheritable.
                                usage = GetAttributeUsage(attributeType);
                                encounteredTypes.Add(attributeTypeKey, usage);
                                if (usage.Inherited)
                                    yield return cad;
                            }
                            else
                            {
                                usage ??= GetAttributeUsage(attributeType);
                                encounteredTypes[attributeTypeKey] = usage;
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
        private static AttributeUsageAttribute GetAttributeUsage(Type attributeType)
        {
            // This is only invoked when the seacher is called with "inherit: true", thus calling the searcher again
            // with "inherit: false" will not cause infinite recursion.
            //
            // Legacy: Why aren't we checking the parent types? Answer: Although AttributeUsageAttribute is itself marked inheritable, desktop Reflection
            // treats it as *non*-inheritable for the purpose of deciding whether another attribute class is inheritable.
            //
            AttributeUsageAttribute? usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>(inherit: false);
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
