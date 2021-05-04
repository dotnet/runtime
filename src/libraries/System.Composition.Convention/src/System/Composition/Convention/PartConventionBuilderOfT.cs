// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Configures a type as a MEF part.
    /// </summary>
    /// <typeparam name="T">The type of the part, or a type to which the part is assignable.</typeparam>
    public class PartConventionBuilder<T> : PartConventionBuilder
    {
        private sealed class MethodExpressionAdapter
        {
            private readonly MethodInfo _methodInfo;

            public MethodExpressionAdapter(Expression<Action<T>> methodSelector)
            {
                _methodInfo = SelectMethods(methodSelector);
            }

            public bool VerifyMethodInfo(MethodInfo mi)
            {
                return mi == _methodInfo;
            }

            private static MethodInfo SelectMethods(Expression<Action<T>> methodSelector)
            {
                if (methodSelector == null)
                {
                    throw new ArgumentNullException(nameof(methodSelector));
                }

                Expression expr = Reduce(methodSelector).Body;
                if (expr.NodeType == ExpressionType.Call)
                {
                    var memberInfo = ((MethodCallExpression)expr).Method as MethodInfo;
                    if (memberInfo != null)
                    {
                        return memberInfo;
                    }
                }

                // An error occurred the expression must be a void Method() Member Expression
                throw new ArgumentException(SR.Format(SR.Argument_ExpressionMustBeVoidMethodWithNoArguments, nameof(methodSelector)), nameof(methodSelector));
            }

            private static Expression<Func<T, object>> Reduce(Expression<Func<T, object>> expr)
            {
                while (expr.CanReduce)
                {
                    expr = (Expression<Func<T, object>>)expr.Reduce();
                }
                return expr;
            }

            private static Expression<Action<T>> Reduce(Expression<Action<T>> expr)
            {
                while (expr.CanReduce)
                {
                    expr = (Expression<Action<T>>)expr.Reduce();
                }
                return expr;
            }
        }

        private sealed class PropertyExpressionAdapter
        {
            private readonly PropertyInfo _propertyInfo;
            private readonly Action<ImportConventionBuilder> _configureImport;
            private readonly Action<ExportConventionBuilder> _configureExport;

            public PropertyExpressionAdapter(
                Expression<Func<T, object>> propertySelector,
                Action<ImportConventionBuilder> configureImport = null,
                Action<ExportConventionBuilder> configureExport = null)
            {
                _propertyInfo = SelectProperties(propertySelector);
                _configureImport = configureImport;
                _configureExport = configureExport;
            }

            public bool VerifyPropertyInfo(PropertyInfo pi)
            {
                return pi == _propertyInfo;
            }

            public void ConfigureImport(PropertyInfo propertyInfo, ImportConventionBuilder importBuilder)
            {
                _configureImport?.Invoke(importBuilder);
            }

            public void ConfigureExport(PropertyInfo propertyInfo, ExportConventionBuilder exportBuilder)
            {
                _configureExport?.Invoke(exportBuilder);
            }

            private static PropertyInfo SelectProperties(Expression<Func<T, object>> propertySelector)
            {
                if (propertySelector == null)
                {
                    throw new ArgumentNullException(nameof(propertySelector));
                }

                Expression expr = Reduce(propertySelector).Body;
                if (expr.NodeType == ExpressionType.MemberAccess)
                {
                    var memberInfo = ((MemberExpression)expr).Member as PropertyInfo;
                    if (memberInfo != null)
                    {
                        return memberInfo;
                    }
                }

                // An error occurred the expression must be a Property Member Expression
                throw new ArgumentException(SR.Format(SR.Argument_ExpressionMustBePropertyMember, nameof(propertySelector)), nameof(propertySelector));
            }

            private static Expression<Func<T, object>> Reduce(Expression<Func<T, object>> expr)
            {
                while (expr.CanReduce)
                {
                    expr = (Expression<Func<T, object>>)expr.Reduce();
                }
                return expr;
            }
        }

        private sealed class ConstructorExpressionAdapter
        {
            private ConstructorInfo _constructorInfo;
            private Dictionary<ParameterInfo, Action<ImportConventionBuilder>> _importBuilders;

            public ConstructorExpressionAdapter(Expression<Func<ParameterImportConventionBuilder, T>> selectConstructor)
            {
                ParseSelectConstructor(selectConstructor);
            }

            public ConstructorInfo SelectConstructor(IEnumerable<ConstructorInfo> constructorInfos)
            {
                return _constructorInfo;
            }

            public void ConfigureConstructorImports(ParameterInfo parameterInfo, ImportConventionBuilder importBuilder)
            {
                if (_importBuilders != null)
                {
                    if (_importBuilders.TryGetValue(parameterInfo, out Action<ImportConventionBuilder> parameterImportBuilder))
                    {
                        parameterImportBuilder(importBuilder);
                    }
                }

                return;
            }

            private void ParseSelectConstructor(Expression<Func<ParameterImportConventionBuilder, T>> constructorSelector)
            {
                if (constructorSelector == null)
                {
                    throw new ArgumentNullException(nameof(constructorSelector));
                }

                Expression expr = Reduce(constructorSelector).Body;
                if (expr.NodeType != ExpressionType.New)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_ExpressionMustBeNew, nameof(constructorSelector)), nameof(constructorSelector));
                }
                var newExpression = (NewExpression)expr;
                _constructorInfo = newExpression.Constructor;

                int index = 0;
                ParameterInfo[] parameterInfos = _constructorInfo.GetParameters();

                foreach (Expression argument in newExpression.Arguments)
                {
                    if (argument.NodeType == ExpressionType.Call)
                    {
                        var methodCallExpression = (MethodCallExpression)argument;
                        if (methodCallExpression.Arguments.Count == 1)
                        {
                            Expression parameter = methodCallExpression.Arguments[0];
                            if (parameter.NodeType == ExpressionType.Lambda)
                            {
                                var lambdaExpression = (LambdaExpression)parameter;
                                Delegate importDelegate = lambdaExpression.Compile();
                                if (_importBuilders == null)
                                {
                                    _importBuilders = new Dictionary<ParameterInfo, Action<ImportConventionBuilder>>();
                                }
                                _importBuilders.Add(parameterInfos[index], (Action<ImportConventionBuilder>)importDelegate);
                                ++index;
                            }
                        }
                    }
                }
            }

            private static Expression<Func<ParameterImportConventionBuilder, T>> Reduce(Expression<Func<ParameterImportConventionBuilder, T>> expr)
            {
                while (expr.CanReduce)
                {
                    expr.Reduce();
                }
                return expr;
            }
        }

        internal PartConventionBuilder(Predicate<Type> selectType) : base(selectType)
        {
        }

        /// <summary>
        /// Select which of the available constructors will be used to instantiate the part.
        /// </summary>
        /// <param name="constructorSelector">Expression that selects a single constructor.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> SelectConstructor(Expression<Func<ParameterImportConventionBuilder, T>> constructorSelector)
        {
            if (constructorSelector == null)
            {
                throw new ArgumentNullException(nameof(constructorSelector));
            }

            var adapter = new ConstructorExpressionAdapter(constructorSelector);
            base.SelectConstructor(adapter.SelectConstructor, adapter.ConfigureConstructorImports);
            return this;
        }

        /// <summary>
        /// Select a property on the part to export.
        /// </summary>
        /// <param name="propertySelector">Expression that selects the exported property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ExportProperty(Expression<Func<T, object>> propertySelector)
        {
            return ExportProperty(propertySelector, null);
        }

        /// <summary>
        /// Select a property on the part to export.
        /// </summary>
        /// <param name="propertySelector">Expression that selects the exported property.</param>
        /// <param name="exportConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ExportProperty(
            Expression<Func<T, object>> propertySelector,
            Action<ExportConventionBuilder> exportConfiguration)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            var adapter = new PropertyExpressionAdapter(propertySelector, null, exportConfiguration);
            base.ExportProperties(adapter.VerifyPropertyInfo, adapter.ConfigureExport);
            return this;
        }


        /// <summary>
        /// Select a property to export from the part.
        /// </summary>
        /// <typeparam name="TContract">Contract type to export.</typeparam>
        /// <param name="propertySelector">Expression to select the matching property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ExportProperty<TContract>(Expression<Func<T, object>> propertySelector)
        {
            return ExportProperty<TContract>(propertySelector, null);
        }

        /// <summary>
        /// Select a property to export from the part.
        /// </summary>
        /// <typeparam name="TContract">Contract type to export.</typeparam>
        /// <param name="propertySelector">Expression to select the matching property.</param>
        /// <param name="exportConfiguration">Action to configure selected properties.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ExportProperty<TContract>(
            Expression<Func<T, object>> propertySelector,
            Action<ExportConventionBuilder> exportConfiguration)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            var adapter = new PropertyExpressionAdapter(propertySelector, null, exportConfiguration);
            base.ExportProperties<TContract>(adapter.VerifyPropertyInfo, adapter.ConfigureExport);
            return this;
        }

        /// <summary>
        /// Select a property on the part to import.
        /// </summary>
        /// <param name="propertySelector">Expression selecting the property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ImportProperty(Expression<Func<T, object>> propertySelector)
        {
            return ImportProperty(propertySelector, null);
        }

        /// <summary>
        /// Select a property on the part to import.
        /// </summary>
        /// <param name="propertySelector">Expression selecting the property.</param>
        /// <param name="importConfiguration">Action configuring the imported property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ImportProperty(
            Expression<Func<T, object>> propertySelector,
            Action<ImportConventionBuilder> importConfiguration)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            var adapter = new PropertyExpressionAdapter(propertySelector, importConfiguration, null);
            base.ImportProperties(adapter.VerifyPropertyInfo, adapter.ConfigureImport);
            return this;
        }

        /// <summary>
        /// Select a property on the part to import.
        /// </summary>
        /// <typeparam name="TContract">Contract type to import.</typeparam>
        /// <param name="propertySelector">Expression selecting the property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ImportProperty<TContract>(Expression<Func<T, object>> propertySelector)
        {
            return ImportProperty<TContract>(propertySelector, null);
        }

        /// <summary>
        /// Select a property on the part to import.
        /// </summary>
        /// <typeparam name="TContract">Contract type to import.</typeparam>
        /// <param name="propertySelector">Expression selecting the property.</param>
        /// <param name="importConfiguration">Action configuring the imported property.</param>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> ImportProperty<TContract>(
            Expression<Func<T, object>> propertySelector,
            Action<ImportConventionBuilder> importConfiguration)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            var adapter = new PropertyExpressionAdapter(propertySelector, importConfiguration, null);
            base.ImportProperties<TContract>(adapter.VerifyPropertyInfo, adapter.ConfigureImport);
            return this;
        }

        /// <summary>
        /// Mark the part as being shared within the entire composition.
        /// </summary>
        /// <returns>A part builder allowing further configuration of the part.</returns>
        public PartConventionBuilder<T> NotifyImportsSatisfied(Expression<Action<T>> methodSelector)
        {
            if (methodSelector == null)
            {
                throw new ArgumentNullException(nameof(methodSelector));
            }

            var adapter = new MethodExpressionAdapter(methodSelector);
            base.NotifyImportsSatisfied(adapter.VerifyMethodInfo);
            return this;
        }
    }
}
