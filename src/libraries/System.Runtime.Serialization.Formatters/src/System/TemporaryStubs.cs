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
    }
}