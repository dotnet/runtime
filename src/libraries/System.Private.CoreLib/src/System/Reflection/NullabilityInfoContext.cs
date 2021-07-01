// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    /// <summary>
    /// Provides APIs for populating nullability information/context from reflection members:
    /// <see cref="ParameterInfo"/>, <see cref="FieldInfo"/>, <see cref="PropertyInfo"/> and <see cref="EventInfo"/>.
    /// </summary>
    public sealed class NullabilityInfoContext
    {
        private const string CompilerServicesNameSpace = "System.Runtime.CompilerServices";
        private readonly Dictionary<Module, NotAnnotatedStatus> _publicOnlyModules = new();
        private readonly Dictionary<MemberInfo, NullabilityState> _context = new();

        [Flags]
        private enum NotAnnotatedStatus
        {
            None = 0x0,    // no restriction, all members annotated
            Private = 0x1, // private members not annotated
            Internal = 0x2 // internal members not annotated
        }

        private NullabilityState GetNullableContext(MemberInfo? memberInfo)
        {
            while (memberInfo != null)
            {
                if (_context.TryGetValue(memberInfo, out NullabilityState state))
                {
                    return state;
                }

                IList<CustomAttributeData> attributes = memberInfo.GetCustomAttributesData();
                foreach (CustomAttributeData attribute in attributes)
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

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="ParameterInfo" />.
        /// If NullablePublicOnlyAttribute is set the private and/or internal member's nullability attributes will be omitted, for example in
        /// .NET SDK the private and internal member's nullability attributes are omitted, in this case the API return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="parameterInfo">The parameter which nullability info gets populated</param>
        /// <exception cref="ArgumentNullException">If the parameterInfo parameter is null</exception>
        /// <returns><see cref="NullabilityInfo" /></returns>
        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        public NullabilityInfo Create(ParameterInfo parameterInfo)
        {
            if (parameterInfo is null)
            {
                throw new ArgumentNullException(nameof(parameterInfo));
            }

            if (parameterInfo.Member is MethodBase method &&
                (method.IsPrivate || method.IsFamilyAndAssembly || method.IsAssembly))
            {
                if (IsPublicOnly(method.IsPrivate, method.IsFamilyAndAssembly, method.IsAssembly, method.Module))
                {
                    return new NullabilityInfo(parameterInfo.ParameterType, NullabilityState.Unknown, NullabilityState.Unknown, null, Array.Empty<NullabilityInfo>());
                }
            }

            IList<CustomAttributeData> attributes = parameterInfo.GetCustomAttributesData();
            NullabilityInfo nullability = GetNullabilityInfo(parameterInfo.Member, parameterInfo.ParameterType, attributes);

            if (nullability.ReadState != NullabilityState.Unknown)
            {
                CheckParameterMetadataType(parameterInfo, nullability);
            }

            CheckNullabilityAttributes(nullability, attributes);
            return nullability;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
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
                    ParameterInfo[] parameters = metaMethod.GetParameters();
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
                    CheckGenericParameters(nullability, metaMethod, metaParameter.ParameterType);
                }
            }
        }

        private void CheckNullabilityAttributes(NullabilityInfo nullability, IList<CustomAttributeData> attributes)
        {
            foreach (CustomAttributeData attribute in attributes)
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

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="PropertyInfo" />.
        /// If NullablePublicOnlyAttribute is set the private and/or internal member's nullability attributes will be omitted, for example in
        /// .NET SDK the private and internal member's nullability attributes are omitted, in this case the API return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="propertyInfo">The parameter which nullability info gets populated</param>
        /// <exception cref="ArgumentNullException">If the propertyInfo parameter is null</exception>
        /// <returns><see cref="NullabilityInfo" /></returns>
        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        public NullabilityInfo Create(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            NullabilityInfo nullability = GetNullabilityInfo(propertyInfo, propertyInfo.PropertyType, propertyInfo.GetCustomAttributesData());
            IList<CustomAttributeData>? getterAttributes = propertyInfo.GetGetMethod(true)?.ReturnParameter.GetCustomAttributesData();
            IList<CustomAttributeData>? setterAttributes = propertyInfo.GetSetMethod(true)?.GetParameters()[0].GetCustomAttributesData();

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

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="EventInfo" />.
        /// If NullablePublicOnlyAttribute is set the private and/or internal member's nullability attributes will be omitted, for example in
        /// .NET SDK the private and internal member's nullability attributes are omitted, in this case the API return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="eventInfo">The parameter which nullability info gets populated</param>
        /// <exception cref="ArgumentNullException">If the eventInfo parameter is null</exception>
        /// <returns><see cref="NullabilityInfo" /></returns>
        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        public NullabilityInfo Create(EventInfo eventInfo)
        {
            if (eventInfo is null)
            {
                throw new ArgumentNullException(nameof(eventInfo));
            }

            return GetNullabilityInfo(eventInfo, eventInfo.EventHandlerType!, eventInfo.GetCustomAttributesData());
        }

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="FieldInfo" />
        /// If NullablePublicOnlyAttribute is set the private and/or internal member's nullability attributes will be omitted, for example in
        /// .NET SDK the private and internal member's nullability attributes are omitted, in this case the API return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="fieldInfo">The parameter which nullability info gets populated</param>
        /// <exception cref="ArgumentNullException">If the fieldInfo parameter is null</exception>
        /// <returns><see cref="NullabilityInfo" /></returns>
        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        public NullabilityInfo Create(FieldInfo fieldInfo)
        {
            if (fieldInfo is null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            if (fieldInfo.IsPrivate || fieldInfo.IsFamilyAndAssembly || fieldInfo.IsAssembly)
            {
                if (IsPublicOnly(fieldInfo.IsPrivate, fieldInfo.IsFamilyAndAssembly, fieldInfo.IsAssembly, fieldInfo.Module))
                {
                    return new NullabilityInfo(fieldInfo.FieldType, NullabilityState.Unknown, NullabilityState.Unknown, null, Array.Empty<NullabilityInfo>());
                }
            }

            IList<CustomAttributeData> attributes = fieldInfo.GetCustomAttributesData();
            NullabilityInfo nullability = GetNullabilityInfo(fieldInfo, fieldInfo.FieldType, attributes);
            CheckNullabilityAttributes(nullability, attributes);
            return nullability;
        }

        private bool IsPublicOnly(bool isPrivate, bool isFamilyAndAssembly, bool isAssembly, Module module)
        {
            if (!_publicOnlyModules.TryGetValue(module, out NotAnnotatedStatus value))
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
            foreach (CustomAttributeData attribute in customAttributes)
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

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes) =>
            GetNullabilityInfo(memberInfo, type, customAttributes, 0);

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, IList<CustomAttributeData> customAttributes, int index)
        {
            NullabilityState state = NullabilityState.Unknown;

            if (type.IsValueType)
            {
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    state = NullabilityState.Nullable;
                }
                else
                {
                    state = NullabilityState.NotNull;
                }

                return new NullabilityInfo(type, state, state, null, Array.Empty<NullabilityInfo>());
            }
            else
            {
                if (!ParseNullableState(customAttributes, index, ref state))
                {
                    state = GetNullableContext(memberInfo);
                }

                NullabilityInfo? elementState = null;
                NullabilityInfo[]? genericArgumentsState = null;

                if (type.IsArray)
                {
                    elementState = GetNullabilityInfo(memberInfo, type.GetElementType()!, customAttributes, index + 1);
                }
                else if (type.IsGenericType)
                {
                    Type[] genericArguments = type.GetGenericArguments();
                    genericArgumentsState = new NullabilityInfo[genericArguments.Length];

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        genericArgumentsState[i] = GetNullabilityInfo(memberInfo, genericArguments[i], customAttributes, i + 1);
                    }
                }

                NullabilityInfo nullability = new NullabilityInfo(type, state, state, elementState, genericArgumentsState ?? Array.Empty<NullabilityInfo>());
                if (state != NullabilityState.Unknown)
                {
                    TryLoadGenericMetaTypeNullability(memberInfo, nullability);
                }

                return nullability;
            }
        }

        private static bool ParseNullableState(IList<CustomAttributeData> customAttributes, int index, ref NullabilityState state)
        {
            foreach (CustomAttributeData attribute in customAttributes)
            {
                if (attribute.AttributeType.Name == "NullableAttribute" &&
                    attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                    attribute.ConstructorArguments.Count == 1)
                {
                    object? o = attribute.ConstructorArguments[0].Value;

                    if (o is byte b)
                    {
                        state = TranslateByte(b);
                        return true;
                    }
                    else if (o is ReadOnlyCollection<CustomAttributeTypedArgument> args &&
                            index < args.Count &&
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

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private bool TryLoadGenericMetaTypeNullability(MemberInfo memberInfo, NullabilityInfo nullability)
        {
            MemberInfo? metaMember = null;
            Type? metaType = null;
            if (memberInfo is FieldInfo field)
            {
                metaType = GetFieldMetaData(field, metaMember);
            }
            else if (memberInfo is PropertyInfo property)
            {
                metaType = GetPropertyMetadata(property, metaMember);
            }

            if (metaType != null)
            {
                CheckGenericParameters(nullability, metaMember!, metaType);
            }

            return false;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private Type GetFieldMetaData(FieldInfo field, MemberInfo? metaMember)
        {
            FieldInfo member = field.Module.ResolveField(field.MetadataToken)!;
            metaMember = member;
            return member.FieldType;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private static Type GetPropertyMetadata(PropertyInfo property, MemberInfo? metaMember)
        {
            if (property.GetGetMethod(true) is MethodInfo method)
            {
                metaMember = property.Module.ResolveMethod(method.MetadataToken)!;
                return ((MethodInfo)metaMember).ReturnType;
            }

            metaMember = property.Module.ResolveMethod(property.GetSetMethod(true)!.MetadataToken);
            return ((MethodInfo)metaMember!).GetParameters()[0].ParameterType;
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private void CheckGenericParameters(NullabilityInfo nullability, MemberInfo metaMember, Type metaType)
        {
            if (metaType.IsGenericParameter)
            {
                NullabilityState state = nullability.ReadState;

                if (!ParseNullableState(metaType.GetCustomAttributesData(), 0, ref state))
                {
                    state = GetNullableContext(metaType);
                }

                nullability.ReadState = state;
                nullability.WriteState = state;
            }
            else if (metaType.ContainsGenericParameters)
            {
                if (nullability.GenericTypeArguments.Length > 0)
                {
                    Type[] genericArguments = metaType.GetGenericArguments();

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        if (genericArguments[i].IsGenericParameter)
                        {
                            NullabilityInfo n = GetNullabilityInfo(metaMember, genericArguments[i], genericArguments[i].GetCustomAttributesData(), i + 1);
                            nullability.GenericTypeArguments[i].ReadState = n.ReadState;
                            nullability.GenericTypeArguments[i].WriteState = n.WriteState;
                        }
                        else
                        {
                            UpdateGenericArrayElements(nullability.GenericTypeArguments[i].ElementType, metaMember, genericArguments[i]);
                        }
                    }
                }
                else
                {
                    UpdateGenericArrayElements(nullability.ElementType, metaMember, metaType);
                }
            }
        }

        [RequiresUnreferencedCode("Nullability attributes are trimmed by the trimmer")]
        private void UpdateGenericArrayElements(NullabilityInfo? elementState, MemberInfo metaMember, Type metaType)
        {
            if (metaType.IsArray && elementState != null
                && metaType.GetElementType()!.IsGenericParameter)
            {
                Type elementType = metaType.GetElementType()!;
                NullabilityInfo n = GetNullabilityInfo(metaMember, elementType, elementType.GetCustomAttributesData(), 0);
                elementState.ReadState = n.ReadState;
                elementState.WriteState = n.WriteState;
            }
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
