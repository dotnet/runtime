// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal delegate Task ReflectionWriter(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context);
internal delegate Task ReflectionValueWriter(XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context);
internal delegate Task ReflectionMemberValueWriter(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context);
internal delegate ValueTask<object> ReflectionClassReader(XmlReaderDelegator reader, XmlObjectSerializerReadContext context);
internal delegate ValueTask<object> ReflectionObjectReader(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, object obj);
internal delegate ValueTask<object?> ReflectionValueReader(XmlReaderDelegator reader, XmlObjectSerializerReadContext context);
internal delegate ValueTask<object?> ReflectionCollectionBodyReader(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, object? collection, bool isGetOnlyCollection);

internal static class ReflectionPlan
{
    internal readonly struct Enabled { }
    internal readonly struct Disabled { }
    internal readonly struct RequiredValue { }
    internal readonly struct NullableValue { }
    internal readonly struct ReferenceValue { }
    internal readonly struct Dynamic { }
    internal readonly struct StringValue { }
    internal readonly struct UriValue { }
    internal readonly struct QualifiedValue { }
    internal readonly struct ByteArrayValue { }
}

internal interface IPrimitiveElementWriter
{
    MethodInfo TypedWriteMethod { get; }
}

internal static class ReflectionSerialization
{
    private static readonly MethodInfo s_consumeCompleted = typeof(ReflectionSerialization).GetMethod(nameof(ConsumeCompleted), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo s_awaitWriterSequence = typeof(ReflectionSerialization).GetMethod(nameof(AwaitWriterSequence), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo s_prepareMember = typeof(ReflectionSerialization).GetMethod(nameof(PrepareMember), BindingFlags.Static | BindingFlags.NonPublic)!;

    internal static ReflectionWriter CreateMemberWriter(MemberInfo member, FastInvokerBuilder.Getter getter,
        ReflectionMemberValueWriter write, bool isGetOnlyCollection) =>
        new MemberWriteOperation(member, getter, write, isGetOnlyCollection).Write;

    // The async path builds its typed writers in AsyncDataContractOperations; these entry points exist so the
    // async-authored class writers compile and are remapped to SyncReflectionPipeline by the sync generator.
    internal static ReflectionMemberValueWriter? CreateTypedPrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns) =>
        null;

    internal static ReflectionMemberValueWriter? CreateNullableTypedPrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns) =>
        null;

    internal static ReflectionMemberValueWriter CreateElementWriter<TMode>(ReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString? childNamespace)
        where TMode : struct, IReflectionSerialization
    {
        ElementWriteOperation<TMode> operation = new ElementWriteOperation<TMode>(writeValue, prefix, name, ns, childNamespace);
        return childNamespace is null ? operation.Write : operation.WriteWithNamespace;
    }

    private interface IElementWriterConnection
    {
        Expression Connect(Expression writer, Expression value, Expression context);
    }

    private sealed class ElementWriteOperation<TMode>(ReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString? childNamespace) : IElementWriterConnection
        where TMode : struct, IReflectionSerialization
    {
        internal Task Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context) =>
            WriteElement<TMode>(writer, value, context, writeValue, prefix, name, ns);

        internal Task WriteWithNamespace(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context)
        {
            Debug.Assert(childNamespace is not null);
            return WriteElementWithNamespace<TMode>(writer, value, context, writeValue, prefix, name, ns, childNamespace);
        }

        public Expression Connect(Expression writer, Expression value, Expression context)
        {
            ParameterExpression storedValue = Expression.Variable(typeof(object), "value");
            ParameterExpression pending = Expression.Variable(typeof(Task), "pending");
            LabelTarget result = Expression.Label(typeof(Task));
            Func<XmlWriterDelegator, string?, XmlDictionaryString, XmlDictionaryString?, Task> start = TMode.WriteStartElement;
            Func<XmlWriterDelegator, Task> end = TMode.WriteEndElement;
            List<Expression> calls = new List<Expression>
            {
                Expression.Assign(storedValue, value),
                Expression.Assign(pending, Expression.Call(start.Method, writer, Expression.Constant(prefix, typeof(string)),
                    Expression.Constant(name), Expression.Constant(ns, typeof(XmlDictionaryString))))
            };
            Expression awaitStart;
            if (childNamespace is null)
            {
                Func<Task, XmlWriterDelegator, object?, XmlObjectSerializerWriteContext, ReflectionValueWriter, Task> continuation =
                    AwaitElementStart<TMode>;
                awaitStart = Expression.Call(continuation.Method, pending, writer, storedValue, context, Expression.Constant(writeValue));
            }
            else
            {
                Func<Task, XmlWriterDelegator, object?, XmlObjectSerializerWriteContext, ReflectionValueWriter, XmlDictionaryString, Task> continuation =
                    AwaitElementStartAndNamespace<TMode>;
                awaitStart = Expression.Call(continuation.Method, pending, writer, storedValue, context,
                    Expression.Constant(writeValue), Expression.Constant(childNamespace));
            }
            AddPending(awaitStart);
            if (childNamespace is not null)
            {
                Func<XmlWriterDelegator, XmlDictionaryString, Task> declare = TMode.WriteNamespaceDecl;
                Func<Task, XmlWriterDelegator, object?, XmlObjectSerializerWriteContext, ReflectionValueWriter, Task> continuation =
                    AwaitElementDeclaration<TMode>;
                calls.Add(Expression.Assign(pending, Expression.Call(declare.Method, writer, Expression.Constant(childNamespace))));
                AddPending(Expression.Call(continuation.Method, pending, writer, storedValue, context, Expression.Constant(writeValue)));
            }
            calls.Add(Expression.Assign(pending, CreateDelegateCall(writeValue, writer, storedValue, context)));
            Func<Task, XmlWriterDelegator, Task> awaitContent = AwaitElementContent<TMode>;
            AddPending(Expression.Call(awaitContent.Method, pending, writer));
            calls.Add(Expression.Label(result, Expression.Call(end.Method, writer)));

            return Expression.Block(new[] { storedValue, pending }, calls);

            void AddPending(Expression continuation) =>
                calls.Add(Expression.IfThen(Expression.Not(Expression.Call(s_consumeCompleted, pending)),
                    Expression.Return(result, continuation)));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareMember(XmlObjectSerializerWriteContext context, bool isGetOnlyCollection)
    {
        if (isGetOnlyCollection)
            context.StoreIsGetOnlyCollection();
        else
            context.ResetIsGetOnlyCollection();
    }

    private sealed class MemberWriteOperation(MemberInfo member, FastInvokerBuilder.Getter getter,
        ReflectionMemberValueWriter write, bool isGetOnlyCollection)
    {
        internal Task Write(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            PrepareMember(context, isGetOnlyCollection);
            return write(writer, obj, getter(obj), context);
        }

        internal Expression Connect(Expression writer, Expression obj, Expression context)
        {
            Expression value = FastInvokerBuilder.CreateGetterExpression(member, obj) ??
                Expression.Invoke(Expression.Constant(getter), obj);
            Expression call;
            if (write.Target is IPrimitiveElementWriter primitive && write.GetInvocationList().Length == 1 &&
                primitive.TypedWriteMethod.GetParameters()[1].ParameterType == value.Type)
            {
                call = Expression.Call(Expression.Constant(write.Target), primitive.TypedWriteMethod, writer, value, context);
            }
            else
            {
                call = CreateDelegateCall(write, writer, obj, Expression.Convert(value, typeof(object)), context);
            }

            return Expression.Block(Expression.Call(s_prepareMember, context, Expression.Constant(isGetOnlyCollection)), call);
        }
    }

    private static Expression CreateDelegateCall(Delegate operation, params Expression[] arguments)
    {
        if (operation.Target is MemberWriteOperation member && operation.GetInvocationList().Length == 1)
            return member.Connect(arguments[0], arguments[1], arguments[2]);
        if (operation.Target is IElementWriterConnection element && operation.GetInvocationList().Length == 1)
            return element.Connect(arguments[0], arguments[2], arguments[3]);

        MethodInfo method = operation.Method;
        return method.DeclaringType is not null && method.GetParameters().Length == arguments.Length &&
            (method.IsStatic ? operation.Target is null : operation.Target is not null) &&
            operation.GetInvocationList().Length == 1
            ? Expression.Call(method.IsStatic ? null : Expression.Constant(operation.Target, method.DeclaringType), method, arguments)
            : Expression.Invoke(Expression.Constant(operation), arguments);
    }

    internal static ReflectionWriter CreateWriterSequence(ReflectionWriter[] operations)
    {
        if (!RuntimeFeature.IsDynamicCodeCompiled)
            return (writer, obj, context) => WriteSequence(operations, writer, obj, context);

        return CompileWriterSequence(operations, null, null);
    }

    internal static ReflectionWriter CreateAdaptedWriterSequence<T>(ReflectionWriter[] operations, Func<object, T> adapt) where T : struct
    {
        if (!RuntimeFeature.IsDynamicCodeCompiled)
            return (writer, obj, context) => WriteSequence(operations, writer, adapt(obj), context);

        return CompileWriterSequence(operations, adapt, typeof(T));
    }

    private static ReflectionWriter CompileWriterSequence(ReflectionWriter[] operations, Delegate? adapt, Type? adaptedType)
    {
        ParameterExpression writerParameter = Expression.Parameter(typeof(XmlWriterDelegator), "writer");
        ParameterExpression objectParameter = Expression.Parameter(typeof(object), "obj");
        ParameterExpression contextParameter = Expression.Parameter(typeof(XmlObjectSerializerWriteContext), "context");
        ParameterExpression pending = Expression.Variable(typeof(Task), "pending");
        LabelTarget result = Expression.Label(typeof(Task));
        List<Expression> calls = new List<Expression>();
        List<ParameterExpression> locals = new List<ParameterExpression> { pending };
        ParameterExpression? adaptedObject = null;
        if (adapt is not null)
        {
            Debug.Assert(adaptedType is not null);
            adaptedObject = Expression.Variable(adaptedType, "adapted");
            locals.Add(adaptedObject);
            calls.Add(Expression.Assign(adaptedObject, CreateDelegateCall(adapt, objectParameter)));
        }
        for (int i = 0; i < operations.Length; i++)
        {
            ReflectionWriter operation = operations[i];
            Expression call = adaptedObject is not null && operation.Target is MemberWriteOperation member &&
                operation.GetInvocationList().Length == 1
                ? member.Connect(writerParameter, adaptedObject, contextParameter)
                : CreateDelegateCall(operation, writerParameter, objectParameter, contextParameter);
            if (i == operations.Length - 1)
            {
                calls.Add(Expression.Return(result, call));
            }
            else
            {
                calls.Add(Expression.Assign(pending, call));
                calls.Add(Expression.IfThen(Expression.Not(Expression.Call(s_consumeCompleted, pending)),
                    Expression.Return(result, Expression.Call(s_awaitWriterSequence, pending,
                        Expression.Constant(operations), Expression.Constant(i + 1),
                        writerParameter, adaptedObject is null ? objectParameter : Expression.Convert(adaptedObject, typeof(object)), contextParameter))));
            }
        }
        calls.Add(Expression.Label(result, Expression.Constant(Task.CompletedTask)));

        return Expression.Lambda<ReflectionWriter>(Expression.Block(locals, calls),
            writerParameter, objectParameter, contextParameter).Compile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ConsumeCompleted(Task operation) => operation.IsCompletedSuccessfully;

    private static Task WriteSequence(ReflectionWriter[] operations,
        XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
    {
        for (int i = 0; i < operations.Length; i++)
        {
            Task operation = operations[i](writer, obj, context);
            if (!ConsumeCompleted(operation))
                return AwaitWriterSequence(operation, operations, i + 1, writer, obj, context);
        }

        return Task.CompletedTask;
    }

    private static async Task AwaitWriterSequence(Task pending, ReflectionWriter[] operations,
        int next, XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
    {
        await pending.ConfigureAwait(false);
        for (int i = next; i < operations.Length; i++)
            await operations[i](writer, obj, context).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task WriteElement<TMode>(XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context,
        ReflectionValueWriter writeValue, string? prefix, XmlDictionaryString name, XmlDictionaryString? ns)
        where TMode : struct, IReflectionSerialization
    {
        Task start = TMode.WriteStartElement(writer, prefix, name, ns);
        if (!ConsumeCompleted(start))
            return AwaitElementStart<TMode>(start, writer, value, context, writeValue);
        return WriteElementContent<TMode>(writer, value, context, writeValue);
    }

    internal static Task WriteElementWithNamespace<TMode>(XmlWriterDelegator writer, object? value,
        XmlObjectSerializerWriteContext context, ReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString childNamespace)
        where TMode : struct, IReflectionSerialization
    {
        Task start = TMode.WriteStartElement(writer, prefix, name, ns);
        if (!ConsumeCompleted(start))
            return AwaitElementStartAndNamespace<TMode>(start, writer, value, context, writeValue, childNamespace);
        Task declaration = TMode.WriteNamespaceDecl(writer, childNamespace);
        if (!ConsumeCompleted(declaration))
            return AwaitElementDeclaration<TMode>(declaration, writer, value, context, writeValue);

        return WriteElementContent<TMode>(writer, value, context, writeValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task WriteElementContent<TMode>(XmlWriterDelegator writer, object? value,
        XmlObjectSerializerWriteContext context, ReflectionValueWriter writeValue)
        where TMode : struct, IReflectionSerialization
    {
        Task content = writeValue(writer, value, context);
        if (!ConsumeCompleted(content))
            return AwaitElementContent<TMode>(content, writer);

        return TMode.WriteEndElement(writer);
    }

    private static async Task AwaitElementStartAndNamespace<TMode>(Task start, XmlWriterDelegator writer,
        object? value, XmlObjectSerializerWriteContext context, ReflectionValueWriter writeValue, XmlDictionaryString childNamespace)
        where TMode : struct, IReflectionSerialization
    {
        await start.ConfigureAwait(false);
        await TMode.WriteNamespaceDecl(writer, childNamespace).ConfigureAwait(false);
        await writeValue(writer, value, context).ConfigureAwait(false);
        await TMode.WriteEndElement(writer).ConfigureAwait(false);
    }

    private static async Task AwaitElementDeclaration<TMode>(Task declaration, XmlWriterDelegator writer,
        object? value, XmlObjectSerializerWriteContext context, ReflectionValueWriter writeValue)
        where TMode : struct, IReflectionSerialization
    {
        await declaration.ConfigureAwait(false);
        await writeValue(writer, value, context).ConfigureAwait(false);
        await TMode.WriteEndElement(writer).ConfigureAwait(false);
    }

    internal static Task WriteCollection<TCountItems>(Func<object, IEnumerator> getEnumerator, ReflectionValueWriter writeItem,
        XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context) where TCountItems : struct
    {
        IEnumerator enumerator = getEnumerator(obj);
        bool transferred = false;
        try
        {
            while (enumerator.MoveNext())
            {
                if (typeof(TCountItems) == typeof(ReflectionPlan.Enabled))
                    context.IncrementItemCount(1);
                Task item = writeItem(writer, enumerator.Current, context);
                if (!ConsumeCompleted(item))
                {
                    Task continuation = AwaitCollectionItems<TCountItems>(item, enumerator, writeItem, writer, context);
                    transferred = true;
                    return continuation;
                }
            }
        }
        finally
        {
            if (!transferred)
                (enumerator as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static async Task AwaitCollectionItems<TCountItems>(Task pending, IEnumerator enumerator,
        ReflectionValueWriter writeItem, XmlWriterDelegator writer, XmlObjectSerializerWriteContext context) where TCountItems : struct
    {
        try
        {
            await pending.ConfigureAwait(false);
            while (enumerator.MoveNext())
            {
                if (typeof(TCountItems) == typeof(ReflectionPlan.Enabled))
                    context.IncrementItemCount(1);
                await writeItem(writer, enumerator.Current, context).ConfigureAwait(false);
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private static async Task AwaitElementStart<TMode>(Task start, XmlWriterDelegator writer,
        object? value, XmlObjectSerializerWriteContext context, ReflectionValueWriter writeValue)
        where TMode : struct, IReflectionSerialization
    {
        await start.ConfigureAwait(false);
        await writeValue(writer, value, context).ConfigureAwait(false);
        await TMode.WriteEndElement(writer).ConfigureAwait(false);
    }

    private static async Task AwaitElementContent<TMode>(Task content, XmlWriterDelegator writer)
        where TMode : struct, IReflectionSerialization
    {
        await content.ConfigureAwait(false);
        await TMode.WriteEndElement(writer).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Complete(ValueTask operation)
    {
        Debug.Assert(operation.IsCompletedSuccessfully, "Synchronous serialization must succeed or throw before returning.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Complete<T>(ValueTask<T> operation)
    {
        Debug.Assert(operation.IsCompleted, "Synchronous serialization must not suspend.");
        return operation.Result;
    }

    internal static ValueTask<TResult> Then<T, TResult, TState>(ValueTask<T> operation, TState state, Func<TState, T, TResult> continuation)
    {
        if (operation.IsCompletedSuccessfully)
        {
            return new ValueTask<TResult>(continuation(state, operation.Result));
        }

        return AwaitThen(operation, state, continuation);
    }

    private static async ValueTask<TResult> AwaitThen<T, TResult, TState>(ValueTask<T> operation, TState state, Func<TState, T, TResult> continuation) =>
        continuation(state, await operation.ConfigureAwait(false));
}

// Synchronous counterpart of the ReflectionSerialization plumbing. Member writers and element writers remember what
// they call so CreateWriterSequence can connect a class's members into one compiled method; when dynamic code
// isn't compiled, the same operations run as ordinary delegate calls.
internal static class SyncReflectionPipeline
{
    private static readonly MethodInfo s_prepareMember = typeof(SyncReflectionPipeline).GetMethod(nameof(PrepareMember), BindingFlags.Static | BindingFlags.NonPublic)!;

    internal static SyncReflectionMemberValueWriter CreateElementWriter<TMode>(SyncReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString? childNamespace)
        where TMode : struct, IReflectionSerialization
    {
        ElementWriteOperation<TMode> operation = new ElementWriteOperation<TMode>(writeValue, prefix, name, ns, childNamespace);
        return childNamespace is null ? operation.Write : operation.WriteWithNamespace;
    }

    private interface IElementWriterConnection
    {
        Expression Connect(Expression writer, Expression value, Expression context);
    }

    private sealed class ElementWriteOperation<TMode>(SyncReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString? childNamespace) : IElementWriterConnection
        where TMode : struct, IReflectionSerialization
    {
        internal void Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context) =>
            WriteElement<TMode>(writer, value, context, writeValue, prefix, name, ns);

        internal void WriteWithNamespace(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context)
        {
            Debug.Assert(childNamespace is not null);
            WriteElementWithNamespace<TMode>(writer, value, context, writeValue, prefix, name, ns, childNamespace);
        }

        public Expression Connect(Expression writer, Expression value, Expression context)
        {
            Action<XmlWriterDelegator, string?, XmlDictionaryString, XmlDictionaryString?> start = SyncReflectionPolicy.WriteStartElement;
            Action<XmlWriterDelegator> end = SyncReflectionPolicy.WriteEndElement;
            List<Expression> calls = new List<Expression>
            {
                Expression.Call(start.Method, writer, Expression.Constant(prefix, typeof(string)),
                    Expression.Constant(name), Expression.Constant(ns, typeof(XmlDictionaryString)))
            };
            if (childNamespace is not null)
            {
                Action<XmlWriterDelegator, XmlDictionaryString> declare = SyncReflectionPolicy.WriteNamespaceDecl;
                calls.Add(Expression.Call(declare.Method, writer, Expression.Constant(childNamespace)));
            }
            calls.Add(CreateDelegateCall(writeValue, writer, value, context));
            calls.Add(Expression.Call(end.Method, writer));

            return Expression.Block(typeof(void), calls);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareMember(XmlObjectSerializerWriteContext context, bool isGetOnlyCollection)
    {
        if (isGetOnlyCollection)
            context.StoreIsGetOnlyCollection();
        else
            context.ResetIsGetOnlyCollection();
    }

    private sealed class MemberWriteOperation(MemberInfo member, FastInvokerBuilder.Getter getter,
        SyncReflectionMemberValueWriter write, bool isGetOnlyCollection)
    {
        internal void Write(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            PrepareMember(context, isGetOnlyCollection);
            write(writer, obj, getter(obj), context);
        }

        internal Expression Connect(Expression writer, Expression obj, Expression context)
        {
            Expression value = FastInvokerBuilder.CreateGetterExpression(member, obj) ??
                Expression.Invoke(Expression.Constant(getter), obj);
            Expression call;
            if (write.Target is IPrimitiveElementWriter primitive && write.GetInvocationList().Length == 1 &&
                primitive.TypedWriteMethod.GetParameters()[1].ParameterType == value.Type)
            {
                call = Expression.Call(Expression.Constant(write.Target), primitive.TypedWriteMethod, writer, value, context);
            }
            else
            {
                call = CreateDelegateCall(write, writer, obj, Expression.Convert(value, typeof(object)), context);
            }

            return Expression.Block(Expression.Call(s_prepareMember, context, Expression.Constant(isGetOnlyCollection)), call);
        }
    }

    private static Expression CreateDelegateCall(Delegate operation, params Expression[] arguments)
    {
        if (operation.Target is MemberWriteOperation member && operation.GetInvocationList().Length == 1)
            return member.Connect(arguments[0], arguments[1], arguments[2]);
        if (operation.Target is IElementWriterConnection element && operation.GetInvocationList().Length == 1)
            return element.Connect(arguments[0], arguments[2], arguments[3]);

        MethodInfo method = operation.Method;
        return method.DeclaringType is not null && method.GetParameters().Length == arguments.Length &&
            (method.IsStatic ? operation.Target is null : operation.Target is not null) &&
            operation.GetInvocationList().Length == 1
            ? Expression.Call(method.IsStatic ? null : Expression.Constant(operation.Target, method.DeclaringType), method, arguments)
            : Expression.Invoke(Expression.Constant(operation), arguments);
    }

    // Typed element writers for primitives that XmlWriterDelegator.WriteTypedPrimitiveElement can write without boxing.
    // Returns null for every other type so the caller falls back to its general primitive writer.
    internal static SyncReflectionMemberValueWriter? CreateTypedPrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns) =>
        CreateTypedPrimitiveElementWriter(type, name, ns, nullable: false);

    internal static SyncReflectionMemberValueWriter? CreateNullableTypedPrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns) =>
        Nullable.GetUnderlyingType(type) is Type underlyingType
            ? CreateTypedPrimitiveElementWriter(underlyingType, name, ns, nullable: true)
            : null;

    private static SyncReflectionMemberValueWriter? CreateTypedPrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns, bool nullable)
    {
        if (type.IsEnum)
            return null;

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => CreateTypedPrimitiveElementWriter<bool>(name, ns, nullable),
            TypeCode.Char => CreateTypedPrimitiveElementWriter<char>(name, ns, nullable),
            TypeCode.Byte => CreateTypedPrimitiveElementWriter<byte>(name, ns, nullable),
            TypeCode.SByte => CreateTypedPrimitiveElementWriter<sbyte>(name, ns, nullable),
            TypeCode.Int16 => CreateTypedPrimitiveElementWriter<short>(name, ns, nullable),
            TypeCode.UInt16 => CreateTypedPrimitiveElementWriter<ushort>(name, ns, nullable),
            TypeCode.Int32 => CreateTypedPrimitiveElementWriter<int>(name, ns, nullable),
            TypeCode.UInt32 => CreateTypedPrimitiveElementWriter<uint>(name, ns, nullable),
            TypeCode.Int64 => CreateTypedPrimitiveElementWriter<long>(name, ns, nullable),
            TypeCode.Single => CreateTypedPrimitiveElementWriter<float>(name, ns, nullable),
            TypeCode.Double => CreateTypedPrimitiveElementWriter<double>(name, ns, nullable),
            TypeCode.Decimal => CreateTypedPrimitiveElementWriter<decimal>(name, ns, nullable),
            TypeCode.DateTime => CreateTypedPrimitiveElementWriter<DateTime>(name, ns, nullable),
            _ => null
        };
    }

    private static SyncReflectionMemberValueWriter CreateTypedPrimitiveElementWriter<T>(XmlDictionaryString name, XmlDictionaryString? ns, bool nullable)
        where T : struct =>
        nullable ? new NullablePrimitiveElementWriter<T>(name, ns).Write : new PrimitiveElementWriter<T>(name, ns).Write;

    private sealed class PrimitiveElementWriter<T>(XmlDictionaryString name, XmlDictionaryString? ns) : IPrimitiveElementWriter where T : struct
    {
        public MethodInfo TypedWriteMethod => typeof(PrimitiveElementWriter<T>).GetMethod(nameof(WriteTyped), BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal void Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context) =>
            writer.WriteTypedPrimitiveElement((T)value!, name, ns);

        private void WriteTyped(XmlWriterDelegator writer, T value, XmlObjectSerializerWriteContext context) =>
            writer.WriteTypedPrimitiveElement(value, name, ns);
    }

    private sealed class NullablePrimitiveElementWriter<T>(XmlDictionaryString name, XmlDictionaryString? ns) : IPrimitiveElementWriter where T : struct
    {
        public MethodInfo TypedWriteMethod => typeof(NullablePrimitiveElementWriter<T>).GetMethod(nameof(WriteTyped), BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal void Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context)
        {
            if (value is null)
                WriteNull(writer, context);
            else
                writer.WriteTypedPrimitiveElement((T)value, name, ns);
        }

        private void WriteTyped(XmlWriterDelegator writer, T? value, XmlObjectSerializerWriteContext context)
        {
            if (value.HasValue)
                writer.WriteTypedPrimitiveElement(value.GetValueOrDefault(), name, ns);
            else
                WriteNull(writer, context);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "A null primitive only writes the nil attribute; typeof(T) is a serializable primitive.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "A null primitive only writes the nil attribute; typeof(T) is a serializable primitive.")]
        private void WriteNull(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context) =>
            context.WriteNull(writer, typeof(T), isMemberTypeSerializable: true, name, ns);
    }

    internal static void WriteCollection<TCountItems>(Func<object, IEnumerator> getEnumerator, SyncReflectionValueWriter writeItem,
        XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context) where TCountItems : struct
    {
        IEnumerator enumerator = getEnumerator(obj);
        try
        {
            while (enumerator.MoveNext())
            {
                if (typeof(TCountItems) == typeof(ReflectionPlan.Enabled))
                    context.IncrementItemCount(1);
                writeItem(writer, enumerator.Current, context);
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    internal static void WriteElementWithNamespace<TMode>(XmlWriterDelegator writer, object? value,
        XmlObjectSerializerWriteContext context, SyncReflectionValueWriter writeValue, string? prefix,
        XmlDictionaryString name, XmlDictionaryString? ns, XmlDictionaryString childNamespace)
        where TMode : struct, IReflectionSerialization
    {
        SyncReflectionPolicy.WriteStartElement(writer, prefix, name, ns);
        SyncReflectionPolicy.WriteNamespaceDecl(writer, childNamespace);
        writeValue(writer, value, context);
        SyncReflectionPolicy.WriteEndElement(writer);
    }

    internal static SyncReflectionWriter CreateMemberWriter(MemberInfo member, FastInvokerBuilder.Getter getter,
        SyncReflectionMemberValueWriter write, bool isGetOnlyCollection) =>
        new MemberWriteOperation(member, getter, write, isGetOnlyCollection).Write;

    internal static void WriteElement<TMode>(XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context,
        SyncReflectionValueWriter writeValue, string? prefix, XmlDictionaryString name, XmlDictionaryString? ns)
        where TMode : struct, IReflectionSerialization
    {
        SyncReflectionPolicy.WriteStartElement(writer, prefix, name, ns);
        writeValue(writer, value, context);
        SyncReflectionPolicy.WriteEndElement(writer);
    }

    internal static SyncReflectionWriter CreateWriterSequence(SyncReflectionWriter[] operations)
    {
        if (!RuntimeFeature.IsDynamicCodeCompiled)
        {
            return (writer, obj, context) =>
            {
                foreach (SyncReflectionWriter operation in operations)
                    operation(writer, obj, context);
            };
        }

        ParameterExpression writerParameter = Expression.Parameter(typeof(XmlWriterDelegator), "writer");
        ParameterExpression objectParameter = Expression.Parameter(typeof(object), "obj");
        ParameterExpression contextParameter = Expression.Parameter(typeof(XmlObjectSerializerWriteContext), "context");
        List<Expression> calls = new List<Expression>(operations.Length + 1);
        foreach (SyncReflectionWriter operation in operations)
            calls.Add(CreateDelegateCall(operation, writerParameter, objectParameter, contextParameter));
        calls.Add(Expression.Empty());

        return Expression.Lambda<SyncReflectionWriter>(Expression.Block(typeof(void), calls),
            writerParameter, objectParameter, contextParameter).Compile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TResult Then<T, TResult, TState>(T value, TState state, Func<TState, T, TResult> continuation) =>
        continuation(state, value);
}

internal interface IPrimitiveMemberReader
{
    Expression Connect(Expression reader, Expression context, Expression obj, LabelTarget result, Expression remaining);
}

internal static class ReflectionReaderPipeline
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    internal static ReflectionObjectReader Compile(ReflectionObjectReader[] members, XmlDictionaryString[] names,
        XmlDictionaryString[] namespaces, int[] nextRequiredMember, ReflectionObjectReader[] remaining)
    {
        ReflectionObjectReader read = CompileSequence(members, names, namespaces, nextRequiredMember, remaining, asynchronous: true);
        return (reader, context, obj) =>
        {
            context.IncrementItemCount(members.Length);
            return reader.UnderlyingReader is XmlBinaryReader ? read(reader, context, obj) : remaining[0](reader, context, obj);
        };
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    internal static SyncReflectionObjectReader Compile(SyncReflectionObjectReader[] members, XmlDictionaryString[] names,
        XmlDictionaryString[] namespaces, int[] nextRequiredMember, SyncReflectionObjectReader[] remaining)
    {
        SyncReflectionObjectReader read = CompileSequence(members, names, namespaces, nextRequiredMember, remaining, asynchronous: false);
        return (reader, context, obj) =>
        {
            context.IncrementItemCount(members.Length);
            return reader.UnderlyingReader is XmlBinaryReader ? read(reader, context, obj) : remaining[0](reader, context, obj);
        };
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    private static TReader CompileSequence<TReader>(TReader[] members, XmlDictionaryString[] names,
        XmlDictionaryString[] namespaces, int[] nextRequiredMember, TReader[] remaining, bool asynchronous) where TReader : Delegate
    {
        ParameterExpression reader = Expression.Parameter(typeof(XmlReaderDelegator), nameof(reader));
        ParameterExpression context = Expression.Parameter(typeof(XmlObjectSerializerReadContext), nameof(context));
        ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
        ParameterExpression binaryReader = Expression.Variable(typeof(XmlBinaryReader), nameof(binaryReader));
        LabelTarget result = Expression.Label(asynchronous ? typeof(ValueTask<object>) : typeof(object));
        Func<XmlBinaryReader, XmlDictionaryString, XmlDictionaryString, bool> matches = IsExpectedMember;
        LabelTarget[] memberLabels = new LabelTarget[members.Length];
        for (int i = 0; i < memberLabels.Length; i++)
            memberLabels[i] = Expression.Label();
        List<Expression> calls = new List<Expression>
        {
            Expression.Assign(binaryReader, Expression.Convert(Expression.Property(reader, nameof(XmlReaderDelegator.UnderlyingReader)), typeof(XmlBinaryReader)))
        };
        for (int i = 0; i < members.Length; i++)
        {
            Expression mismatch = Expression.Return(result, Call(remaining[i], reader, context, obj));
            if (i + 1 < members.Length && nextRequiredMember[i] != i)
            {
                mismatch = Expression.Block(
                    Expression.IfThen(Expression.Call(matches.Method, binaryReader,
                        Expression.Constant(names[i + 1]), Expression.Constant(namespaces[i + 1])),
                        Expression.Goto(memberLabels[i + 1])), mismatch);
            }
            calls.Add(Expression.IfThen(Expression.Not(Expression.Call(matches.Method, binaryReader,
                Expression.Constant(names[i]), Expression.Constant(namespaces[i]))), mismatch));
            calls.Add(Expression.Label(memberLabels[i]));
            Debug.Assert(members[i].Target is IPrimitiveMemberReader);
            IPrimitiveMemberReader primitive = (IPrimitiveMemberReader)members[i].Target!;
            calls.Add(primitive.Connect(reader, context, obj, result, Expression.Constant(remaining[i + 1])));
        }
        Expression completed = asynchronous
            ? Expression.New(typeof(ValueTask<object>).GetConstructor(new[] { typeof(object) })!, obj)
            : obj;
        calls.Add(Expression.Label(result, Expression.Condition(
            Expression.Equal(Expression.Property(binaryReader, nameof(XmlBinaryReader.NodeType)), Expression.Constant(XmlNodeType.EndElement)),
            completed, Call(remaining[members.Length], reader, context, obj))));

        return Expression.Lambda<TReader>(Expression.Block(new[] { binaryReader }, calls), reader, context, obj).Compile();
    }

    private static Expression Call(Delegate operation, params Expression[] arguments) =>
        Expression.Invoke(Expression.Constant(operation), arguments);

    // Keep name comparisons out of the per-member compiled bodies.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsExpectedMember(XmlBinaryReader reader, XmlDictionaryString name, XmlDictionaryString ns) =>
        reader.NodeType == XmlNodeType.Element && reader.IsLocalName(name) && reader.IsNamespaceUri(ns);
}

internal interface IReflectionSerialization
{
    static abstract Task WriteStartElement(XmlWriterDelegator writer, string? prefix, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteStartElementString(XmlWriterDelegator writer, string? prefix, string name, string? ns);
    static abstract Task WriteEndElement(XmlWriterDelegator writer);
    static abstract Task WriteNamespaceDecl(XmlWriterDelegator writer, XmlDictionaryString ns);
    static abstract Task WriteJsonNameWithMapping(XmlWriterDelegator writer, XmlDictionaryString[] names, int index);
    static abstract Task WriteAttributeString(XmlWriterDelegator writer, string? prefix, string name, string? ns, string value);
    static abstract Task IncrementCollectionCount(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, int count);
    static abstract Task WriteBooleanArray(XmlWriterDelegator writer, bool[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteDateTimeArray(XmlWriterDelegator writer, DateTime[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteDecimalArray(XmlWriterDelegator writer, decimal[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteInt32Array(XmlWriterDelegator writer, int[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteInt64Array(XmlWriterDelegator writer, long[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteSingleArray(XmlWriterDelegator writer, float[] value, XmlDictionaryString name, XmlDictionaryString? ns);
    static abstract Task WriteDoubleArray(XmlWriterDelegator writer, double[] value, XmlDictionaryString name, XmlDictionaryString? ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteXmlValue(DataContract contract, XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteJsonValue(Json.JsonDataContract contract, XmlWriterDelegator writer, object value, Json.XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteXmlElement(PrimitiveDataContract contract, XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteNull(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, Type type, bool isSerializable);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task SerializeValue(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task SerializeReference(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteExtensionData(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ExtensionDataObject? extensionData, int memberIndex);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract Task WriteISerializable(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ISerializable value);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<object?> Deserialize(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle, string name, string ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<object?> GetExistingObject(XmlObjectSerializerReadContext context, string id, Type type, string name, string ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<object?> ReadXmlValue(DataContract contract, XmlReaderDelegator reader, XmlObjectSerializerReadContext context);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<string?> ReadIfNullOrRef(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type, bool isSerializable);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<SerializationInfo> ReadSerializationInfo(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type);

    static abstract ValueTask<bool> MoveToNextElement(XmlReaderDelegator reader);
    static abstract ValueTask<T> ReadPrimitiveElement<T>(XmlReaderDelegator reader) where T : struct;
    static abstract ValueTask<bool> IsStartElement(XmlReaderDelegator reader, XmlDictionaryString name, XmlDictionaryString ns);
    static abstract ValueTask<bool> IsStartElement(XmlReaderDelegator reader);
    static abstract ValueTask SkipUnknownElement(XmlReaderDelegator reader, XmlObjectSerializerReadContext context);
    static abstract ValueTask<object?> ReadBooleanArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadDateTimeArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadDecimalArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadInt32Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadInt64Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadSingleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);
    static abstract ValueTask<object?> ReadDoubleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<object?> ReadJsonValue(Json.JsonDataContract contract, XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<int> GetMemberIndex(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, ExtensionDataObject? extensionData);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<int> GetMemberIndexWithRequiredMembers(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, int requiredIndex, ExtensionDataObject? extensionData);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    static abstract ValueTask<int> GetJsonMemberIndex(XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString[] names, int memberIndex, ExtensionDataObject? extensionData);
}

internal readonly struct SyncReflectionSerialization : IReflectionSerialization
{
    public static ValueTask<T> ReadPrimitiveElement<T>(XmlReaderDelegator reader) where T : struct =>
        new ValueTask<T>(reader.ReadPrimitiveElement<T>());

    public static Task WriteStartElement(XmlWriterDelegator writer, string? prefix, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteStartElement(prefix, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteStartElementString(XmlWriterDelegator writer, string? prefix, string name, string? ns)
    {
        writer.WriteStartElement(prefix, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteEndElement(XmlWriterDelegator writer)
    {
        writer.WriteEndElement();
        return Task.CompletedTask;
    }

    public static Task WriteNamespaceDecl(XmlWriterDelegator writer, XmlDictionaryString ns)
    {
        writer.WriteNamespaceDecl(ns);
        return Task.CompletedTask;
    }

    public static Task WriteJsonNameWithMapping(XmlWriterDelegator writer, XmlDictionaryString[] names, int index)
    {
        Json.XmlObjectSerializerWriteContextComplexJson.WriteJsonNameWithMapping(writer, names, index);
        return Task.CompletedTask;
    }

    public static Task WriteAttributeString(XmlWriterDelegator writer, string? prefix, string name, string? ns, string value)
    {
        writer.WriteAttributeString(prefix, name, ns, value);
        return Task.CompletedTask;
    }

    public static Task IncrementCollectionCount(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, int count)
    {
        context.IncrementCollectionCount(writer, count);
        return Task.CompletedTask;
    }

    public static Task WriteBooleanArray(XmlWriterDelegator writer, bool[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteBooleanArray(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteDateTimeArray(XmlWriterDelegator writer, DateTime[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteDateTimeArray(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteDecimalArray(XmlWriterDelegator writer, decimal[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteDecimalArray(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteInt32Array(XmlWriterDelegator writer, int[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteInt32Array(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteInt64Array(XmlWriterDelegator writer, long[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteInt64Array(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteSingleArray(XmlWriterDelegator writer, float[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteSingleArray(value, name, ns);
        return Task.CompletedTask;
    }

    public static Task WriteDoubleArray(XmlWriterDelegator writer, double[] value, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        writer.WriteDoubleArray(value, name, ns);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteXmlValue(DataContract contract, XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context)
    {
        contract.WriteXmlValue(writer, value, context);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteJsonValue(Json.JsonDataContract contract, XmlWriterDelegator writer, object value, Json.XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
    {
        contract.WriteJsonValue(writer, value, context, declaredTypeHandle);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteXmlElement(PrimitiveDataContract contract, XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        contract.WriteXmlElement(writer, value, context, name, ns);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteNull(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, Type type, bool isSerializable)
    {
        context.WriteNull(writer, type, isSerializable);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task SerializeValue(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle)
    {
        context.InternalSerialize(writer, value, isDeclaredType, writeXsiType, declaredTypeId, declaredTypeHandle);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task SerializeReference(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle)
    {
        context.InternalSerializeReference(writer, value, isDeclaredType, writeXsiType, declaredTypeId, declaredTypeHandle);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteExtensionData(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ExtensionDataObject? extensionData, int memberIndex)
    {
        context.WriteExtensionData(writer, extensionData, memberIndex);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteISerializable(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ISerializable value)
    {
        context.WriteISerializable(writer, value);
        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> Deserialize(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle, string name, string ns) =>
        new ValueTask<object?>(context.InternalDeserialize(reader, declaredTypeId, declaredTypeHandle, name, ns));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> GetExistingObject(XmlObjectSerializerReadContext context, string id, Type type, string name, string ns) =>
        new ValueTask<object?>(context.GetExistingObject(id, type, name, ns));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> ReadXmlValue(DataContract contract, XmlReaderDelegator reader, XmlObjectSerializerReadContext context) =>
        new ValueTask<object?>(contract.ReadXmlValue(reader, context));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<string?> ReadIfNullOrRef(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type, bool isSerializable) =>
        new ValueTask<string?>(context.ReadIfNullOrRef(reader, type, isSerializable));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<SerializationInfo> ReadSerializationInfo(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type) =>
        new ValueTask<SerializationInfo>(context.ReadSerializationInfo(reader, type));

    public static ValueTask<bool> MoveToNextElement(XmlReaderDelegator reader) =>
        new ValueTask<bool>(XmlObjectSerializerReadContext.MoveToNextElement(reader));

    public static ValueTask<bool> IsStartElement(XmlReaderDelegator reader, XmlDictionaryString name, XmlDictionaryString ns) =>
        new ValueTask<bool>(reader.IsStartElement(name, ns));

    public static ValueTask<bool> IsStartElement(XmlReaderDelegator reader) =>
        new ValueTask<bool>(reader.IsStartElement());

    public static ValueTask SkipUnknownElement(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
    {
        context.SkipUnknownElement(reader);
        return ValueTask.CompletedTask;
    }

    public static ValueTask<object?> ReadBooleanArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadBooleanArray(context, name, ns, size, out bool[]? value) ? value : null);

    public static ValueTask<object?> ReadDateTimeArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadDateTimeArray(context, name, ns, size, out DateTime[]? value) ? value : null);

    public static ValueTask<object?> ReadDecimalArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadDecimalArray(context, name, ns, size, out decimal[]? value) ? value : null);

    public static ValueTask<object?> ReadInt32Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadInt32Array(context, name, ns, size, out int[]? value) ? value : null);

    public static ValueTask<object?> ReadInt64Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadInt64Array(context, name, ns, size, out long[]? value) ? value : null);

    public static ValueTask<object?> ReadSingleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadSingleArray(context, name, ns, size, out float[]? value) ? value : null);

    public static ValueTask<object?> ReadDoubleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        new ValueTask<object?>(reader.TryReadDoubleArray(context, name, ns, size, out double[]? value) ? value : null);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> ReadJsonValue(Json.JsonDataContract contract, XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context) =>
        new ValueTask<object?>(contract.ReadJsonValue(reader, context));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetMemberIndex(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, ExtensionDataObject? extensionData) =>
        new ValueTask<int>(context.GetMemberIndex(reader, names, namespaces, memberIndex, extensionData));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetMemberIndexWithRequiredMembers(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, int requiredIndex, ExtensionDataObject? extensionData) =>
        new ValueTask<int>(context.GetMemberIndexWithRequiredMembers(reader, names, namespaces, memberIndex, requiredIndex, extensionData));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetJsonMemberIndex(XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString[] names, int memberIndex, ExtensionDataObject? extensionData) =>
        new ValueTask<int>(context.GetJsonMemberIndex(reader, names, memberIndex, extensionData));
}
