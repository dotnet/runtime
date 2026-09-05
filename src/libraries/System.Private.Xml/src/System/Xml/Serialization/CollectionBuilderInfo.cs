// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Xml.Serialization
{
    // The factory method named by a CollectionBuilderAttribute on a collection type.
    //
    // XmlSerializer normally fills a collection in place, by constructing it and then calling Add or assigning
    // through its default indexer. Read-only and immutable collections cannot be populated that way. When such a
    // type opts in to a collection builder, the readers accumulate elements into a temporary array and create the
    // real collection from that array once the whole collection has been read.
    //
    // The attribute's contract requires a Create(ReadOnlySpan<T>) overload, so that is the one used. Builders
    // commonly offer a Create(T[]) overload too, which the reflection-based reader falls back to when dynamic code
    // is unavailable and it therefore cannot close a generic type over the element type.
    internal sealed class CollectionBuilderInfo
    {
        private const string CollectionBuilderAttributeFullName = "System.Runtime.CompilerServices.CollectionBuilderAttribute";

        private readonly MethodInfo? _arrayFactory;
        private Func<Array, object>? _build;

        private CollectionBuilderInfo(Type elementType, MethodInfo spanFactory, MethodInfo spanConversion, MethodInfo? arrayFactory)
        {
            ElementType = elementType;
            _arrayFactory = arrayFactory;
            SpanFactory = spanFactory;
            SpanConversion = spanConversion;
        }

        // The element type the factory method accepts.
        internal Type ElementType { get; }

        // Create(ReadOnlySpan<T>).
        internal MethodInfo SpanFactory { get; }

        // The implicit T[] to ReadOnlySpan<T> conversion, which gets the generating readers from the array they
        // accumulate into to the span SpanFactory takes.
        internal MethodInfo SpanConversion { get; }

        // The C# expression the source generating reader emits to create the collection, up to but not including
        // the argument that supplies its elements.
        internal string GetCSharpFactoryCall()
        {
            string name = $"{CodeIdentifier.GetCSharpName(SpanFactory.DeclaringType!)}.{SpanFactory.Name}";

            if (SpanFactory.IsGenericMethod)
            {
                Type[] arguments = SpanFactory.GetGenericArguments();
                string[] names = new string[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    names[i] = CodeIdentifier.GetCSharpName(arguments[i]);
                }

                name = $"{name}<{string.Join(", ", names)}>";
            }

            // Builders that take a span usually offer a params array overload too, so the argument is cast to make
            // the call unambiguous.
            return $"{name}((global::System.ReadOnlySpan<{CodeIdentifier.GetCSharpName(ElementType)}>)";
        }

        // Returns null when the type is not attributed, or when no factory method matching the attribute and the
        // shape the readers need could be resolved. Either way the collection stays unsupported, which is the
        // behavior that predates collection builder support.
        [RequiresUnreferencedCode("Resolves the factory method named by CollectionBuilderAttribute.")]
        internal static CollectionBuilderInfo? Find(Type collectionType)
        {
            if (!TryGetBuilderAttribute(collectionType, out Type? builderType, out string? methodName))
            {
                return null;
            }

            Type[] typeArguments = collectionType.IsGenericType ? collectionType.GetGenericArguments() : Type.EmptyTypes;
            MethodInfo? spanFactory = null;
            MethodInfo? arrayFactory = null;

            foreach (MethodInfo candidate in builderType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!candidate.Name.Equals(methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                MethodInfo? factory = Close(candidate, typeArguments);
                if (factory == null || !collectionType.IsAssignableFrom(factory.ReturnType))
                {
                    continue;
                }

                ParameterInfo[] parameters = factory.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                Type parameterType = parameters[0].ParameterType;
                if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
                {
                    spanFactory ??= factory;
                }
                else if (parameterType.IsArray && parameterType.GetArrayRank() == 1)
                {
                    arrayFactory ??= factory;
                }
            }

            if (spanFactory == null)
            {
                return null;
            }

            Type elementType = spanFactory.GetParameters()[0].ParameterType.GetGenericArguments()[0];
            if (elementType.IsPointer || elementType.IsByRef || elementType.ContainsGenericParameters)
            {
                return null;
            }

            MethodInfo? spanConversion = FindSpanConversion(spanFactory.GetParameters()[0].ParameterType, elementType);
            if (spanConversion == null)
            {
                return null;
            }

            // Only keep the array overload when it agrees with the span overload, so the two paths cannot disagree
            // about what the collection contains.
            if (arrayFactory != null && arrayFactory.GetParameters()[0].ParameterType.GetElementType() != elementType)
            {
                arrayFactory = null;
            }

            return new CollectionBuilderInfo(elementType, spanFactory, spanConversion, arrayFactory);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Every XmlSerializer entry point that can reach this code is annotated with RequiresDynamicCode. " +
                            "The array type is only used to look the conversion up; failing to find it leaves the collection " +
                            "unsupported, which is the behavior that predates collection builder support.")]
        private static MethodInfo? FindSpanConversion(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type spanType,
            Type elementType) =>
            spanType.GetMethod(
                "op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                new Type[] { elementType.MakeArrayType() },
                modifiers: null);

        // Creates the array the readers accumulate elements into.
        [RequiresUnreferencedCode("Creates an array of the collection's element type.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Every XmlSerializer entry point that can reach this code is annotated with RequiresDynamicCode.")]
        internal Array CreateAccumulator(int length) => Array.CreateInstance(ElementType, length);

        // Creates the collection from the accumulated elements.
        [RequiresUnreferencedCode("Invokes the factory method named by CollectionBuilderAttribute.")]
        internal object Build(Array elements) => (_build ??= CreateBuilder())(elements);

        [RequiresUnreferencedCode("Invokes the factory method named by CollectionBuilderAttribute.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The generic instantiation is only created when dynamic code is supported, or when the builder " +
                            "offers no array overload to fall back to.")]
        private Func<Array, object> CreateBuilder()
        {
            // Calling the span overload means closing a generic type over the element type. Where that is not
            // possible, an array overload lets the reflection-based reader keep working without instantiating any
            // generic code.
            if (_arrayFactory is MethodInfo arrayFactory && !RuntimeFeature.IsDynamicCodeSupported)
            {
                return elements => arrayFactory.Invoke(null, new object?[] { elements })!;
            }

            var invoker = (SpanInvoker)Activator.CreateInstance(
                typeof(SpanInvoker<,>).MakeGenericType(ElementType, SpanFactory.ReturnType), SpanFactory)!;
            return invoker.Build;
        }

        private static bool TryGetBuilderAttribute(
            Type type,
            [NotNullWhen(true)] out Type? builderType,
            [NotNullWhen(true)] out string? methodName)
        {
            builderType = null;
            methodName = null;

            // CollectionBuilderAttribute is not inherited, and reading it through CustomAttributeData avoids
            // instantiating the attribute or forcing types to load just to answer the question.
            foreach (CustomAttributeData attribute in type.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName != CollectionBuilderAttributeFullName)
                {
                    continue;
                }

                IList<CustomAttributeTypedArgument> arguments = attribute.ConstructorArguments;
                if (arguments.Count != 2)
                {
                    continue;
                }

                builderType = arguments[0].Value as Type;
                methodName = arguments[1].Value as string;
                return builderType != null && !string.IsNullOrEmpty(methodName);
            }

            return false;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Every XmlSerializer entry point that can reach this code is annotated with RequiresDynamicCode. " +
                            "A failure to resolve the factory method is caught here and simply leaves the collection unsupported, " +
                            "which is the behavior that predates collection builder support.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "Every XmlSerializer entry point that can reach this code is annotated with RequiresUnreferencedCode. " +
                            "A failure to resolve the factory method is caught here and simply leaves the collection unsupported, " +
                            "which is the behavior that predates collection builder support.")]
        private static MethodInfo? Close(MethodInfo candidate, Type[] typeArguments)
        {
            if (!candidate.IsGenericMethodDefinition)
            {
                return typeArguments.Length == 0 ? candidate : null;
            }

            if (typeArguments.Length == 0 || candidate.GetGenericArguments().Length != typeArguments.Length)
            {
                return null;
            }

            try
            {
                return candidate.MakeGenericMethod(typeArguments);
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException or InvalidOperationException)
            {
                // The builder method's constraints reject the collection's type arguments, or this instantiation is
                // not available without dynamic code.
                return null;
            }
        }

        private delegate TCollection SpanBuilder<TElement, TCollection>(ReadOnlySpan<TElement> values);

        private abstract class SpanInvoker
        {
            internal abstract object Build(Array elements);
        }

        private sealed class SpanInvoker<TElement, TCollection> : SpanInvoker
        {
            private readonly SpanBuilder<TElement, TCollection> _factory;

            public SpanInvoker(MethodInfo factory) => _factory = factory.CreateDelegate<SpanBuilder<TElement, TCollection>>();

            internal override object Build(Array elements) => _factory((TElement[])elements)!;
        }
    }
}
