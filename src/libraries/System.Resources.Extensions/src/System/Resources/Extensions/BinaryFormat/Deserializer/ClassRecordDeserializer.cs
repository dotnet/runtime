// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

#pragma warning disable SYSLIB0050 // Type or member is obsolete

/// <summary>
///  Base class for deserializing <see cref="ClassRecord"/>s.
/// </summary>
internal abstract class ClassRecordDeserializer : ObjectRecordDeserializer
{
    private readonly bool _onlyAllowPrimitives;

    private protected ClassRecordDeserializer(ClassRecord classRecord, object @object, IDeserializer deserializer)
        : base(classRecord, deserializer)
    {
        Object = @object;

        // We want to be able to complete IObjectReference without having to evaluate their dependencies
        // for circular references. See ValidateNewMemberObjectValue below for more.
        _onlyAllowPrimitives = @object is IObjectReference;
    }

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal static ObjectRecordDeserializer Create(ClassRecord classRecord, IDeserializer deserializer)
    {
        Type type = deserializer.TypeResolver.GetType(classRecord.TypeName);
        SerializationRecordId id = classRecord.Id;

        ISerializationSurrogate? surrogate = deserializer.GetSurrogate(type);

        if (!type.IsSerializable && surrogate is null)
        {
            // SurrogateSelectors allow populating types that are not marked as serializable.
            throw new SerializationException(SR.Format(SR.Serialization_TypeNotSerializable, type));
        }

        object @object =
#if NETCOREAPP
            RuntimeHelpers.GetUninitializedObject(type);
#else
            Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#endif

        // Invoke any OnDeserializing methods.
        SerializationEvents.GetOnDeserializingForType(type, @object)?.Invoke(deserializer.Options.StreamingContext);

        ObjectRecordDeserializer? recordDeserializer;

        if (surrogate is not null || typeof(ISerializable).IsAssignableFrom(type))
        {
            recordDeserializer = new ClassRecordSerializationInfoDeserializer(classRecord, @object, type, surrogate, deserializer);
        }
        else
        {
            // Directly set fields for non-ISerializable types.
            recordDeserializer = new ClassRecordFieldInfoDeserializer(classRecord, @object, type, deserializer);
        }

        return recordDeserializer;
    }

    private protected override void ValidateNewMemberObjectValue(object value)
    {
        if (!_onlyAllowPrimitives)
        {
            return;
        }

        // The goal with this restriction is to know definitively that we can complete the contianing object when we
        // finish with it's members, even if it is going to be replaced with another instance (as IObjectReference does).
        // If there are no reference types we know that there is no way for references to this object getting it in an
        // in an unconverted state or converted with uncompleted state (due to some direct or indirect reference from
        // this object).
        //
        // If we wanted support to be fully open-ended we would have queue completion along with pending SerializationInfo
        // objects to rehydrate in the proper order (depth-first) and reject any case where the object is involved in
        // a cycle.

        Type type = value.GetType();
        if (type.IsArray)
        {
            type = type.GetElementType()!;
        }

        bool primitive = type.IsPrimitive || type.IsEnum || type == typeof(string);
        if (!primitive)
        {
            throw new SerializationException(SR.Format(SR.Serialization_IObjectReferenceOnlyPrimivite, type));
        }
    }
}

#pragma warning restore SYSLIB0050 // Type or member is obsolete

