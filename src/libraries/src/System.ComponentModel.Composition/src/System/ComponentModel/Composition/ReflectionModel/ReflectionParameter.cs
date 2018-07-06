// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionParameter : ReflectionItem
    {
        private readonly ParameterInfo _parameter;

        public ReflectionParameter(ParameterInfo parameter)
        {
           if (parameter == null)
           {
                throw new ArgumentNullException(nameof(parameter));
            }

            _parameter = parameter;
        }

        public ParameterInfo UnderlyingParameter
        {
            get { return _parameter; }
        }

        public override string Name
        {
            get { return UnderlyingParameter.Name; }
        }

        public override string GetDisplayName()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} (Parameter=\"{1}\")",  // NOLOC
                UnderlyingParameter.Member.GetDisplayName(),
                UnderlyingParameter.Name);
        }

        public override Type ReturnType
        {
            get { return UnderlyingParameter.ParameterType; }
        }

        public override ReflectionItemType ItemType
        {
            get { return ReflectionItemType.Parameter; }
        }
    }
}
