// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class FieldData
    {
        private readonly object _parent;
        private readonly FieldInfo _field;

        public FieldData(object parent, FieldInfo field)
        {
            _parent = parent;
            _field = field;
        }

        private (object Root, FieldInfo[] FieldInfos) GetFieldAccessors(int count)
        {
            if (_parent is FieldData parentField)
            {
                var accessors = parentField.GetFieldAccessors(count + 1);

                accessors.FieldInfos[^count] = _field;

                return accessors;
            }
            else
            {
                FieldInfo[] fieldInfos = new FieldInfo[count];

                fieldInfos[0] = _field;

                return (_parent, fieldInfos);
            }
        }

        public object? ToObject()
        {
            (object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

            return TypedReference.ToObject(typedReference);
        }

        public void SetValueDirect(FieldInfo field, object? value)
        {
            (object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

            field.SetValueDirect(typedReference, value!);
        }

        public static void SetValueDirect(object obj, FieldInfo field, object? value)
        {
            var typedReference = __makeref(obj);

            field.SetValueDirect(typedReference, value!);
        }
    }
}
