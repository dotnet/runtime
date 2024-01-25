// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Internal.Runtime.Augments;

//==================================================================================================================
// Dependency note:
//   This class must depend only on the CustomAttribute properties that return IEnumerable<CustomAttributeData>.
//   All of the other custom attribute api route back here so calls to them will cause an infinite recursion.
//==================================================================================================================

namespace Internal.Reflection.Extensions.NonPortable
{
    public static class CustomAttributeInstantiator
    {
        //
        // Turn a CustomAttributeData into a live Attribute object. There's nothing actually non-portable about this one,
        // however, it is included as a concession to that the fact the Reflection.Execution which implements this contract
        // also needs this functionality to implement default values, and we don't want to duplicate this code.
        //
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "property setters and fiels which are accessed by any attribute instantiation which is present in the code linker has analyzed." +
                            "As such enumerating all fields and properties may return different results after trimming" +
                            "but all those which are needed to actually have data should be there.")]
        public static Attribute Instantiate(this CustomAttributeData cad)
        {
            if (cad == null)
                return null;
            Type attributeType = cad.AttributeType;

            //
            // Find the public constructor that matches the supplied arguments.
            //
            ConstructorInfo? matchingCtor = null;
            ReadOnlySpan<ParameterInfo> matchingParameters = default;
            IList<CustomAttributeTypedArgument> constructorArguments = cad.ConstructorArguments;
            foreach (ConstructorInfo ctor in attributeType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ReadOnlySpan<ParameterInfo> parameters = ctor.GetParametersAsSpan();
                if (parameters.Length != constructorArguments.Count)
                    continue;
                int i;
                for (i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (!(parameterType.Equals(constructorArguments[i].ArgumentType) ||
                          parameterType == typeof(object)))
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
                throw new MissingMethodException(attributeType.FullName, ConstructorInfo.ConstructorName);

            //
            // Found the right constructor. Instantiate the Attribute.
            //
            int arity = matchingParameters.Length;
            object?[] invokeArguments = new object[arity];
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
                object? argumentValue = namedArgument.TypedValue.Convert();
                Type walk = attributeType;
                string name = namedArgument.MemberName;
                if (namedArgument.IsField)
                {
                    // Field
                    for (; ; )
                    {
                        FieldInfo? fieldInfo = walk.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(newAttribute, argumentValue);
                            break;
                        }
                        Type? baseType = walk.BaseType;
                        if (baseType == null)
                            throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidFieldFail, name));
                        walk = baseType;
                    }
                }
                else
                {
                    // Property
                    for (; ; )
                    {
                        PropertyInfo? propertyInfo = walk.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(newAttribute, argumentValue);
                            break;
                        }
                        Type? baseType = walk.BaseType;
                        if (baseType == null)
                            throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidPropFail, name));
                        walk = baseType;
                    }
                }
            }

            return newAttribute;
        }

        //
        // Convert the argument value reported by Reflection into an actual object.
        //
        private static object? Convert(this CustomAttributeTypedArgument typedArgument)
        {
            Type argumentType = typedArgument.ArgumentType;
            if (!argumentType.IsArray)
            {
                bool isEnum = argumentType.IsEnum;
                object? argumentValue = typedArgument.Value;
                if (isEnum)
                    argumentValue = Enum.ToObject(argumentType, argumentValue!);
                return argumentValue;
            }
            else
            {
                IList<CustomAttributeTypedArgument>? typedElements = (IList<CustomAttributeTypedArgument>?)(typedArgument.Value);
                if (typedElements == null)
                    return null;
                Array array = Array.CreateInstanceFromArrayType(argumentType, typedElements.Count);
                for (int i = 0; i < typedElements.Count; i++)
                {
                    object? elementValue = typedElements[i].Convert();
                    array.SetValue(elementValue, i);
                }
                return array;
            }
        }
    }
}
