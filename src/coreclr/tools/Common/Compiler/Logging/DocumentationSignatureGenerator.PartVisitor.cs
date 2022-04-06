// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

using Internal.TypeSystem;

namespace ILCompiler.Logging
{

    public sealed partial class DocumentationSignatureGenerator
    {
        /// <summary>
        ///  A visitor that generates the part of the documentation comment after the initial type
        ///  and colon.
        ///  Adapted from Roslyn's DocumentattionCommentIDVisitor.PartVisitor:
        ///  https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.PartVisitor.cs
        /// </summary>
        internal sealed class PartVisitor : TypeNameFormatter
        {
            internal static readonly PartVisitor Instance = new PartVisitor();

            private PartVisitor()
            {
            }

            public override void AppendName(StringBuilder builder, ArrayType arrayType)
            {
                AppendName(builder, arrayType.ElementType);

                // Rank-one arrays are displayed different than rectangular arrays
                if (arrayType.IsSzArray)
                {
                    builder.Append("[]");
                }
                else
                {
                    // C# arrays only support zero lower bounds
                    builder.Append("[0:");
                    for (int i = 1; i < arrayType.Rank; i++)
                    {
                        //if (arrayType.Dimensions[0].LowerBound != 0)
                        //    throw new NotImplementedException();
                        builder.Append(",0:");
                    }

                    builder.Append(']');
                }
            }

#if false
            public void VisitField(FieldDefinition field, StringBuilder builder)
            {
                VisitTypeReference(field.DeclaringType, builder);
                builder.Append('.').Append(field.Name);
            }

            private void VisitParameters(IEnumerable<ParameterDefinition> parameters, bool isVararg, StringBuilder builder)
            {
                builder.Append('(');
                bool needsComma = false;

                foreach (var parameter in parameters)
                {
                    if (needsComma)
                        builder.Append(',');

                    // byrefs are tracked on the parameter type, not the parameter,
                    // so we don't have VisitParameter that Roslyn uses.
                    VisitTypeReference(parameter.ParameterType, builder);
                    needsComma = true;
                }

                // note: the C# doc comment generator outputs an extra comma for varargs
                // methods that also have fixed parameters
                if (isVararg && needsComma)
                    builder.Append(',');

                builder.Append(')');
            }

            public void VisitMethodDefinition(MethodDefinition method, StringBuilder builder)
            {
                VisitTypeReference(method.DeclaringType, builder);
                builder.Append('.').Append(GetEscapedMetadataName(method));

                if (method.HasGenericParameters)
                    builder.Append("``").Append(method.GenericParameters.Count);

                if (method.HasParameters || (method.CallingConvention == MethodCallingConvention.VarArg))
                    VisitParameters(method.Parameters, method.CallingConvention == MethodCallingConvention.VarArg, builder);

                if (method.Name == "op_Implicit" || method.Name == "op_Explicit")
                {
                    builder.Append('~');
                    VisitTypeReference(method.ReturnType, builder);
                }
            }

            public void VisitProperty(PropertyDefinition property, StringBuilder builder)
            {
                VisitTypeReference(property.DeclaringType, builder);
                builder.Append('.').Append(GetEscapedMetadataName(property));

                if (property.Parameters.Count > 0)
                    VisitParameters(property.Parameters, false, builder);
            }

            public void VisitEvent(EventDefinition evt, StringBuilder builder)
            {
                VisitTypeReference(evt.DeclaringType, builder);
                builder.Append('.').Append(GetEscapedMetadataName(evt));
            }
#endif

            public override void AppendName(StringBuilder builder, FunctionPointerType type)
            {
                // Not defined how this should look like
                // https://github.com/dotnet/roslyn/issues/48363
            }

            public override void AppendName(StringBuilder builder, GenericParameterDesc genericParameter)
            {
                // Is this a type parameter on a type?
                if (genericParameter.Kind == GenericParameterKind.Method)
                {
                    builder.Append("``");
                }
                else
                {
                    Debug.Assert(genericParameter.Kind == GenericParameterKind.Type);

                    // If the containing type is nested within other types.
                    // e.g. A<T>.B<U>.M<V>(T t, U u, V v) should be M(`0, `1, ``0).
                    // Roslyn needs to add generic arities of parents, but the innermost type redeclares 
                    // all generic parameters so we don't need to add them.
                    builder.Append('`');
                }

                builder.Append(genericParameter.Index);
            }

            public override void AppendName(StringBuilder builder, SignatureMethodVariable type) => builder.Append("``").Append(type.Index);
            public override void AppendName(StringBuilder builder, SignatureTypeVariable type) => builder.Append('`').Append(type.Index);

            protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
            {
                AppendName(sb, containingType);
                sb.Append('.');
                sb.Append(nestedType.Name);
            }

            protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
            {
                string @namespace = type.Namespace;
                if (!string.IsNullOrEmpty(@namespace))
                    sb.Append(@namespace).Append('.');
                sb.Append(type.Name);
            }

            protected override void AppendNameForInstantiatedType(StringBuilder builder, DefType type)
            {
                int containingArity = 0;
                DefType containingType = type.ContainingType;
                if (containingType != null)
                {
                    AppendName(builder, containingType);
                    containingArity = containingType.Instantiation.Length;
                }
                else
                {
                    string @namespace = type.Namespace;
                    if (!string.IsNullOrEmpty(@namespace))
                        builder.Append(@namespace).Append('.');
                }

                string unmangledName = type.Name;
                int totalArity = type.Instantiation.Length;
                string expectedSuffix = $"`{totalArity.ToString()}";
                if (unmangledName.EndsWith(expectedSuffix))
                    unmangledName = unmangledName.Substring(0, unmangledName.Length - expectedSuffix.Length);

                builder.Append(unmangledName);

                // Append type arguments excluding arguments for re-declared parent generic parameters
                builder.Append('{');
                bool needsComma = false;
                for (int i = containingArity; i < totalArity; ++i)
                {
                    if (needsComma)
                        builder.Append(',');
                    var typeArgument = type.Instantiation[i];
                    AppendName(builder, typeArgument);
                    needsComma = true;
                }
                builder.Append('}');
            }

            public override void AppendName(StringBuilder builder, PointerType type)
            {
                AppendName(builder, type.ParameterType);
                builder.Append('*');
            }

            public override void AppendName(StringBuilder builder, ByRefType type)
            {
                AppendName(builder, type.ParameterType);
                builder.Append('@');
            }

#if false
            private static string GetEscapedMetadataName(IMemberDefinition member)
            {
                var name = member.Name.Replace('.', '#');
                // Not sure if the following replacements are necessary, but
                // they are included to match Roslyn.
                return name.Replace('<', '{').Replace('>', '}');
            }
#endif
        }
    }
}
