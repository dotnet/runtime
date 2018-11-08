// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

//==================================================================================================================
// Dependency note:
//   This class must depend only on the CustomAttribute properties that return IEnumerable<CustomAttributeData>.
//   All of the other custom attribute api route back here so calls to them will cause an infinite recursion.
//==================================================================================================================

namespace System.Reflection.Tests
{
    internal static class CustomAttributeInstantiator
    {
        //
        // Turn a CustomAttributeData into a live Attribute object.
        //
        public static T Instantiate<T>(this CustomAttributeData cad) where T : Attribute
        {
            if (cad == null)
                return null;
            Type attributeType = cad.AttributeType;

            //
            // Find the public constructor that matches the supplied arguments.
            //
            ConstructorInfo matchingCtor = null;
            ParameterInfo[] matchingParameters = null;
            IList<CustomAttributeTypedArgument> constructorArguments = cad.ConstructorArguments;
            foreach (ConstructorInfo ctor in attributeType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length != constructorArguments.Count)
                    continue;
                int i;
                for (i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (!(parameterType.Equals(constructorArguments[i].ArgumentType) ||
                          parameterType.Equals(typeof(object))))
                        break;
                }
                if (i == parameters.Length)
                {
                    matchingCtor = ctor;
                    matchingParameters = parameters;
                    break;
                }
            }
            if (matchingCtor == null)
                throw new Exception(attributeType.FullName);

            //
            // Found the right constructor. Instantiate the Attribute.
            //
            int arity = matchingParameters.Length;
            object[] invokeArguments = new object[arity];
            for (int i = 0; i < arity; i++)
            {
                invokeArguments[i] = constructorArguments[i].Convert();
            }
            Attribute newAttribute = (Attribute)(matchingCtor.Invoke(invokeArguments));

            //
            // If there any named arguments, evaluate them and set the appropriate field or property.
            //
            foreach (CustomAttributeNamedArgument namedArgument in cad.NamedArguments)
            {
                object argumentValue = namedArgument.TypedValue.Convert();
                Type walk = attributeType;
                string name = namedArgument.MemberName;
                if (namedArgument.IsField)
                {
                    // Field
                    for (;;)
                    {
                        FieldInfo fieldInfo = walk.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(newAttribute, argumentValue);
                            break;
                        }
                        Type baseType = walk.BaseType;
                        walk = baseType ?? throw new CustomAttributeFormatException("No such field: " + name);
                    }
                }
                else
                {
                    // Property
                    for (;;)
                    {
                        PropertyInfo propertyInfo = walk.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(newAttribute, argumentValue);
                            break;
                        }
                        Type baseType = walk.BaseType;
                        walk = baseType ?? throw new CustomAttributeFormatException("No such property: " + name);
                    }
                }
            }

            return (T)newAttribute;
        }

        //
        // Convert the argument value reported by Reflection into an actual object.
        //
        private static object Convert(this CustomAttributeTypedArgument typedArgument)
        {
            Type argumentType = typedArgument.ArgumentType;
            if (!argumentType.IsArray)
            {
                bool isEnum = argumentType.IsEnum;
                object argumentValue = typedArgument.Value;
                if (isEnum)
                    argumentValue = Enum.ToObject(argumentType, argumentValue);
                return argumentValue;
            }
            else
            {
                IList<CustomAttributeTypedArgument> typedElements = (IList<CustomAttributeTypedArgument>)(typedArgument.Value);
                if (typedElements == null)
                    return null;
                Type elementType = argumentType.GetElementType();
                Array array = Array.CreateInstance(elementType, typedElements.Count);
                for (int i = 0; i < typedElements.Count; i++)
                {
                    object elementValue = typedElements[i].Convert();
                    array.SetValue(elementValue, i);
                }
                return array;
            }
        }

        //
        // Only public instance fields can be targets of named arguments.
        //
        private static bool IsValidNamedArgumentTarget(this FieldInfo fieldInfo)
        {
            if ((fieldInfo.Attributes & (FieldAttributes.FieldAccessMask | FieldAttributes.Static | FieldAttributes.Literal)) !=
                FieldAttributes.Public)
                return false;
            return true;
        }

        //
        // Only public read/write instance properties can be targets of named arguments.
        //
        private static bool IsValidNamedArgumentTarget(this PropertyInfo propertyInfo)
        {
            MethodInfo getter = propertyInfo.GetMethod;
            MethodInfo setter = propertyInfo.SetMethod;
            if (getter == null)
                return false;
            if ((getter.Attributes & (MethodAttributes.Static | MethodAttributes.MemberAccessMask)) != MethodAttributes.Public)
                return false;
            if (setter == null)
                return false;
            if ((setter.Attributes & (MethodAttributes.Static | MethodAttributes.MemberAccessMask)) != MethodAttributes.Public)
                return false;
            return true;
        }
    }
}
