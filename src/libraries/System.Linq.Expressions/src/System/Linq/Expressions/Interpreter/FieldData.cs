// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class FieldData
    {
        private readonly object? _parent;
        private readonly FieldInfo _field;

        private FieldData(object? parent, FieldInfo field)
        {
            _parent = parent;
            _field = field;
        }

        private (FieldInfo? StaticField, object Root, FieldInfo[] FieldInfos) GetFieldAccessors(int count)
        {
            switch (_parent)
            {
                case null:
                {
                    object root = _field.GetValue(null)!;
                    Assert.NotNull(root);

                    return (_field, root, []);
                }

                case FieldData { _parent: null, _field: FieldInfo staticField }:
                {
                    FieldInfo[] fieldInfos = new FieldInfo[count];

                    fieldInfos[0] = _field;

                    object root = staticField.GetValue(null)!;
                    Assert.NotNull(root);

                    return (staticField, root, fieldInfos);
                }

                case FieldData parentField:
                {
                    (FieldInfo? StaticField, object Root, FieldInfo[] FieldInfos) accessors = parentField.GetFieldAccessors(count + 1);

                    accessors.FieldInfos[^count] = _field!;

                    return accessors;
                }

                case object root:
                {
                    FieldInfo[] fieldInfos = new FieldInfo[count];

                    fieldInfos[0] = _field;

                    return (null, root, fieldInfos);
                }
            }
        }

        public object? ToObject()
        {
            (_, object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            if (fieldInfos is [])
            {
                return root;
            }
            else
            {
                var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

                return TypedReference.ToObject(typedReference);
            }
        }

        private void SetValueDirect(FieldInfo field, object? value)
        {
            (FieldInfo? staticField, object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            if (fieldInfos is [])
            {
                field.SetValue(root, value);
            }
            else
            {
                var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

                field.SetValueDirect(typedReference, value!);
            }

            staticField?.SetValue(null, root);
        }

        private object? GetValueDirect(FieldInfo field)
        {
            (_, object root, FieldInfo[] fieldInfos) = GetFieldAccessors(1);

            if (fieldInfos is [])
            {
                return field.GetValue(root);
            }
            else
            {
                var typedReference = TypedReference.MakeTypedReference(root, fieldInfos);

                return field.GetValueDirect(typedReference);
            }
        }

        public static void SetRawObjectValue(object? rawObject, FieldInfo field, object? value)
        {
            switch (rawObject, field.DeclaringType)
            {
                case (FieldData fieldData, { IsPrimitive: false, IsValueType: true }):
                    fieldData.SetValueDirect(field, value);
                    break;

                case (FieldData fieldData, _):
                    field.SetValue(fieldData.ToObject(), value);
                    break;

                default:
                    field.SetValue(rawObject, value);
                    break;
            }
        }

        public static object? GetRawObject(object? self, FieldInfo field)
        {
            return
                (self, field.FieldType) switch
                {
                    (_, { IsPrimitive: false, IsValueType: true }) => new FieldData(self, field),
                    (FieldData fieldData, _) => fieldData.GetValueDirect(field),
                    (_, _) => field.GetValue(self),
                };
        }
    }
}
