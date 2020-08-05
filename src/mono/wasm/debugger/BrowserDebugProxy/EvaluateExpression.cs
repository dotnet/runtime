// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{

    internal class EvaluateExpression
    {

        class FindThisExpression : CSharpSyntaxWalker
        {
            public List<string> thisExpressions = new List<string>();
            public SyntaxTree syntaxTree;
            public FindThisExpression(SyntaxTree syntax)
            {
                syntaxTree = syntax;
            }
            public override void Visit(SyntaxNode node)
            {
                if (node is ThisExpressionSyntax)
                {
                    if (node.Parent is MemberAccessExpressionSyntax thisParent && thisParent.Name is IdentifierNameSyntax)
                    {
                        IdentifierNameSyntax var = thisParent.Name as IdentifierNameSyntax;
                        thisExpressions.Add(var.Identifier.Text);
                        var newRoot = syntaxTree.GetRoot().ReplaceNode(node.Parent, thisParent.Name);
                        syntaxTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
                        this.Visit(GetExpressionFromSyntaxTree(syntaxTree));
                    }
                }
                else
                    base.Visit(node);
            }

            public async Task CheckIfIsProperty(MonoProxy proxy, MessageId msg_id, int scope_id, CancellationToken token)
            {
                foreach (var var in thisExpressions)
                {
                    JToken value = await proxy.TryGetVariableValue(msg_id, scope_id, var, true, token);
                    if (value == null)
                        throw new Exception($"The property {var} does not exist in the current context");
                }
            }
        }

        class FindVariableNMethodCall : CSharpSyntaxWalker
        {
            public List<IdentifierNameSyntax> variables = new List<IdentifierNameSyntax>();
            public List<ThisExpressionSyntax> thisList = new List<ThisExpressionSyntax>();
            public List<InvocationExpressionSyntax> methodCall = new List<InvocationExpressionSyntax>();
            public List<object> values = new List<Object>();

            public override void Visit(SyntaxNode node)
            {
                if (node is IdentifierNameSyntax identifier && !variables.Any(x => x.Identifier.Text == identifier.Identifier.Text))
                    variables.Add(identifier);
                if (node is InvocationExpressionSyntax)
                {
                    methodCall.Add(node as InvocationExpressionSyntax);
                    throw new Exception("Method Call is not implemented yet");
                }
                if (node is AssignmentExpressionSyntax)
                    throw new Exception("Assignment is not implemented yet");
                base.Visit(node);
            }
            public async Task<SyntaxTree> ReplaceVars(SyntaxTree syntaxTree, MonoProxy proxy, MessageId msg_id, int scope_id, CancellationToken token)
            {
                CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
                foreach (var var in variables)
                {
                    ClassDeclarationSyntax classDeclaration = root.Members.ElementAt(0) as ClassDeclarationSyntax;
                    MethodDeclarationSyntax method = classDeclaration.Members.ElementAt(0) as MethodDeclarationSyntax;

                    JToken value = await proxy.TryGetVariableValue(msg_id, scope_id, var.Identifier.Text, false, token);

                    if (value == null)
                        throw new Exception($"The name {var.Identifier.Text} does not exist in the current context");

                    values.Add(ConvertJSToCSharpType(value["value"]));

                    var updatedMethod = method.AddParameterListParameters(
                        SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier(var.Identifier.Text))
                        .WithType(SyntaxFactory.ParseTypeName(GetTypeFullName(value["value"]))));
                    root = root.ReplaceNode(method, updatedMethod);
                }
                syntaxTree = syntaxTree.WithRootAndOptions(root, syntaxTree.Options);
                return syntaxTree;
            }

            private object ConvertJSToCSharpType(JToken variable)
            {
                var value = variable["value"];
                var type = variable["type"].Value<string>();
                var subType = variable["subtype"]?.Value<string>();

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
                throw new Exception($"Evaluate of this datatype {type} not implemented yet");
            }

            private string GetTypeFullName(JToken variable)
            {
                var type = variable["type"].ToString();
                var subType = variable["subtype"]?.Value<string>();
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
                throw new Exception($"Evaluate of this datatype {type} not implemented yet");
            }
        }

        static SyntaxNode GetExpressionFromSyntaxTree(SyntaxTree syntaxTree)
        {
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            ClassDeclarationSyntax classDeclaration = root.Members.ElementAt(0) as ClassDeclarationSyntax;
            MethodDeclarationSyntax methodDeclaration = classDeclaration.Members.ElementAt(0) as MethodDeclarationSyntax;
            BlockSyntax blockValue = methodDeclaration.Body;
            ReturnStatementSyntax returnValue = blockValue.Statements.ElementAt(0) as ReturnStatementSyntax;
            InvocationExpressionSyntax expressionInvocation = returnValue.Expression as InvocationExpressionSyntax;
            MemberAccessExpressionSyntax expressionMember = expressionInvocation.Expression as MemberAccessExpressionSyntax;
            ParenthesizedExpressionSyntax expressionParenthesized = expressionMember.Expression as ParenthesizedExpressionSyntax;
            return expressionParenthesized.Expression;
        }

        internal static async Task<string> CompileAndRunTheExpression(MonoProxy proxy, MessageId msg_id, int scope_id, string expression, CancellationToken token)
        {
            FindVariableNMethodCall findVarNMethodCall = new FindVariableNMethodCall();
            string retString;
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
				using System;
				public class CompileAndRunTheExpression
				{
					public string Evaluate()
					{
						return (" + expression + @").ToString(); 
					}
				}");

            FindThisExpression findThisExpression = new FindThisExpression(syntaxTree);
            var expressionTree = GetExpressionFromSyntaxTree(syntaxTree);
            findThisExpression.Visit(expressionTree);
            await findThisExpression.CheckIfIsProperty(proxy, msg_id, scope_id, token);
            syntaxTree = findThisExpression.syntaxTree;

            expressionTree = GetExpressionFromSyntaxTree(syntaxTree);
            findVarNMethodCall.Visit(expressionTree);

            syntaxTree = await findVarNMethodCall.ReplaceVars(syntaxTree, proxy, msg_id, scope_id, token);

            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "compileAndRunTheExpression",
                syntaxTrees : new [] { syntaxTree },
                references : references,
                options : new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using(var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type type = assembly.GetType("CompileAndRunTheExpression");
                object obj = Activator.CreateInstance(type);
                var ret = type.InvokeMember("Evaluate",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                    null,
                    obj,
                    findVarNMethodCall.values.ToArray());
                retString = ret.ToString();
            }
            return retString;
        }
    }
}