// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System
{
    internal sealed class TypedReference // TODO: Replace with System.TypedReference when available and functional
    {
        internal object _target;
        internal FieldInfo[] _fields;

        internal static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (flds == null)
            {
                throw new ArgumentNullException(nameof(flds));
            }
            if (flds.Length == 0)
            {
                throw new ArgumentException(SR.Arg_ArrayZeroError);
            }

            return new TypedReference { _target = target, _fields = flds };
        }

        internal static void SetTypedReference(object target, object value)
        {
            throw new NotSupportedException();
        }
    }
}

namespace System.Reflection
{
    internal static class CustomReflectionExtensions
    {
        // TODO: Replace with FieldInfo.SetValueDirect when available and functional
        internal static void SetValueDirect(this FieldInfo field, TypedReference obj, object value) // TODO: Replace with FieldInfo.SetValueDirect when available
        {
            if (field == null)
            {
                throw new NullReferenceException();
            }
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            // This is extremely inefficient, but without runtime support, we can't do much better.

            object[] values = new object[obj._fields.Length + 1];

            values[0] = obj._target;
            for (int i = 0; i < obj._fields.Length; i++)
            {
                values[i + 1] = obj._fields[i].GetValue(values[i]);
            }

            field.SetValue(values[values.Length - 1], value);

            for (int i = values.Length - 2; i >= 0; i--)
            {
                obj._fields[i].SetValue(values[i], values[i + 1]);
            }
        }

        internal static TypeCode GetTypeCode(this Type type) // TODO: Replace with Type.TypeCode when it's available
        {
            if (type == null) return TypeCode.Empty;
            else if (type == typeof(bool)) return TypeCode.Boolean;
            else if (type == typeof(char)) return TypeCode.Char;
            else if (type == typeof(sbyte)) return TypeCode.SByte;
            else if (type == typeof(byte)) return TypeCode.Byte;
            else if (type == typeof(short)) return TypeCode.Int16;
            else if (type == typeof(ushort)) return TypeCode.UInt16;
            else if (type == typeof(int)) return TypeCode.Int32;
            else if (type == typeof(uint)) return TypeCode.UInt32;
            else if (type == typeof(long)) return TypeCode.Int64;
            else if (type == typeof(ulong)) return TypeCode.UInt64;
            else if (type == typeof(float)) return TypeCode.Single;
            else if (type == typeof(double)) return TypeCode.Double;
            else if (type == typeof(decimal)) return TypeCode.Decimal;
            else if (type == typeof(System.DateTime)) return TypeCode.DateTime;
            else if (type == typeof(string)) return TypeCode.String;
            else if (type.GetTypeInfo().IsEnum) return GetTypeCode(Enum.GetUnderlyingType(type));
            return TypeCode.Object;
        }
    }
}

namespace System.Runtime.Remoting.Messaging // TODO: Use the implementation from remoting stubs when available
{
    public delegate object HeaderHandler(Header[] headers);

    [Serializable]
    public class Header
    {
        public string HeaderNamespace;
        public bool MustUnderstand;
        public string Name;
        public object Value;

        public Header(string _Name, object _Value) : this(_Name, _Value, true)
        {
        }

        public Header(string _Name, object _Value, bool _MustUnderstand)
        {
            Name = _Name;
            Value = _Value;
            MustUnderstand = _MustUnderstand;
        }

        public Header(string _Name, object _Value, bool _MustUnderstand, string _HeaderNamespace)
        {
            Name = _Name;
            Value = _Value;
            MustUnderstand = _MustUnderstand;
            HeaderNamespace = _HeaderNamespace;
        }
    }
}
