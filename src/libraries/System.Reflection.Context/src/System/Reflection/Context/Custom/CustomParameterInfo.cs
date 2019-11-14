// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Context.Projection;

namespace System.Reflection.Context.Custom
{
    internal class CustomParameterInfo : ProjectingParameterInfo
    {
        public CustomParameterInfo(ParameterInfo template, CustomReflectionContext context)
            : base(template, context.Projector)
        {
            ReflectionContext = context;
        }

        public CustomReflectionContext ReflectionContext { get; }

        // Currently only the results of GetCustomAttributes can be customizaed.
        // We don't need to override GetCustomAttributesData.
        public override object[] GetCustomAttributes(bool inherit)
        {
            return GetCustomAttributes(typeof(object), inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return AttributeUtils.GetCustomAttributes(ReflectionContext, this, attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return AttributeUtils.IsDefined(this, attributeType, inherit);
        }
    }
}
