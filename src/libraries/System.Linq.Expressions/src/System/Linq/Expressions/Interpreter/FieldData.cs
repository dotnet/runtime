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

        private void SetValueDirect(FieldInfo field, object? value)
        {
            (object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

            field.SetValueDirect(typedReference, value!);
        }

        private object? GetValueDirect(FieldInfo field)
        {
            (object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

            return field.GetValueDirect(typedReference);
        }


        public static void SetValue(object self, FieldInfo field, object? value)
        {
            switch (self, field.DeclaringType)
            {
                case (FieldData fieldData, { IsPrimitive: false, IsValueType: true }):
                    fieldData.SetValueDirect(field, value);
                    break;

                case (FieldData fieldData, _):
                    field.SetValue(fieldData.ToObject(), value);
                    break;

                default:
                    field.SetValue(self, value);
                    break;
            }
        }

        public static object? GetValue(object self, FieldInfo field)
        {
            return
                (self, field.FieldType) switch
                {
                    (_, { IsPrimitive: false, IsValueType: true }) => new FieldData(self!, field),
                    (FieldData fieldData, _) => fieldData.GetValueDirect(field),
                    (_, _) => field.GetValue(self),
                };
        }
    }
}
