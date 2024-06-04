// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed partial class FieldOnTypeBuilderInstantiation : FieldInfo
    {
        #region Private Static Members
        internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
        {
            FieldInfo m;

            if (type._hashtable.Contains(Field))
            {
                m = (type._hashtable[Field] as FieldInfo)!;
            }
            else
            {
                m = new FieldOnTypeBuilderInstantiation(Field, type);
                type._hashtable[Field] = m;
            }

            return m;
        }
        #endregion

        #region Private Data Members
        private FieldInfo _field;
        private TypeBuilderInstantiation _type;
        #endregion

        #region Constructor
        internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
        {
            _field = field;
            _type = type;
        }
        #endregion

        internal FieldInfo FieldInfo => _field;

        #region MemberInfo Overrides
        public override MemberTypes MemberType => MemberTypes.Field;
        public override string Name => _field.Name;
        public override Type? DeclaringType => _type;
        public override Type? ReflectedType => _type;
        public override object[] GetCustomAttributes(bool inherit) { return _field.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return _field.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return _field.IsDefined(attributeType, inherit); }
        public override int MetadataToken => _field.MetadataToken;
        public override Module Module => _field.Module;
        #endregion

        #region Public Abstract\Virtual Members
        public override Type[] GetRequiredCustomModifiers() { return _field.GetRequiredCustomModifiers(); }
        public override Type[] GetOptionalCustomModifiers() { return _field.GetOptionalCustomModifiers(); }
        public override void SetValueDirect(TypedReference obj, object value)
        {
            throw new NotImplementedException();
        }
        public override object GetValueDirect(TypedReference obj)
        {
            throw new NotImplementedException();
        }
        public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();
        public override Type FieldType => _field.FieldType;
        public override object GetValue(object? obj) { throw new InvalidOperationException(); }
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture) { throw new InvalidOperationException(); }
        public override FieldAttributes Attributes => _field.Attributes;
        #endregion

    }
}
