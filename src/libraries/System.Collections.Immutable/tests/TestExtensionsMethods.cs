// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Collections.Immutable.Tests
{
    internal static partial class TestExtensionsMethods
    {
        private static readonly double s_GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        internal static void ValidateDefaultThisBehavior(Action a)
        {
            Assert.Throws<NullReferenceException>(a);
        }

        internal static void ValidateDefaultThisBehavior<TArg>(ReadOnlySpan<TArg> span, AssertExtensions.AssertThrowsActionReadOnly<TArg> action)
        {
            try
            {
                action(span);
            }
            catch (NullReferenceException nullRefEx) when (nullRefEx.GetType() == typeof(NullReferenceException))
            {
                return;
            }
            catch (Exception ex)
            {
                throw ThrowsException.ForIncorrectExceptionType(typeof(NullReferenceException), ex);
            }

            throw ThrowsException.ForNoException(typeof(NullReferenceException));
        }

        internal static IBinaryTree<T> GetBinaryTreeProxy<T>(this IReadOnlyCollection<T> value)
        {
            FieldInfo rootField = value.GetType().GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(rootField);

            object root = rootField.GetValue(value);
            Assert.NotNull(root);

            Type interfaceType = root.GetType().GetInterface(nameof(IBinaryTree));
            Assert.NotNull(interfaceType);

            return new BinaryTreeReflectionProxy<T>(root, interfaceType);
        }

        private sealed class BinaryTreeReflectionProxy<T>(object underlyingValue, Type interfaceType) : IBinaryTree<T>
        {
            private TValue GetProperty<TValue>(string propertyName)
                => (TValue)interfaceType.GetProperty(propertyName)!.GetValue(underlyingValue);

            public int Height => GetProperty<int>(nameof(Height));
            public bool IsEmpty => GetProperty<bool>(nameof(IsEmpty));
            public int Count => GetProperty<int>(nameof(Count));

            public T Value => GetProperty<T>(nameof(Value));
            public IBinaryTree<T>? Left => GetProperty<object?>(nameof(Left)) is { } leftValue
                ? new BinaryTreeReflectionProxy<T>(leftValue, interfaceType)
                : null;

            public IBinaryTree<T>? Right => GetProperty<object?>(nameof(Right)) is { } rightValue
                ? new BinaryTreeReflectionProxy<T>(rightValue, interfaceType)
                : null;

            IBinaryTree? IBinaryTree.Left => Left;
            IBinaryTree? IBinaryTree.Right => Right;
        }
    }
}
