// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{

    internal class EvaluateExpression
    {
        private class FindVariableNMethodCall : CSharpSyntaxWalker
        {
            public List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();
            public List<InvocationExpressionSyntax> methodCall = new List<InvocationExpressionSyntax>();
            public List<MemberAccessExpressionSyntax> memberAccesses = new List<MemberAccessExpressionSyntax>();
            public List<object> argValues = new List<object>();

            public override void Visit(SyntaxNode node)
            {
                // TODO: PointerMemberAccessExpression
                if (node is MemberAccessExpressionSyntax maes
                    && node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                    && !(node.Parent is MemberAccessExpressionSyntax))
                {
                    memberAccesses.Add(maes);
                }

                if (node is IdentifierNameSyntax identifier
                    && !(identifier.Parent is MemberAccessExpressionSyntax)
                    && !identifiers.Any(x => x.Identifier.Text == identifier.Identifier.Text))
                {
                    identifiers.Add(identifier);
                }

                if (node is InvocationExpressionSyntax)
                {
                    methodCall.Add(node as InvocationExpressionSyntax);
                    throw new Exception("Method Call is not implemented yet");
                }
                if (node is AssignmentExpressionSyntax)
                    throw new Exception("Assignment is not implemented yet");
                base.Visit(node);
            }

            public SyntaxTree ReplaceVars(SyntaxTree syntaxTree, IEnumerable<JObject> ma_values, IEnumerable<JObject> id_values)
            {
                CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
                var memberAccessToParamName = new Dictionary<string, string>();

                // 1. Replace all this.a occurrences with this_a_ABDE
                root = root.ReplaceNodes(memberAccesses, (maes, _) =>
                {
                    string ma_str = maes.ToString();
                    if (!memberAccessToParamName.TryGetValue(ma_str, out string id_name))
                    {
                        // Generate a random suffix
                        string suffix = Guid.NewGuid().ToString().Substring(0, 5);
                        string prefix = ma_str.Trim().Replace(".", "_");
                        id_name = $"{prefix}_{suffix}";

                        memberAccessToParamName[ma_str] = id_name;
                    }

                    return SyntaxFactory.IdentifierName(id_name);
                });

                var paramsSet = new HashSet<string>();

                // 2. For every unique member ref, add a corresponding method param
                foreach ((MemberAccessExpressionSyntax maes, JObject value) in memberAccesses.Zip(ma_values))
                {
                    string node_str = maes.ToString();
                    if (!memberAccessToParamName.TryGetValue(node_str, out string id_name))
                    {
                        throw new Exception($"BUG: Expected to find an id name for the member access string: {node_str}");
                    }

                    root = UpdateWithNewMethodParam(root, id_name, value);
                }

                foreach ((IdentifierNameSyntax idns, JObject value) in identifiers.Zip(id_values))
                {
                    root = UpdateWithNewMethodParam(root, idns.Identifier.Text, value);
                }

                return syntaxTree.WithRootAndOptions(root, syntaxTree.Options);

                CompilationUnitSyntax UpdateWithNewMethodParam(CompilationUnitSyntax root, string id_name, JObject value)
                {
                    var classDeclaration = root.Members.ElementAt(0) as ClassDeclarationSyntax;
                    var method = classDeclaration.Members.ElementAt(0) as MethodDeclarationSyntax;

                    if (paramsSet.Contains(id_name))
                    {
                        // repeated member access expression
                        // eg. this.a + this.a
                        return root;
                    }

                    argValues.Add(ConvertJSToCSharpType(value));

                    MethodDeclarationSyntax updatedMethod = method.AddParameterListParameters(
                        SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier(id_name))
                            .WithType(SyntaxFactory.ParseTypeName(GetTypeFullName(value))));

                    paramsSet.Add(id_name);
                    root = root.ReplaceNode(method, updatedMethod);

                    return root;
                }
            }

            private object ConvertJSToCSharpType(JToken variable)
            {
                JToken value = variable["value"];
                string type = variable["type"].Value<string>();
                string subType = variable["subtype"]?.Value<string>();

                switch (type)
                {
                    case "string":
                        return value?.Value<string>();
                    case "number":
                        return value?.Value<double>();
                    case "boolean":
                        return value?.Value<bool>();
                    case "object":
                        if (subType == "null")
                            return null;
                        break;
                }
                throw new Exception($"Evaluate of this datatype {type} not implemented yet");//, "Unsupported");
            }

            private string GetTypeFullName(JToken variable)
            {
                string type = variable["type"].ToString();
                string subType = variable["subtype"]?.Value<string>();
                object value = ConvertJSToCSharpType(variable);

                switch (type)
                {
                    case "object":
                        {
                            if (subType == "null")
                                return variable["className"].Value<string>();
                            break;
                        }
                    default:
                        return value.GetType().FullName;
                }
                throw new ReturnAsErrorException($"GetTypefullName: Evaluate of this datatype {type} not implemented yet", "Unsupported");
            }
        }

        private static SyntaxNode GetExpressionFromSyntaxTree(SyntaxTree syntaxTree)
        {
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            ClassDeclarationSyntax classDeclaration = root.Members.ElementAt(0) as ClassDeclarationSyntax;
            MethodDeclarationSyntax methodDeclaration = classDeclaration.Members.ElementAt(0) as MethodDeclarationSyntax;
            BlockSyntax blockValue = methodDeclaration.Body;
            ReturnStatementSyntax returnValue = blockValue.Statements.ElementAt(0) as ReturnStatementSyntax;
            ParenthesizedExpressionSyntax expressionParenthesized = returnValue.Expression as ParenthesizedExpressionSyntax;

            return expressionParenthesized?.Expression;
        }

        private static async Task<IList<JObject>> ResolveMemberAccessExpressions(IEnumerable<MemberAccessExpressionSyntax> member_accesses,
                                MemberReferenceResolver resolver, CancellationToken token)
        {
            var memberAccessValues = new List<JObject>();
            foreach (MemberAccessExpressionSyntax maes in member_accesses)
            {
                string memberAccessString = maes.ToString();
                JObject value = await resolver.Resolve(memberAccessString, token);
                if (value == null)
                    throw new ReturnAsErrorException($"Failed to resolve member access for {memberAccessString}", "ReferenceError");

                memberAccessValues.Add(value);
            }

            return memberAccessValues;
        }

        private static async Task<IList<JObject>> ResolveIdentifiers(IEnumerable<IdentifierNameSyntax> identifiers, MemberReferenceResolver resolver, CancellationToken token)
        {
            var values = new List<JObject>();
            foreach (IdentifierNameSyntax var in identifiers)
            {
                JObject value = await resolver.Resolve(var.Identifier.Text, token);
                if (value == null)
                    throw new ReturnAsErrorException($"The name {var.Identifier.Text} does not exist in the current context", "ReferenceError");

                values.Add(value);
            }

            return values;
        }

        internal static async Task<JObject> CompileAndRunTheExpression(string expression, MemberReferenceResolver resolver, CancellationToken token)
        {
            expression = expression.Trim();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
                using System;
                public class CompileAndRunTheExpression
                {
                    public static object Evaluate()
                    {
                        return (" + expression + @");
                    }
                }", cancellationToken: token);

            SyntaxNode expressionTree = GetExpressionFromSyntaxTree(syntaxTree);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");

            FindVariableNMethodCall findVarNMethodCall = new FindVariableNMethodCall();
            findVarNMethodCall.Visit(expressionTree);

            // this fails with `"a)"`
            // because the code becomes: return (a));
            // and the returned expression from GetExpressionFromSyntaxTree is `a`!
            if (expressionTree.Kind() == SyntaxKind.IdentifierName || expressionTree.Kind() == SyntaxKind.ThisExpression)
            {
                string var_name = expressionTree.ToString();
                JObject value = await resolver.Resolve(var_name, token);
                if (value == null)
                    throw new ReturnAsErrorException($"Cannot find member named '{var_name}'.", "ReferenceError");

                return value;
            }

            IList<JObject> memberAccessValues = await ResolveMemberAccessExpressions(findVarNMethodCall.memberAccesses, resolver, token);

            // eg. "this.dateTime", "  dateTime.TimeOfDay"
            if (expressionTree.Kind() == SyntaxKind.SimpleMemberAccessExpression && findVarNMethodCall.memberAccesses.Count == 1)
            {
                return memberAccessValues[0];
            }

            IList<JObject> identifierValues = await ResolveIdentifiers(findVarNMethodCall.identifiers, resolver, token);

            syntaxTree = findVarNMethodCall.ReplaceVars(syntaxTree, memberAccessValues, identifierValues);
            expressionTree = GetExpressionFromSyntaxTree(syntaxTree);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");

            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "compileAndRunTheExpression",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(expressionTree, cancellationToken: token);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms, cancellationToken: token);
                if (!result.Success)
                {
                    var sb = new StringBuilder();
                    foreach (Diagnostic d in result.Diagnostics)
                        sb.Append(d.ToString());

                    throw new ReturnAsErrorException(sb.ToString(), "CompilationError");
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type type = assembly.GetType("CompileAndRunTheExpression");

                object ret = type.InvokeMember("Evaluate",
                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                    null,
                    null,
                    findVarNMethodCall.argValues.ToArray());

                return JObject.FromObject(ConvertCSharpToJSType(ret, typeInfo.Type));
            }
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(decimal), typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(float), typeof(double)
        };

        private static object ConvertCSharpToJSType(object v, ITypeSymbol type)
        {
            if (v == null)
                return new { type = "object", subtype = "null", className = type.ToString() };

            if (v is string s)
            {
                return new { type = "string", value = s, description = s };
            }
            else if (NumericTypes.Contains(v.GetType()))
            {
                return new { type = "number", value = v, description = v.ToString() };
            }
            else
            {
                return new { type = "object", value = v, description = v.ToString(), className = type.ToString() };
            }
        }

    }

    internal class ReturnAsErrorException : Exception
    {
        public Result Error { get; }
        public ReturnAsErrorException(JObject error)
            => Error = Result.Err(error);

        public ReturnAsErrorException(string message, string className)
        {
            Error = Result.Err(JObject.FromObject(new
            {
                result = new
                {
                    type = "object",
                    subtype = "error",
                    description = message,
                    className
                }
            }));
        }
    }
}
