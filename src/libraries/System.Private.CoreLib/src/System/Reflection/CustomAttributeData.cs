// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    public partial class CustomAttributeData
    {
        #region Public Static Members
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Module target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            return target.GetCustomAttributesData();
        }
        #endregion

        #region Object Override
        public override string ToString()
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);

            vsb.Append('[');
            vsb.Append(Constructor.DeclaringType!.FullName);
            vsb.Append('(');

            bool first = true;

            int count = ConstructorArguments.Count;
            for (int i = 0; i < count; i++)
            {
                if (!first) vsb.Append(", ");
                vsb.Append(ConstructorArguments[i].ToString());
                first = false;
            }

            count = NamedArguments.Count;
            for (int i = 0; i < count; i++)
            {
                if (!first) vsb.Append(", ");
                vsb.Append(NamedArguments[i].ToString());
                first = false;
            }

            vsb.Append(")]");

            return vsb.ToString();
        }
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object? obj) => obj == (object)this;
        #endregion

        #region Public Members
        public virtual Type AttributeType => Constructor.DeclaringType!;
        #endregion
    }
}
