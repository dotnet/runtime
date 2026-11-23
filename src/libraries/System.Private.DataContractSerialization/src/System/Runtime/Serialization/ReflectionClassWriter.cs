// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal abstract class ReflectionClassWriter
    {
        private static readonly MethodInfo s_getCollectionCount = typeof(ReflectionClassWriter).GetMethod(nameof(GetCollectionCount), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo s_createDictionaryEnumerator = typeof(ReflectionClassWriter).GetMethod(nameof(CreateGenericDictionaryEnumerator), BindingFlags.NonPublic | BindingFlags.Static)!;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionWriter GenerateClassWriter<TMode>(ClassDataContract classContract, XmlDictionaryString[]? memberNames)
            where TMode : struct, IReflectionSerialization
        {
            List<ClassDataContract> hierarchy = new List<ClassDataContract>();
            for (ClassDataContract? current = classContract; current is not null; current = current.BaseClassContract)
            {
                hierarchy.Add(current);
            }
            hierarchy.Reverse();

            List<ReflectionWriter> before = new List<ReflectionWriter>();
            List<ReflectionWriter> after = new List<ReflectionWriter>();
            List<ReflectionWriter> members = new List<ReflectionWriter>();
            int memberIndex = 0;
            foreach (ClassDataContract current in hierarchy)
            {
                if (current.OnSerializing is MethodInfo onSerializing)
                {
                    before.Add(CreateCallback(onSerializing));
                }
                if (current.OnSerialized is MethodInfo onSerialized)
                {
                    after.Add(CreateCallback(onSerialized));
                }

                if (!classContract.IsISerializable)
                {
                    int memberCount = current.Members!.Count;
                    if (memberCount != 0)
                    {
                        members.Add((writer, obj, context) =>
                        {
                            context.IncrementItemCount(memberCount);
                            return Task.CompletedTask;
                        });
                    }
                    foreach (DataMember member in current.Members)
                    {
                        members.Add(CreateMemberWriter<TMode>(current, classContract, member, memberNames, memberIndex++));
                    }
                }
            }

            if (classContract.IsISerializable)
            {
                members.Add(static (writer, obj, context) => TMode.WriteISerializable(writer, context, (ISerializable)obj));
            }
            else if (classContract.HasExtensionData)
            {
                members.Insert(0, static (writer, obj, context) =>
                    TMode.WriteExtensionData(writer, context, ((IExtensibleDataObject)obj).ExtensionData, -1));
            }

            members.AddRange(after);
            if (classContract.UnderlyingType == typeof(DateTimeOffsetAdapter))
            {
                if (typeof(TMode) == typeof(SyncReflectionSerialization))
                {
                    ReflectionWriter write = CreateSequence(members.ToArray());
                    before.Add((writer, obj, context) => write(writer, DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj), context));
                }
                else
                {
                    before.Add(ReflectionSerialization.CreateAdaptedWriterSequence(members.ToArray(),
                        static obj => DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj)));
                }
            }
            else if (classContract.UnderlyingType == typeof(MemoryStreamAdapter))
            {
                ReflectionWriter write = CreateSequence(members.ToArray());
                before.Add((writer, obj, context) => write(writer, MemoryStreamAdapter.GetMemoryStreamAdapter((MemoryStream)obj), context));
            }
            else
            {
                before.AddRange(members);
            }

            return CreateSequence(before.ToArray());
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionWriter CreateMemberWriter<TMode>(ClassDataContract classContract, ClassDataContract rootContract, DataMember member, XmlDictionaryString[]? memberNames, int memberIndex)
            where TMode : struct, IReflectionSerialization
        {
            FastInvokerBuilder.Getter getter = member.Getter;
            bool isGetOnlyCollection = member.IsGetOnlyCollection;
            if (member.EmitDefaultValue)
            {
                ReflectionMemberValueWriter write = CreateEmittedMemberWriter<TMode>(classContract, rootContract, member, memberNames, memberIndex);
                return ReflectionSerialization.CreateMemberWriter(member.MemberInfo, getter, write, isGetOnlyCollection);
            }

            Lazy<ReflectionMemberValueWriter> writeWhenEmitted = new Lazy<ReflectionMemberValueWriter>(() =>
                CreateEmittedMemberWriter<TMode>(classContract, rootContract, member, memberNames, memberIndex));
            object? defaultValue = XmlFormatGeneratorStatics.GetDefaultValue(member.MemberType);
            if (member.IsRequired)
            {
                return defaultValue is null
                    ? CreateDefaultMemberWriter<ReflectionPlan.Enabled, ReflectionPlan.Enabled>(getter, writeWhenEmitted, defaultValue, isGetOnlyCollection, member.Name, classContract.UnderlyingType)
                    : CreateDefaultMemberWriter<ReflectionPlan.Enabled, ReflectionPlan.Disabled>(getter, writeWhenEmitted, defaultValue, isGetOnlyCollection, member.Name, classContract.UnderlyingType);
            }

            return defaultValue is null
                ? CreateDefaultMemberWriter<ReflectionPlan.Disabled, ReflectionPlan.Enabled>(getter, writeWhenEmitted, defaultValue, isGetOnlyCollection, member.Name, classContract.UnderlyingType)
                : CreateDefaultMemberWriter<ReflectionPlan.Disabled, ReflectionPlan.Disabled>(getter, writeWhenEmitted, defaultValue, isGetOnlyCollection, member.Name, classContract.UnderlyingType);
        }

        private static ReflectionWriter CreateDefaultMemberWriter<TRequired, TNullDefault>(
            FastInvokerBuilder.Getter getter, Lazy<ReflectionMemberValueWriter> write, object? defaultValue,
            bool isGetOnlyCollection, string memberName, Type declaringType)
            where TRequired : struct
            where TNullDefault : struct
        {
            return (writer, obj, context) =>
            {
                if (isGetOnlyCollection)
                    context.StoreIsGetOnlyCollection();
                else
                    context.ResetIsGetOnlyCollection();
                object? value = getter(obj);
                bool isDefault = typeof(TNullDefault) == typeof(ReflectionPlan.Enabled)
                    ? value is null : value is not null && value.Equals(defaultValue);
                if (isDefault)
                {
                    if (typeof(TRequired) == typeof(ReflectionPlan.Enabled))
                    {
                        XmlObjectSerializerWriteContext.ThrowRequiredMemberMustBeEmitted(memberName, declaringType);
                    }
                    return Task.CompletedTask;
                }

                return write.Value(writer, obj, value, context);
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionMemberValueWriter CreateEmittedMemberWriter<TMode>(ClassDataContract classContract, ClassDataContract rootContract, DataMember member, XmlDictionaryString[]? memberNames, int memberIndex)
            where TMode : struct, IReflectionSerialization
        {
            ReflectionMemberValueWriter write = CreateMemberValueWriter<TMode>(classContract, rootContract, member, memberNames, memberIndex);
            if (!classContract.HasExtensionData)
            {
                return write;
            }

            return async (writer, obj, value, context) =>
            {
                await write(writer, obj, value, context).ConfigureAwait(false);
                await TMode.WriteExtensionData(writer, context, ((IExtensibleDataObject)obj).ExtensionData, memberIndex).ConfigureAwait(false);
            };
        }

        private static ReflectionWriter CreateCallback(MethodInfo callback) =>
            (writer, obj, context) =>
            {
                callback.Invoke(obj, new object[] { context.GetStreamingContext() });
                return Task.CompletedTask;
            };

        private static ReflectionWriter CreateSequence(ReflectionWriter[] operations)
        {
            if (operations.Length == 0)
            {
                return static (writer, obj, context) => Task.CompletedTask;
            }
            if (operations.Length == 1)
            {
                return operations[0];
            }

            return ReflectionSerialization.CreateWriterSequence(operations);
        }

        protected static ReflectionWriter Compose(ReflectionWriter first, ReflectionWriter second) =>
            ReflectionSerialization.CreateWriterSequence(new[] { first, second });

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionWriter GenerateCollectionWriter<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization
        {
            ReflectionWriter? count = CreateCollectionCounter<TMode>(collectionContract);
            ReflectionWriter body;
            if (collectionContract.Kind == CollectionKind.Array)
            {
                ReflectionWriter? primitiveWriter = CreatePrimitiveArrayWriter<TMode>(collectionContract.ItemType, GetCollectionItemName(collectionContract), GetCollectionNamespace(collectionContract));
                if (primitiveWriter is not null)
                {
                    body = primitiveWriter;
                }
                else
                {
                    ReflectionValueWriter writeItem = CreateCollectionItemWriter<TMode>(collectionContract);
                    body = async (writer, obj, context) =>
                    {
                        Array array = (Array)obj;
                        for (int i = 0; i < array.Length; i++)
                        {
                            await writeItem(writer, array.GetValue(i), context).ConfigureAwait(false);
                        }
                    };
                }
            }
            else
            {
                Func<object, IEnumerator> getEnumerator = CreateEnumerator(collectionContract);
                ReflectionValueWriter writeItem = CreateCollectionItemWriter<TMode>(collectionContract);
                body = count is null
                    ? CreateCollectionItemsWriter<ReflectionPlan.Enabled>(getEnumerator, writeItem)
                    : CreateCollectionItemsWriter<ReflectionPlan.Disabled>(getEnumerator, writeItem);
            }

            return WrapCollectionWriter<TMode>(collectionContract, count, body);
        }

        private static ReflectionWriter CreateCollectionItemsWriter<TCountItems>(
            Func<object, IEnumerator> getEnumerator, ReflectionValueWriter writeItem)
            where TCountItems : struct =>
            (writer, obj, context) =>
                ReflectionSerialization.WriteCollection<TCountItems>(getEnumerator, writeItem, writer, obj, context);

        protected abstract XmlDictionaryString GetCollectionItemName(CollectionDataContract collectionContract);
        protected abstract XmlDictionaryString? GetCollectionNamespace(CollectionDataContract collectionContract);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionValueWriter CreateCollectionItemWriter<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionWriter WrapCollectionWriter<TMode>(CollectionDataContract collectionContract, ReflectionWriter? count, ReflectionWriter body)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionWriter? CreatePrimitiveArrayWriter<TMode>(Type itemType, XmlDictionaryString name, XmlDictionaryString? ns)
            where TMode : struct, IReflectionSerialization
        {
            if (PrimitiveDataContract.GetPrimitiveDataContract(itemType) is null)
            {
                return null;
            }

            return Type.GetTypeCode(itemType) switch
            {
                TypeCode.Boolean => (writer, obj, context) => TMode.WriteBooleanArray(writer, (bool[])obj, name, ns),
                TypeCode.DateTime => (writer, obj, context) => TMode.WriteDateTimeArray(writer, (DateTime[])obj, name, ns),
                TypeCode.Decimal => (writer, obj, context) => TMode.WriteDecimalArray(writer, (decimal[])obj, name, ns),
                TypeCode.Int32 => (writer, obj, context) => TMode.WriteInt32Array(writer, (int[])obj, name, ns),
                TypeCode.Int64 => (writer, obj, context) => TMode.WriteInt64Array(writer, (long[])obj, name, ns),
                TypeCode.Single => (writer, obj, context) => TMode.WriteSingleArray(writer, (float[])obj, name, ns),
                TypeCode.Double => (writer, obj, context) => TMode.WriteDoubleArray(writer, (double[])obj, name, ns),
                _ => null
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The reflected generic helper reads ICollection<T>.Count and is not annotated.")]
        private static ReflectionWriter? CreateCollectionCounter<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization
        {
            Func<object, int> getCount;
            switch (collectionContract.Kind)
            {
                case CollectionKind.Array:
                    return static (writer, obj, context) => TMode.IncrementCollectionCount(writer, context, ((Array)obj).Length);
                case CollectionKind.Collection:
                case CollectionKind.List:
                case CollectionKind.Dictionary:
                    return static (writer, obj, context) => TMode.IncrementCollectionCount(writer, context, ((ICollection)obj).Count);
                case CollectionKind.GenericCollection:
                case CollectionKind.GenericList:
                    getCount = CreateCollectionCountGetter(collectionContract.ItemType);
                    break;
                case CollectionKind.GenericDictionary:
                    Type itemType = typeof(KeyValuePair<,>).MakeGenericType(collectionContract.ItemType.GetGenericArguments());
                    getCount = CreateCollectionCountGetter(itemType);
                    break;
                default:
                    return null;
            }

            return (writer, obj, context) => TMode.IncrementCollectionCount(writer, context, getCount(obj));
        }

        private static int GetCollectionCount<T>(object obj) => ((ICollection<T>)obj).Count;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ICollection<>))]
        private static Func<object, int> CreateCollectionCountGetter(Type itemType)
        {
            if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            {
                return s_getCollectionCount.MakeGenericMethod(itemType).CreateDelegate<Func<object, int>>();
            }

            Type collectionType = typeof(ICollection<>).MakeGenericType(itemType);
            PropertyInfo countProperty = collectionType.GetProperty(nameof(ICollection<object>.Count))!;
            return obj => GetReflectedCollectionCount(countProperty, obj);
        }

        private static int GetReflectedCollectionCount(PropertyInfo countProperty, object obj)
        {
            try
            {
                return (int)countProperty.GetValue(obj)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(exception.InnerException);
                throw;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [DynamicDependency(nameof(IEnumerator<object>.Current), typeof(IEnumerator<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The reflected generic helper adapts an existing dictionary enumerator and is not annotated.")]
        protected static Func<object, IEnumerator> CreateEnumerator(CollectionDataContract collectionContract)
        {
            if (collectionContract.Kind == CollectionKind.GenericDictionary)
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                {
                    Type pairType = typeof(KeyValuePair<,>).MakeGenericType(collectionContract.ItemType.GetGenericArguments());
                    Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(pairType);
                    MethodInvoker current = MethodInvoker.Create(enumeratorType.GetProperty(nameof(IEnumerator<object>.Current))!.GetMethod!);
                    PropertyInfo key = pairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
                    PropertyInfo value = pairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;
                    return obj => new AotDictionaryEnumerator(((IEnumerable)obj).GetEnumerator(), current, key, value);
                }
                return s_createDictionaryEnumerator.MakeGenericMethod(collectionContract.ItemType.GetGenericArguments()).CreateDelegate<Func<object, IEnumerator>>();
            }
            if (collectionContract.Kind == CollectionKind.Dictionary)
            {
                return static obj => new CollectionDataContract.DictionaryEnumerator(((IDictionary)obj).GetEnumerator());
            }

            return static obj => ((IEnumerable)obj).GetEnumerator();
        }

        private static IEnumerator CreateGenericDictionaryEnumerator<TKey, TValue>(object obj) =>
            new CollectionDataContract.GenericDictionaryEnumerator<TKey, TValue>((IEnumerator<KeyValuePair<TKey, TValue>>)((IEnumerable)obj).GetEnumerator());

        private sealed class AotDictionaryEnumerator(IEnumerator enumerator, MethodInvoker current, PropertyInfo key, PropertyInfo value) : IEnumerator, IDisposable
        {
            public object Current
            {
                get
                {
                    object pair = current.Invoke(enumerator)!;
                    return new KeyValue<object, object?>(key.GetValue(pair)!, value.GetValue(pair));
                }
            }

            public bool MoveNext() => enumerator.MoveNext();
            public void Reset() => enumerator.Reset();
            public void Dispose() { }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract ReflectionMemberValueWriter CreateMemberValueWriter<TMode>(ClassDataContract classContract, ClassDataContract rootContract, DataMember member, XmlDictionaryString[]? memberNames, int memberIndex)
            where TMode : struct, IReflectionSerialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected static ReflectionMemberValueWriter CreatePrimitiveElementWriter<TMode>(
            PrimitiveDataContract contract, XmlDictionaryString name, XmlDictionaryString? ns)
            where TMode : struct, IReflectionSerialization
        {
            Type type = contract.UnderlyingType;
            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                if (ReflectionSerialization.CreateTypedPrimitiveElementWriter(type, name, ns) is ReflectionMemberValueWriter typedWriter)
                    return typedWriter;

                return Type.GetTypeCode(type) switch
                {
                    TypeCode.UInt64 => (writer, obj, value, context) =>
                    {
                        writer.WriteUnsignedLong((ulong)value!, name, ns);
                        return Task.CompletedTask;
                    },
                    TypeCode.String => (writer, obj, value, context) =>
                    {
                        context.WriteString(writer, (string?)value, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(Guid) => (writer, obj, value, context) =>
                    {
                        writer.WriteGuid((Guid)value!, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(TimeSpan) => (writer, obj, value, context) =>
                    {
                        writer.WriteTimeSpan((TimeSpan)value!, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(DateOnly) => (writer, obj, value, context) =>
                    {
                        writer.WriteDateOnly((DateOnly)value!, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(TimeOnly) => (writer, obj, value, context) =>
                    {
                        writer.WriteTimeOnly((TimeOnly)value!, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(Uri) => (writer, obj, value, context) =>
                    {
                        writer.WriteUri((Uri?)value, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(XmlQualifiedName) => (writer, obj, value, context) =>
                    {
                        context.WriteQName(writer, (XmlQualifiedName?)value, name, ns);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(byte[]) => (writer, obj, value, context) =>
                    {
                        writer.WriteStartElement(name, ns);
                        writer.WriteBase64((byte[]?)value);
                        writer.WriteEndElement();
                        return Task.CompletedTask;
                    },
                    _ => (writer, obj, value, context) => TMode.WriteXmlElement(contract, writer, value, context, name, ns)
                };
            }

            if (type == typeof(string))
            {
                return (writer, obj, value, context) =>
                    context.WriteReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(writer, value, type, name, ns, null);
            }
            if (type == typeof(Uri))
            {
                return (writer, obj, value, context) =>
                    context.WriteReferencePrimitiveElementAsync<ReflectionPlan.UriValue>(writer, value, type, name, ns, null);
            }
            if (type == typeof(XmlQualifiedName))
            {
                string? prefix = ns is not null && ns.Value.Length > 0 ? Globals.ElementPrefix : null;
                return (writer, obj, value, context) =>
                    context.WriteReferencePrimitiveElementAsync<ReflectionPlan.QualifiedValue>(writer, value, type, name, ns, prefix);
            }
            if (type == typeof(byte[]))
            {
                return ReflectionSerialization.CreateElementWriter<TMode>(
                    static (writer, value, context) => writer.WriteBase64Async((byte[]?)value), null, name, ns, null);
            }

            return AsyncDataContractOperations.CreatePrimitiveElementWriter(type, name, ns);
        }

        protected static ReflectionMemberValueWriter? TryCreateNullablePrimitiveElementWriter<TMode>(
            Type type, XmlDictionaryString name, XmlDictionaryString? ns)
            where TMode : struct, IReflectionSerialization
        {
            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                return ReflectionSerialization.CreateNullableTypedPrimitiveElementWriter(type, name, ns);
            }

            return AsyncDataContractOperations.TryCreateNullablePrimitiveElementWriter(type, name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionValueWriter CreatePrimitiveValueWriter<TMode>(PrimitiveDataContract contract)
            where TMode : struct, IReflectionSerialization
        {
            Type type = contract.UnderlyingType;
            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                return Type.GetTypeCode(type) switch
                {
                    TypeCode.Boolean => static (writer, value, context) =>
                    {
                        writer.WriteBoolean((bool)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Char => static (writer, value, context) =>
                    {
                        writer.WriteChar((char)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Byte => static (writer, value, context) =>
                    {
                        writer.WriteUnsignedByte((byte)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.SByte => static (writer, value, context) =>
                    {
                        writer.WriteSignedByte((sbyte)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Int16 => static (writer, value, context) =>
                    {
                        writer.WriteShort((short)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.UInt16 => static (writer, value, context) =>
                    {
                        writer.WriteUnsignedShort((ushort)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Int32 => static (writer, value, context) =>
                    {
                        writer.WriteInt((int)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.UInt32 => static (writer, value, context) =>
                    {
                        writer.WriteUnsignedInt((uint)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Int64 => static (writer, value, context) =>
                    {
                        writer.WriteLong((long)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.UInt64 => static (writer, value, context) =>
                    {
                        writer.WriteUnsignedLong((ulong)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Single => static (writer, value, context) =>
                    {
                        writer.WriteFloat((float)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Double => static (writer, value, context) =>
                    {
                        writer.WriteDouble((double)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.Decimal => static (writer, value, context) =>
                    {
                        writer.WriteDecimal((decimal)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.DateTime => static (writer, value, context) =>
                    {
                        writer.WriteDateTime((DateTime)value!);
                        return Task.CompletedTask;
                    },
                    TypeCode.String => static (writer, value, context) =>
                    {
                        writer.WriteString((string)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(Guid) => static (writer, value, context) =>
                    {
                        writer.WriteGuid((Guid)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(TimeSpan) => static (writer, value, context) =>
                    {
                        writer.WriteTimeSpan((TimeSpan)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(DateOnly) => static (writer, value, context) =>
                    {
                        writer.WriteDateOnly((DateOnly)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(TimeOnly) => static (writer, value, context) =>
                    {
                        writer.WriteTimeOnly((TimeOnly)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(byte[]) => static (writer, value, context) =>
                    {
                        writer.WriteBase64((byte[]?)value);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(Uri) => static (writer, value, context) =>
                    {
                        writer.WriteUri((Uri)value!);
                        return Task.CompletedTask;
                    },
                    _ when type == typeof(XmlQualifiedName) => static (writer, value, context) =>
                    {
                        writer.WriteQName((XmlQualifiedName)value!);
                        return Task.CompletedTask;
                    },
                    _ => (writer, value, context) => TMode.WriteXmlValue(contract, writer, value!, context)
                };
            }

            return AsyncDataContractOperations.CreatePrimitiveValueWriter(type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected static ReflectionValueWriter CreateValueWriter<TMode>(Type type, bool writeXsiType, PrimitiveDataContract? primitiveContract)
            where TMode : struct, IReflectionSerialization
        {
            Type? nullableType = Nullable.GetUnderlyingType(type);
            bool isNullable = nullableType is not null;
            type = nullableType ?? type;
            primitiveContract = isNullable ? PrimitiveDataContract.GetPrimitiveDataContract(type) : primitiveContract;
            RuntimeTypeHandle handle = type.TypeHandle;
            int id = DataContract.GetId(handle);

            if (primitiveContract is not null && primitiveContract.UnderlyingType != typeof(object) && !writeXsiType)
            {
                ReflectionValueWriter write = CreatePrimitiveValueWriter<TMode>(primitiveContract);
                if (!isNullable && type.IsValueType)
                {
                    return write;
                }

                bool isSerializable = DataContract.IsTypeSerializable(type);
                return (writer, value, context) => value is null
                    ? TMode.WriteNull(writer, context, type, isSerializable)
                    : write(writer, value, context);
            }

            if (isNullable)
            {
                bool isSerializable = DataContract.IsTypeSerializable(type);
                return (writer, value, context) => value is null
                    ? TMode.WriteNull(writer, context, type, isSerializable)
                    : TMode.SerializeValue(writer, context, value, true, writeXsiType, id, handle);
            }
            if (type.IsValueType)
            {
                if (typeof(TMode) == typeof(SyncReflectionSerialization))
                {
                    return (writer, value, context) => TMode.SerializeValue(writer, context, value!, true, writeXsiType, id, handle);
                }

                return AsyncDataContractOperations.CreateContractValueWriter(type, true, writeXsiType, id, handle);
            }

            bool isReferenceSerializable = DataContract.IsTypeSerializable(type);
            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                return (writer, value, context) => value is null
                    ? TMode.WriteNull(writer, context, type, isReferenceSerializable)
                    : TMode.SerializeReference(writer, context, value, value.GetType().TypeHandle.Equals(handle), writeXsiType, id, handle);
            }

            return AsyncDataContractOperations.CreateContractValueWriter(type, isReferenceSerializable, writeXsiType, id, handle);
        }

    }
}
