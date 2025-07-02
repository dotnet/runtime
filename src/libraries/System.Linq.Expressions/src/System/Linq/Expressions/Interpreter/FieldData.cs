// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class FieldData(object _parent, FieldInfo _field)
    {
        public FieldData(FieldInfo field) : this(Static.Instance, field) { }

        private object Parent { get; } = _parent;
        private FieldInfo Field { get; } = _field;

        private sealed class Static
        {
            private Static() {}
            public static Static Instance { get; } = new Static();
        }

        private (FieldInfo? StaticField, object Root, FieldInfo[] FieldInfos) GetFieldAccessors(int count)
        {
            if (Parent is Static)
            {
                object root = Field.GetValue(null)!;
                Assert.NotNull(root);

                return (Field, root, []);
            }
            if (Parent is FieldData { Parent: Static, Field: FieldInfo staticField })
            {
                FieldInfo[] fieldInfos = new FieldInfo[count];

                fieldInfos[0] = Field;

                object root = staticField.GetValue(null)!;
                Assert.NotNull(root);

                return (staticField, root, fieldInfos);
            }
            else if (Parent is not FieldData parentField)
            {
                FieldInfo[] fieldInfos = new FieldInfo[count];

                fieldInfos[0] = Field;

                return (null, Parent, fieldInfos);
            }
            else
            {
                var accessors = parentField.GetFieldAccessors(count + 1);

                accessors.FieldInfos[^count] = Field!;

                return accessors;
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

        public static void SetValue(object? self, FieldInfo field, object? value)
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

        public static object? GetValue(object? self, FieldInfo field)
        {
            return
                (self, field.FieldType) switch
                {
                    (null, { IsPrimitive: false, IsValueType: true }) => new FieldData(field),
                    (_, { IsPrimitive: false, IsValueType: true }) => new FieldData(self, field),
                    (FieldData fieldData, _) => fieldData.GetValueDirect(field),
                    (_, _) => field.GetValue(self),
                };
        }
    }
}
