// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>Represents accessing a field or property.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Field" />, <see cref="O:System.Linq.Expressions.Expression.Property" /> or <see cref="O:System.Linq.Expressions.Expression.PropertyOrField" /> factory methods to create a <see cref="System.Linq.Expressions.MemberExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.MemberExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents getting the value of a field member.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet5":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet5":::</example>
    [DebuggerTypeProxy(typeof(MemberExpressionProxy))]
    public class MemberExpression : Expression
    {
        /// <summary>Gets the field or property to be accessed.</summary>
        /// <value>The <see cref="System.Reflection.MemberInfo" /> that represents the field or property to be accessed.</value>
        public MemberInfo Member => GetMember();

        /// <summary>Gets the containing object of the field or property.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the containing object of the field or property.</value>
        public Expression? Expression { get; }

        // param order: factories args in order, then other args
        internal MemberExpression(Expression? expression)
        {
            Expression = expression;
        }

        internal static PropertyExpression Make(Expression? expression, PropertyInfo property)
        {
            Debug.Assert(property != null);
            return new PropertyExpression(expression, property);
        }

        internal static FieldExpression Make(Expression? expression, FieldInfo field)
        {
            Debug.Assert(field != null);
            return new FieldExpression(expression, field);
        }

        internal static MemberExpression Make(Expression? expression, MemberInfo member)
        {
            FieldInfo? fi = member as FieldInfo;
            return fi == null ? (MemberExpression)Make(expression, (PropertyInfo)member) : Make(expression, fi);
        }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.MemberExpression.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.MemberAccess;

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual MemberInfo GetMember()
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitMember(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="expression">The <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public MemberExpression Update(Expression? expression)
        {
            if (expression == Expression)
            {
                return this;
            }
            return Expression.MakeMemberAccess(expression, Member);
        }
    }

    internal sealed class FieldExpression : MemberExpression
    {
        private readonly FieldInfo _field;

        public FieldExpression(Expression? expression, FieldInfo member)
            : base(expression)
        {
            _field = member;
        }

        internal override MemberInfo GetMember() => _field;

        public sealed override Type Type => _field.FieldType;
    }

    internal sealed class PropertyExpression : MemberExpression
    {
        private readonly PropertyInfo _property;
        public PropertyExpression(Expression? expression, PropertyInfo member)
            : base(expression)
        {
            _property = member;
        }

        internal override MemberInfo GetMember() => _property;

        public sealed override Type Type => _property.PropertyType;
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a field.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property equal to. For <see langword="static" /> (<see langword="Shared" /> in Visual Basic), <paramref name="expression" /> must be <see langword="null" />.</param>
        /// <param name="field">The <see cref="System.Reflection.FieldInfo" /> to set the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" /> and the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> and <see cref="System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="field" /> is <see langword="null" />.
        /// -or-
        /// The field represented by <paramref name="field" /> is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic) and <paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="expression" />.Type is not assignable to the declaring type of the field represented by <paramref name="field" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.FieldInfo.FieldType" /> property of <paramref name="field" />.</remarks>
        public static MemberExpression Field(Expression? expression, FieldInfo field)
        {
            ContractUtils.RequiresNotNull(field, nameof(field));

            if (field.IsStatic)
            {
                if (expression != null) throw Error.OnlyStaticFieldsHaveNullInstance(nameof(expression));
            }
            else
            {
                if (expression == null) throw Error.OnlyStaticFieldsHaveNullInstance(nameof(field));
                ExpressionUtils.RequiresCanRead(expression, nameof(expression));
                if (!TypeUtils.AreReferenceAssignable(field.DeclaringType!, expression.Type))
                {
                    throw Error.FieldInfoNotDefinedForType(field.DeclaringType, field.Name, expression.Type);
                }
            }
            return MemberExpression.Make(expression, field);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a field given the name of the field.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> whose <see cref="System.Linq.Expressions.Expression.Type" /> contains a field named <paramref name="fieldName" />. This can be null for static fields.</param>
        /// <param name="fieldName">The name of a field to be accessed.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="System.Reflection.FieldInfo" /> that represents the field denoted by <paramref name="fieldName" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="fieldName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">No field named <paramref name="fieldName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.FieldInfo.FieldType" /> property of the <see cref="System.Reflection.FieldInfo" /> that represents the field denoted by <paramref name="fieldName" />.
        /// This method searches <paramref name="expression" />.Type and its base types for a field that has the name <paramref name="fieldName" />. Public fields are given preference over non-public fields. If a matching field is found, this method passes <paramref name="expression" /> and the <see cref="System.Reflection.FieldInfo" /> that represents that field to <see cref="O:System.Linq.Expressions.Expression.Field" />.</remarks>
        /// <example>The following code example shows how to create an expression that represents accessing a field.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet37":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet37":::</example>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static MemberExpression Field(Expression expression, string fieldName)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(fieldName, nameof(fieldName));

            // bind to public names first
            FieldInfo? fi = expression.Type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy)
                           ?? expression.Type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (fi == null)
            {
                throw Error.InstanceFieldNotDefinedForType(fieldName, expression.Type);
            }
            return Expression.Field(expression, fi);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a field.</summary>
        /// <param name="expression">The containing object of the field. This can be null for static fields.</param>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> that contains the field.</param>
        /// <param name="fieldName">The field to be accessed.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.MemberExpression" />.</returns>
        public static MemberExpression Field(
            Expression? expression,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type,
            string fieldName)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(fieldName, nameof(fieldName));

            // bind to public names first
            FieldInfo? fi = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy)
                           ?? type.GetField(fieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

            if (fi == null)
            {
                throw Error.FieldNotDefinedForType(fieldName, type);
            }
            return Expression.Field(expression, fi);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a property.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> whose <see cref="System.Linq.Expressions.Expression.Type" /> contains a property named <paramref name="propertyName" />. This can be <see langword="null" /> for static properties.</param>
        /// <param name="propertyName">The name of a property to be accessed.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> that represents the property denoted by <paramref name="propertyName" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="propertyName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">No property named <paramref name="propertyName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> property of the <see cref="System.Reflection.PropertyInfo" /> that represents the property denoted by <paramref name="propertyName" />.
        /// This method searches <paramref name="expression" />.Type and its base types for a property that has the name <paramref name="propertyName" />. Public properties are given preference over non-public properties. If a matching property is found, this method passes <paramref name="expression" /> and the <see cref="System.Reflection.PropertyInfo" /> that represents that property to <see cref="O:System.Linq.Expressions.Expression.Property" />.</remarks>
        /// <example>The following example shows how to create an expression that represents accessing a property.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet38":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet38":::</example>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static MemberExpression Property(Expression expression, string propertyName)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(propertyName, nameof(propertyName));
            // bind to public names first
            PropertyInfo? pi = expression.Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy)
                              ?? expression.Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (pi == null)
            {
                throw Error.InstancePropertyNotDefinedForType(propertyName, expression.Type, nameof(propertyName));
            }
            return Property(expression, pi);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> accessing a property.</summary>
        /// <param name="expression">The containing object of the property. This can be null for static properties.</param>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> that contains the property.</param>
        /// <param name="propertyName">The property to be accessed.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.MemberExpression" />.</returns>
        public static MemberExpression Property(
            Expression? expression,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties| DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
            string propertyName)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(propertyName, nameof(propertyName));
            // bind to public names first
            PropertyInfo? pi = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy)
                              ?? type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (pi == null)
            {
                throw Error.PropertyNotDefinedForType(propertyName, type, nameof(propertyName));
            }
            return Property(expression, pi);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a property.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property equal to. This can be null for static properties.</param>
        /// <param name="property">The <see cref="System.Reflection.PropertyInfo" /> to set the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" /> and the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> and <see cref="System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="property" /> is <see langword="null" />.
        /// -or-
        /// The property that <paramref name="property" /> represents is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic) and <paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="expression" />.Type is not assignable to the declaring type of the property that <paramref name="property" /> represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> property of <see cref="O:System.Linq.Expressions.MemberExpression.Member" />.
        /// If the property represented by <paramref name="property" /> is <see langword="static" /> (`Shared` in Visual Basic), <paramref name="expression" /> can be <see langword="null" />.</remarks>
        public static MemberExpression Property(Expression? expression, PropertyInfo property)
        {
            ContractUtils.RequiresNotNull(property, nameof(property));

            MethodInfo? mi = property.GetGetMethod(nonPublic: true);

            if (mi == null)
            {
                mi = property.GetSetMethod(nonPublic: true);

                if (mi == null)
                {
                    throw Error.PropertyDoesNotHaveAccessor(property, nameof(property));
                }
                else if (mi.GetParametersCached().Length != 1)
                {
                    throw Error.IncorrectNumberOfMethodCallArguments(mi, nameof(property));
                }
            }
            else if (mi.GetParametersCached().Length != 0)
            {
                throw Error.IncorrectNumberOfMethodCallArguments(mi, nameof(property));
            }

            if (mi.IsStatic)
            {
                if (expression != null) throw Error.OnlyStaticPropertiesHaveNullInstance(nameof(expression));
            }
            else
            {
                if (expression == null) throw Error.OnlyStaticPropertiesHaveNullInstance(nameof(property));
                ExpressionUtils.RequiresCanRead(expression, nameof(expression));
                if (!TypeUtils.IsValidInstanceType(property, expression.Type))
                {
                    throw Error.PropertyNotDefinedForType(property, expression.Type, nameof(property));
                }
            }

            ValidateMethodInfo(mi, nameof(property));

            return MemberExpression.Make(expression, property);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a property by using a property accessor method.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property equal to. This can be null for static properties.</param>
        /// <param name="propertyAccessor">The <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" /> and the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> is <see langword="null" />.
        /// -or-
        /// The method that <paramref name="propertyAccessor" /> represents is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic) and <paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="expression" />.Type is not assignable to the declaring type of the method represented by <paramref name="propertyAccessor" />.
        /// -or-
        /// The method that <paramref name="propertyAccessor" /> represents is not a property accessor method.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> property of <see cref="O:System.Linq.Expressions.MemberExpression.Member" />.
        /// If the method represented by <paramref name="propertyAccessor" /> is <see langword="static" /> (`Shared` in Visual Basic), <paramref name="expression" /> can be <see langword="null" />.</remarks>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberExpression Property(Expression? expression, MethodInfo propertyAccessor)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            ValidateMethodInfo(propertyAccessor, nameof(propertyAccessor));
            return Property(expression, GetProperty(propertyAccessor, nameof(propertyAccessor)));
        }

        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        private static PropertyInfo GetProperty(MethodInfo mi, string? paramName, int index = -1)
        {
            Type? type = mi.DeclaringType;
            if (type != null)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
                flags |= (mi.IsStatic) ? BindingFlags.Static : BindingFlags.Instance;
                PropertyInfo[] props = type.GetProperties(flags);
                foreach (PropertyInfo pi in props)
                {
                    if (pi.CanRead && CheckMethod(mi, pi.GetGetMethod(nonPublic: true)!))
                    {
                        return pi;
                    }
                    if (pi.CanWrite && CheckMethod(mi, pi.GetSetMethod(nonPublic: true)!))
                    {
                        return pi;
                    }
                }
            }

            throw Error.MethodNotPropertyAccessor(mi.DeclaringType, mi.Name, paramName, index);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Since the methods are already supplied, they won't be trimmed. Just checking for method equality.")]
        private static bool CheckMethod(MethodInfo method, MethodInfo propertyMethod)
        {
            if (method.Equals(propertyMethod))
            {
                return true;
            }
            // If the type is an interface then the handle for the method got by the compiler will not be the
            // same as that returned by reflection.
            // Check for this condition and try and get the method from reflection.
            Type type = method.DeclaringType!;
            if (type.IsInterface && method.Name == propertyMethod.Name && type.GetMethod(method.Name) == propertyMethod)
            {
                return true;
            }
            return false;
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing a property or field.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> whose <see cref="System.Linq.Expressions.Expression.Type" /> contains a property or field named <paramref name="propertyOrFieldName" />.</param>
        /// <param name="propertyOrFieldName">The name of a property or field to be accessed.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> or <see cref="System.Reflection.FieldInfo" /> that represents the property or field denoted by <paramref name="propertyOrFieldName" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="propertyOrFieldName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">No property or field named <paramref name="propertyOrFieldName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberExpression" /> is equal to the <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> or <see cref="O:System.Reflection.FieldInfo.FieldType" /> properties of the <see cref="System.Reflection.PropertyInfo" /> or <see cref="System.Reflection.FieldInfo" />, respectively, that represents the property or field denoted by <paramref name="propertyOrFieldName" />.
        /// This method searches <paramref name="expression" />.Type and its base types for an instance property or field that has the name <paramref name="propertyOrFieldName" />. Static properties or fields are not supported. Public properties and fields are given preference over non-public properties and fields. Also, properties are given preference over fields. If a matching property or field is found, this method passes <paramref name="expression" /> and the <see cref="System.Reflection.PropertyInfo" /> or <see cref="System.Reflection.FieldInfo" /> that represents that property or field to <see cref="O:System.Linq.Expressions.Expression.Property" /> or <see cref="O:System.Linq.Expressions.Expression.Field" />, respectively.</remarks>
        /// <example>The following example shows how to create an expression that represents accessing a property or field.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet39":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet39":::</example>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static MemberExpression PropertyOrField(Expression expression, string propertyOrFieldName)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            // bind to public names first
            PropertyInfo? pi = expression.Type.GetProperty(propertyOrFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (pi != null)
                return Property(expression, pi);
            FieldInfo? fi = expression.Type.GetField(propertyOrFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (fi != null)
                return Field(expression, fi);
            pi = expression.Type.GetProperty(propertyOrFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (pi != null)
                return Property(expression, pi);
            fi = expression.Type.GetField(propertyOrFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
            if (fi != null)
                return Field(expression, fi);

            throw Error.NotAMemberOfType(propertyOrFieldName, expression.Type, nameof(propertyOrFieldName));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing either a field or a property.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> that represents the object that the member belongs to. This can be null for static members.</param>
        /// <param name="member">The <see cref="System.Reflection.MemberInfo" /> that describes the field or property to be accessed.</param>
        /// <returns>The <see cref="System.Linq.Expressions.MemberExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.</exception>
        /// <remarks>This method can be used to create a <see cref="System.Linq.Expressions.MemberExpression" /> that represents accessing either a field or a property, depending on the type of <paramref name="member" />. If <paramref name="member" /> is of type <see cref="System.Reflection.FieldInfo" />, this method calls <see cref="O:System.Linq.Expressions.Expression.Field" /> to create the <see cref="System.Linq.Expressions.MemberExpression" />. If <paramref name="member" /> is of type <see cref="System.Reflection.PropertyInfo" />, this method calls <see cref="O:System.Linq.Expressions.Expression.Property" /> to create the <see cref="System.Linq.Expressions.MemberExpression" />.</remarks>
        public static MemberExpression MakeMemberAccess(Expression? expression, MemberInfo member)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));

            if (member is FieldInfo fi)
            {
                return Expression.Field(expression, fi);
            }
            if (member is PropertyInfo pi)
            {
                return Expression.Property(expression, pi);
            }
            throw Error.MemberNotFieldOrProperty(member, nameof(member));
        }
    }
}
