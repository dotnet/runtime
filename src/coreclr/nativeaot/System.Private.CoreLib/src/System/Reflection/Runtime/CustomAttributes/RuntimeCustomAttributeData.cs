// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Runtime.CustomAttributes
{
    //
    // Common base class for the Runtime's implementation of CustomAttributeData.
    //
    internal abstract partial class RuntimeCustomAttributeData : CustomAttributeData
    {
        public abstract override Type AttributeType { get; }

        public abstract override ConstructorInfo Constructor { get; }

        public sealed override IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                return new ReadOnlyCollection<CustomAttributeTypedArgument>(GetConstructorArguments(throwIfMissingMetadata: true));
            }
        }

        // Equals/GetHashCode no need to override (they just implement reference equality but desktop never unified these things.)

        public sealed override IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                return new ReadOnlyCollection<CustomAttributeNamedArgument>(GetNamedArguments(throwIfMissingMetadata: true));
            }
        }

        public sealed override string ToString()
        {
            string ctorArgs = "";
            IList<CustomAttributeTypedArgument> constructorArguments = GetConstructorArguments(throwIfMissingMetadata: false);
            if (constructorArguments == null)
                return LastResortToString;
            for (int i = 0; i < constructorArguments.Count; i++)
                ctorArgs += string.Format(i == 0 ? "{0}" : ", {0}", ComputeTypedArgumentString(constructorArguments[i], typed: false));

            string namedArgs = "";
            IList<CustomAttributeNamedArgument> namedArguments = GetNamedArguments(throwIfMissingMetadata: false);
            if (namedArguments == null)
                return LastResortToString;
            for (int i = 0; i < namedArguments.Count; i++)
            {
                CustomAttributeNamedArgument namedArgument = namedArguments[i];

                // Legacy: Desktop sets "typed" to "namedArgument.ArgumentType != typeof(Object)" - on Project N, this property is not available
                // (nor conveniently computable as it's not captured in the Project N metadata.) The only consequence is that for
                // the rare case of fields and properties typed "Object", we won't decorate the argument value with its actual type name.
                bool typed = true;
                namedArgs += string.Format(
                    i == 0 && ctorArgs.Length == 0 ? "{0} = {1}" : ", {0} = {1}",
                    namedArgument.MemberName,
                    ComputeTypedArgumentString(namedArgument.TypedValue, typed));
            }

            return string.Format("[{0}({1}{2})]", AttributeType.FormatTypeNameForReflection(), ctorArgs, namedArgs);
        }

        protected static ConstructorInfo ResolveAttributeConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type attributeType, Type[] parameterTypes)
        {
            int parameterCount = parameterTypes.Length;
            foreach (ConstructorInfo candidate in attributeType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ParameterInfo[] candidateParameters = candidate.GetParametersNoCopy();
                if (parameterCount != candidateParameters.Length)
                    continue;

                for (int i = 0; i < parameterCount; i++)
                {
                    if (!parameterTypes[i].Equals(candidateParameters[i]))
                        continue;
                }

                return candidate;
            }

            throw new MissingMethodException();
        }

        //
        // If throwIfMissingMetadata is false, returns null rather than throwing a missing metadata exception.
        //
        internal abstract IList<CustomAttributeTypedArgument> GetConstructorArguments(bool throwIfMissingMetadata);

        //
        // If throwIfMissingMetadata is false, returns null rather than throwing a missing metadata exception.
        //
        internal abstract IList<CustomAttributeNamedArgument> GetNamedArguments(bool throwIfMissingMetadata);

        //
        // Computes the ToString() value for a CustomAttributeTypedArgument struct.
        //
        private static string ComputeTypedArgumentString(CustomAttributeTypedArgument cat, bool typed)
        {
            Type argumentType = cat.ArgumentType;
            if (argumentType == null)
                return cat.ToString();

            object? value = cat.Value;
            if (argumentType.IsEnum)
                return string.Format(typed ? "{0}" : "({1}){0}", value, argumentType.FullName);

            if (value == null)
                return string.Format(typed ? "null" : "({0})null", argumentType.Name);

            if (argumentType == typeof(string))
                return string.Format("\"{0}\"", value);

            if (argumentType == typeof(char))
                return string.Format("'{0}'", value);

            if (argumentType == typeof(Type))
                return string.Format("typeof({0})", ((Type)value).FullName);

            else if (argumentType.IsArray)
            {
                IList<CustomAttributeTypedArgument> array = (IList<CustomAttributeTypedArgument>)value;

                Type elementType = argumentType.GetElementType()!;
                string result = string.Format(@"new {0}[{1}] {{ ", elementType.IsEnum ? elementType.FullName : elementType.Name, array.Count);

                for (int i = 0; i < array.Count; i++)
                    result += string.Format(i == 0 ? "{0}" : ", {0}", ComputeTypedArgumentString(array[i], elementType != typeof(object)));

                return result += " }";
            }

            return string.Format(typed ? "{0}" : "({1}){0}", value, argumentType.Name);
        }

        private string LastResortToString
        {
            get
            {
                // This emulates Object.ToString() for consistency with prior .Net Native implementations.
                return GetType().ToString();
            }
        }

        //
        // Wrap a custom attribute argument (or an element of an array-typed custom attribute argument) in a CustomAttributeTypeArgument structure
        // for insertion into a CustomAttributeData value.
        //
        protected CustomAttributeTypedArgument WrapInCustomAttributeTypedArgument(object? value, Type argumentType)
        {
            if (argumentType == typeof(object))
            {
                // If the declared attribute type is System.Object, we must report the type based on the runtime value.
                if (value == null)
                    argumentType = typeof(string);  // Why is null reported as System.String? Because that's what the desktop CLR does.
                else if (value is Type)
                    argumentType = typeof(Type);    // value.GetType() will not actually be System.Type - rather it will be some internal implementation type. We only want to report it as System.Type.
                else
                    argumentType = value.GetType();
            }

            // Handle the array case
            if (value is IEnumerable enumerableValue && !(value is string))
            {
                if (!argumentType.IsArray)
                    throw new BadImageFormatException();
                Type reportedElementType = argumentType.GetElementType()!;
                LowLevelListWithIList<CustomAttributeTypedArgument> elementTypedArguments = new LowLevelListWithIList<CustomAttributeTypedArgument>();
                foreach (object elementValue in enumerableValue)
                {
                    CustomAttributeTypedArgument elementTypedArgument = WrapInCustomAttributeTypedArgument(elementValue, reportedElementType);
                    elementTypedArguments.Add(elementTypedArgument);
                }
                return new CustomAttributeTypedArgument(argumentType, new ReadOnlyCollection<CustomAttributeTypedArgument>(elementTypedArguments));
            }
            else
            {
                return new CustomAttributeTypedArgument(argumentType, value);
            }
        }
    }
}
