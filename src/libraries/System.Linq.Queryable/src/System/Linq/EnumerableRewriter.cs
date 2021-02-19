// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    internal class EnumerableRewriter : ExpressionVisitor
    {
        // We must ensure that if a LabelTarget is rewritten that it is always rewritten to the same new target
        // or otherwise expressions using it won't match correctly.
        private Dictionary<LabelTarget, LabelTarget>? _targetCache;
        // Finding equivalent types can be relatively expensive, and hitting with the same types repeatedly is quite likely.
        private Dictionary<Type, Type>? _equivalentTypeCache;

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        public EnumerableRewriter()
        {
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression? obj = Visit(m.Object);
            ReadOnlyCollection<Expression> args = Visit(m.Arguments);

            // check for args changed
            if (obj != m.Object || args != m.Arguments)
            {
                MethodInfo mInfo = m.Method;
                Type[]? typeArgs = (mInfo.IsGenericMethod) ? mInfo.GetGenericArguments() : null;

                if ((mInfo.IsStatic || mInfo.DeclaringType!.IsAssignableFrom(obj!.Type))
                    && ArgsMatch(mInfo, args, typeArgs))
                {
                    // current method is still valid
                    return Expression.Call(obj, mInfo, args);
                }
                else if (mInfo.DeclaringType == typeof(Queryable))
                {
                    // convert Queryable method to Enumerable method
                    MethodInfo seqMethod = FindEnumerableMethodForQueryable(mInfo.Name, args, typeArgs);
                    args = FixupQuotedArgs(seqMethod, args);
                    return Expression.Call(obj, seqMethod, args);
                }
                else
                {
                    // rebind to new method
                    MethodInfo method = FindMethod(mInfo.DeclaringType!, mInfo.Name, args, typeArgs);
                    args = FixupQuotedArgs(method, args);
                    return Expression.Call(obj, method, args);
                }
            }
            return m;
        }

        private ReadOnlyCollection<Expression> FixupQuotedArgs(MethodInfo mi, ReadOnlyCollection<Expression> argList)
        {
            ParameterInfo[] pis = mi.GetParameters();
            if (pis.Length > 0)
            {
                List<Expression>? newArgs = null;
                for (int i = 0, n = pis.Length; i < n; i++)
                {
                    Expression arg = argList[i];
                    ParameterInfo pi = pis[i];
                    arg = FixupQuotedExpression(pi.ParameterType, arg);
                    if (newArgs == null && arg != argList[i])
                    {
                        newArgs = new List<Expression>(argList.Count);
                        for (int j = 0; j < i; j++)
                        {
                            newArgs.Add(argList[j]);
                        }
                    }

                    newArgs?.Add(arg);
                }
                if (newArgs != null)
                    argList = newArgs.AsReadOnly();
            }
            return argList;
        }

        private Expression FixupQuotedExpression(Type type, Expression expression)
        {
            Expression expr = expression;
            while (true)
            {
                if (type.IsAssignableFrom(expr.Type))
                    return expr;
                if (expr.NodeType != ExpressionType.Quote)
                    break;
                expr = ((UnaryExpression)expr).Operand;
            }
            if (!type.IsAssignableFrom(expr.Type) && type.IsArray && expr.NodeType == ExpressionType.NewArrayInit)
            {
                Type strippedType = StripExpression(expr.Type);
                if (type.IsAssignableFrom(strippedType))
                {
                    Type elementType = type.GetElementType()!;
                    NewArrayExpression na = (NewArrayExpression)expr;
                    List<Expression> exprs = new List<Expression>(na.Expressions.Count);
                    for (int i = 0, n = na.Expressions.Count; i < n; i++)
                    {
                        exprs.Add(FixupQuotedExpression(elementType, na.Expressions[i]));
                    }
                    expression = Expression.NewArrayInit(elementType, exprs);
                }
            }
            return expression;
        }

        protected override Expression VisitLambda<T>(Expression<T> node) => node;

        private static Type GetPublicType(Type t)
        {
            // If we create a constant explicitly typed to be a private nested type,
            // such as Lookup<,>.Grouping or a compiler-generated iterator class, then
            // we cannot use the expression tree in a context which has only execution
            // permissions.  We should endeavour to translate constants into
            // new constants which have public types.
            if (t.IsGenericType && t.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IGrouping<,>)))
                return typeof(IGrouping<,>).MakeGenericType(t.GetGenericArguments());
            if (!t.IsNestedPrivate)
                return t;
            foreach (Type iType in t.GetInterfaces())
            {
                if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iType;
            }
            if (typeof(IEnumerable).IsAssignableFrom(t))
                return typeof(IEnumerable);
            return t;
        }

        private Type GetEquivalentType(Type type)
        {
            Type? equiv;
            if (_equivalentTypeCache == null)
            {
                // Pre-loading with the non-generic IQueryable and IEnumerable not only covers this case
                // without any reflection-based introspection, but also means the slightly different
                // code needed to catch this case can be omitted safely.
                _equivalentTypeCache = new Dictionary<Type, Type>
                    {
                        { typeof(IQueryable), typeof(IEnumerable) },
                        { typeof(IEnumerable), typeof(IEnumerable) }
                    };
            }
            if (!_equivalentTypeCache.TryGetValue(type, out equiv))
            {
                Type pubType = GetPublicType(type);
                if (pubType.IsInterface && pubType.IsGenericType)
                {
                    Type genericType = pubType.GetGenericTypeDefinition();
                    if (genericType == typeof(IOrderedEnumerable<>))
                        equiv = pubType;
                    else if (genericType == typeof(IOrderedQueryable<>))
                        equiv = typeof(IOrderedEnumerable<>).MakeGenericType(pubType.GenericTypeArguments[0]);
                    else if (genericType == typeof(IEnumerable<>))
                        equiv = pubType;
                    else if (genericType == typeof(IQueryable<>))
                        equiv = typeof(IEnumerable<>).MakeGenericType(pubType.GenericTypeArguments[0]);
                }
                if (equiv == null)
                {
                    var interfacesWithInfo = pubType.GetInterfaces().Select(IntrospectionExtensions.GetTypeInfo).ToArray();
                    var singleTypeGenInterfacesWithGetType = interfacesWithInfo
                        .Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 1)
                        .Select(i => new { Info = i, GenType = i.GetGenericTypeDefinition() })
                        .ToArray();
                    Type? typeArg = singleTypeGenInterfacesWithGetType
                        .Where(i => i.GenType == typeof(IOrderedQueryable<>) || i.GenType == typeof(IOrderedEnumerable<>))
                        .Select(i => i.Info.GenericTypeArguments[0])
                        .Distinct()
                        .SingleOrDefault();
                    if (typeArg != null)
                        equiv = typeof(IOrderedEnumerable<>).MakeGenericType(typeArg);
                    else
                    {
                        typeArg = singleTypeGenInterfacesWithGetType
                            .Where(i => i.GenType == typeof(IQueryable<>) || i.GenType == typeof(IEnumerable<>))
                            .Select(i => i.Info.GenericTypeArguments[0])
                            .Distinct()
                            .Single();
                        equiv = typeof(IEnumerable<>).MakeGenericType(typeArg);
                    }
                }
                _equivalentTypeCache.Add(type, equiv);
            }
            return equiv;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value is EnumerableQuery sq)
            {
                if (sq.Enumerable != null)
                {
                    Type t = GetPublicType(sq.Enumerable.GetType());
                    return Expression.Constant(sq.Enumerable, t);
                }
                Expression exp = sq.Expression;
                if (exp != c)
                    return Visit(exp);
            }
            return c;
        }

        private static ILookup<string, MethodInfo>? s_seqMethods;
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "Enumerable methods don't have trim annotations.")]
        private static MethodInfo FindEnumerableMethodForQueryable(string name, ReadOnlyCollection<Expression> args, params Type[]? typeArgs)
        {
            if (s_seqMethods == null)
            {
                s_seqMethods = GetEnumerableStaticMethods(typeof(Enumerable)).ToLookup(m => m.Name);
            }
            MethodInfo? mi = s_seqMethods[name].FirstOrDefault(m => ArgsMatch(m, args, typeArgs));
            Debug.Assert(mi != null, "All static methods with arguments on Queryable have equivalents on Enumerable.");
            if (typeArgs != null)
                return mi.MakeGenericMethod(typeArgs);
            return mi;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
                Justification = "This method is intentionally hiding the Enumerable type from the trimmer so it doesn't preserve all Enumerable's methods. " +
                "This is safe because all Queryable methods have a DynamicDependency to the corresponding Enumerable method.")]
            static MethodInfo[] GetEnumerableStaticMethods(Type type) =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        }

        [RequiresUnreferencedCode(Queryable.InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        private static MethodInfo FindMethod(Type type, string name, ReadOnlyCollection<Expression> args, Type[]? typeArgs)
        {
            using (IEnumerator<MethodInfo> en = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == name).GetEnumerator())
            {
                if (!en.MoveNext())
                    throw Error.NoMethodOnType(name, type);
                do
                {
                    MethodInfo mi = en.Current;
                    if (ArgsMatch(mi, args, typeArgs))
                        return (typeArgs != null) ? mi.MakeGenericMethod(typeArgs) : mi;
                } while (en.MoveNext());
            }
            throw Error.NoMethodOnTypeMatchingArguments(name, type);
        }

        private static bool ArgsMatch(MethodInfo m, ReadOnlyCollection<Expression> args, Type[]? typeArgs)
        {
            ParameterInfo[] mParams = m.GetParameters();
            if (mParams.Length != args.Count)
                return false;
            if (!m.IsGenericMethod && typeArgs != null && typeArgs.Length > 0)
            {
                return false;
            }
            if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
            {
                m = m.GetGenericMethodDefinition();
            }
            if (m.IsGenericMethodDefinition)
            {
                if (typeArgs == null || typeArgs.Length == 0)
                    return false;
                if (m.GetGenericArguments().Length != typeArgs.Length)
                    return false;

                mParams = GetConstrutedGenericParameters(m, typeArgs);

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                    Justification = "MakeGenericMethod is only called to get the parameter types, which are only used to make a 'match' decision. The generic method is not invoked.")]
                static ParameterInfo[] GetConstrutedGenericParameters(MethodInfo method, Type[] genericTypes) =>
                    method.MakeGenericMethod(genericTypes).GetParameters();
            }
            for (int i = 0, n = args.Count; i < n; i++)
            {
                Type parameterType = mParams[i].ParameterType;
                if (parameterType == null)
                    return false;
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;
                Expression arg = args[i];
                if (!parameterType.IsAssignableFrom(arg.Type))
                {
                    if (arg.NodeType == ExpressionType.Quote)
                    {
                        arg = ((UnaryExpression)arg).Operand;
                    }
                    if (!parameterType.IsAssignableFrom(arg.Type) &&
                        !parameterType.IsAssignableFrom(StripExpression(arg.Type)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static Type StripExpression(Type type)
        {
            bool isArray = type.IsArray;
            Type tmp = isArray ? type.GetElementType()! : type;
            Type? eType = TypeHelper.FindGenericType(typeof(Expression<>), tmp);
            if (eType != null)
                tmp = eType.GetGenericArguments()[0];
            if (isArray)
            {
                int rank = type.GetArrayRank();
                return (rank == 1) ? tmp.MakeArrayType() : tmp.MakeArrayType(rank);
            }
            return type;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            Type type = c.Type;
            if (!typeof(IQueryable).IsAssignableFrom(type))
                return base.VisitConditional(c);
            Expression test = Visit(c.Test);
            Expression ifTrue = Visit(c.IfTrue);
            Expression ifFalse = Visit(c.IfFalse);
            Type trueType = ifTrue.Type;
            Type falseType = ifFalse.Type;
            if (trueType.IsAssignableFrom(falseType))
                return Expression.Condition(test, ifTrue, ifFalse, trueType);
            if (falseType.IsAssignableFrom(trueType))
                return Expression.Condition(test, ifTrue, ifFalse, falseType);
            return Expression.Condition(test, ifTrue, ifFalse, GetEquivalentType(type));
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            Type type = node.Type;
            if (!typeof(IQueryable).IsAssignableFrom(type))
                return base.VisitBlock(node);
            ReadOnlyCollection<Expression> nodes = Visit(node.Expressions);
            ReadOnlyCollection<ParameterExpression> variables = VisitAndConvert(node.Variables, "EnumerableRewriter.VisitBlock");
            if (type == node.Expressions.Last().Type)
                return Expression.Block(variables, nodes);
            return Expression.Block(GetEquivalentType(type), variables, nodes);
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            Type type = node.Value!.Type;
            if (!typeof(IQueryable).IsAssignableFrom(type))
                return base.VisitGoto(node);
            LabelTarget target = VisitLabelTarget(node.Target);
            Expression value = Visit(node.Value);
            return Expression.MakeGoto(node.Kind, target, value, GetEquivalentType(typeof(EnumerableQuery).IsAssignableFrom(type) ? value.Type : type));
        }

        protected override LabelTarget VisitLabelTarget(LabelTarget? node)
        {
            LabelTarget? newTarget;
            if (_targetCache == null)
                _targetCache = new Dictionary<LabelTarget, LabelTarget>();
            else if (_targetCache.TryGetValue(node!, out newTarget))
                return newTarget;
            Type type = node!.Type;
            if (!typeof(IQueryable).IsAssignableFrom(type))
                newTarget = base.VisitLabelTarget(node);
            else
                newTarget = Expression.Label(GetEquivalentType(type), node.Name);
            _targetCache.Add(node, newTarget);
            return newTarget;
        }
    }
}
