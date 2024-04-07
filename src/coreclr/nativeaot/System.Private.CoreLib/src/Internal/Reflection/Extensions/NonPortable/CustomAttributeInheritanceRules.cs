// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

using Internal.Reflection.Augments;

//==================================================================================================================
// Dependency note:
//   This class must depend only on the CustomAttribute properties that return IEnumerable<CustomAttributeData>.
//   All of the other custom attribute api route back here so calls to them will cause an infinite recursion.
//==================================================================================================================

namespace Internal.Reflection.Extensions.NonPortable
{
    internal static class CustomAttributeInheritanceRules
    {
        //==============================================================================================================================
        // Api helpers: Computes the effective set of custom attributes for various Reflection elements and returns them
        //              as CustomAttributeData objects.
        //==============================================================================================================================
        public static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(this Assembly element, Type optionalAttributeTypeFilter, bool skipTypeValidation = false)
        {
            return AssemblyCustomAttributeSearcher.Default.GetMatchingCustomAttributes(element, optionalAttributeTypeFilter, inherit: false, skipTypeValidation: skipTypeValidation);
        }

        public static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(this Module element, Type optionalAttributeTypeFilter, bool skipTypeValidation = false)
        {
            return ModuleCustomAttributeSearcher.Default.GetMatchingCustomAttributes(element, optionalAttributeTypeFilter, inherit: false, skipTypeValidation: skipTypeValidation);
        }

        public static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(this ParameterInfo element, Type optionalAttributeTypeFilter, bool inherit, bool skipTypeValidation = false)
        {
            return ParameterCustomAttributeSearcher.Default.GetMatchingCustomAttributes(element, optionalAttributeTypeFilter, inherit, skipTypeValidation: skipTypeValidation);
        }

        public static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(this MemberInfo element, Type optionalAttributeTypeFilter, bool inherit, bool skipTypeValidation = false)
        {
            {
                Type? type = element as Type;
                if (type != null)
                    return TypeCustomAttributeSearcher.Default.GetMatchingCustomAttributes(type, optionalAttributeTypeFilter, inherit, skipTypeValidation: skipTypeValidation);
            }
            {
                ConstructorInfo? constructorInfo = element as ConstructorInfo;
                if (constructorInfo != null)
                    return ConstructorCustomAttributeSearcher.Default.GetMatchingCustomAttributes(constructorInfo, optionalAttributeTypeFilter, inherit: false, skipTypeValidation: skipTypeValidation);
            }
            {
                MethodInfo? methodInfo = element as MethodInfo;
                if (methodInfo != null)
                    return MethodCustomAttributeSearcher.Default.GetMatchingCustomAttributes(methodInfo, optionalAttributeTypeFilter, inherit, skipTypeValidation: skipTypeValidation);
            }
            {
                FieldInfo? fieldInfo = element as FieldInfo;
                if (fieldInfo != null)
                    return FieldCustomAttributeSearcher.Default.GetMatchingCustomAttributes(fieldInfo, optionalAttributeTypeFilter, inherit: false, skipTypeValidation: skipTypeValidation);
            }
            {
                PropertyInfo? propertyInfo = element as PropertyInfo;
                if (propertyInfo != null)
                    return PropertyCustomAttributeSearcher.Default.GetMatchingCustomAttributes(propertyInfo, optionalAttributeTypeFilter, inherit, skipTypeValidation: skipTypeValidation);
            }
            {
                EventInfo? eventInfo = element as EventInfo;
                if (eventInfo != null)
                    return EventCustomAttributeSearcher.Default.GetMatchingCustomAttributes(eventInfo, optionalAttributeTypeFilter, inherit, skipTypeValidation: skipTypeValidation);
            }

            ArgumentNullException.ThrowIfNull(element);

            throw new NotSupportedException(); // Shouldn't get here.
        }




        //==============================================================================================================================
        // Searcher class for Assemblies.
        //==============================================================================================================================
        private sealed class AssemblyCustomAttributeSearcher : CustomAttributeSearcher<Assembly>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(Assembly element)
            {
                return element.CustomAttributes;
            }

            public static readonly AssemblyCustomAttributeSearcher Default = new AssemblyCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for Modules.
        //==============================================================================================================================
        private sealed class ModuleCustomAttributeSearcher : CustomAttributeSearcher<Module>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(Module element)
            {
                return element.CustomAttributes;
            }

            public static readonly ModuleCustomAttributeSearcher Default = new ModuleCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for TypeInfos.
        //==============================================================================================================================
        private sealed class TypeCustomAttributeSearcher : CustomAttributeSearcher<Type>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(Type element)
            {
                return element.CustomAttributes;
            }

            public sealed override Type GetParent(Type e)
            {
                Type? baseType = e.BaseType;
                if (baseType == null)
                    return null;

                // Optimization: We shouldn't have any public inheritable attributes on Object or ValueType so don't bother scanning this one.
                //  Since many types derive directly from Object, this should a lot of type.
                if (baseType == typeof(object) || baseType == typeof(ValueType))
                    return null;

                return baseType;
            }

            public static readonly TypeCustomAttributeSearcher Default = new TypeCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for FieldInfos.
        //==============================================================================================================================
        private sealed class FieldCustomAttributeSearcher : CustomAttributeSearcher<FieldInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(FieldInfo element)
            {
                return element.CustomAttributes;
            }

            public static readonly FieldCustomAttributeSearcher Default = new FieldCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for ConstructorInfos.
        //==============================================================================================================================
        private sealed class ConstructorCustomAttributeSearcher : CustomAttributeSearcher<ConstructorInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(ConstructorInfo element)
            {
                return element.CustomAttributes;
            }

            public static readonly ConstructorCustomAttributeSearcher Default = new ConstructorCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for MethodInfos.
        //==============================================================================================================================
        private sealed class MethodCustomAttributeSearcher : CustomAttributeSearcher<MethodInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(MethodInfo element)
            {
                return element.CustomAttributes;
            }

            public sealed override MethodInfo GetParent(MethodInfo e)
            {
                return ReflectionAugments.ReflectionCoreCallbacks.GetImplicitlyOverriddenBaseClassMethod(e);
            }

            public static readonly MethodCustomAttributeSearcher Default = new MethodCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for PropertyInfos.
        //==============================================================================================================================
        private sealed class PropertyCustomAttributeSearcher : CustomAttributeSearcher<PropertyInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(PropertyInfo element)
            {
                return element.CustomAttributes;
            }

            public sealed override PropertyInfo GetParent(PropertyInfo e)
            {
                return ReflectionAugments.ReflectionCoreCallbacks.GetImplicitlyOverriddenBaseClassProperty(e);
            }

            public static readonly PropertyCustomAttributeSearcher Default = new PropertyCustomAttributeSearcher();
        }


        //==============================================================================================================================
        // Searcher class for EventInfos.
        //==============================================================================================================================
        private sealed class EventCustomAttributeSearcher : CustomAttributeSearcher<EventInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(EventInfo element)
            {
                return element.CustomAttributes;
            }

            public sealed override EventInfo GetParent(EventInfo e)
            {
                return ReflectionAugments.ReflectionCoreCallbacks.GetImplicitlyOverriddenBaseClassEvent(e);
            }

            public static readonly EventCustomAttributeSearcher Default = new EventCustomAttributeSearcher();
        }

        //==============================================================================================================================
        // Searcher class for ParameterInfos.
        //==============================================================================================================================
        private sealed class ParameterCustomAttributeSearcher : CustomAttributeSearcher<ParameterInfo>
        {
            protected sealed override IEnumerable<CustomAttributeData> GetDeclaredCustomAttributes(ParameterInfo element)
            {
                return element.CustomAttributes;
            }

            public sealed override ParameterInfo GetParent(ParameterInfo e)
            {
                MethodInfo? method = e.Member as MethodInfo;
                if (method == null)
                    return null;     // This is a constructor parameter.
                MethodInfo? methodParent = new MethodCustomAttributeSearcher().GetParent(method);
                if (methodParent == null)
                    return null;

                if (e.Position >= 0)
                {
                    return methodParent.GetParametersAsSpan()[e.Position];
                }
                else
                {
                    Debug.Assert(e.Position == -1);
                    return methodParent.ReturnParameter;
                }
            }

            public static readonly ParameterCustomAttributeSearcher Default = new ParameterCustomAttributeSearcher();
        }
    }
}
