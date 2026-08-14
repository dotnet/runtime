// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Xml.Serialization
{
    /// <summary>
    /// Describes the factory method named by a <see cref="CollectionBuilderAttribute"/> on a collection type.
    /// </summary>
    /// <remarks>
    /// <see cref="XmlSerializer"/> normally fills a collection in place, by constructing it and then calling
    /// <c>Add</c> or assigning through its default indexer. Read-only and immutable collections cannot be
    /// populated that way. When such a type opts in to a collection builder, the readers instead accumulate
    /// elements in a temporary array and create the real collection from that array once the whole collection
    /// has been read.
    /// </remarks>
    internal sealed class CollectionBuilderInfo
    {
        private const string CollectionBuilderAttributeFullName = "System.Runtime.CompilerServices.CollectionBuilderAttribute";

        private CollectionBuilderInvoker? _invoker;

        private CollectionBuilderInfo(Type elementType, MethodInfo? spanFactory, MethodInfo? arrayFactory, MethodInfo? spanConversion)
        {
            ElementType = elementType;
            SpanFactory = spanFactory;
            ArrayFactory = arrayFactory;
            SpanConversion = spanConversion;
        }

        /// <summary>The type of the elements the factory method accepts.</summary>
        internal Type ElementType { get; }

        /// <summary>
        /// The <c>Create(ReadOnlySpan&lt;T&gt;)</c> overload. This is the shape the
        /// <see cref="CollectionBuilderAttribute"/> contract documents, so it is always preferred.
        /// </summary>
        internal MethodInfo? SpanFactory { get; }

        /// <summary>
        /// The <c>Create(T[])</c> overload, when the builder offers one. Only used by the reflection-based
        /// serializer when dynamic code is unavailable, because invoking the span overload requires closing
        /// a generic type over the element type.
        /// </summary>
        internal MethodInfo? ArrayFactory { get; }

        /// <summary>The factory method the IL and source generating serializers should call.</summary>
        internal MethodInfo Factory => SpanFactory ?? ArrayFactory!;

        /// <summary>Whether <see cref="Factory"/> accepts a <see cref="ReadOnlySpan{T}"/> rather than an array.</summary>
        [MemberNotNullWhen(true, nameof(SpanConversion))]
        internal bool FactoryTakesSpan => SpanFactory != null;

        /// <summary>
        /// The implicit conversion from the element array the readers accumulate into to the
        /// <see cref="ReadOnlySpan{T}"/> that <see cref="Factory"/> takes. Null when the factory takes an array.
        /// </summary>
        internal MethodInfo? SpanConversion { get; }

        /// <summary>
        /// The C# expression the source generating serializer emits to create the collection, up to but not
        /// including the argument that supplies its elements.
        /// </summary>
        internal string GetCSharpFactoryCall()
        {
            string name = $"{CodeIdentifier.GetCSharpName(Factory.DeclaringType!)}.{Factory.Name}";

            if (Factory.IsGenericMethod)
            {
                Type[] arguments = Factory.GetGenericArguments();
                string[] names = new string[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    names[i] = CodeIdentifier.GetCSharpName(arguments[i]);
                }

                name = $"{name}<{string.Join(", ", names)}>";
            }

            // The builders that take a span usually offer a params array overload too, so the argument is cast
            // to make the call unambiguous.
            return FactoryTakesSpan
                ? $"{name}((global::System.ReadOnlySpan<{CodeIdentifier.GetCSharpName(ElementType)}>)"
                : $"{name}(";
        }

        /// <summary>
        /// Looks for a usable collection builder on <paramref name="collectionType"/>, returning <see langword="null"/>
        /// when the type is not attributed or no factory method matching the attribute could be resolved.
        /// </summary>
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

            Type? elementType = spanFactory != null
                ? spanFactory.GetParameters()[0].ParameterType.GetGenericArguments()[0]
                : arrayFactory?.GetParameters()[0].ParameterType.GetElementType();

            if (elementType == null || elementType.IsPointer || elementType.IsByRef || elementType.ContainsGenericParameters)
            {
                return null;
            }

            // Only keep the array overload when it agrees with the span overload, so that the two paths
            // cannot disagree about what the collection contains.
            if (spanFactory != null && arrayFactory != null && arrayFactory.GetParameters()[0].ParameterType.GetElementType() != elementType)
            {
                arrayFactory = null;
            }

            MethodInfo? spanConversion = null;
            if (spanFactory != null)
            {
                spanConversion = FindSpanConversion(spanFactory.GetParameters()[0].ParameterType, elementType);

                if (spanConversion == null)
                {
                    // Without the conversion the span overload cannot be called from generated code.
                    if (arrayFactory == null)
                    {
                        return null;
                    }

                    spanFactory = null;
                }
            }

            return new CollectionBuilderInfo(elementType, spanFactory, arrayFactory, spanConversion);
        }

        /// <summary>
        /// Finds the implicit conversion from the element array the readers accumulate into to the span the
        /// factory method takes.
        /// </summary>
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

        /// <summary>Creates the collection described by this builder from the accumulated elements.</summary>
        [RequiresUnreferencedCode("Invokes the factory method named by CollectionBuilderAttribute.")]
        internal object Build(Array elements) => (_invoker ??= CreateInvoker()).Build(elements);

        /// <summary>Creates the empty array the readers accumulate elements into.</summary>
        [RequiresUnreferencedCode("Creates an array of the collection's element type.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Every XmlSerializer entry point that can reach this code is annotated with RequiresDynamicCode.")]
        internal Array CreateAccumulator(int length) => Array.CreateInstance(ElementType, length);

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
                // The builder method's constraints reject the collection's type arguments, or this
                // instantiation is not available without dynamic code.
                return null;
            }
        }

        [RequiresUnreferencedCode("Invokes the factory method named by CollectionBuilderAttribute.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The generic instantiation is only created when dynamic code is supported, or when the builder " +
                            "offers no array overload to fall back to.")]
        private CollectionBuilderInvoker CreateInvoker()
        {
            // The span overload is preferred, but invoking it means closing a generic type over the element
            // type. Where that is not possible, an array overload lets the reflection-based serializer keep
            // working without emitting or instantiating any generic code.
            if (SpanFactory != null && (ArrayFactory == null || RuntimeFeature.IsDynamicCodeSupported))
            {
                Type invokerType = typeof(SpanInvoker<,>).MakeGenericType(ElementType, SpanFactory.ReturnType);
                return (CollectionBuilderInvoker)Activator.CreateInstance(invokerType, SpanFactory)!;
            }

            return new ArrayInvoker(ArrayFactory!);
        }

        private delegate TCollection SpanBuilder<TElement, TCollection>(ReadOnlySpan<TElement> values);

        private abstract class CollectionBuilderInvoker
        {
            internal abstract object Build(Array elements);
        }

        private sealed class ArrayInvoker : CollectionBuilderInvoker
        {
            private readonly MethodInfo _factory;

            internal ArrayInvoker(MethodInfo factory) => _factory = factory;

            internal override object Build(Array elements) => _factory.Invoke(null, new object?[] { elements })!;
        }

        private sealed class SpanInvoker<TElement, TCollection> : CollectionBuilderInvoker
        {
            private readonly SpanBuilder<TElement, TCollection> _factory;

            public SpanInvoker(MethodInfo factory) => _factory = factory.CreateDelegate<SpanBuilder<TElement, TCollection>>();

            internal override object Build(Array elements) => _factory((TElement[])elements)!;
        }
    }
}
