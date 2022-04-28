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
            return $"#{origin.Index}";
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
                default:
                    Debug.Fail(member.GetType().ToString());
                    break;
            }
            if (!decoded.HasValue)
                return false;

            attribute = decoded.Value;
            return true;
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
        /// <remarks>Unlike <see cref="DoesMethodRequires(MethodDesc, string, out CustomAttributeValue?)"/>
        /// if a declaring type has Requires, all methods in that type are considered "in scope" of that Requires. So this includes also
        /// instance methods (not just statics and .ctors).</remarks>
        internal static bool IsMethodInRequiresScope(this MethodDesc method, string requiresAttribute)
        {
            if (method.HasCustomAttribute("System.Diagnostics.CodeAnalysis", requiresAttribute) && !method.IsStaticConstructor)
                return true;

            if (method.OwningType is TypeDesc type && TryGetRequiresAttribute(type, requiresAttribute, out _))
                return true;

            return false;
        }

        /// <summary>
		/// Determines if method requires (and thus any usage of such method should be warned about).
		/// </summary>
		/// <remarks>Unlike <see cref="IsMethodInRequiresScope(MethodDesc, string)"/> only static methods 
		/// and .ctors are reported as requires when the declaring type has Requires on it.</remarks>
		internal static bool DoesMethodRequires(this MethodDesc method, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            attribute = null;
            if (method.IsStaticConstructor)
                return false;

            if (TryGetRequiresAttribute(method, requiresAttribute, out attribute))
                return true;

            if ((method.Signature.IsStatic || method.IsConstructor) && method.OwningType is not null &&
                TryGetRequiresAttribute(method.OwningType, requiresAttribute, out attribute))
                return true;

            return false;
        }

        internal static bool DoesFieldRequires(this FieldDesc field, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            if (!field.IsStatic || field.OwningType is null)
            {
                attribute = null;
                return false;
            }

            return TryGetRequiresAttribute(field.OwningType, requiresAttribute, out attribute);
        }

        internal static bool DoesMemberRequires(this TypeSystemEntity member, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            attribute = null;
            return member switch
            {
                MethodDesc method => DoesMethodRequires(method, requiresAttribute, out attribute),
                FieldDesc field => DoesFieldRequires(field, requiresAttribute, out attribute),
                _ => false
            };
        }
    }
}
