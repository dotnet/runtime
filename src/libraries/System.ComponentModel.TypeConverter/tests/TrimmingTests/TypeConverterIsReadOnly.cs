// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;

/// <summary>
/// Tests that SimplePropertyDescriptor.IsReadOnly works correctly when trimming.
/// </summary>
class Program
{
    static int Main()
    {
        PropertyDescriptor property = MyTypeConverter.CreatePropertyDescriptor(isReadOnly: false);
        if (property.IsReadOnly)
        {
            return -1;
        }

        property = MyTypeConverter.CreatePropertyDescriptor(isReadOnly: true);
        if (!property.IsReadOnly)
        {
            return -2;
        }

        Type readOnlyAttributeType = property.Attributes[0].GetType();
        if (readOnlyAttributeType.Name != "ReadOnlyAttribute")
        {
            return -3;
        }

        // check to make sure the 'ReadOnlyAttribute.Default' static field is preserved
        if (readOnlyAttributeType.GetField("Default") == null)
        {
            return -4;
        }

        return 100;
    }
}

internal class MyTypeConverter : TypeConverter
{
    protected class MyPropertyDescriptor : SimplePropertyDescriptor
    {
        private readonly bool _isReadOnly;

        public MyPropertyDescriptor(bool isReadOnly) : base(typeof(Program), "property", typeof(int))
        {
            _isReadOnly = isReadOnly;
        }

        protected override void FillAttributes(IList attributeList)
        {
            if (_isReadOnly)
            {
                attributeList.Add(ReadOnlyAttribute.Yes);
            }

            base.FillAttributes(attributeList);
        }

        public override object GetValue(object component) => null;
        public override void SetValue(object component, object value) { }
    }

    public static PropertyDescriptor CreatePropertyDescriptor(bool isReadOnly)
    {
        return new MyPropertyDescriptor(isReadOnly);
    }
}
