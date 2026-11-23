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

namespace System.Runtime.Serialization
{
    internal abstract class ReflectionReader
    {
        private delegate object CollectionSetItemDelegate(object resultCollection, object? collectionItem, int itemIndex, bool isGetOnlyCollection);

        private static readonly MethodInfo s_createCollectionSetter = typeof(ReflectionReader).GetMethod(nameof(CreateCollectionSetter), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo s_trimCollection = typeof(ReflectionReader).GetMethod(nameof(TrimCollection), BindingFlags.NonPublic | BindingFlags.Static)!;
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionClassReader GenerateClassReader<TMode>(ClassDataContract classContract, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
            where TMode : struct, IReflectionSerialization
        {
            List<ClassDataContract> hierarchy = new List<ClassDataContract>();
            for (ClassDataContract? current = classContract; current is not null; current = current.BaseClassContract)
            {
                hierarchy.Add(current);
            }
            hierarchy.Reverse();

            List<MethodInfo> before = new List<MethodInfo>();
            List<MethodInfo> after = new List<MethodInfo>();
            List<ReflectionObjectReader> members = new List<ReflectionObjectReader>();
            foreach (ClassDataContract current in hierarchy)
            {
                if (current.OnDeserializing is MethodInfo onDeserializing)
                {
                    before.Add(onDeserializing);
                }
                if (current.OnDeserialized is MethodInfo onDeserialized)
                {
                    after.Add(onDeserialized);
                }
                if (!classContract.IsISerializable)
                {
                    foreach (DataMember member in current.Members!)
                    {
                        members.Add(CreateMemberReader<TMode>(classContract, member));
                    }
                }
            }

            Func<object> create = CreateObjectFactory(classContract);
            MethodInfo[] beforeCallbacks = before.ToArray();
            MethodInfo[] afterCallbacks = after.ToArray();
            ReflectionObjectReader readBody;
            if (classContract.IsISerializable)
            {
                Type type = classContract.UnderlyingType;
                ConstructorInfo constructor = classContract.GetISerializableConstructor()!;
                Func<(XmlObjectSerializerReadContext Context, object Target), SerializationInfo, object> initialize = (state, info) =>
                {
                    constructor.Invoke(state.Target, new object[] { info, state.Context.GetStreamingContext() });
                    return state.Target;
                };
                readBody = (reader, context, obj) => ReflectionSerialization.Then(TMode.ReadSerializationInfo(reader, context, type), (context, obj), initialize);
            }
            else
            {
                readBody = CreateMembersReader<TMode>(classContract, members.ToArray(), memberNames, memberNamespaces);
            }

            List<Func<XmlObjectSerializerReadContext, object, string, object>> finish = new List<Func<XmlObjectSerializerReadContext, object, string, object>>();
#pragma warning disable SYSLIB0050 // IObjectReference is obsolete
            bool hasFactory = typeof(IObjectReference).IsAssignableFrom(classContract.UnderlyingType);
            if (hasFactory)
            {
                finish.Add(static (context, obj, objectId) => context.GetRealObject((IObjectReference)obj, objectId));
            }
#pragma warning restore SYSLIB0050
            if (hasFactory)
            {
                finish.Add(static (context, obj, objectId) => ResolveAdapterObject(obj));
                finish.Add(static (context, obj, objectId) =>
                {
                    (obj as IDeserializationCallback)?.OnDeserialization(null);
                    return obj;
                });
            }
            else
            {
                if (classContract.UnderlyingType == typeof(DateTimeOffsetAdapter))
                {
                    finish.Add(static (context, obj, objectId) => DateTimeOffsetAdapter.GetDateTimeOffset((DateTimeOffsetAdapter)obj));
                }
                else if (classContract.UnderlyingType == typeof(MemoryStreamAdapter))
                {
                    finish.Add(static (context, obj, objectId) => MemoryStreamAdapter.GetMemoryStream((MemoryStreamAdapter)obj));
                }
                if (typeof(IDeserializationCallback).IsAssignableFrom(classContract.UnderlyingType))
                {
                    finish.Add(static (context, obj, objectId) =>
                    {
                        ((IDeserializationCallback)obj).OnDeserialization(null);
                        return obj;
                    });
                }
            }
            Func<XmlObjectSerializerReadContext, object, string, object>[] finishOperations = finish.ToArray();

            if (finishOperations.Length == 0 && afterCallbacks.Length == 0)
            {
                if (beforeCallbacks.Length == 0)
                {
                    return (reader, context) =>
                    {
                        object obj = create();
                        context.AddNewObject(obj);
                        return readBody(reader, context, obj);
                    };
                }

                return (reader, context) =>
                {
                    object obj = create();
                    context.AddNewObject(obj);
                    InvokeCallbacks(beforeCallbacks, context, obj);
                    return readBody(reader, context, obj);
                };
            }

            Func<XmlObjectSerializerReadContext, object, string, object> finishObject;
            if (finishOperations.Length == 0)
            {
                finishObject = (context, obj, objectId) =>
                {
                    InvokeCallbacks(afterCallbacks, context, obj);
                    return obj;
                };
            }
            else if (afterCallbacks.Length == 0)
            {
                finishObject = (context, obj, objectId) =>
                {
                    foreach (Func<XmlObjectSerializerReadContext, object, string, object> operation in finishOperations)
                    {
                        obj = operation(context, obj, objectId);
                    }
                    return obj;
                };
            }
            else
            {
                finishObject = (context, obj, objectId) =>
                {
                    foreach (Func<XmlObjectSerializerReadContext, object, string, object> operation in finishOperations)
                    {
                        obj = operation(context, obj, objectId);
                    }
                    InvokeCallbacks(afterCallbacks, context, obj);
                    return obj;
                };
            }

            if (hasFactory)
            {
                return beforeCallbacks.Length == 0
                    ? CreateFinishingClassReader<ReflectionPlan.Disabled, ReflectionPlan.Enabled>(create, readBody, beforeCallbacks, finishObject)
                    : CreateFinishingClassReader<ReflectionPlan.Enabled, ReflectionPlan.Enabled>(create, readBody, beforeCallbacks, finishObject);
            }
            return beforeCallbacks.Length == 0
                ? CreateFinishingClassReader<ReflectionPlan.Disabled, ReflectionPlan.Disabled>(create, readBody, beforeCallbacks, finishObject)
                : CreateFinishingClassReader<ReflectionPlan.Enabled, ReflectionPlan.Disabled>(create, readBody, beforeCallbacks, finishObject);
        }

        private static ReflectionClassReader CreateFinishingClassReader<TBeforeCallbacks, TObjectId>(
            Func<object> create, ReflectionObjectReader readBody, MethodInfo[] beforeCallbacks,
            Func<XmlObjectSerializerReadContext, object, string, object> finishObject)
            where TBeforeCallbacks : struct
            where TObjectId : struct =>
            (reader, context) =>
            {
                object obj = create();
                string objectId = typeof(TObjectId) == typeof(ReflectionPlan.Enabled) ? context.GetObjectId() : Globals.NewObjectId;
                context.AddNewObject(obj);
                if (typeof(TBeforeCallbacks) == typeof(ReflectionPlan.Enabled))
                {
                    InvokeCallbacks(beforeCallbacks, context, obj);
                }
                ValueTask<object> result = readBody(reader, context, obj);
                if (result.IsCompletedSuccessfully)
                    return new ValueTask<object>(finishObject(context, result.Result, objectId));

                return AwaitFinishedClass(result, context, objectId, finishObject);
            };

        private static async ValueTask<object> AwaitFinishedClass(ValueTask<object> result,
            XmlObjectSerializerReadContext context, string objectId,
            Func<XmlObjectSerializerReadContext, object, string, object> finishObject) =>
            finishObject(context, await result.ConfigureAwait(false), objectId);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionObjectReader CreateMemberReader<TMode>(ClassDataContract classContract, DataMember member)
            where TMode : struct, IReflectionSerialization
        {
            string ns = member.IsGetOnlyCollection ? GetClassContractNamespace(classContract) : classContract.XmlName.Namespace;
            if (!member.IsGetOnlyCollection)
            {
                ReflectionObjectReader? primitiveMember = CreatePrimitiveMemberReader<TMode>(member);
                if (primitiveMember is not null)
                    return primitiveMember;
            }
            if (member.IsGetOnlyCollection)
            {
                ReflectionValueReader readValue = CreateValueReader<TMode>(member.MemberType, member.Name, ns, member.MemberPrimitiveContract);
                FastInvokerBuilder.Getter getter = member.Getter;
                return async (reader, context, obj) =>
                {
                    context.StoreCollectionMemberInfo(getter(obj));
                    await readValue(reader, context).ConfigureAwait(false);
                    return obj;
                };
            }

            FastInvokerBuilder.Setter setter = member.Setter;
            ReflectionValueReader readMemberValue = CreateValueReader<TMode>(member.MemberType, member.Name, ns,
                member.MemberPrimitiveContract, setter, out ReflectionObjectReader? readMember);
            if (readMember is not null)
                return readMember;

            return (reader, context, obj) =>
            {
                context.ResetCollectionMemberInfo();
                ValueTask<object?> value = readMemberValue(reader, context);
                if (value.IsCompletedSuccessfully)
                    return new ValueTask<object>(setter(obj, value.Result));

                return AwaitMemberRead(value, setter, obj);
            };
        }

        private static async ValueTask<object> AwaitMemberRead(ValueTask<object?> value, FastInvokerBuilder.Setter setter, object obj) =>
            setter(obj, await value.ConfigureAwait(false));

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionObjectReader? CreatePrimitiveMemberReader<TMode>(DataMember member)
            where TMode : struct, IReflectionSerialization
        {
            Type type = member.MemberType;
            if (!type.IsValueType || type.IsEnum)
                return null;

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => CreatePrimitiveMemberReader<TMode, bool>(member),
                TypeCode.Char => CreatePrimitiveMemberReader<TMode, char>(member),
                TypeCode.Byte => CreatePrimitiveMemberReader<TMode, byte>(member),
                TypeCode.SByte => CreatePrimitiveMemberReader<TMode, sbyte>(member),
                TypeCode.Int16 => CreatePrimitiveMemberReader<TMode, short>(member),
                TypeCode.UInt16 => CreatePrimitiveMemberReader<TMode, ushort>(member),
                TypeCode.Int32 => CreatePrimitiveMemberReader<TMode, int>(member),
                TypeCode.UInt32 => CreatePrimitiveMemberReader<TMode, uint>(member),
                TypeCode.Int64 => CreatePrimitiveMemberReader<TMode, long>(member),
                TypeCode.UInt64 => CreatePrimitiveMemberReader<TMode, ulong>(member),
                TypeCode.Single => CreatePrimitiveMemberReader<TMode, float>(member),
                TypeCode.Double => CreatePrimitiveMemberReader<TMode, double>(member),
                TypeCode.Decimal => CreatePrimitiveMemberReader<TMode, decimal>(member),
                TypeCode.DateTime => CreatePrimitiveMemberReader<TMode, DateTime>(member),
                _ => null
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionObjectReader CreatePrimitiveMemberReader<TMode, T>(DataMember member)
            where TMode : struct, IReflectionSerialization
            where T : struct
        {
            return new PrimitiveMemberReader<TMode, T>(member).Read;
        }

        private sealed class PrimitiveMemberReader<TMode, T> : IPrimitiveMemberReader
            where TMode : struct, IReflectionSerialization
            where T : struct
        {
            private readonly DataMember _member;
            private readonly Func<object, T, object> _setter;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal PrimitiveMemberReader(DataMember member)
            {
                _member = member;
                _setter = FastInvokerBuilder.CreateTypedSetter<T>(member);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ValueTask<object> Read(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, object obj)
            {
                ValueTask<T> value = ReadValue(reader, context);
                if (value.IsCompletedSuccessfully)
                    return new ValueTask<object>(Assign(value.Result, context, obj));

                return AwaitValue(value, context, obj);
            }

            public Expression Connect(Expression reader, Expression context, Expression obj, LabelTarget result, Expression remaining)
            {
                Func<XmlReaderDelegator, XmlObjectSerializerReadContext, ValueTask<T>> read = ReadValue;
                Expression readValue = Expression.Call(Expression.Constant(this), read.Method, reader, context);
                ParameterExpression value = Expression.Variable(typeof(T), "value");
                Action<T, XmlObjectSerializerReadContext> register = RegisterValue;
                Expression assignment = FastInvokerBuilder.CreateSetterExpression(_member.MemberInfo, obj, value) ??
                    Expression.Assign(obj, Expression.Invoke(Expression.Constant(_setter), obj, value));
                Expression complete = Expression.Block(Expression.Call(register.Method, value, context), assignment);
                if (typeof(TMode) == typeof(SyncReflectionSerialization))
                {
                    return Expression.Block(new[] { value }, Expression.Assign(value, readValue), complete);
                }

                ParameterExpression pending = Expression.Variable(typeof(ValueTask<T>), "pending");
                Func<ValueTask<T>, XmlReaderDelegator, XmlObjectSerializerReadContext, object, ReflectionObjectReader, ValueTask<object>> resume = AwaitRemaining;
                return Expression.Block(new[] { pending, value }, Expression.Assign(pending, readValue),
                    Expression.IfThen(Expression.Not(Expression.Property(pending, nameof(ValueTask<T>.IsCompletedSuccessfully))),
                        Expression.Return(result, Expression.Call(Expression.Constant(this), resume.Method, pending, reader, context, obj, remaining))),
                    Expression.Assign(value, Expression.Property(pending, nameof(ValueTask<T>.Result))), complete);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ValueTask<T> ReadValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
            {
                context.ResetCollectionMemberInfo();
                if (reader.AttributeCount == 0)
                {
                    context.ResetAttributes();
                }
                else
                {
                    context.ReadAttributes(reader);
                    ValueTask<string?> objectId = TMode.ReadIfNullOrRef(reader, context, typeof(T), true);
                    if (!objectId.IsCompletedSuccessfully)
                        return AwaitAttributes(reader, context, objectId);

                    ValidateAttributes(objectId.Result, context);
                }
                if (typeof(TMode) == typeof(SyncReflectionSerialization))
                    return TMode.ReadPrimitiveElement<T>(reader);

                return reader.ReadPrimitiveElementAsync<T>(context.CancellationToken);
            }

            private object Assign(T value, XmlObjectSerializerReadContext context, object obj)
            {
                RegisterValue(value, context);
                return _setter(obj, value);
            }

            private static void RegisterValue(T value, XmlObjectSerializerReadContext context) => context.AddNewValue(value);

            private static void ValidateAttributes(string? objectId, XmlObjectSerializerReadContext context)
            {
                if (objectId is null)
                    throw new SerializationException(SR.Format(SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(typeof(T))));
                if (objectId.Length != 0)
                    throw new SerializationException(SR.Format(SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(typeof(T))));
                if (!string.IsNullOrEmpty(context.GetObjectId()))
                    throw new SerializationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(typeof(T))));
            }

            private static async ValueTask<T> AwaitAttributes(XmlReaderDelegator reader,
                XmlObjectSerializerReadContext context, ValueTask<string?> objectId)
            {
                ValidateAttributes(await objectId.ConfigureAwait(false), context);
                return await TMode.ReadPrimitiveElement<T>(reader).ConfigureAwait(false);
            }

            private async ValueTask<object> AwaitValue(ValueTask<T> value, XmlObjectSerializerReadContext context, object obj) =>
                Assign(await value.ConfigureAwait(false), context, obj);

            private async ValueTask<object> AwaitRemaining(ValueTask<T> value, XmlReaderDelegator reader,
                XmlObjectSerializerReadContext context, object obj, ReflectionObjectReader remaining)
            {
                obj = Assign(await value.ConfigureAwait(false), context, obj);
                return await remaining(reader, context, obj).ConfigureAwait(false);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected static ReflectionValueReader CreateValueReader<TMode>(Type type, string name, string ns, PrimitiveDataContract? primitiveContract = null)
            where TMode : struct, IReflectionSerialization
            => CreateValueReader<TMode>(type, name, ns, primitiveContract, null, out _);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionValueReader CreateValueReader<TMode>(Type type, string name, string ns,
            PrimitiveDataContract? primitiveContract, FastInvokerBuilder.Setter? setter, out ReflectionObjectReader? readMember)
            where TMode : struct, IReflectionSerialization
        {
            readMember = null;
            Type? nullableType = Nullable.GetUnderlyingType(type);
            bool isNullable = nullableType is not null;
            type = nullableType ?? type;
            primitiveContract = isNullable
                ? PrimitiveDataContract.GetPrimitiveDataContract(type)
                : primitiveContract ?? PrimitiveDataContract.GetPrimitiveDataContract(type);
            RuntimeTypeHandle handle = type.TypeHandle;
            int id = DataContract.GetId(handle);
            ReflectionValueReader read;
            if (primitiveContract is not null && primitiveContract.UnderlyingType != typeof(object))
            {
                read = CreatePrimitiveValueReader<TMode>(primitiveContract);
            }
            else
            {
                read = (reader, context) => TMode.Deserialize(reader, context, id, handle, name, ns);
            }

            bool isValueType = type.IsValueType;
            bool isPrimitive = primitiveContract is not null && primitiveContract.UnderlyingType != typeof(object);
            if (!isPrimitive && !isNullable && !isValueType)
            {
                return read;
            }

            if (isValueType)
            {
                return isNullable
                    ? CreateValueReader<TMode, ReflectionPlan.NullableValue>(read, type, name, ns, setter, out readMember)
                    : CreateValueReader<TMode, ReflectionPlan.RequiredValue>(read, type, name, ns, setter, out readMember);
            }

            return CreateValueReader<TMode, ReflectionPlan.ReferenceValue>(read, type, name, ns, setter, out readMember);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionValueReader CreatePrimitiveValueReader<TMode>(PrimitiveDataContract contract)
            where TMode : struct, IReflectionSerialization
        {
            Type type = contract.UnderlyingType;
            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                return Type.GetTypeCode(type) switch
                {
                    TypeCode.Boolean => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsBoolean(), context),
                    TypeCode.Char => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsChar(), context),
                    TypeCode.Byte => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsUnsignedByte(), context),
                    TypeCode.SByte => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsSignedByte(), context),
                    TypeCode.Int16 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsShort(), context),
                    TypeCode.UInt16 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsUnsignedShort(), context),
                    TypeCode.Int32 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsInt(), context),
                    TypeCode.UInt32 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsUnsignedInt(), context),
                    TypeCode.Int64 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsLong(), context),
                    TypeCode.UInt64 => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsUnsignedLong(), context),
                    TypeCode.Single => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsFloat(), context),
                    TypeCode.Double => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsDouble(), context),
                    TypeCode.Decimal => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsDecimal(), context),
                    TypeCode.DateTime => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsDateTime(), context),
                    TypeCode.String => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsString(), context),
                    _ when type == typeof(Guid) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsGuid(), context),
                    _ when type == typeof(TimeSpan) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsTimeSpan(), context),
                    _ when type == typeof(DateOnly) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsDateOnly(), context),
                    _ when type == typeof(TimeOnly) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsTimeOnly(), context),
                    _ when type == typeof(byte[]) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsBase64(), context),
                    _ when type == typeof(Uri) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsUri(), context),
                    _ when type == typeof(XmlQualifiedName) => static (reader, context) => ReadPrimitiveValue(reader.ReadElementContentAsQName(), context),
                    _ => (reader, context) => TMode.ReadXmlValue(contract, reader, context)
                };
            }

            return AsyncDataContractOperations.CreatePrimitiveReader(contract);
        }

        private static ValueTask<object?> ReadPrimitiveValue(object value, XmlObjectSerializerReadContext context)
        {
            context.AddNewObject(value);
            return new ValueTask<object?>(value);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionValueReader CreateValueReader<TMode, TValueKind>(ReflectionValueReader read, Type type, string name, string ns,
            FastInvokerBuilder.Setter? setter, out ReflectionObjectReader? readMember)
            where TMode : struct, IReflectionSerialization
            where TValueKind : struct
        {
            var valueReader = new ValueReader<TMode, TValueKind>(read, type, name, ns, setter);
            readMember = setter is null ? null : valueReader.ReadMember;
            return valueReader.Read;
        }

        private sealed class ValueReader<TMode, TValueKind>
            where TMode : struct, IReflectionSerialization
            where TValueKind : struct
        {
            private readonly ReflectionValueReader _read;
            private readonly Type _type;
            private readonly string _name;
            private readonly string _ns;
            private readonly bool _isSerializable;
            private readonly FastInvokerBuilder.Setter? _setter;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ValueReader(ReflectionValueReader read, Type type, string name, string ns, FastInvokerBuilder.Setter? setter)
            {
                _read = read;
                _type = type;
                _name = name;
                _ns = ns;
                _isSerializable = DataContract.IsTypeSerializable(type);
                _setter = setter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ValueTask<object?> Read(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
            {
                if (reader.AttributeCount == 0)
                {
                    context.ResetAttributes();
                    return _read(reader, context);
                }
                context.ReadAttributes(reader);
                ValueTask<string?> objectId = TMode.ReadIfNullOrRef(reader, context, _type, _isSerializable);
                if (objectId.IsCompletedSuccessfully)
                    return ReadValue<TMode, TValueKind>(objectId.Result, _read, reader, context, _type, _name, _ns);

                return AwaitValueRead<TMode, TValueKind>(objectId, _read, reader, context, _type, _name, _ns);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ValueTask<object> ReadMember(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, object obj)
            {
                Debug.Assert(_setter is not null);
                context.ResetCollectionMemberInfo();
                ValueTask<object?> value = Read(reader, context);
                if (value.IsCompletedSuccessfully)
                    return new ValueTask<object>(_setter(obj, value.Result));

                return AwaitMemberRead(value, _setter, obj);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static async ValueTask<object?> AwaitValueRead<TMode, TValueKind>(ValueTask<string?> objectId,
            ReflectionValueReader read, XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type, string name, string ns)
            where TMode : struct, IReflectionSerialization
            where TValueKind : struct =>
            await ReadValue<TMode, TValueKind>(await objectId.ConfigureAwait(false), read, reader, context, type, name, ns).ConfigureAwait(false);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ValueTask<object?> ReadValue<TMode, TValueKind>(string? objectId, ReflectionValueReader read,
            XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type, string name, string ns)
            where TMode : struct, IReflectionSerialization
            where TValueKind : struct
        {
            if (objectId is null)
            {
                if (typeof(TValueKind) == typeof(ReflectionPlan.RequiredValue))
                    throw new SerializationException(SR.Format(SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
                return new ValueTask<object?>((object?)null);
            }
            if (objectId.Length != 0)
            {
                if (typeof(TValueKind) != typeof(ReflectionPlan.ReferenceValue))
                    throw new SerializationException(SR.Format(SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
                return TMode.GetExistingObject(context, objectId, type, name, ns);
            }
            if (typeof(TValueKind) != typeof(ReflectionPlan.ReferenceValue) && !string.IsNullOrEmpty(context.GetObjectId()))
                throw new SerializationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));

            return read(reader, context);
        }

        private static void InvokeCallbacks(MethodInfo[] callbacks, XmlObjectSerializerReadContext context, object obj)
        {
            foreach (MethodInfo callback in callbacks)
            {
                callback.Invoke(obj, new object[] { context.GetStreamingContext() });
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        private static Func<object> CreateObjectFactory(ClassDataContract classContract)
        {
            Type type = classContract.UnderlyingType;
            if (!RuntimeFeature.IsDynamicCodeSupported && type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(KeyValue<,>))
            {
                return static () => new KeyValue<object, object?>();
            }
            ConstructorInfo? constructor = classContract.GetNonAttributedTypeConstructor();
            if (constructor is null || type == Globals.TypeOfSchemaDefinedType)
            {
                if (RuntimeFeature.IsDynamicCodeCompiled && type.IsValueType && Nullable.GetUnderlyingType(type) is null)
                {
                    return Expression.Lambda<Func<object>>(Expression.Convert(Expression.Default(type), typeof(object))).Compile();
                }
                return () => XmlFormatReaderGenerator.UnsafeGetUninitializedObject(type);
            }
            if (constructor.IsPublic)
            {
                return FastInvokerBuilder.GetMakeNewInstanceFunc(type);
            }

            return () => constructor.Invoke(Array.Empty<object>());
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionObjectReader CreateMembersReader<TMode>(ClassDataContract classContract, ReflectionObjectReader[] members, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionValueReader GenerateCollectionReader<TMode>(CollectionDataContract collectionContract, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, bool? isGetOnlyCollection)
            where TMode : struct, IReflectionSerialization
        {
            if (collectionContract.IsReadOnlyContract)
            {
                DataContract.ThrowInvalidDataContractException(collectionContract.DeserializationExceptionMessage, null);
            }

            return isGetOnlyCollection switch
            {
                true => GenerateCollectionReader<TMode, ReflectionPlan.Enabled>(collectionContract, itemName, itemNamespace),
                false => GenerateCollectionReader<TMode, ReflectionPlan.Disabled>(collectionContract, itemName, itemNamespace),
                null => GenerateCollectionReader<TMode, ReflectionPlan.Dynamic>(collectionContract, itemName, itemNamespace)
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionValueReader GenerateCollectionReader<TMode, TGetOnly>(CollectionDataContract collectionContract, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
            where TMode : struct, IReflectionSerialization
            where TGetOnly : struct
        {
            if (typeof(TGetOnly) != typeof(ReflectionPlan.Disabled) && collectionContract.Kind != CollectionKind.Array && collectionContract.AddMethod is null)
            {
                Type type = collectionContract.UnderlyingType;
                string message = SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(type));
                if (typeof(TGetOnly) == typeof(ReflectionPlan.Enabled))
                {
                    throw new InvalidDataContractException(message);
                }
                ReflectionValueReader normal = GenerateCollectionReader<TMode, ReflectionPlan.Disabled>(collectionContract, itemName, itemNamespace);
                return (reader, context) =>
                {
                    if (context.IsGetOnlyCollection)
                    {
                        throw new InvalidDataContractException(message);
                    }
                    return normal(reader, context);
                };
            }

            ReflectionValueReader? readArray = collectionContract.Kind == CollectionKind.Array && typeof(TGetOnly) != typeof(ReflectionPlan.Enabled)
                ? CreatePrimitiveArrayReader<TMode>(collectionContract.ItemType, itemName, itemNamespace) : null;
            if (IsArrayLikeCollection(collectionContract))
            {
                return readArray is not null
                    ? CreateCollectionReader<TMode, TGetOnly, ReflectionPlan.Enabled, ReflectionPlan.Enabled>(collectionContract, itemName, itemNamespace, readArray)
                    : CreateCollectionReader<TMode, TGetOnly, ReflectionPlan.Enabled, ReflectionPlan.Disabled>(collectionContract, itemName, itemNamespace, null);
            }

            return CreateCollectionReader<TMode, TGetOnly, ReflectionPlan.Disabled, ReflectionPlan.Disabled>(collectionContract, itemName, itemNamespace, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionValueReader CreateCollectionReader<TMode, TGetOnly, TArrayLike, TPrimitiveArray>(
            CollectionDataContract contract, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, ReflectionValueReader? readArray)
            where TMode : struct, IReflectionSerialization
            where TGetOnly : struct
            where TArrayLike : struct
            where TPrimitiveArray : struct
        {
            bool? fixedGetOnly = typeof(TGetOnly) == typeof(ReflectionPlan.Dynamic) ? null :
                typeof(TGetOnly) == typeof(ReflectionPlan.Enabled);
            Lazy<ReflectionCollectionBodyReader> readItems = new Lazy<ReflectionCollectionBodyReader>(() =>
                CreateCollectionItemsReader<TMode, TGetOnly, TArrayLike>(contract, itemName, itemNamespace, fixedGetOnly));
            Type collectionType = contract.UnderlyingType;
            ReflectionCollectionBodyReader body = (reader, context, collection, isGetOnly) =>
            {
                if (typeof(TGetOnly) == typeof(ReflectionPlan.Enabled) ||
                    typeof(TGetOnly) == typeof(ReflectionPlan.Dynamic) && isGetOnly)
                {
                    return ReadGetOnlyCollectionBody<TMode>(reader, context, collection, readItems, collectionType, itemName, itemNamespace);
                }

                Debug.Assert(collection is not null);
                return readItems.Value(reader, context, collection, isGetOnly);
            };
            body = WrapCollectionBodyReader<TMode>(contract, body);
            Lazy<Func<XmlObjectSerializerReadContext, object>> create = new Lazy<Func<XmlObjectSerializerReadContext, object>>(() => CreateCollectionFactory(contract));

            return (reader, context) =>
            {
                bool isGetOnly = typeof(TGetOnly) == typeof(ReflectionPlan.Enabled) ||
                    typeof(TGetOnly) == typeof(ReflectionPlan.Dynamic) && context.IsGetOnlyCollection;
                context.IsGetOnlyCollection = false;
                if (typeof(TPrimitiveArray) == typeof(ReflectionPlan.Enabled) && !isGetOnly)
                {
                    Debug.Assert(readArray is not null);
                    ValueTask<object?> array = readArray(reader, context);
                    if (!array.IsCompletedSuccessfully)
                    {
                        return AwaitPrimitiveArray<TArrayLike>(array, reader, context, body, create);
                    }
                    object? value = array.Result;
                    if (value is not null)
                    {
                        return new ValueTask<object?>(value);
                    }
                }
                return ReadRemainingCollection<TArrayLike>(reader, context, isGetOnly, body, create);
            };
        }

        private static async ValueTask<object?> AwaitPrimitiveArray<TArrayLike>(ValueTask<object?> pending,
            XmlReaderDelegator reader, XmlObjectSerializerReadContext context, ReflectionCollectionBodyReader body,
            Lazy<Func<XmlObjectSerializerReadContext, object>> create)
            where TArrayLike : struct
        {
            object? array = await pending.ConfigureAwait(false);
            if (array is not null)
            {
                return array;
            }
            return await ReadRemainingCollection<TArrayLike>(reader, context, false, body, create).ConfigureAwait(false);
        }

        private static async ValueTask<object?> ReadRemainingCollection<TArrayLike>(XmlReaderDelegator reader,
            XmlObjectSerializerReadContext context, bool isGetOnly, ReflectionCollectionBodyReader body,
            Lazy<Func<XmlObjectSerializerReadContext, object>> create)
            where TArrayLike : struct
        {
            if (isGetOnly)
            {
                await body(reader, context, context.GetCollectionMember(), true).ConfigureAwait(false);
                return null;
            }

            string objectId = context.GetObjectId();
            int size = typeof(TArrayLike) == typeof(ReflectionPlan.Enabled) ? context.GetArraySize() : -1;
            if (size >= 0)
            {
                context.IncrementItemCount(size);
            }
            object collection = create.Value(context);
            context.AddNewObject(collection);
            object? value = await body(reader, context, collection, false).ConfigureAwait(false);
            context.ReplaceDeserializedObject(objectId, collection, value);
            return value;
        }

        private static async ValueTask<object?> ReadGetOnlyCollectionBody<TMode>(XmlReaderDelegator reader,
            XmlObjectSerializerReadContext context, object? collection, Lazy<ReflectionCollectionBodyReader> readItems,
            Type collectionType, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
            where TMode : struct, IReflectionSerialization
        {
            if (!await TMode.IsStartElement(reader, itemName, itemNamespace).ConfigureAwait(false))
                return collection;
            if (collection is null)
                XmlObjectSerializerReadContext.ThrowNullValueReturnedForGetOnlyCollectionException(collectionType);

            return await readItems.Value(reader, context, collection, true).ConfigureAwait(false);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The reflected helpers have no member-preservation requirements on their generic parameters.")]
        private ReflectionCollectionBodyReader CreateCollectionItemsReader<TMode, TGetOnly, TArrayLike>(CollectionDataContract collectionContract, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, bool? isGetOnlyCollection)
            where TMode : struct, IReflectionSerialization
            where TGetOnly : struct
            where TArrayLike : struct
        {
            ReflectionValueReader readItem = collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary
                ? CreateDictionaryItemReader<TMode>(collectionContract)
                : CreateValueReader<TMode>(collectionContract.ItemType, GetCollectionContractItemName(collectionContract), GetCollectionContractNamespace(collectionContract));
            bool isArrayLikeCollection = IsArrayLikeCollection(collectionContract);
            (CollectionSetItemDelegate Setter, Func<object, int, object> Trim)? primitiveArrayPlan = isArrayLikeCollection
                ? CreatePrimitiveArrayPlan(collectionContract, isGetOnlyCollection)
                : null;
            CollectionSetItemDelegate? aotCollectionSetter = null;
            if (!RuntimeFeature.IsDynamicCodeSupported && primitiveArrayPlan is null &&
                collectionContract.ItemType.IsValueType &&
                collectionContract.Kind is not (CollectionKind.Dictionary or CollectionKind.GenericDictionary))
            {
                if (!isArrayLikeCollection || isGetOnlyCollection is true && collectionContract.Kind != CollectionKind.Array)
                {
                    aotCollectionSetter = CreateAotCollectionSetter(collectionContract);
                }
                else
                {
                    CollectionSetItemDelegate? getOnlySetter =
                        isGetOnlyCollection is null && collectionContract.Kind is (CollectionKind.GenericList or CollectionKind.GenericCollection)
                            ? CreateAotCollectionSetter(collectionContract)
                            : null;
                    primitiveArrayPlan = CreateAotArrayPlan(collectionContract, getOnlySetter);
                }
            }
            CollectionSetItemDelegate setItem = collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary
                ? CreateDictionarySetter(collectionContract)
                : primitiveArrayPlan?.Setter ?? aotCollectionSetter ??
                    (CollectionSetItemDelegate)s_createCollectionSetter.MakeGenericMethod(collectionContract.ItemType)
                        .Invoke(null, new object?[] { collectionContract, isGetOnlyCollection })!;
            Func<object, int, object> trim = isGetOnlyCollection is not true && isArrayLikeCollection
                ? primitiveArrayPlan?.Trim ?? s_trimCollection.MakeGenericMethod(collectionContract.ItemType).CreateDelegate<Func<object, int, object>>()
                : static (collection, count) => collection;
            return CreateCollectionItemsReader<TMode, TGetOnly, TArrayLike>(readItem, setItem, trim, itemName, itemNamespace);
        }

        private static ReflectionCollectionBodyReader CreateCollectionItemsReader<TMode, TGetOnly, TArrayLike>(
            ReflectionValueReader readItem, CollectionSetItemDelegate setItem, Func<object, int, object> trim,
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
            where TMode : struct, IReflectionSerialization
            where TGetOnly : struct
            where TArrayLike : struct
        {
            return async (reader, context, collection, isGetOnly) =>
            {
                Debug.Assert(collection is not null);
                int index = 0;
                int expectedSize = typeof(TArrayLike) == typeof(ReflectionPlan.Enabled) &&
                    (typeof(TGetOnly) == typeof(ReflectionPlan.Disabled) || typeof(TGetOnly) == typeof(ReflectionPlan.Dynamic) && !isGetOnly)
                    ? context.GetArraySize() : -1;
                while (true)
                {
                    if (await TMode.IsStartElement(reader, itemName, itemNamespace).ConfigureAwait(false))
                    {
                        if (expectedSize >= 0 && index == expectedSize)
                        {
                            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, expectedSize, itemName.Value, itemNamespace.Value));
                        }
                        if (expectedSize < 0)
                        {
                            context.IncrementItemCount(1);
                        }
                        object? value = await readItem(reader, context).ConfigureAwait(false);
                        collection = setItem(collection, value, index++, isGetOnly);
                    }
                    else
                    {
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        if (!await TMode.IsStartElement(reader).ConfigureAwait(false))
                        {
                            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, reader);
                        }
                        await TMode.SkipUnknownElement(reader, context).ConfigureAwait(false);
                    }
                }

                if (expectedSize >= 0)
                {
                    if (index != expectedSize)
                    {
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArraySizeAttributeIncorrect, expectedSize, index));
                    }
                }
                if (typeof(TArrayLike) == typeof(ReflectionPlan.Disabled) || typeof(TGetOnly) == typeof(ReflectionPlan.Enabled) ||
                    typeof(TGetOnly) == typeof(ReflectionPlan.Dynamic) && isGetOnly)
                {
                    return collection;
                }
                return trim(collection, index);
            };

        }

        private static (CollectionSetItemDelegate Setter, Func<object, int, object> Trim)? CreatePrimitiveArrayPlan(CollectionDataContract contract, bool? isGetOnlyCollection)
        {
            Type type = contract.ItemType;
            if (type == typeof(bool))
                return Create<bool>(contract, isGetOnlyCollection);
            if (type == typeof(DateTime))
                return Create<DateTime>(contract, isGetOnlyCollection);
            if (type == typeof(decimal))
                return Create<decimal>(contract, isGetOnlyCollection);
            if (type == typeof(int))
                return Create<int>(contract, isGetOnlyCollection);
            if (type == typeof(long))
                return Create<long>(contract, isGetOnlyCollection);
            if (type == typeof(float))
                return Create<float>(contract, isGetOnlyCollection);
            if (type == typeof(double))
                return Create<double>(contract, isGetOnlyCollection);
            if (type == typeof(byte))
                return Create<byte>(contract, isGetOnlyCollection);
            if (type == typeof(sbyte))
                return Create<sbyte>(contract, isGetOnlyCollection);
            if (type == typeof(char))
                return Create<char>(contract, isGetOnlyCollection);
            if (type == typeof(short))
                return Create<short>(contract, isGetOnlyCollection);
            if (type == typeof(ushort))
                return Create<ushort>(contract, isGetOnlyCollection);
            if (type == typeof(uint))
                return Create<uint>(contract, isGetOnlyCollection);
            if (type == typeof(ulong))
                return Create<ulong>(contract, isGetOnlyCollection);
            if (type == typeof(Guid))
                return Create<Guid>(contract, isGetOnlyCollection);
            if (type == typeof(TimeSpan))
                return Create<TimeSpan>(contract, isGetOnlyCollection);
            if (type == typeof(DateTimeOffset))
                return Create<DateTimeOffset>(contract, isGetOnlyCollection);

            return null;

            static (CollectionSetItemDelegate, Func<object, int, object>) Create<T>(CollectionDataContract collectionContract, bool? getOnly) =>
                (CreateCollectionSetter<T>(collectionContract, getOnly), TrimCollection<T>);
        }

        private static (CollectionSetItemDelegate Setter, Func<object, int, object> Trim) CreateAotArrayPlan(
            CollectionDataContract contract, CollectionSetItemDelegate? getOnlySetter)
        {
            Type itemType = contract.ItemType;
            Type collectionType = contract.UnderlyingType;
            return (SetItem, Trim);

            object SetItem(object collection, object? value, int index, bool isGetOnly)
            {
                if (isGetOnly && getOnlySetter is not null)
                {
                    return getOnlySetter(collection, value, index, isGetOnly);
                }
                Array array = (Array)collection;
                if (index >= array.Length)
                {
                    if (isGetOnly)
                    {
                        XmlObjectSerializerReadContext.ThrowArrayExceededSizeException(array.Length, collectionType);
                    }
                    if (index == int.MaxValue)
                    {
                        throw XmlObjectSerializer.CreateSerializationException(
                            SR.Format(SR.MaxArrayLengthExceeded, int.MaxValue, DataContract.GetClrTypeFullName(itemType)));
                    }
                    int newSize = index < int.MaxValue / 2 ? Math.Max(1, index * 2) : int.MaxValue;
                    Array resized = Array.CreateInstance(itemType, newSize);
                    Array.Copy(array, resized, array.Length);
                    array = resized;
                }
                array.SetValue(value, index);
                return array;
            }

            object Trim(object collection, int count)
            {
                Array array = (Array)collection;
                if (array.Length != count)
                {
                    Array resized = Array.CreateInstance(itemType, count);
                    Array.Copy(array, resized, count);
                    return resized;
                }
                return array;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionValueReader CreateDictionaryItemReader<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionCollectionBodyReader WrapCollectionBodyReader<TMode>(CollectionDataContract collectionContract, ReflectionCollectionBodyReader body)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionValueReader? CreatePrimitiveArrayReader<TMode>(Type itemType, XmlDictionaryString name, XmlDictionaryString ns)
            where TMode : struct, IReflectionSerialization
        {
            if (PrimitiveDataContract.GetPrimitiveDataContract(itemType) is null)
            {
                return null;
            }

            return Type.GetTypeCode(itemType) switch
            {
                TypeCode.Boolean => (reader, context) => TMode.ReadBooleanArray(reader, context, name, ns, context.GetArraySize()),
                TypeCode.DateTime => (reader, context) => TMode.ReadDateTimeArray(reader, context, name, ns, context.GetArraySize()),
                TypeCode.Decimal => (reader, context) => TMode.ReadDecimalArray(reader, context, name, ns, context.GetArraySize()),
                TypeCode.Int32 => (reader, context) => TMode.ReadInt32Array(reader, context, name, ns, context.GetArraySize()),
                TypeCode.Int64 => (reader, context) => TMode.ReadInt64Array(reader, context, name, ns, context.GetArraySize()),
                TypeCode.Single => (reader, context) => TMode.ReadSingleArray(reader, context, name, ns, context.GetArraySize()),
                TypeCode.Double => (reader, context) => TMode.ReadDoubleArray(reader, context, name, ns, context.GetArraySize()),
                _ => null
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static Func<XmlObjectSerializerReadContext, object> CreateCollectionFactory(CollectionDataContract collectionContract)
        {
            if (IsArrayLikeCollection(collectionContract))
            {
                Type itemType = collectionContract.ItemType;
                if (RuntimeFeature.IsDynamicCodeCompiled)
                {
                    ParameterExpression length = Expression.Parameter(typeof(int), nameof(length));
                    Func<int, object> createArray = Expression.Lambda<Func<int, object>>(
                        Expression.Convert(Expression.NewArrayBounds(itemType, length), typeof(object)), length).Compile();
                    return context => createArray(context.GetArraySize() >= 0 ? context.GetArraySize() : Math.Min(32, context.RemainingItemCount));
                }
                return context => Array.CreateInstance(itemType, context.GetArraySize() >= 0 ? context.GetArraySize() : Math.Min(32, context.RemainingItemCount));
            }
            if (collectionContract.Kind == CollectionKind.GenericDictionary && collectionContract.UnderlyingType.IsInterface)
            {
                Type type = typeof(Dictionary<,>).MakeGenericType(collectionContract.ItemType.GetGenericArguments());
                Func<object> createDictionary = FastInvokerBuilder.GetMakeNewInstanceFunc(type);
                return context => createDictionary();
            }
            if (collectionContract.UnderlyingType.IsValueType)
            {
                Type type = collectionContract.UnderlyingType;
                return context => Activator.CreateInstance(type)!;
            }
            if (collectionContract.UnderlyingType == typeof(IDictionary))
            {
                return static context => new Dictionary<object, object>();
            }

            ConstructorInfo collectionConstructor = collectionContract.Constructor!;
            if (collectionConstructor.IsPublic)
            {
                Func<object> createCollection = FastInvokerBuilder.GetMakeNewInstanceFunc(collectionContract.UnderlyingType);
                return context => createCollection();
            }
            return context => collectionConstructor.Invoke(Array.Empty<object>());
        }

        private static object TrimCollection<T>(object collection, int count) =>
            XmlObjectSerializerReadContext.TrimArraySize((T[])collection, count);

        [DynamicDependency(nameof(ICollection<object>.Add), typeof(ICollection<>))]
        private static CollectionSetItemDelegate CreateAotCollectionSetter(CollectionDataContract collectionContract)
        {
            Type collectionType = collectionContract.UnderlyingType;
            Type? collectionInterface = null;
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                collectionType.GetGenericArguments()[0] == collectionContract.ItemType)
            {
                collectionInterface = collectionType;
            }
            else
            {
                foreach (Type implementedInterface in collectionType.GetInterfaces())
                {
                    if (implementedInterface.IsGenericType &&
                        implementedInterface.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                        implementedInterface.GetGenericArguments()[0] == collectionContract.ItemType)
                    {
                        collectionInterface = implementedInterface;
                        break;
                    }
                }
            }

            if (collectionInterface is not null)
            {
                MethodInfo add = collectionInterface.GetMethod(nameof(ICollection<object>.Add))!;
                MethodInvoker invoke = MethodInvoker.Create(add);
                return (collection, value, index, isGetOnly) =>
                {
                    invoke.Invoke(collection, value);
                    return collection;
                };
            }

            if (typeof(IList).IsAssignableFrom(collectionType))
            {
                return static (collection, value, index, isGetOnly) =>
                {
                    ((IList)collection).Add(value);
                    return collection;
                };
            }

            MethodInfo? addMethod = collectionContract.AddMethod;
            if (addMethod is null)
            {
                throw new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(collectionType)));
            }
            return (collection, value, index, isGetOnly) =>
            {
                addMethod.Invoke(collection, new object?[] { value });
                return collection;
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        private static CollectionSetItemDelegate CreateCollectionSetter<T>(CollectionDataContract collectionContract, bool? isGetOnlyCollection)
        {
            if (isGetOnlyCollection is true && collectionContract.Kind == CollectionKind.Array)
            {
                Type collectionType = collectionContract.UnderlyingType;
                return (collection, value, index, isGetOnly) =>
                {
                    T[] array = (T[])collection;
                    if (index == array.Length)
                    {
                        XmlObjectSerializerReadContext.ThrowArrayExceededSizeException(array.Length, collectionType);
                    }
                    array[index] = (T)value!;
                    return collection;
                };
            }
            if (isGetOnlyCollection is null && collectionContract.Kind == CollectionKind.Array)
            {
                Type collectionType = collectionContract.UnderlyingType;
                return (collection, value, index, isGetOnly) =>
                {
                    T[] array = (T[])collection;
                    if (isGetOnly)
                    {
                        if (index == array.Length)
                        {
                            XmlObjectSerializerReadContext.ThrowArrayExceededSizeException(array.Length, collectionType);
                        }
                    }
                    else
                    {
                        array = XmlObjectSerializerReadContext.EnsureArraySize(array, index);
                    }
                    array[index] = (T)value!;
                    return array;
                };
            }
            if (isGetOnlyCollection is null && IsArrayLikeInterface(collectionContract))
            {
                if (typeof(ICollection<T>).IsAssignableFrom(collectionContract.UnderlyingType))
                {
                    return static (collection, value, index, isGetOnly) =>
                    {
                        if (isGetOnly)
                        {
                            ((ICollection<T>)collection).Add((T)value!);
                            return collection;
                        }
                        T[] array = XmlObjectSerializerReadContext.EnsureArraySize((T[])collection, index);
                        array[index] = (T)value!;
                        return array;
                    };
                }
                if (typeof(IList).IsAssignableFrom(collectionContract.UnderlyingType))
                {
                    return static (collection, value, index, isGetOnly) =>
                    {
                        if (isGetOnly)
                        {
                            ((IList)collection).Add(value);
                            return collection;
                        }
                        T[] array = XmlObjectSerializerReadContext.EnsureArraySize((T[])collection, index);
                        array[index] = (T)value!;
                        return array;
                    };
                }
            }
            if (isGetOnlyCollection is not true && IsArrayLikeCollection(collectionContract))
            {
                return static (collection, value, index, isGetOnly) =>
                {
                    T[] array = XmlObjectSerializerReadContext.EnsureArraySize((T[])collection, index);
                    array[index] = (T)value!;
                    return array;
                };
            }
            if (typeof(ICollection<T>).IsAssignableFrom(collectionContract.UnderlyingType))
            {
                return static (collection, value, index, isGetOnly) =>
                {
                    ((ICollection<T>)collection).Add((T)value!);
                    return collection;
                };
            }
            if (typeof(IList).IsAssignableFrom(collectionContract.UnderlyingType))
            {
                return static (collection, value, index, isGetOnly) =>
                {
                    ((IList)collection).Add(value);
                    return collection;
                };
            }

            MethodInfo? addMethod = collectionContract.AddMethod;
            if (addMethod is null)
            {
                throw new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType)));
            }
            return (collection, value, index, isGetOnly) =>
            {
                addMethod.Invoke(collection, new object?[] { value });
                return collection;
            };
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool UsesDictionaryInterface(CollectionDataContract collectionContract)
        {
            Type dictionaryInterface = typeof(IDictionary<,>).MakeGenericType(collectionContract.ItemType.GetGenericArguments());
            MethodInfo add = collectionContract.AddMethod!;
            if (add.DeclaringType == dictionaryInterface)
            {
                return true;
            }
            if (collectionContract.UnderlyingType.IsInterface)
            {
                return false;
            }

            InterfaceMapping map = collectionContract.UnderlyingType.GetInterfaceMap(dictionaryInterface);
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Name == Globals.AddMethodName)
                {
                    return map.TargetMethods[i].GetBaseDefinition().MethodHandle.Equals(add.GetBaseDefinition().MethodHandle);
                }
            }

            throw new UnreachableException();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static CollectionSetItemDelegate CreateDictionarySetter(CollectionDataContract collectionContract)
        {
            if (collectionContract.Kind == CollectionKind.Dictionary)
            {
                return static (collection, value, index, isGetOnly) =>
                {
                    IKeyValue pair = (IKeyValue)value!;
                    ((IDictionary)collection).Add(pair.Key!, pair.Value);
                    return collection;
                };
            }
            if (UsesDictionaryInterface(collectionContract))
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                {
                    Action<object, object?, object?> interfaceAdd = CreateCustomDictionaryAdder(collectionContract.AddMethod!);
                    return (collection, value, index, isGetOnly) =>
                    {
                        IKeyValue pair = (IKeyValue)value!;
                        interfaceAdd(collection, pair.Key, pair.Value);
                        return collection;
                    };
                }
                return static (collection, value, index, isGetOnly) =>
                {
                    ((IKeyValue)value!).AddToDictionary(collection);
                    return collection;
                };
            }

            Action<object, object?, object?> add = CreateCustomDictionaryAdder(collectionContract.AddMethod!);
            return (collection, value, index, isGetOnly) =>
            {
                IKeyValue pair = (IKeyValue)value!;
                add(collection, pair.Key, pair.Value);
                return collection;
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected static Action<object, object?, object?> CreateDictionaryAdder(CollectionDataContract collectionContract)
        {
            if (collectionContract.Kind == CollectionKind.GenericDictionary)
            {
                if (UsesDictionaryInterface(collectionContract))
                {
                    if (!RuntimeFeature.IsDynamicCodeSupported)
                    {
                        return CreateCustomDictionaryAdder(collectionContract.AddMethod!);
                    }
                    IKeyValue pair = (IKeyValue)XmlFormatReaderGenerator.UnsafeGetUninitializedObject(collectionContract.ItemType);
                    return pair.CreateDictionaryAdder();
                }

                return CreateCustomDictionaryAdder(collectionContract.AddMethod!);
            }

            return static (collection, key, value) => ((IDictionary)collection).Add(key!, value);
        }

        private static Action<object, object?, object?> CreateCustomDictionaryAdder(MethodInfo add)
        {
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                ParameterExpression collection = Expression.Parameter(typeof(object), nameof(collection));
                ParameterExpression key = Expression.Parameter(typeof(object), nameof(key));
                ParameterExpression value = Expression.Parameter(typeof(object), nameof(value));
                Type declaringType = add.DeclaringType!;
                Expression target = declaringType.IsValueType
                    ? Expression.Unbox(collection, declaringType) : Expression.Convert(collection, declaringType);
                ParameterInfo[] parameters = add.GetParameters();
                Expression call = Expression.Block(
                    Expression.Call(target, add,
                        Expression.Convert(key, parameters[0].ParameterType),
                        Expression.Convert(value, parameters[1].ParameterType)),
                    Expression.Empty());
                ParameterExpression exception = Expression.Parameter(typeof(Exception), nameof(exception));
                return Expression.Lambda<Action<object, object?, object?>>(
                    Expression.TryCatch(call, Expression.Catch(exception,
                        Expression.Throw(Expression.New(typeof(TargetInvocationException).GetConstructor(new[] { typeof(Exception) })!, exception)))),
                    collection, key, value).Compile();
            }

            MethodInvoker invoke = MethodInvoker.Create(add);
            return (collection, key, value) =>
            {
                try
                {
                    invoke.Invoke(collection, key, value);
                }
                catch (Exception exception)
                {
                    throw new TargetInvocationException(exception);
                }
            };
        }

        protected abstract string GetCollectionContractItemName(CollectionDataContract collectionContract);
        protected abstract string GetCollectionContractNamespace(CollectionDataContract collectionContract);
        protected abstract string GetClassContractNamespace(ClassDataContract classContract);

        private static object ResolveAdapterObject(object obj)
        {
            Type objType = obj.GetType();
            if (objType == typeof(DateTimeOffsetAdapter))
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffset((DateTimeOffsetAdapter)obj);
            }
            else if (objType == typeof(MemoryStreamAdapter))
            {
                obj = MemoryStreamAdapter.GetMemoryStream((MemoryStreamAdapter)obj);
            }
            return obj;
        }

        private static bool IsArrayLikeInterface(CollectionDataContract collectionContract)
        {
            if (collectionContract.UnderlyingType.IsInterface)
            {
                switch (collectionContract.Kind)
                {
                    case CollectionKind.Collection:
                    case CollectionKind.GenericCollection:
                    case CollectionKind.Enumerable:
                    case CollectionKind.GenericEnumerable:
                    case CollectionKind.List:
                    case CollectionKind.GenericList:
                        return true;
                }
            }
            return false;
        }

        private static bool IsArrayLikeCollection(CollectionDataContract collectionContract)
        {
            return collectionContract.Kind == CollectionKind.Array || IsArrayLikeInterface(collectionContract);
        }
    }
}
