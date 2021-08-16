// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection
{
    public class CustomAttributeData
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

        protected CustomAttributeData()
        {
        }

        #region Object Override
        public override string ToString()
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);

            vsb.Append('[');
            vsb.Append(Constructor.DeclaringType!.FullName);
            vsb.Append('(');

            bool first = true;

            IList<CustomAttributeTypedArgument> constructorArguments = ConstructorArguments;
            int constructorArgumentsCount = constructorArguments.Count;
            for (int i = 0; i < constructorArgumentsCount; i++)
            {
                if (!first) vsb.Append(", ");
                vsb.Append(constructorArguments[i].ToString());
                first = false;
            }

            IList<CustomAttributeNamedArgument> namedArguments = NamedArguments;
            int namedArgumentsCount = namedArguments.Count;
            for (int i = 0; i < namedArgumentsCount; i++)
            {
                if (!first) vsb.Append(", ");
                vsb.Append(namedArguments[i].ToString());
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

        // Expected to be overriden
        public virtual ConstructorInfo Constructor => null!;
        public virtual IList<CustomAttributeTypedArgument> ConstructorArguments => null!;
        public virtual IList<CustomAttributeNamedArgument> NamedArguments => null!;
        #endregion
    }
}
