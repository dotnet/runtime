// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    public sealed class NullabilityInfoContext
    {
        private const string CompilerServicesNameSpace = "System.Runtime.CompilerServices";
        private readonly Dictionary<Module, NotAnnotatedStatus> _publicOnlyModules = new();
        private readonly Dictionary<MemberInfo, NullabilityState> _context = new();
        private static readonly NullabilityInfo[] s_emptyArray = Array.Empty<NullabilityInfo>();

        [Flags]
        private enum NotAnnotatedStatus
        {
            None = 0x0, // no restriction, all members annotated
            Private = 0x1, // private members not annotated
            Internal = 0x2 // internal members not annotated
        }

        private NullabilityState GetNullableContext(MemberInfo? memberInfo)
        {
            while (memberInfo != null)
            {
                if (_context.TryGetValue(memberInfo, out var state))
                {
                    return state;
                }

                var attributes = memberInfo.GetCustomAttributesData();
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeType.Name == "NullableContextAttribute" &&
                        attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                        attribute.ConstructorArguments.Count == 1)
                    {
                        state = TranslateByte(attribute.ConstructorArguments[0].Value);
                        _context.Add(memberInfo, state);
                        return state;
                    }
                }

                memberInfo = memberInfo.DeclaringType;

            }

            return NullabilityState.Unknown;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        public NullabilityInfo Create(ParameterInfo parameterInfo)
        {
            if (parameterInfo.Member is MethodBase method &&
                (method.IsPrivate || method.IsFamilyAndAssembly || method.IsAssembly))
            {
                if (IsPublicOnly(method.IsPrivate, method.IsFamilyAndAssembly, method.IsAssembly, method.Module))
                {
                    return new NullabilityInfo(parameterInfo.ParameterType, NullabilityState.Unknown, NullabilityState.Unknown, null, s_emptyArray);
                }
            }

            var attributes = parameterInfo.GetCustomAttributesData();
            var nullability = GetNullabilityInfo(parameterInfo.Member, parameterInfo.ParameterType, attributes);

            if (nullability.ReadState != NullabilityState.Unknown)
            {
                CheckParameterMetadataType(parameterInfo, nullability);
            }

            CheckNullabilityAttributes(nullability, attributes);

            return nullability;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        private void CheckParameterMetadataType(ParameterInfo parameter, NullabilityInfo nullability)
        {
            if (parameter.Member is MethodInfo method)
            {
                var metaMethod = (MethodInfo)method.Module.ResolveMethod(method.MetadataToken)!;
                ParameterInfo? metaParameter = null;
                if (string.IsNullOrEmpty(parameter.Name))
                {
                    metaParameter = metaMethod.ReturnParameter;
                }
                else
                {
                    var parameters = metaMethod.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameter.Position == i &&
                            parameter.Name == parameters[i].Name)
                        {
                            metaParameter = parameters[i];
                            break;
                        }
                    }
                }

                if (metaParameter != null)
                {
                    if (metaParameter.ParameterType.IsGenericParameter)
                    {
                        NullabilityState state = nullability.ReadState;

                        if (!ParseNullableState(metaParameter.ParameterType.GetCustomAttributesData(), 0, ref state))
                        {
                            state = GetNullableContext(metaParameter.ParameterType);
                        }

                        nullability.ReadState = state;
                        nullability.WriteState = state;
                    }
                    else if (metaParameter.ParameterType.ContainsGenericParameters && nullability.TypeArguments.Length > 0)
                    {
                        var genericArguments = metaParameter.ParameterType.GetGenericArguments();

                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            if (genericArguments[i].IsGenericParameter)
                            {
                                var n = GetNullabilityInfo(metaMethod, genericArguments[i], genericArguments[i].GetCustomAttributesData(), i + 1);
                                nullability.TypeArguments[i].ReadState = n.ReadState;
                                nullability.TypeArguments[i].WriteState = n.WriteState;
                            }
                        }
                    }
                }
            }
        }

        private void CheckNullabilityAttributes(NullabilityInfo nullability, IList<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                {
                    if (attribute.AttributeType.Name == "NotNullAttribute" &&
                        nullability.ReadState == NullabilityState.Nullable)
                    {
                        nullability.ReadState = NullabilityState.NotNull;
                        break;
                    }
                    else if ((attribute.AttributeType.Name == "MaybeNullAttribute" ||
                            attribute.AttributeType.Name == "MaybeNullWhenAttribute") &&
                            nullability.ReadState == NullabilityState.NotNull &&
                            !nullability.Type.IsValueType)
                    {
                        nullability.ReadState = NullabilityState.Nullable;
                        break;
                    }

                    if (attribute.AttributeType.Name == "DisallowNullAttribute" &&
                        nullability.WriteState == NullabilityState.Nullable)
                    {
                        nullability.WriteState = NullabilityState.NotNull;
                        break;
                    }
                    else if (attribute.AttributeType.Name == "AllowNullAttribute" &&
                        nullability.WriteState == NullabilityState.NotNull &&
                        !nullability.Type.IsValueType)
                    {
                        nullability.WriteState = NullabilityState.Nullable;
                        break;
                    }
                }
            }
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        public NullabilityInfo Create(PropertyInfo propertyInfo)
        {
            var nullability = GetNullabilityInfo(propertyInfo, propertyInfo.PropertyType, propertyInfo.GetCustomAttributesData());
            var getterAttributes = propertyInfo.GetGetMethod(true)?.ReturnParameter.GetCustomAttributesData();
            var setterAttributes = propertyInfo.GetSetMethod(true)?.GetParameters()[0].GetCustomAttributesData();

            if (getterAttributes != null)
            {
                CheckNullabilityAttributes(nullability, getterAttributes);
            }

            if (setterAttributes != null)
            {
                CheckNullabilityAttributes(nullability, setterAttributes);
            }

            return nullability;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        public NullabilityInfo Create(EventInfo eventInfo)
        {
            return GetNullabilityInfo(eventInfo, eventInfo.EventHandlerType!, eventInfo.GetCustomAttributesData());
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        public NullabilityInfo Create(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsPrivate || fieldInfo.IsFamilyAndAssembly || fieldInfo.IsAssembly)
            {
                if (IsPublicOnly(fieldInfo.IsPrivate, fieldInfo.IsFamilyAndAssembly, fieldInfo.IsAssembly, fieldInfo.Module))
                {
                    return new NullabilityInfo(fieldInfo.FieldType, NullabilityState.Unknown, NullabilityState.Unknown, null, s_emptyArray);
                }
            }

            var attributes = fieldInfo.GetCustomAttributesData();
            var nullability = GetNullabilityInfo(fieldInfo, fieldInfo.FieldType, attributes);
            CheckNullabilityAttributes(nullability, attributes);

            return nullability;
        }

        private bool IsPublicOnly(bool isPrivate, bool isFamilyAndAssembly, bool isAssembly, Module module)
        {
            if (!_publicOnlyModules.TryGetValue(module, out var value))
            {
                value = PopulateAnnotationInfo(module.GetCustomAttributesData());
                _publicOnlyModules.Add(module, value);
            }

            if (value == NotAnnotatedStatus.None)
            {
                return false;
            }

            if ((isPrivate || isFamilyAndAssembly) && value.HasFlag(NotAnnotatedStatus.Private) ||
                 isAssembly && value.HasFlag(NotAnnotatedStatus.Internal))
            {
                return true;
            }

            return false;
        }

        private NotAnnotatedStatus PopulateAnnotationInfo(IList<CustomAttributeData> customAttributes)
        {
            foreach (var attribute in customAttributes)
            {
                if (attribute.AttributeType.Name == "NullablePublicOnlyAttribute" &&
                    attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                    attribute.ConstructorArguments.Count == 1)
                {
                    if (attribute.ConstructorArguments[0].Value is bool boolValue && boolValue)
                    {
                        return NotAnnotatedStatus.Internal | NotAnnotatedStatus.Private;
                    }
                    else
                    {
                        return NotAnnotatedStatus.Private;
                    }
                }
            }

            return NotAnnotatedStatus.None;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes)
        {
            return GetNullabilityInfo(memberInfo, type, customAttributes, 0);
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes, int index)
        {
            NullabilityState state = NullabilityState.Unknown;

            if (type.IsValueType)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                {
                    type = underlyingType;
                    state = NullabilityState.Nullable;
                }
                else
                {
                    state = NullabilityState.NotNull;
                }
            }
            else
            {
                if (!ParseNullableState(customAttributes, index, ref state))
                {
                    state = GetNullableContext(memberInfo);
                }

                if (state != NullabilityState.Unknown)
                {
                    TryLoadGenericMetaTypeNullability(memberInfo, ref state);
                }
            }

            NullabilityInfo? elementState = null;
            NullabilityInfo[]? genericArgumentsState = null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType != null)
                {
                    elementState = GetNullabilityInfo(memberInfo, elementType, customAttributes, index + 1);
                }
            }
            else if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                genericArgumentsState = new NullabilityInfo[genericArguments.Length];

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var info = GetNullabilityInfo(memberInfo, genericArguments[i], customAttributes, i + 1);
                    genericArgumentsState[i] = info;
                }
            }

            return new NullabilityInfo(type, state, state, elementState, genericArgumentsState ?? s_emptyArray);
        }

        private static bool ParseNullableState(IList<CustomAttributeData> customAttributes, int index, ref NullabilityState state)
        {
            foreach (var attribute in customAttributes)
            {
                if (attribute.AttributeType.Name == "NullableAttribute" &&
                    attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                    attribute.ConstructorArguments.Count == 1)
                {
                    var o = attribute.ConstructorArguments[0].Value;

                    if (o is byte b)
                    {
                        state = TranslateByte(b);
                        return true;
                    }
                    else if (o is ReadOnlyCollection<CustomAttributeTypedArgument> args)
                        if (index < args.Count &&
                             args[index].Value is byte elementB)
                        {
                            state = TranslateByte(elementB);
                            return true;
                        }

                    break;
                }
            }

            return false;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        private bool TryLoadGenericMetaTypeNullability(MemberInfo memberInfo, ref NullabilityState state)
        {

            var type = memberInfo switch
            {
                FieldInfo field => field.Module.ResolveField(field.MetadataToken)!.FieldType,
                PropertyInfo property => GetPropertyMetadataType(property),
                _ => null
            };

            if (type != null && type.IsGenericParameter)
            {
                if (!ParseNullableState(type.GetCustomAttributesData(), 0, ref state))
                {
                    state = GetNullableContext(type);
                }
            }

            return false;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the linker")]
        private static Type GetPropertyMetadataType(PropertyInfo property)
        {
            if (property.GetGetMethod(true) is MethodInfo method)
            {
                return ((MethodInfo)property.Module.ResolveMethod(method.MetadataToken)!).ReturnType;
            }

            return property.Module.ResolveMethod(property.GetSetMethod(true)!.MetadataToken)!.GetParameters()[0].ParameterType;
        }

        private static NullabilityState TranslateByte(object? value)
        {
            return value is byte b ? TranslateByte(b) : NullabilityState.Unknown;
        }

        private static NullabilityState TranslateByte(byte b) =>
            b switch
            {
                1 => NullabilityState.NotNull,
                2 => NullabilityState.Nullable,
                _ => NullabilityState.Unknown
            };
    }
}
