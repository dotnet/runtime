// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Internal;

namespace System.ComponentModel.Composition
{
    internal static class ContractNameServices
    {
        private const char NamespaceSeparator = '.';
        private const char ArrayOpeningBracket = '[';
        private const char ArrayClosingBracket = ']';
        private const char ArraySeparator = ',';
        private const char PointerSymbol = '*';
        private const char ReferenceSymbol = '&';
        private const char GenericArityBackQuote = '`';
        private const char NestedClassSeparator = '+';
        private const char ContractNameGenericOpeningBracket = '(';
        private const char ContractNameGenericClosingBracket = ')';
        private const char ContractNameGenericArgumentSeparator = ',';
        private const char CustomModifiersSeparator = ' ';
        private const char GenericFormatOpeningBracket = '{';
        private const char GenericFormatClosingBracket = '}';

        [ThreadStatic]
        private static Dictionary<Type, string>? typeIdentityCache;

        private static Dictionary<Type, string> TypeIdentityCache
        {
            get
            {
                return typeIdentityCache = typeIdentityCache ?? new Dictionary<Type, string>();
            }
        }

        internal static string GetTypeIdentity(Type type)
        {
            return GetTypeIdentity(type, true);
        }

        internal static string GetTypeIdentity(Type type, bool formatGenericName)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (!TypeIdentityCache.TryGetValue(type, out string? typeIdentity))
            {
                if (!type.IsAbstract && type.HasBaseclassOf(typeof(Delegate)))
                {
                    MethodInfo method = type.GetMethod("Invoke")!;
                    typeIdentity = ContractNameServices.GetTypeIdentityFromMethod(method);
                }
                else if (type.IsGenericParameter)
                {
                    StringBuilder typeIdentityStringBuilder = new StringBuilder();
                    WriteTypeArgument(typeIdentityStringBuilder, false, type, formatGenericName);
                    typeIdentityStringBuilder.Remove(typeIdentityStringBuilder.Length - 1, 1);
                    typeIdentity = typeIdentityStringBuilder.ToString();
                }
                else
                {
                    StringBuilder typeIdentityStringBuilder = new StringBuilder();
                    WriteTypeWithNamespace(typeIdentityStringBuilder, type, formatGenericName);
                    typeIdentity = typeIdentityStringBuilder.ToString();
                }

                if (string.IsNullOrEmpty(typeIdentity))
                {
                    throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                }
                TypeIdentityCache.Add(type, typeIdentity);
            }

            return typeIdentity;
        }

        internal static string GetTypeIdentityFromMethod(MethodInfo method)
        {
            return GetTypeIdentityFromMethod(method, true);
        }

        internal static string GetTypeIdentityFromMethod(MethodInfo method, bool formatGenericName)
        {
            StringBuilder methodNameStringBuilder = new StringBuilder();

            WriteTypeWithNamespace(methodNameStringBuilder, method.ReturnType, formatGenericName);

            methodNameStringBuilder.Append('(');

            ParameterInfo[] parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                {
                    methodNameStringBuilder.Append(',');
                }

                WriteTypeWithNamespace(methodNameStringBuilder, parameters[i].ParameterType, formatGenericName);
            }
            methodNameStringBuilder.Append(')');

            return methodNameStringBuilder.ToString();
        }

        private static void WriteTypeWithNamespace(StringBuilder typeName, Type type, bool formatGenericName)
        {
            // Writes type with namesapce
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                typeName.Append(type.Namespace);
                typeName.Append(NamespaceSeparator);
            }
            WriteType(typeName, type, formatGenericName);
        }

        private static void WriteType(StringBuilder typeName, Type type, bool formatGenericName)
        {
            // Writes type name
            if (type.IsGenericType)
            {
                //
                // Reflection format stores all the generic arguments (including the ones for parent types) on the leaf type.
                // These arguments are placed in a queue and are written out based on generic arity (`X) of each type
                //
                Queue<Type> genericTypeArguments = new Queue<Type>(type.GetGenericArguments());
                WriteGenericType(typeName, type, type.IsGenericTypeDefinition, genericTypeArguments, formatGenericName);
                if (genericTypeArguments.Count != 0)
                {
                    throw new Exception(SR.Expecting_Empty_Queue);
                }
            }
            else
            {
                WriteNonGenericType(typeName, type, formatGenericName);
            }
        }

        private static void WriteNonGenericType(StringBuilder typeName, Type type, bool formatGenericName)
        {
            //
            // Writes non-generic type
            //
            if (type.DeclaringType != null)
            {
                WriteType(typeName, type.DeclaringType, formatGenericName);
                typeName.Append(NestedClassSeparator);
            }
            if (type.IsArray)
            {
                WriteArrayType(typeName, type, formatGenericName);
            }
            else if (type.IsPointer)
            {
                WritePointerType(typeName, type, formatGenericName);
            }
            else if (type.IsByRef)
            {
                WriteByRefType(typeName, type, formatGenericName);
            }
            else
            {
                typeName.Append(type.Name);
            }
        }

        private static void WriteArrayType(StringBuilder typeName, Type type, bool formatGenericName)
        {
            //
            // Writes array type  e.g <TypeName>[]
            // Note that jagged arrays are stored in reverse order
            // e.g. C#: Int32[][,]  Reflection: Int32[,][]
            // we are following C# order for arrays
            //
            Type rootElementType = FindArrayElementType(type);
            WriteType(typeName, rootElementType, formatGenericName);
            Type? elementType = type;
            do
            {
                WriteArrayTypeDimensions(typeName, elementType);
            }
            while ((elementType = elementType.GetElementType()) != null && elementType.IsArray);
        }

        private static void WritePointerType(StringBuilder typeName, Type type, bool formatGenericName)
        {
            //
            // Writes pointer type  e.g <TypeName>*
            //
            WriteType(typeName, type.GetElementType()!, formatGenericName);
            typeName.Append(PointerSymbol);
        }

        private static void WriteByRefType(StringBuilder typeName, Type type, bool formatGenericName)
        {
            //
            // Writes by ref type e.g <TypeName>&
            //
            WriteType(typeName, type.GetElementType()!, formatGenericName);
            typeName.Append(ReferenceSymbol);
        }

        private static void WriteArrayTypeDimensions(StringBuilder typeName, Type type)
        {
            //
            // Writes array type dimensions e.g. [,,]
            //
            typeName.Append(ArrayOpeningBracket);
            int rank = type.GetArrayRank();
            for (int i = 1; i < rank; i++)
            {
                typeName.Append(ArraySeparator);
            }
            typeName.Append(ArrayClosingBracket);
        }

        private static void WriteGenericType(StringBuilder typeName, Type type, bool isDefinition, Queue<Type> genericTypeArguments, bool formatGenericName)
        {
            //
            // Writes generic type including parent generic types
            // genericTypeArguments contains type arguments obtained from the most nested type
            // isDefinition parameter indicates if we are dealing with generic type definition
            //
            if (type.DeclaringType != null)
            {
                if (type.DeclaringType.IsGenericType)
                {
                    WriteGenericType(typeName, type.DeclaringType, isDefinition, genericTypeArguments, formatGenericName);
                }
                else
                {
                    WriteNonGenericType(typeName, type.DeclaringType, formatGenericName);
                }
                typeName.Append(NestedClassSeparator);
            }
            WriteGenericTypeName(typeName, type, isDefinition, genericTypeArguments, formatGenericName);
        }

        private static void WriteGenericTypeName(StringBuilder typeName, Type type, bool isDefinition, Queue<Type> genericTypeArguments, bool formatGenericName)
        {
            //
            // Writes generic type name, e.g. generic name and generic arguments
            //
            if (!type.IsGenericType)
            {
                throw new Exception(SR.Expecting_Generic_Type);
            }
            int genericArity = GetGenericArity(type);
            string genericTypeName = FindGenericTypeName(type.GetGenericTypeDefinition().Name);
            typeName.Append(genericTypeName);
            WriteTypeArgumentsString(typeName, genericArity, isDefinition, genericTypeArguments, formatGenericName);
        }

        private static void WriteTypeArgumentsString(StringBuilder typeName, int argumentsCount, bool isDefinition, Queue<Type> genericTypeArguments, bool formatGenericName)
        {
            //
            // Writes type arguments in brackets, e.g. (<contract_name1>, <contract_name2>, ...)
            //
            if (argumentsCount == 0)
            {
                return;
            }
            typeName.Append(ContractNameGenericOpeningBracket);
            for (int i = 0; i < argumentsCount; i++)
            {
                if (genericTypeArguments.Count == 0)
                {
                    throw new Exception(SR.Expecting_AtleastOne_Type);
                }
                Type genericTypeArgument = genericTypeArguments.Dequeue();
                WriteTypeArgument(typeName, isDefinition, genericTypeArgument, formatGenericName);
            }
            typeName.Remove(typeName.Length - 1, 1);
            typeName.Append(ContractNameGenericClosingBracket);
        }

        private static void WriteTypeArgument(StringBuilder typeName, bool isDefinition, Type genericTypeArgument, bool formatGenericName)
        {
            if (!isDefinition && !genericTypeArgument.IsGenericParameter)
            {
                WriteTypeWithNamespace(typeName, genericTypeArgument, formatGenericName);
            }

            if (formatGenericName && genericTypeArgument.IsGenericParameter)
            {
                typeName.Append(GenericFormatOpeningBracket);
                typeName.Append(genericTypeArgument.GenericParameterPosition);
                typeName.Append(GenericFormatClosingBracket);
            }
            typeName.Append(ContractNameGenericArgumentSeparator);
        }

        //internal for testability
        internal static void WriteCustomModifiers(StringBuilder typeName, string customKeyword, Type[] types, bool formatGenericName)
        {
            //
            // Writes custom modifiers in the format: customKeyword(<contract_name>,<contract_name>,...)
            //
            typeName.Append(CustomModifiersSeparator);
            typeName.Append(customKeyword);
            Queue<Type> typeArguments = new Queue<Type>(types);
            WriteTypeArgumentsString(typeName, types.Length, false, typeArguments, formatGenericName);
            if (typeArguments.Count != 0)
            {
                throw new Exception(SR.Expecting_Empty_Queue);
            }
        }

        private static Type FindArrayElementType(Type type)
        {
            //
            // Gets array element type by calling GetElementType() until the element is not an array
            //
            Type? elementType = type;
            while ((elementType = elementType.GetElementType()) != null && elementType.IsArray) { }
            return elementType!;
        }

        private static string FindGenericTypeName(string genericName)
        {
            //
            // Gets generic type name omitting the backquote and arity indicator
            // List`1 -> List
            // Arity indicator is returned as output parameter
            //
            int indexOfBackQuote = genericName.IndexOf(GenericArityBackQuote);
            if (indexOfBackQuote > -1)
            {
                genericName = genericName.Substring(0, indexOfBackQuote);
            }
            return genericName;
        }

        private static int GetGenericArity(Type type)
        {
            if (type.DeclaringType == null)
            {
                return type.GetGenericArguments().Length;
            }

            // The generic arity is equal to the difference in the number of generic arguments
            // from the type and the declaring type.

            int delclaringTypeGenericArguments = type.DeclaringType.GetGenericArguments().Length;
            int typeGenericArguments = type.GetGenericArguments().Length;

            if (typeGenericArguments < delclaringTypeGenericArguments)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            return typeGenericArguments - delclaringTypeGenericArguments;
        }
    }
}
