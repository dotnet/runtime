// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// The older-style CustomAttribute-related members on the various Reflection types. The implementation dependency
// stack on .Net Native differs from that of CoreClr due to the difference in development history.
//
// - IEnumerable<CustomAttributeData> xInfo.get_CustomAttributes is at the very bottom of the dependency stack.
//
// - CustomAttributeExtensions layers on top of that (primarily because it's the one with the nice generic methods.)
//
// - Everything else is a thin layer over one of these two.
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.LowLevelLinq;
using Internal.Reflection.Extensions.NonPortable;


namespace System.Reflection.Runtime.Assemblies
{
    internal partial class RuntimeAssemblyInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this).ToArray();  // inherit is meaningless for Assemblies

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, skipTypeValidation: true); // inherit is meaningless for Assemblies
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, skipTypeValidation: true); // inherit is meaningless for Assemblies
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.MethodInfos
{
    internal abstract partial class RuntimeConstructorInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit).ToArray();

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.EventInfos
{
    internal abstract partial class RuntimeEventInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit: false).ToArray();  // Desktop compat: for events, this form of the api ignores "inherit"

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for events, this form of the api ignores "inherit"
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for events, this form of the api ignores "inherit"
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.FieldInfos
{
    internal abstract partial class RuntimeFieldInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit).ToArray();

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.MethodInfos
{
    internal abstract partial class RuntimeMethodInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit).ToArray();

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.Modules
{
    internal abstract partial class RuntimeModule
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this).ToArray();  // inherit is meaningless for Modules

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, skipTypeValidation: true); // inherit is meaningless for Modules
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, skipTypeValidation: true); // inherit is meaningless for Modules
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.ParameterInfos
{
    internal abstract partial class RuntimeParameterInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit: false).ToArray(); // Desktop compat: for parameters, this form of the api ignores "inherit"

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for parameters, this form of the api ignores "inherit"
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for parameters, this form of the api ignores "inherit"
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.PropertyInfos
{
    internal abstract partial class RuntimePropertyInfo
    {
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this, inherit: false).ToArray(); // Desktop compat: for properties, this form of the api ignores "inherit"

        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for properties, this form of the api ignores "inherit"
            return cads.InstantiateAsArray(attributeType);
        }

        public sealed override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.GetMatchingCustomAttributes(attributeType, inherit: false, skipTypeValidation: true); // Desktop compat: for properties, this form of the api ignores "inherit"
            return cads.Any();
        }
    }
}

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        public IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public object[] GetCustomAttributes(bool inherit) => CustomAttributeExtensions.GetCustomAttributes(this.ToType(), inherit).ToArray();

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.ToType().GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.InstantiateAsArray(attributeType);
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            IEnumerable<CustomAttributeData> cads = this.ToType().GetMatchingCustomAttributes(attributeType, inherit: inherit, skipTypeValidation: true);
            return cads.Any();
        }
    }
}
