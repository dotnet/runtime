// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    internal sealed class RuntimeParameterInfo : ParameterInfo
    {
        internal MarshalAsAttribute? marshalAs;

        // Called by the runtime
        internal RuntimeParameterInfo(string name, Type type, int position, int attrs, object defaultValue, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            NameImpl = name;
            ClassImpl = type;
            PositionImpl = position;
            AttrsImpl = (ParameterAttributes)attrs;
            DefaultValueImpl = defaultValue;
            MemberImpl = member;
            this.marshalAs = marshalAs;
        }

        internal static void FormatParameters(StringBuilder sb, ParameterInfo[] p, CallingConventions callingConvention)
        {
            for (int i = 0; i < p.Length; ++i)
            {
                if (i > 0)
                    sb.Append(", ");

                Type t = p[i].ParameterType;

                string typeName = t.FormatTypeName();

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (t.IsByRef)
                {
                    sb.Append(typeName.TrimEnd(new char[] { '&' }));
                    sb.Append(" ByRef");
                }
                else
                {
                    sb.Append(typeName);
                }
            }

            if ((callingConvention & CallingConventions.VarArgs) != 0)
            {
                if (p.Length > 0)
                    sb.Append(", ");
                sb.Append("...");
            }
        }

        internal RuntimeParameterInfo(ParameterBuilder? pb, Type? type, MemberInfo member, int position)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            if (pb != null)
            {
                this.NameImpl = pb.Name;
                this.PositionImpl = pb.Position - 1;    // ParameterInfo.Position is zero-based
                this.AttrsImpl = (ParameterAttributes)pb.Attributes;
            }
            else
            {
                this.NameImpl = null;
                this.PositionImpl = position - 1;
                this.AttrsImpl = ParameterAttributes.None;
            }
        }

        internal static ParameterInfo New(ParameterBuilder? pb, Type? type, MemberInfo member, int position)
        {
            return new RuntimeParameterInfo(pb, type, member, position);
        }

        /*FIXME this constructor looks very broken in the position parameter*/
        internal RuntimeParameterInfo(ParameterInfo? pinfo, Type? type, MemberInfo member, int position)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            if (pinfo != null)
            {
                this.NameImpl = pinfo.Name;
                this.PositionImpl = pinfo.Position - 1; // ParameterInfo.Position is zero-based
                this.AttrsImpl = (ParameterAttributes)pinfo.Attributes;
            }
            else
            {
                this.NameImpl = null;
                this.PositionImpl = position - 1;
                this.AttrsImpl = ParameterAttributes.None;
            }
        }

        internal RuntimeParameterInfo(ParameterInfo pinfo, MemberInfo member)
        {
            this.ClassImpl = pinfo.ParameterType;
            this.MemberImpl = member;
            this.NameImpl = pinfo.Name;
            this.PositionImpl = pinfo.Position;
            this.AttrsImpl = pinfo.Attributes;
            this.DefaultValueImpl = GetDefaultValueImpl(pinfo);
        }

        /* to build a ParameterInfo for the return type of a method */
        internal RuntimeParameterInfo(Type type, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            this.NameImpl = null;
            this.PositionImpl = -1; // since parameter positions are zero-based, return type pos is -1
            this.AttrsImpl = ParameterAttributes.Retval;
            this.marshalAs = marshalAs;
        }

        public override
        object? DefaultValue
        {
            get
            {
                if (ClassImpl == typeof(decimal) || ClassImpl == typeof(decimal?))
                {
                    /* default values for decimals are encoded using a custom attribute */
                    DecimalConstantAttribute[] attrs = (DecimalConstantAttribute[])GetCustomAttributes(typeof(DecimalConstantAttribute), false);
                    if (attrs.Length > 0)
                        return attrs[0].Value;
                }
                else if (ClassImpl == typeof(DateTime) || ClassImpl == typeof(DateTime?))
                {
                    /* default values for DateTime are encoded using a custom attribute */
                    DateTimeConstantAttribute[] attrs = (DateTimeConstantAttribute[])GetCustomAttributes(typeof(DateTimeConstantAttribute), false);
                    if (attrs.Length > 0)
                        return attrs[0].Value;
                }
                return DefaultValueImpl;
            }
        }

        public override
        object? RawDefaultValue
        {
            get
            {
                if (DefaultValue != null && DefaultValue.GetType().IsEnum)
                    return ((Enum)DefaultValue).GetValue();
                /*FIXME right now DefaultValue doesn't throw for reflection-only assemblies. Change this once the former is fixed.*/
                return DefaultValue;
            }
        }

        public
        override
        int MetadataToken
        {
            get
            {
                if (MemberImpl is PropertyInfo prop)
                {
                    MethodInfo mi = prop.GetGetMethod(true) ?? prop.GetSetMethod(true)!;

                    return mi.GetParametersInternal()[PositionImpl].MetadataToken;
                }
                else if (MemberImpl is MethodBase)
                {
                    return GetMetadataToken();
                }
                throw new ArgumentException("Can't produce MetadataToken for member of type " + MemberImpl.GetType());
            }
        }


        public
        override
        object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, inherit);
        }

        public
        override
        object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
        }

        internal static object? GetDefaultValueImpl(ParameterInfo pinfo)
        {
            FieldInfo field = typeof(ParameterInfo).GetField("DefaultValueImpl", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return field.GetValue(pinfo);
        }

        public
        override
        bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributes(this);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern int GetMetadataToken();

        public override Type[] GetOptionalCustomModifiers() => GetCustomModifiers(true);

        internal object[]? GetPseudoCustomAttributes()
        {
            int count = 0;

            if (IsIn)
                count++;
            if (IsOut)
                count++;
            if (IsOptional)
                count++;
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            object[] attrs = new object[count];
            count = 0;

            if (IsIn)
                attrs[count++] = new InAttribute();
            if (IsOut)
                attrs[count++] = new OutAttribute();
            if (IsOptional)
                attrs[count++] = new OptionalAttribute();

            if (marshalAs != null)
            {
                attrs[count++] = (MarshalAsAttribute)marshalAs.CloneInternal();
            }

            return attrs;
        }

        internal CustomAttributeData[]? GetPseudoCustomAttributesData()
        {
            int count = 0;

            if (IsIn)
                count++;
            if (IsOut)
                count++;
            if (IsOptional)
                count++;
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            CustomAttributeData[] attrsData = new CustomAttributeData[count];
            count = 0;

            if (IsIn)
                attrsData[count++] = new CustomAttributeData((typeof(InAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (IsOut)
                attrsData[count++] = new CustomAttributeData((typeof(OutAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (IsOptional)
                attrsData[count++] = new CustomAttributeData((typeof(OptionalAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (marshalAs != null)
            {
                var ctorArgs = new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(UnmanagedType), marshalAs.Value) };
                attrsData[count++] = new CustomAttributeData(
                    (typeof(MarshalAsAttribute)).GetConstructor(new[] { typeof(UnmanagedType) })!,
                    ctorArgs,
                    Array.Empty<CustomAttributeNamedArgument>());//FIXME Get named params
            }

            return attrsData;
        }

        public override Type[] GetRequiredCustomModifiers() => GetCustomModifiers(false);

        public override bool HasDefaultValue
        {
            get
            {
                object? defaultValue = DefaultValue;
                if (defaultValue == null)
                    return true;

                if (defaultValue.GetType() == typeof(DBNull) || defaultValue.GetType() == typeof(Missing))
                    return false;

                return true;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type[] GetTypeModifiers(Type type, MemberInfo member, int position, bool optional);

        internal static ParameterInfo New(ParameterInfo pinfo, Type? type, MemberInfo member, int position)
        {
            return new RuntimeParameterInfo(pinfo, type, member, position);
        }

        internal static ParameterInfo New(ParameterInfo pinfo, MemberInfo member)
        {
            return new RuntimeParameterInfo(pinfo, member);
        }

        internal static ParameterInfo New(Type type, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            return new RuntimeParameterInfo(type, member, marshalAs);
        }

        private Type[] GetCustomModifiers(bool optional) => GetTypeModifiers(ParameterType, Member, Position, optional) ?? Type.EmptyTypes;
    }
}
