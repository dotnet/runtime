// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Composition.Convention;

namespace System.Composition.TypedParts.ActivationFeatures
{
    /// <summary>
    /// Modifies activators of parts with property imports so that the properties
    /// are set appropriately.
    /// </summary>
    internal sealed class PropertyInjectionFeature : ActivationFeature
    {
        private readonly AttributedModelProvider _attributeContext;
        private static readonly MethodInfo s_activatorInvokeMethod = typeof(CompositeActivator).GetTypeInfo().GetDeclaredMethod("Invoke");

        public PropertyInjectionFeature(AttributedModelProvider attributeContext)
        {
            _attributeContext = attributeContext;
        }

        public override IEnumerable<CompositionDependency> GetDependencies(TypeInfo partType, DependencyAccessor definitionAccessor)
        {
            var partTypeAsType = partType.AsType();
            var imports = (from pi in partTypeAsType.GetRuntimeProperties()
                               .Where(pi => pi.CanWrite && pi.SetMethod.IsPublic && !(pi.SetMethod.IsStatic))
                           let attrs = _attributeContext.GetDeclaredAttributes(pi.DeclaringType, pi).ToArray()
                           let site = new PropertyImportSite(pi)
                           where attrs.Any(a => a is ImportAttribute || a is ImportManyAttribute)
                           select new { Site = site, ImportInfo = ContractHelpers.GetImportInfo(pi.PropertyType, attrs, site) }).ToArray();

            if (imports.Length == 0)
                return NoDependencies;

            var result = new List<CompositionDependency>();

            foreach (var i in imports)
            {
                if (!i.ImportInfo.AllowDefault)
                {
                    result.Add(definitionAccessor.ResolveRequiredDependency(i.Site, i.ImportInfo.Contract, false));
                }
                else
                {
                    CompositionDependency optional;
                    if (definitionAccessor.TryResolveOptionalDependency(i.Site, i.ImportInfo.Contract, false, out optional))
                        result.Add(optional);
                    // Variation from CompositionContainer behaviour: we don't have to support recomposition
                    // so we don't require that defaultable imports be set to null.
                }
            }

            return result;
        }

        public override CompositeActivator RewriteActivator(
            TypeInfo partType,
            CompositeActivator activator,
            IDictionary<string, object> partMetadata,
            IEnumerable<CompositionDependency> dependencies)
        {
            var propertyDependencies = dependencies
                .Where(dep => dep.Site is PropertyImportSite)
                .ToDictionary(d => ((PropertyImportSite)d.Site).Property);

            if (propertyDependencies.Count == 0)
                return activator;

            var lc = Expression.Parameter(typeof(LifetimeContext));
            var op = Expression.Parameter(typeof(CompositionOperation));
            var inst = Expression.Parameter(typeof(object));
            var typed = Expression.Variable(partType.AsType());

            var statements = new List<Expression>();
            var assignTyped = Expression.Assign(typed, Expression.Convert(inst, partType.AsType()));
            statements.Add(assignTyped);

            foreach (var d in propertyDependencies)
            {
                var property = d.Key;

                var assignment = Expression.Assign(
                    Expression.MakeMemberAccess(typed, property),
                    Expression.Convert(
                        Expression.Call(
                            Expression.Constant(d.Value.Target.GetDescriptor().Activator),
                            s_activatorInvokeMethod,
                            lc,
                            op),
                        property.PropertyType));

                statements.Add(assignment);
            }

            statements.Add(inst);

            var setAll = Expression.Block(new[] { typed }, statements);
            var setAction = Expression.Lambda<Func<object, LifetimeContext, CompositionOperation, object>>(
                setAll, inst, lc, op).Compile();

            return (c, o) =>
            {
                var i = activator(c, o);
                o.AddNonPrerequisiteAction(() => setAction(i, c, o));
                return i;
            };
        }
    }
}
