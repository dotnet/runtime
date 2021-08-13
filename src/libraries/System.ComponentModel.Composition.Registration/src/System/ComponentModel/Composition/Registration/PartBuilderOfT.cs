// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.ComponentModel.Composition.Registration
{
    public class PartBuilder<T> : PartBuilder
    {
        private sealed class PropertyExpressionAdapter
        {
            private readonly PropertyInfo _propertyInfo;
            private readonly Action<ImportBuilder> _configureImport;
            private readonly Action<ExportBuilder> _configureExport;

            public PropertyExpressionAdapter(Expression<Func<T, object>> propertyFilter,
                Action<ImportBuilder> configureImport = null,
                Action<ExportBuilder> configureExport = null)
            {
                _propertyInfo = SelectProperties(propertyFilter);
                _configureImport = configureImport;
                _configureExport = configureExport;
            }

            public bool VerifyPropertyInfo(PropertyInfo pi)
            {
                return pi == _propertyInfo;
            }

            public void ConfigureImport(PropertyInfo propertyInfo, ImportBuilder importBuilder)
            {
                _configureImport?.Invoke(importBuilder);
            }

            public void ConfigureExport(PropertyInfo propertyInfo, ExportBuilder exportBuilder)
            {
                _configureExport?.Invoke(exportBuilder);
            }

            private static PropertyInfo SelectProperties(Expression<Func<T, object>> propertyFilter)
            {
                if (propertyFilter == null)
                    throw new ArgumentNullException(nameof(propertyFilter));

                Expression expr = Reduce(propertyFilter).Body;
                if (expr.NodeType == ExpressionType.MemberAccess)
                {
                    MemberInfo memberInfo = ((MemberExpression)expr).Member;
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        return (PropertyInfo)memberInfo;
                    }
                }

                // An error occured the expression must be a Property Member Expression
                throw new ArgumentException(SR.Format(SR.Argument_ExpressionMustBePropertyMember, nameof(propertyFilter)), nameof(propertyFilter));
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
            private Dictionary<ParameterInfo, Action<ImportBuilder>> _importBuilders;

            public ConstructorExpressionAdapter(Expression<Func<ParameterImportBuilder, T>> selectConstructor)
            {
                ParseSelectConstructor(selectConstructor);
            }

            public ConstructorInfo SelectConstructor(ConstructorInfo[] constructorInfos)
            {
                return _constructorInfo;
            }

            public void ConfigureConstructorImports(ParameterInfo parameterInfo, ImportBuilder importBuilder)
            {
                if (_importBuilders != null &&
                    _importBuilders.TryGetValue(parameterInfo, out Action<ImportBuilder> parameterImportBuilder))
                {
                    parameterImportBuilder(importBuilder);
                }
            }

            private void ParseSelectConstructor(Expression<Func<ParameterImportBuilder, T>> constructorFilter)
            {
                if (constructorFilter == null)
                    throw new ArgumentNullException(nameof(constructorFilter));

                Expression expr = Reduce(constructorFilter).Body;
                if (expr.NodeType != ExpressionType.New)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_ExpressionMustBePropertyMember, nameof(constructorFilter)), nameof(constructorFilter));
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
                                    _importBuilders = new Dictionary<ParameterInfo, Action<ImportBuilder>>();
                                }

                                _importBuilders.Add(parameterInfos[index], (Action<ImportBuilder>)importDelegate);
                                ++index;
                            }
                        }
                    }
                }
            }

            private static Expression<Func<ParameterImportBuilder, T>> Reduce(Expression<Func<ParameterImportBuilder, T>> expr)
            {
                while (expr.CanReduce)
                {
                    expr.Reduce();
                }

                return expr;
            }
        }

        internal PartBuilder(Predicate<Type> selectType) : base(selectType)
        {
        }

        public PartBuilder<T> SelectConstructor(Expression<Func<ParameterImportBuilder, T>> constructorFilter)
        {
            if (constructorFilter == null)
                throw new ArgumentNullException(nameof(constructorFilter));

            var adapter = new ConstructorExpressionAdapter(constructorFilter);
            SelectConstructor(adapter.SelectConstructor, adapter.ConfigureConstructorImports);

            return this;
        }


        public PartBuilder<T> ExportProperty(Expression<Func<T, object>> propertyFilter)
        {
            return ExportProperty(propertyFilter, null);
        }

        public PartBuilder<T> ExportProperty(
            Expression<Func<T, object>> propertyFilter,
            Action<ExportBuilder> exportConfiguration)
        {
            if (propertyFilter == null)
                throw new ArgumentNullException(nameof(propertyFilter));

            var adapter = new PropertyExpressionAdapter(propertyFilter, null, exportConfiguration);
            ExportProperties(adapter.VerifyPropertyInfo, adapter.ConfigureExport);

            return this;
        }

        public PartBuilder<T> ExportProperty<TContract>(Expression<Func<T, object>> propertyFilter)
        {
            return ExportProperty<TContract>(propertyFilter, null);
        }

        public PartBuilder<T> ExportProperty<TContract>(Expression<Func<T, object>> propertyFilter,
            Action<ExportBuilder> exportConfiguration)
        {
            if (propertyFilter == null)
                throw new ArgumentNullException(nameof(propertyFilter));

            var adapter = new PropertyExpressionAdapter(propertyFilter, null, exportConfiguration);
            ExportProperties<TContract>(adapter.VerifyPropertyInfo, adapter.ConfigureExport);

            return this;
        }

        public PartBuilder<T> ImportProperty(Expression<Func<T, object>> propertyFilter)
        {
            return ImportProperty(propertyFilter, null);
        }

        public PartBuilder<T> ImportProperty(Expression<Func<T, object>> propertyFilter,
            Action<ImportBuilder> importConfiguration)
        {
            if (propertyFilter == null)
                throw new ArgumentNullException(nameof(propertyFilter));

            var adapter = new PropertyExpressionAdapter(propertyFilter, importConfiguration, null);
            ImportProperties(adapter.VerifyPropertyInfo, adapter.ConfigureImport);

            return this;
        }

        public PartBuilder<T> ImportProperty<TContract>(Expression<Func<T, object>> propertyFilter)
        {
            return ImportProperty<TContract>(propertyFilter, null);
        }

        public PartBuilder<T> ImportProperty<TContract>(Expression<Func<T, object>> propertyFilter,
            Action<ImportBuilder> importConfiguration)
        {
            if (propertyFilter == null)
                throw new ArgumentNullException(nameof(propertyFilter));

            var adapter = new PropertyExpressionAdapter(propertyFilter, importConfiguration, null);
            ImportProperties<TContract>(adapter.VerifyPropertyInfo, adapter.ConfigureImport);

            return this;
        }
    }
}
