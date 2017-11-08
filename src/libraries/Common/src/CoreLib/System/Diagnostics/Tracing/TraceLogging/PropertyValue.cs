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
            public Boolean AsBoolean;
            [FieldOffset(0)]
            public Byte AsByte;
            [FieldOffset(0)]
            public SByte AsSByte;
            [FieldOffset(0)]
            public Char AsChar;
            [FieldOffset(0)]
            public Int16 AsInt16;
            [FieldOffset(0)]
            public UInt16 AsUInt16;
            [FieldOffset(0)]
            public Int32 AsInt32;
            [FieldOffset(0)]
            public UInt32 AsUInt32;
            [FieldOffset(0)]
            public Int64 AsInt64;
            [FieldOffset(0)]
            public UInt64 AsUInt64;
            [FieldOffset(0)]
            public IntPtr AsIntPtr;
            [FieldOffset(0)]
            public UIntPtr AsUIntPtr;
            [FieldOffset(0)]
            public Single AsSingle;
            [FieldOffset(0)]
            public Double AsDouble;
            [FieldOffset(0)]
            public Guid AsGuid;
            [FieldOffset(0)]
            public DateTime AsDateTime;
            [FieldOffset(0)]
            public DateTimeOffset AsDateTimeOffset;
            [FieldOffset(0)]
            public TimeSpan AsTimeSpan;
            [FieldOffset(0)]
            public Decimal AsDecimal;
        }

        // Anything not covered by the Scalar union gets stored in this reference.
        readonly object _reference;
        readonly Scalar _scalar;
        readonly int _scalarLength;

        private PropertyValue(object value)
        {
            _reference = value;
            _scalar = default(Scalar);
            _scalarLength = 0;
        }

        private PropertyValue(Scalar scalar, int scalarLength)
        {
            _reference = null;
            _scalar = scalar;
            _scalarLength = scalarLength;
        }

        private PropertyValue(Boolean value) : this(new Scalar() { AsBoolean = value }, sizeof(Boolean)) { }
        private PropertyValue(Byte value) : this(new Scalar() { AsByte = value }, sizeof(Byte)) { }
        private PropertyValue(SByte value) : this(new Scalar() { AsSByte = value }, sizeof(SByte)) { }
        private PropertyValue(Char value) : this(new Scalar() { AsChar = value }, sizeof(Char)) { }
        private PropertyValue(Int16 value) : this(new Scalar() { AsInt16 = value }, sizeof(Int16)) { }
        private PropertyValue(UInt16 value) : this(new Scalar() { AsUInt16 = value }, sizeof(UInt16)) { }
        private PropertyValue(Int32 value) : this(new Scalar() { AsInt32 = value }, sizeof(Int32)) { }
        private PropertyValue(UInt32 value) : this(new Scalar() { AsUInt32 = value }, sizeof(UInt32)) { }
        private PropertyValue(Int64 value) : this(new Scalar() { AsInt64 = value }, sizeof(Int64)) { }
        private PropertyValue(UInt64 value) : this(new Scalar() { AsUInt64 = value }, sizeof(UInt64)) { }
        private PropertyValue(IntPtr value) : this(new Scalar() { AsIntPtr = value }, sizeof(IntPtr)) { }
        private PropertyValue(UIntPtr value) : this(new Scalar() { AsUIntPtr = value }, sizeof(UIntPtr)) { }
        private PropertyValue(Single value) : this(new Scalar() { AsSingle = value }, sizeof(Single)) { }
        private PropertyValue(Double value) : this(new Scalar() { AsDouble = value }, sizeof(Double)) { }
        private PropertyValue(Guid value) : this(new Scalar() { AsGuid = value }, sizeof(Guid)) { }
        private PropertyValue(DateTime value) : this(new Scalar() { AsDateTime = value }, sizeof(DateTime)) { }
        private PropertyValue(DateTimeOffset value) : this(new Scalar() { AsDateTimeOffset = value }, sizeof(DateTimeOffset)) { }
        private PropertyValue(TimeSpan value) : this(new Scalar() { AsTimeSpan = value }, sizeof(TimeSpan)) { }
        private PropertyValue(Decimal value) : this(new Scalar() { AsDecimal = value }, sizeof(Decimal)) { }

        public static Func<object, PropertyValue> GetFactory(Type type)
        {
            if (type == typeof(Boolean)) return value => new PropertyValue((Boolean)value);
            if (type == typeof(Byte)) return value => new PropertyValue((Byte)value);
            if (type == typeof(SByte)) return value => new PropertyValue((SByte)value);
            if (type == typeof(Char)) return value => new PropertyValue((Char)value);
            if (type == typeof(Int16)) return value => new PropertyValue((Int16)value);
            if (type == typeof(UInt16)) return value => new PropertyValue((UInt16)value);
            if (type == typeof(Int32)) return value => new PropertyValue((Int32)value);
            if (type == typeof(UInt32)) return value => new PropertyValue((UInt32)value);
            if (type == typeof(Int64)) return value => new PropertyValue((Int64)value);
            if (type == typeof(UInt64)) return value => new PropertyValue((UInt64)value);
            if (type == typeof(IntPtr)) return value => new PropertyValue((IntPtr)value);
            if (type == typeof(UIntPtr)) return value => new PropertyValue((UIntPtr)value);
            if (type == typeof(Single)) return value => new PropertyValue((Single)value);
            if (type == typeof(Double)) return value => new PropertyValue((Double)value);
            if (type == typeof(Guid)) return value => new PropertyValue((Guid)value);
            if (type == typeof(DateTime)) return value => new PropertyValue((DateTime)value);
            if (type == typeof(DateTimeOffset)) return value => new PropertyValue((DateTimeOffset)value);
            if (type == typeof(TimeSpan)) return value => new PropertyValue((TimeSpan)value);
            if (type == typeof(Decimal)) return value => new PropertyValue((Decimal)value);

            return value => new PropertyValue(value);
        }


        public object ReferenceValue
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
            if (property.DeclaringType.GetTypeInfo().IsValueType)
                return GetBoxedValueTypePropertyGetter(property);
            else
                return GetReferenceTypePropertyGetter(property);
        }

        /// <summary>
        /// Gets a delegate that gets the value of a property of a value type.  We unfortunately cannot avoid boxing the value type,
        /// without making this generic over the value type.  That would result in a large number of generic instantiations, and furthermore
        /// does not work correctly on .Net Native (we cannot express the needed instantiations in an rd.xml file).  We expect that user-defined
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
        /// to build a fast getter.  We can get away with this on .Net Native, because we really only need one runtime instantiation of the
        /// generic type, since it's only instantiated over reference types (and thus all instances are shared).
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Func<PropertyValue, PropertyValue> GetReferenceTypePropertyGetter(PropertyInfo property)
        {
            var helper = (TypeHelper)Activator.CreateInstance(typeof(ReferenceTypeHelper<>).MakeGenericType(property.DeclaringType));
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
                return property.GetMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(property.DeclaringType, propertyType));
            }
        }

#if ES_BUILD_PN
        public
#else
        private
#endif
        sealed class ReferenceTypeHelper<TContainer> : TypeHelper where TContainer : class
        {
            public override Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
            {
                var type = property.PropertyType;

                if (!Statics.IsValueType(type))
                {
                    var getter = (Func<TContainer, object>)GetGetMethod(property, type);
                    return container => new PropertyValue(getter((TContainer)container.ReferenceValue));
                }
                else
                {
                    if (type.GetTypeInfo().IsEnum)
                        type = Enum.GetUnderlyingType(type);

                    if (type == typeof(Boolean)) { var f = (Func<TContainer, Boolean>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Byte)) { var f = (Func<TContainer, Byte>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(SByte)) { var f = (Func<TContainer, SByte>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Char)) { var f = (Func<TContainer, Char>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Int16)) { var f = (Func<TContainer, Int16>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(UInt16)) { var f = (Func<TContainer, UInt16>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Int32)) { var f = (Func<TContainer, Int32>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(UInt32)) { var f = (Func<TContainer, UInt32>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Int64)) { var f = (Func<TContainer, Int64>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(UInt64)) { var f = (Func<TContainer, UInt64>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(IntPtr)) { var f = (Func<TContainer, IntPtr>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(UIntPtr)) { var f = (Func<TContainer, UIntPtr>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Single)) { var f = (Func<TContainer, Single>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Double)) { var f = (Func<TContainer, Double>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Guid)) { var f = (Func<TContainer, Guid>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(DateTime)) { var f = (Func<TContainer, DateTime>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(DateTimeOffset)) { var f = (Func<TContainer, DateTimeOffset>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(TimeSpan)) { var f = (Func<TContainer, TimeSpan>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }
                    if (type == typeof(Decimal)) { var f = (Func<TContainer, Decimal>)GetGetMethod(property, type); return container => new PropertyValue(f((TContainer)container.ReferenceValue)); }

                    return container => new PropertyValue(property.GetValue(container.ReferenceValue));
                }
            }
        }
    }
}
