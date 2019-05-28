// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Holds property values of any type.  For common value types, we have inline storage so that we don't need
    /// to box the values.  For all other types, we store the value in a single object reference field.
    /// 
    /// To get the value of a property quickly, use a delegate produced by <see cref="PropertyValue.GetPropertyGetter(PropertyInfo)"/>.
    /// </summary>
#if ES_BUILD_PN
    [CLSCompliant(false)]
    public
#else
    internal
#endif
    unsafe readonly struct PropertyValue
    {
        /// <summary>
        /// Union of well-known value types, to avoid boxing those types.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Scalar
        {
            [FieldOffset(0)]
            public bool AsBoolean;
            [FieldOffset(0)]
            public byte AsByte;
            [FieldOffset(0)]
            public sbyte AsSByte;
            [FieldOffset(0)]
            public char AsChar;
            [FieldOffset(0)]
            public short AsInt16;
            [FieldOffset(0)]
            public ushort AsUInt16;
            [FieldOffset(0)]
            public int AsInt32;
            [FieldOffset(0)]
            public uint AsUInt32;
            [FieldOffset(0)]
            public long AsInt64;
            [FieldOffset(0)]
            public ulong AsUInt64;
            [FieldOffset(0)]
            public IntPtr AsIntPtr;
            [FieldOffset(0)]
            public UIntPtr AsUIntPtr;
            [FieldOffset(0)]
            public float AsSingle;
            [FieldOffset(0)]
            public double AsDouble;
            [FieldOffset(0)]
            public Guid AsGuid;
            [FieldOffset(0)]
            public DateTime AsDateTime;
            [FieldOffset(0)]
            public DateTimeOffset AsDateTimeOffset;
            [FieldOffset(0)]
            public TimeSpan AsTimeSpan;
            [FieldOffset(0)]
            public decimal AsDecimal;
        }

        // Anything not covered by the Scalar union gets stored in this reference.
        readonly object? _reference;
        readonly Scalar _scalar;
        readonly int _scalarLength;

        private PropertyValue(object? value)
        {
            _reference = value;
            _scalar = default;
            _scalarLength = 0;
        }

        private PropertyValue(Scalar scalar, int scalarLength)
        {
            _reference = null;
            _scalar = scalar;
            _scalarLength = scalarLength;
        }

        private PropertyValue(bool value) : this(new Scalar() { AsBoolean = value }, sizeof(bool)) { }
        private PropertyValue(byte value) : this(new Scalar() { AsByte = value }, sizeof(byte)) { }
        private PropertyValue(sbyte value) : this(new Scalar() { AsSByte = value }, sizeof(sbyte)) { }
        private PropertyValue(char value) : this(new Scalar() { AsChar = value }, sizeof(char)) { }
        private PropertyValue(short value) : this(new Scalar() { AsInt16 = value }, sizeof(short)) { }
        private PropertyValue(ushort value) : this(new Scalar() { AsUInt16 = value }, sizeof(ushort)) { }
        private PropertyValue(int value) : this(new Scalar() { AsInt32 = value }, sizeof(int)) { }
        private PropertyValue(uint value) : this(new Scalar() { AsUInt32 = value }, sizeof(uint)) { }
        private PropertyValue(long value) : this(new Scalar() { AsInt64 = value }, sizeof(long)) { }
        private PropertyValue(ulong value) : this(new Scalar() { AsUInt64 = value }, sizeof(ulong)) { }
        private PropertyValue(IntPtr value) : this(new Scalar() { AsIntPtr = value }, sizeof(IntPtr)) { }
        private PropertyValue(UIntPtr value) : this(new Scalar() { AsUIntPtr = value }, sizeof(UIntPtr)) { }
        private PropertyValue(float value) : this(new Scalar() { AsSingle = value }, sizeof(float)) { }
        private PropertyValue(double value) : this(new Scalar() { AsDouble = value }, sizeof(double)) { }
        private PropertyValue(Guid value) : this(new Scalar() { AsGuid = value }, sizeof(Guid)) { }
        private PropertyValue(DateTime value) : this(new Scalar() { AsDateTime = value }, sizeof(DateTime)) { }
        private PropertyValue(DateTimeOffset value) : this(new Scalar() { AsDateTimeOffset = value }, sizeof(DateTimeOffset)) { }
        private PropertyValue(TimeSpan value) : this(new Scalar() { AsTimeSpan = value }, sizeof(TimeSpan)) { }
        private PropertyValue(decimal value) : this(new Scalar() { AsDecimal = value }, sizeof(decimal)) { }

        public static Func<object?, PropertyValue> GetFactory(Type type)
        {
            if (type == typeof(bool)) return value => new PropertyValue((bool)value!);
            if (type == typeof(byte)) return value => new PropertyValue((byte)value!);
            if (type == typeof(sbyte)) return value => new PropertyValue((sbyte)value!);
            if (type == typeof(char)) return value => new PropertyValue((char)value!);
            if (type == typeof(short)) return value => new PropertyValue((short)value!);
            if (type == typeof(ushort)) return value => new PropertyValue((ushort)value!);
            if (type == typeof(int)) return value => new PropertyValue((int)value!);
            if (type == typeof(uint)) return value => new PropertyValue((uint)value!);
            if (type == typeof(long)) return value => new PropertyValue((long)value!);
            if (type == typeof(ulong)) return value => new PropertyValue((ulong)value!);
            if (type == typeof(IntPtr)) return value => new PropertyValue((IntPtr)value!);
            if (type == typeof(UIntPtr)) return value => new PropertyValue((UIntPtr)value!);
            if (type == typeof(float)) return value => new PropertyValue((float)value!);
            if (type == typeof(double)) return value => new PropertyValue((double)value!);
            if (type == typeof(Guid)) return value => new PropertyValue((Guid)value!);
            if (type == typeof(DateTime)) return value => new PropertyValue((DateTime)value!);
            if (type == typeof(DateTimeOffset)) return value => new PropertyValue((DateTimeOffset)value!);
            if (type == typeof(TimeSpan)) return value => new PropertyValue((TimeSpan)value!);
            if (type == typeof(decimal)) return value => new PropertyValue((decimal)value!);

            return value => new PropertyValue(value);
        }

        public object? ReferenceValue
        {
            get
            {
                Debug.Assert(_scalarLength == 0, "This ReflectedValue refers to an unboxed value type, not a reference type or boxed value type.");
                return _reference;
            }
        }

        public Scalar ScalarValue
        {
            get
            {
                Debug.Assert(_scalarLength > 0, "This ReflectedValue refers to a reference type or boxed value type, not an unboxed value type");
                return _scalar;
            }
        }

        public int ScalarLength
        {
            get
            {
                Debug.Assert(_scalarLength > 0, "This ReflectedValue refers to a reference type or boxed value type, not an unboxed value type");
                return _scalarLength;
            }
        }

        /// <summary>
        /// Gets a delegate that gets the value of a given property.
        /// </summary>
        public static Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
        {
            if (property.DeclaringType!.GetTypeInfo().IsValueType)
                return GetBoxedValueTypePropertyGetter(property);
            else
                return GetReferenceTypePropertyGetter(property);
        }

        /// <summary>
        /// Gets a delegate that gets the value of a property of a value type.  We unfortunately cannot avoid boxing the value type,
        /// without making this generic over the value type.  That would result in a large number of generic instantiations, and furthermore
        /// does not work correctly on .NET Native (we cannot express the needed instantiations in an rd.xml file).  We expect that user-defined
        /// value types will be rare, and in any case the boxing only happens for events that are actually enabled.
        /// </summary>
        private static Func<PropertyValue, PropertyValue> GetBoxedValueTypePropertyGetter(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (type.GetTypeInfo().IsEnum)
                type = Enum.GetUnderlyingType(type);

            var factory = GetFactory(type);

            return container => factory(property.GetValue(container.ReferenceValue));
        }

        /// <summary>
        /// For properties of reference types, we use a generic helper class to get the value.  This enables us to use MethodInfo.CreateDelegate
        /// to build a fast getter.  We can get away with this on .NET Native, because we really only need one runtime instantiation of the
        /// generic type, since it's only instantiated over reference types (and thus all instances are shared).
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Func<PropertyValue, PropertyValue> GetReferenceTypePropertyGetter(PropertyInfo property)
        {
            var helper = (TypeHelper)Activator.CreateInstance(typeof(ReferenceTypeHelper<>).MakeGenericType(property.DeclaringType!))!;
            return helper.GetPropertyGetter(property);
        }

#if ES_BUILD_PN
        public
#else
        private
#endif
        abstract class TypeHelper
        {
            public abstract Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property);

            protected Delegate GetGetMethod(PropertyInfo property, Type propertyType)
            {
                return property.GetMethod!.CreateDelegate(typeof(Func<,>).MakeGenericType(property.DeclaringType!, propertyType));
            }
        }

#if ES_BUILD_PN
        public
#else
        private
#endif
        sealed class ReferenceTypeHelper<TContainer> : TypeHelper where TContainer : class?
        {
            public override Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
            {
                var type = property.PropertyType;

                if (!Statics.IsValueType(type))
                {
                    var getter = (Func<TContainer, object?>)GetGetMethod(property, type);
                    return container => new PropertyValue(getter((TContainer)container.ReferenceValue!));
                }
                else
                {
                    if (type.GetTypeInfo().IsEnum)
                        type = Enum.GetUnderlyingType(type);

                    if (type == typeof(bool)) { var f = (Func<TContainer, bool>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(byte)) { var f = (Func<TContainer, byte>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(sbyte)) { var f = (Func<TContainer, sbyte>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(char)) { var f = (Func<TContainer, char>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(short)) { var f = (Func<TContainer, short>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(ushort)) { var f = (Func<TContainer, ushort>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(int)) { var f = (Func<TContainer, int>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(uint)) { var f = (Func<TContainer, uint>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(long)) { var f = (Func<TContainer, long>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(ulong)) { var f = (Func<TContainer, ulong>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(IntPtr)) { var f = (Func<TContainer, IntPtr>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(UIntPtr)) { var f = (Func<TContainer, UIntPtr>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(float)) { var f = (Func<TContainer, float>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(double)) { var f = (Func<TContainer, double>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(Guid)) { var f = (Func<TContainer, Guid>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(DateTime)) { var f = (Func<TContainer, DateTime>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(DateTimeOffset)) { var f = (Func<TContainer, DateTimeOffset>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(TimeSpan)) { var f = (Func<TContainer, TimeSpan>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }
                    if (type == typeof(decimal)) { var f = (Func<TContainer, decimal>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue!)); }

                    return container => new PropertyValue(property.GetValue(container.ReferenceValue));
                }
            }
        }
    }
}
