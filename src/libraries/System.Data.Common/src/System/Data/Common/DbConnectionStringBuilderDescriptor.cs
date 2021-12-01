// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Data.Common
{
    internal sealed class DbConnectionStringBuilderDescriptor : PropertyDescriptor
    {
        internal DbConnectionStringBuilderDescriptor(string propertyName, Type componentType, Type propertyType, bool isReadOnly, Attribute[] attributes) : base(propertyName, attributes)
        {
            ComponentType = componentType;
            PropertyType = propertyType;
            IsReadOnly = isReadOnly;
        }

        internal bool RefreshOnChange { get; set; }
        public override Type ComponentType { get; }
        public override bool IsReadOnly { get; }
        public override Type PropertyType { get; }

        public override bool CanResetValue(object component)
        {
            DbConnectionStringBuilder? builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }

        public override object? GetValue(object? component)
        {
            DbConnectionStringBuilder? builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                object? value;
                if (builder.TryGetValue(DisplayName, out value))
                {
                    return value;
                }
            }
            return null;
        }

        public override void ResetValue(object component)
        {
            DbConnectionStringBuilder? builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                builder.Remove(DisplayName);

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }

        public override void SetValue(object? component, object? value)
        {
            DbConnectionStringBuilder? builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                // via the editor, empty string does a defacto Reset
                if ((typeof(string) == PropertyType) && string.Empty.Equals(value))
                {
                    value = null;
                }
                builder[DisplayName] = value;

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }

        public override bool ShouldSerializeValue(object component)
        {
            DbConnectionStringBuilder? builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }
    }
}
