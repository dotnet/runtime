// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using ILCompiler.Logging;
using ILLink.Shared;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Dataflow
{
    static class DiagnosticUtilities
    {
        internal static Origin GetMethodParameterFromIndex(MethodDesc method, int parameterIndex)
        {
            int declaredParameterIndex;
            if (!method.Signature.IsStatic)
            {
                if (parameterIndex == 0)
                    return new MethodOrigin(method);

                declaredParameterIndex = parameterIndex - 1;
            }
            else
                declaredParameterIndex = parameterIndex;

            return new ParameterOrigin(method, declaredParameterIndex);
        }

        internal static string GetParameterNameForErrorMessage(ParameterOrigin origin)
        {
            return GetParameterNameForErrorMessage(origin.Method, origin.Index);
        }

        internal static string GetParameterNameForErrorMessage(MethodDesc method, int parameterIndex)
        {
            if (method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
                return ecmaMethod.GetParameterDisplayName(parameterIndex);

            return $"#{parameterIndex}";
        }

        internal static string GetMethodSignatureDisplayName(MethodDesc method)
        {
            return method.GetDisplayName();
        }

        internal static string GetGenericParameterDeclaringMemberDisplayName(GenericParameterOrigin origin)
        {
            var param = (EcmaGenericParameter)origin.GenericParameter;
            var parent = param.Module.GetObject(param.MetadataReader.GetGenericParameter(param.Handle).Parent);
            if (parent is MethodDesc m)
                return m.GetDisplayName();
            else
                return ((TypeDesc)parent).GetDisplayName();
        }

        internal static bool TryGetRequiresAttribute(TypeSystemEntity member, string requiresAttributeName, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            attribute = default;
            CustomAttributeValue<TypeDesc>? decoded = default;
            switch (member)
            {
                case MethodDesc method:
                    var ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;
                    if (ecmaMethod == null)
                        return false;
                    decoded = ecmaMethod.GetDecodedCustomAttribute("System.Diagnostics.CodeAnalysis", requiresAttributeName);
                    break;
                case MetadataType type:
                    var ecmaType = type as EcmaType;
                    if (ecmaType == null)
                        return false;
                    decoded = ecmaType.GetDecodedCustomAttribute("System.Diagnostics.CodeAnalysis", requiresAttributeName);
                    break;
                case PropertyPseudoDesc property:
                    decoded = property.GetDecodedCustomAttribute("System.Diagnostics.CodeAnalysis", requiresAttributeName);
                    break;
                default:
                    Debug.Fail("Trying to operate with unsupported TypeSystemEntity " + member.GetType().ToString());
                    break;
            }
            if (!decoded.HasValue)
                return false;

            attribute = decoded.Value;
            return true;
        }

        public static CustomAttributeValue<TypeDesc>? GetDecodedCustomAttribute(this PropertyPseudoDesc prop, string attributeNamespace, string attributeName)
        {
            var ecmaType = prop.OwningType as EcmaType;
            var metadataReader = ecmaType.MetadataReader;

            var attributeHandle = metadataReader.GetCustomAttributeHandle(prop.GetCustomAttributes,
                attributeNamespace, attributeName);

            if (attributeHandle.IsNil)
                return null;

            return metadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(ecmaType.EcmaModule));
        }

        internal static string GetRequiresAttributeMessage(CustomAttributeValue<TypeDesc> attribute)
        {
            if (attribute.FixedArguments.Length != 0)
                return (string)attribute.FixedArguments[0].Value;

            return null;
        }

        internal static string GetRequiresAttributeUrl(CustomAttributeValue<TypeDesc> attribute)
        {
            if (attribute.NamedArguments.Length != 0 && attribute.NamedArguments[0].Name == "Url")
                return (string)attribute.NamedArguments[0].Value;

            return null;
        }

        /// <summary>
        /// Determines if method is within a declared Requires scope - this typically means that trim analysis
        /// warnings should be suppressed in such a method.
        /// </summary>
        /// <remarks>Unlike <see cref="DoesMemberRequire(TypeSystemEntity, string, out CustomAttributeValue{TypeDesc}?)"/>
        /// if a declaring type has Requires, all methods in that type are considered "in scope" of that Requires. So this includes also
        /// instance methods (not just statics and .ctors).</remarks>
        internal static bool IsInRequiresScope(this MethodDesc method, string requiresAttribute) =>
            method.IsInRequiresScope(requiresAttribute, true);

        /// <summary>
        /// True if member of a call is considered to be annotated with the Requires... attribute.
        /// Doesn't check the associated symbol for overrides and virtual methods because we should warn on mismatched between the property AND the accessors
        /// </summary>
        /// <param name="method">
        ///	MethodDesc that is either an overriding member or an overriden/virtual member
        /// </param>
        internal static bool IsOverrideInRequiresScope(this MethodDesc method, string requiresAttribute) =>
            method.IsInRequiresScope(requiresAttribute, false);

        private static bool IsInRequiresScope(this MethodDesc method, string requiresAttribute, bool checkAssociatedSymbol)
        {
            if (method.HasCustomAttribute("System.Diagnostics.CodeAnalysis", requiresAttribute) && !method.IsStaticConstructor)
                return true;

            if (method.OwningType is TypeDesc type && TryGetRequiresAttribute(type, requiresAttribute, out _))
                return true;

            if (checkAssociatedSymbol && method.GetPropertyForAccessor() is PropertyPseudoDesc property && TryGetRequiresAttribute(property, requiresAttribute, out _))
                return true;

            return false;
        }

        internal static bool DoesMethodRequire(this MethodDesc method, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            attribute = null;
            if (method.IsStaticConstructor)
                return false;

            if (TryGetRequiresAttribute(method, requiresAttribute, out attribute))
                return true;

            if ((method.Signature.IsStatic || method.IsConstructor) && method.OwningType is TypeDesc owningType &&
                !owningType.IsArray && TryGetRequiresAttribute(owningType, requiresAttribute, out attribute))
                return true;

            return false;
        }

        internal static bool DoesFieldRequire(this FieldDesc field, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            if (!field.IsStatic || field.OwningType is not TypeDesc owningType || owningType.IsArray)
            {
                attribute = null;
                return false;
            }

            return TryGetRequiresAttribute(field.OwningType, requiresAttribute, out attribute);
        }

        internal static bool DoesPropertyRequire(this PropertyPseudoDesc property, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute) =>
            TryGetRequiresAttribute(property, requiresAttribute, out attribute);

        /// <summary>
        /// Determines if member requires (and thus any usage of such method should be warned about).
        /// </summary>
        /// <remarks>Unlike <see cref="IsInRequiresScope(MethodDesc, string)"/> only static methods 
        /// and .ctors are reported as requires when the declaring type has Requires on it.</remarks>
        internal static bool DoesMemberRequire(this TypeSystemEntity member, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            attribute = null;
            return member switch
            {
                MethodDesc method => DoesMethodRequire(method, requiresAttribute, out attribute),
                FieldDesc field => DoesFieldRequire(field, requiresAttribute, out attribute),
                PropertyPseudoDesc property => DoesPropertyRequire(property, requiresAttribute, out attribute),
                _ => false
            };
        }
    }
}
