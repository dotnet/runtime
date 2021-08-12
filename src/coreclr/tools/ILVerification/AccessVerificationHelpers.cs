// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILVerify
{
    internal static class AccessVerificationHelpers
    {
        /// <summary>
        /// Returns whether the class <paramref name="currentClass"/> can access the class <paramref name="targetClass"/>.
        /// </summary>
        internal static bool CanAccess(this TypeDesc currentClass, TypeDesc targetClass)
        {
            if (targetClass.IsGenericParameter || targetClass.IsSignatureVariable)
                return true; // Generic parameters are always accessible

            if (targetClass.IsParameterizedType)
                return currentClass.CanAccess(((ParameterizedType)targetClass).ParameterType);

            if (targetClass.IsFunctionPointer)
                return currentClass.CanAccessSignature(((FunctionPointerType)targetClass).Signature);

            // Check access to class instantiations if generic class
            if (targetClass.HasInstantiation && !currentClass.CanAccessInstantiation(targetClass.Instantiation))
                return false;

            var currentTypeDef = (MetadataType)currentClass.GetTypeDefinition();
            var targetTypeDef = (EcmaType)targetClass.GetTypeDefinition();

            var targetContainingType = targetTypeDef.ContainingType;
            if (targetContainingType == null)
            {
                // a non-nested class can be either all public or accessible only from its own assembly (and friends)
                if ((targetTypeDef.Attributes & TypeAttributes.Public) != 0)
                    return true;
                else
                    return currentTypeDef.Module == targetTypeDef.Module || targetTypeDef.Module.GrantsFriendAccessTo(currentTypeDef.Module);
            }

            // Target class is nested
            MethodAttributes visibility = NestedToMethodAccessAttribute(targetTypeDef.Attributes);

            // Translate access check into member access check, i.e. check whether the current class can access
            // a member of the enclosing class with the visibility of target class
            return currentTypeDef.CanAccessMember(targetContainingType, visibility, null);
        }

        /// <summary>
        /// Returns whether the class '<paramref name="currentClass"/>' can access the method '<paramref name="targetMethod"/>' through
        /// the instance '<paramref name="instance"/>'. The instance can be null, if the method to be accessed is static.
        /// </summary>
        internal static bool CanAccess(this TypeDesc currentType, MethodDesc targetMethod, TypeDesc instance = null)
        {
            // If generic method, check instantiation access
            if (targetMethod.HasInstantiation && !currentType.CanAccessInstantiation(targetMethod.Instantiation))
                return false;

            var targetMethodDef = targetMethod.GetTypicalMethodDefinition() as EcmaMethod;
            var currentTypeDef = (MetadataType)currentType.GetTypeDefinition();

            if (targetMethodDef != null) // Non metadata methods, such as ArrayMethods, may be null at this point
            {
                if (!currentTypeDef.CanAccessMember(targetMethod.OwningType, targetMethodDef.Attributes & MethodAttributes.MemberAccessMask, instance))
                    return false;
            }

            return currentTypeDef.CanAccessSignature(targetMethod.Signature);
        }

        /// <summary>
        /// Returns whether the class '<paramref name="currentClass"/>' can access the field '<paramref name="targetField"/>' through
        /// the instance '<paramref name="instance"/>'. The instance can be null, if the field to be accessed is static.
        /// </summary>
        internal static bool CanAccess(this TypeDesc currentType, FieldDesc targetField, TypeDesc instance = null)
        {
            // Check access to field owning type
            var targetFieldDef = (EcmaField)targetField.GetTypicalFieldDefinition();
            var currentTypeDef = (MetadataType)currentType.GetTypeDefinition();

            var targetFieldAccess = FieldToMethodAccessAttribute(targetFieldDef.Attributes);

            if (!currentTypeDef.CanAccessMember(targetField.OwningType, targetFieldAccess, instance))
                return false;

            // Check access to field type itself
            return currentType.CanAccess(targetField.FieldType);
        }

        private static bool CanAccessMember(this MetadataType currentType, TypeDesc targetType, MethodAttributes memberVisibility, TypeDesc instance)
        {
            if (instance == null)
                instance = currentType;

            // Check access to class defining member
            if (!currentType.CanAccess(targetType))
                return false;

            var targetTypeDef = (MetadataType)targetType.GetTypeDefinition();

            if (memberVisibility == MethodAttributes.Public)
                return true;

            // This is module-scope checking, to support C++ file & function statics.
            if (memberVisibility == MethodAttributes.PrivateScope)
                return currentType.Module == targetTypeDef.Module;

            if (memberVisibility == MethodAttributes.Assembly)
                return currentType.Module == targetTypeDef.Module || targetTypeDef.Module.GrantsFriendAccessTo(currentType.Module);

            if (memberVisibility == MethodAttributes.FamANDAssem)
            {
                if (currentType.Module != targetTypeDef.Module && !targetTypeDef.Module.GrantsFriendAccessTo(currentType.Module))
                    return false;
            }

            // Nested classes can access all members of their parent class.
            do
            {
                // Classes have access to all of their own members
                if (currentType == targetTypeDef)
                    return true;

                switch (memberVisibility)
                {
                    case MethodAttributes.FamORAssem:
                        if (currentType.Module == targetTypeDef.Module || targetTypeDef.Module.GrantsFriendAccessTo(currentType.Module))
                            return true;

                        // Check if current class is subclass of target
                        if (CanAccessFamily(currentType, targetTypeDef, instance))
                            return true;
                        break;
                    case MethodAttributes.Family:
                    case MethodAttributes.FamANDAssem:
                        // Assembly access was already checked earlier, so only need to check family access
                        if (CanAccessFamily(currentType, targetTypeDef, instance))
                            return true;
                        break;
                    case MethodAttributes.Private:
                        break; // Already handled by loop
                    default:
                        Debug.Assert(false);
                        break;
                }

                var containingType = currentType.ContainingType;
                if (containingType != null)
                    currentType = (MetadataType)containingType.GetTypeDefinition();
                else
                    currentType = null;
            } while (currentType != null);

            return false;
        }

        private static bool CanAccessInstantiation(this TypeDesc currentType, Instantiation instantiation)
        {
            foreach (var inst in instantiation)
            {
                if (!currentType.CanAccess(inst))
                    return false;
            }

            return true;
        }

        private static bool CanAccessSignature(this TypeDesc currentType, MethodSignature signature)
        {
            if (!currentType.CanAccess(signature.ReturnType))
                return false;

            for (int i = 0; i < signature.Length; ++i)
            {
                if (!currentType.CanAccess(signature[i]))
                    return false;
            }

            return true;
        }

        private static bool CanAccessFamily(TypeDesc currentType, TypeDesc targetTypeDef, TypeDesc instanceType)
        {
            // if instanceType is generics and inherit from targetTypeDef members of targetTypeDef are accessible
            if (instanceType.IsGenericParameter)
            {
                return instanceType.CanCastTo(targetTypeDef);
            }

            // Iterate through all containing types of instance
            while (instanceType != null)
            {
                var curInstTypeDef = instanceType;
                var currentTypeDef = currentType.GetTypeDefinition();
                // Iterate through all super types of current instance type
                while (curInstTypeDef != null)
                {
                    if (currentTypeDef == curInstTypeDef.GetTypeDefinition())
                    {
                        // At this point we know that the instance type is able to access the same family fields as current type
                        // Now iterate through all super types of current type to see if current type can access family target type
                        while (currentTypeDef != null)
                        {
                            if (currentTypeDef == targetTypeDef)
                                return true;

                            currentTypeDef = currentTypeDef.BaseType;
                            if (currentTypeDef != null)
                                currentTypeDef = currentTypeDef.GetTypeDefinition();
                        }

                        return false;
                    }

                    curInstTypeDef = curInstTypeDef.BaseType;
                }

                instanceType = ((MetadataType)instanceType.GetTypeDefinition()).ContainingType;
            }

            return false;
        }

        private static EcmaAssembly ToEcmaAssembly(this ModuleDesc module)
        {
            return module.Assembly as EcmaAssembly;
        }

        private static bool GrantsFriendAccessTo(this ModuleDesc module, ModuleDesc friendModule)
        {
            var assembly = module.ToEcmaAssembly();
            if (assembly != null)
            {
                var friendAssembly = friendModule.ToEcmaAssembly();
                if (assembly == friendAssembly)
                {
                    return true;
                }
                var friendName = friendAssembly.GetName();

                foreach (var attribute in assembly.GetDecodedCustomAttributes("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"))
                {
                    AssemblyName friendAttributeName = new AssemblyName((string)attribute.FixedArguments[0].Value);
                    if (!friendName.Name.Equals(friendAttributeName.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Comparing PublicKeyToken, since GetPublicKey returns null due to a bug
                    if (IsSamePublicKey(friendAttributeName.GetPublicKeyToken(), friendName.GetPublicKeyToken()))
                        return true;
                }
            }
            return false;
        }

        private static bool IsSamePublicKey(byte[] key1, byte[] key2)
        {
            if (key1 == null)
                return key2 == null || key2.Length == 0;
            if (key2 == null)
                return key1 == null || key1.Length == 0;

            if (key1.Length != key2.Length)
                return false;

            for (int i = 0; i < key1.Length; ++i)
            {
                if (key1[i] != key2[i])
                    return false;
            }

            return true;
        }

        private static MethodAttributes NestedToMethodAccessAttribute(TypeAttributes nestedVisibility)
        {
            switch (nestedVisibility & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NestedAssembly:
                    return MethodAttributes.Assembly;
                case TypeAttributes.NestedFamANDAssem:
                    return MethodAttributes.FamANDAssem;
                case TypeAttributes.NestedFamily:
                    return MethodAttributes.Family;
                case TypeAttributes.NestedFamORAssem:
                    return MethodAttributes.FamORAssem;
                case TypeAttributes.NestedPrivate:
                    return MethodAttributes.Private;
                case TypeAttributes.NestedPublic:
                    return MethodAttributes.Public;
                default:
                    Debug.Assert(false);
                    return MethodAttributes.Public;
            }
        }

        private static MethodAttributes FieldToMethodAccessAttribute(FieldAttributes attributes)
        {
            switch (attributes & FieldAttributes.FieldAccessMask)
            {
                case FieldAttributes.Assembly:
                    return MethodAttributes.Assembly;
                case FieldAttributes.FamANDAssem:
                    return MethodAttributes.FamANDAssem;
                case FieldAttributes.Family:
                    return MethodAttributes.Family;
                case FieldAttributes.FamORAssem:
                    return MethodAttributes.FamORAssem;
                case FieldAttributes.Private:
                    return MethodAttributes.Private;
                case FieldAttributes.PrivateScope:
                    return MethodAttributes.PrivateScope;
                case FieldAttributes.Public:
                    return MethodAttributes.Public;
                default:
                    Debug.Assert(false);
                    return MethodAttributes.Public;
            }
        }
    }
}
