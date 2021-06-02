// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection
{
    public sealed class NullabilityInfoContext
    {
        private static NullableState GetNullableContext(MemberInfo? memberInfo)
        {
            while (memberInfo != null)
            {
                var attributes = memberInfo.GetCustomAttributesData();
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeType.Name == "NullableContextAttribute" &&
                        attribute.AttributeType.Namespace == "System.Runtime.CompilerServices" &&
                        attribute.ConstructorArguments.Count == 1)
                    {
                        return TranslateByte(attribute.ConstructorArguments[0].Value);
                    }
                }

                memberInfo = memberInfo.DeclaringType;
            }

            return NullableState.Unknown;
        }

        public NullabilityInfo Create(ParameterInfo parameterInfo)
        {
            return GetNullabilityInfo(parameterInfo.Member, parameterInfo.ParameterType, parameterInfo.GetCustomAttributesData());
        }

        public NullabilityInfo Create(PropertyInfo propertyInfo)
        {
            var nullability = GetNullabilityInfo(propertyInfo, propertyInfo.PropertyType, propertyInfo.GetCustomAttributesData());
            var getterAttributes = propertyInfo.GetGetMethod()?.ReturnParameter.GetCustomAttributesData();
            var setterAttributes = propertyInfo.GetSetMethod()?.GetParameters()[0].GetCustomAttributesData();

            if (getterAttributes != null)
            {
                foreach (var attribute in getterAttributes)
                {
                    if (nullability.ReadState == NullableState.Nullable)
                    {
                        if (attribute.AttributeType.Name == "NotNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.ReadState = NullableState.NonNullable;
                            break;
                        }
                    }
                    else if (nullability.ReadState == NullableState.NonNullable)
                    {
                        if (attribute.AttributeType.Name == "MaybeNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.ReadState = NullableState.Nullable;
                            break;
                        }
                    }
                }
            }

            if (setterAttributes != null)
            {
                foreach (var attribute in setterAttributes)
                {
                    if (nullability.WriteState == NullableState.Nullable)
                    {
                        if (attribute.AttributeType.Name == "DisallowNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.WriteState = NullableState.NonNullable;
                            break;
                        }
                    }
                    else if (nullability.WriteState == NullableState.NonNullable)
                    {
                        if (attribute.AttributeType.Name == "AllowNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.WriteState = NullableState.Nullable;
                            break;
                        }
                    }
                }
            }

            return nullability;
        }

        public NullabilityInfo Create(EventInfo eventInfo)
        {
            return GetNullabilityInfo(eventInfo, eventInfo.EventHandlerType!, eventInfo.GetCustomAttributesData());
        }

        public NullabilityInfo Create(FieldInfo fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributesData();
            var nullability = GetNullabilityInfo(fieldInfo, fieldInfo.FieldType, attributes);

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    if (nullability.ReadState == NullableState.Nullable)
                    {
                        if (attribute.AttributeType.Name == "NotNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.ReadState = NullableState.NonNullable;
                            break;
                        }
                    }
                    else if (nullability.ReadState == NullableState.NonNullable)
                    {
                        if (attribute.AttributeType.Name == "MaybeNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.ReadState = NullableState.Nullable;
                            break;
                        }
                    }

                    if (nullability.WriteState == NullableState.Nullable)
                    {
                        if (attribute.AttributeType.Name == "DisallowNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.WriteState = NullableState.NonNullable;
                            break;
                        }
                    }
                    else if (nullability.WriteState == NullableState.NonNullable)
                    {
                        if (attribute.AttributeType.Name == "AllowNullAttribute" &&
                            attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                        {
                            nullability.WriteState = NullableState.Nullable;
                            break;
                        }
                    }
                }
            }

            return nullability;
        }

        private static NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes)
        {
            var offset = 0;
            return GetNullabilityInfo(memberInfo, type, customAttributes, ref offset);
        }

        private static NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes, ref int offset)
        {
            NullableState state = NullableState.Unknown;
            bool found = false;
            if (type.IsValueType)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if ( underlyingType != null)
                {
                    type = underlyingType;
                    state = NullableState.Nullable;
                    found = true;
                }
            }
            else
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute.AttributeType.Name == "NullableAttribute" &&
                        attribute.AttributeType.Namespace == "System.Runtime.CompilerServices" &&
                        attribute.ConstructorArguments.Count == 1)
                    {
                        var o = attribute.ConstructorArguments[0].Value;

                        if (o is byte b)
                        {
                            found = true;
                            state = TranslateByte(b);
                        }
                        else if (o is ReadOnlyCollection<CustomAttributeTypedArgument> args)
                            if (offset < args.Count &&
                                 args[offset].Value is byte elementB)
                            state = TranslateByte(elementB);

                        break;
                    }
                }

                if (!found)
                {
                    state = GetNullableContext(memberInfo);
                }
            }

            // We consumed one element in the nullable array.
            offset++;

            ReadOnlyCollection<NullableState>? elementsState = null;
            ReadOnlyCollection<NullableState>? genericArgumentsState = null;

            if (type.IsArray)
            {
                var elements = new List<NullableState>();
                var elementType = type.GetElementType()!;
                int i = 0;
                do
                {
                    // add to elements
                    var n = GetNullabilityInfo(memberInfo, elementType, elementType.GetCustomAttributesData(), ref offset);
                    elementType = type.GetElementType()!;
                    elements[i++] = n.ReadState;
                } while (type.IsArray);

                elementsState = elements.AsReadOnly();
            }
            else if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                var argumentsState = new List<NullableState>(genericArguments.Length);

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var n = GetNullabilityInfo(memberInfo, genericArguments[i], genericArguments[i].GetCustomAttributesData(), ref offset);
                    argumentsState[i] = n.ReadState;
                }

                genericArgumentsState = argumentsState.AsReadOnly();
            }

            return new NullabilityInfo(type, state, state, elementsState, genericArgumentsState);
        }

        private static NullableState TranslateByte(object? singleValue)
        {
            return singleValue is byte b ? TranslateByte(b) : NullableState.Unknown;
        }

        private static NullableState TranslateByte(byte b) =>
            b switch
            {
                1 => NullableState.NonNullable,
                2 => NullableState.Nullable,
                _ => NullableState.Unknown
            };
    }
}
