// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Composition.TypedParts.ActivationFeatures
{
    /// <summary>
    /// Modifies activators of parts that have <see cref="OnImportsSatisfiedAttribute"/> so that
    /// their [OnImportsSatisfied] method is correctly called.
    /// </summary>
    internal sealed class OnImportsSatisfiedFeature : ActivationFeature
    {
        private readonly AttributedModelProvider _attributeContext;

        public OnImportsSatisfiedFeature(AttributedModelProvider attributeContext)
        {
            if (attributeContext == null) throw new ArgumentNullException(nameof(attributeContext));
            _attributeContext = attributeContext;
        }

        public override CompositeActivator RewriteActivator(
            TypeInfo partType,
            CompositeActivator activator,
            IDictionary<string, object> partMetadata,
            IEnumerable<CompositionDependency> dependencies)
        {
            var result = activator;

            var partTypeAsType = partType.AsType();
            var importsSatisfiedMethods = partTypeAsType.GetRuntimeMethods()
                .Where(mi => _attributeContext.GetDeclaredAttribute<OnImportsSatisfiedAttribute>(mi.DeclaringType, mi) != null);

            foreach (var m in importsSatisfiedMethods)
            {
                if (!(m.IsPublic || m.IsAssembly) | m.IsStatic || m.ReturnType != typeof(void) ||
                    m.IsGenericMethodDefinition || m.GetParameters().Length != 0)
                {
                    string message = SR.Format(SR.OnImportsSatisfiedFeature_AttributeError, partType, m.Name);
                    throw new CompositionFailedException(message);
                }

                var ois = Expression.Parameter(typeof(object), "ois");
                var call = Expression.Lambda<Action<object>>(
                    Expression.Call(Expression.Convert(ois, partType.AsType()), m), ois).Compile();

                var prev = result;
                result = (c, o) =>
                {
                    var psn = prev(c, o);
                    o.AddPostCompositionAction(() => call(psn));
                    return psn;
                };
            }

            return result;
        }
    }
}
