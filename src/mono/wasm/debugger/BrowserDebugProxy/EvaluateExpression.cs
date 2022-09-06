// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal static class ExpressionEvaluator
    {
        internal static Script<object> script = CSharpScript.Create(
            "",
            ScriptOptions.Default.WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(JObject).Assembly
                ));
        private sealed class ExpressionSyntaxReplacer : CSharpSyntaxWalker
        {
            private static Regex regexForReplaceVarName = new Regex(@"[^A-Za-z0-9_]", RegexOptions.Singleline);
            public List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();
            public List<InvocationExpressionSyntax> methodCalls = new List<InvocationExpressionSyntax>();
            public List<MemberAccessExpressionSyntax> memberAccesses = new List<MemberAccessExpressionSyntax>();
            public List<ElementAccessExpressionSyntax> elementAccess = new List<ElementAccessExpressionSyntax>();
            public List<object> argValues = new List<object>();
            public Dictionary<string, JObject> memberAccessValues = new Dictionary<string, JObject>();
            private int visitCount;
            public bool hasMethodCalls;
            public bool hasElementAccesses;
            internal List<string> variableDefinitions = new List<string>();

            public void VisitInternal(SyntaxNode node)
            {
                Visit(node);
                visitCount++;
            }
            public override void Visit(SyntaxNode node)
            {
                // TODO: PointerMemberAccessExpression
                if (visitCount == 0)
                {
                    if (node is MemberAccessExpressionSyntax maes
                        && node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                        && !(node.Parent is MemberAccessExpressionSyntax)
                        && !(node.Parent is InvocationExpressionSyntax)
                        && !(node.Parent is ElementAccessExpressionSyntax))
                    {
                        memberAccesses.Add(maes);
                    }

                    if (node is IdentifierNameSyntax identifier
                        && !(identifier.Parent is MemberAccessExpressionSyntax)
                        && !(identifier.Parent is InvocationExpressionSyntax)
                        && !(node.Parent is ElementAccessExpressionSyntax)
                        && !identifiers.Any(x => x.Identifier.Text == identifier.Identifier.Text))
                    {
                        identifiers.Add(identifier);
                    }
                }

                if (node is InvocationExpressionSyntax)
                {
                    if (visitCount == 1)
                        methodCalls.Add(node as InvocationExpressionSyntax);
                    hasMethodCalls = true;
                }

                if (node is ElementAccessExpressionSyntax)
                {
                    if (visitCount == 1)
                        elementAccess.Add(node as ElementAccessExpressionSyntax);
                    hasElementAccesses = true;
                }

                if (node is AssignmentExpressionSyntax)
                    throw new Exception("Assignment is not implemented yet");
                base.Visit(node);
            }

            public SyntaxTree ReplaceVars(SyntaxTree syntaxTree, IEnumerable<JObject> ma_values, IEnumerable<JObject> id_values, IEnumerable<JObject> method_values, IEnumerable<JObject> ea_values)
            {
                var memberAccessToParamName = new Dictionary<string, string>();
                var methodCallToParamName = new Dictionary<string, string>();
                var elementAccessToParamName = new Dictionary<string, string>();

                CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

                // 1. Replace all this.a occurrences with this_a_ABDE
                root = root.ReplaceNodes(memberAccesses, (maes, _) =>
                {
                    string ma_str = maes.ToString();
                    if (!memberAccessToParamName.TryGetValue(ma_str, out string id_name))
                    {
                        // Generate a random suffix
                        string suffix = Guid.NewGuid().ToString().Substring(0, 5);
                        string prefix = regexForReplaceVarName.Replace(ma_str, "_");
                        id_name = $"{prefix}_{suffix}";

                        memberAccessToParamName[ma_str] = id_name;
                    }

                    return SyntaxFactory.IdentifierName(id_name);
                });

                 // 1.1 Replace all this.a() occurrences with this_a_ABDE
                root = root.ReplaceNodes(methodCalls, (m, _) =>
                {
                    string iesStr = m.ToString();
                    if (!methodCallToParamName.TryGetValue(iesStr, out string id_name))
                    {
                        // Generate a random suffix
                        string suffix = Guid.NewGuid().ToString().Substring(0, 5);
                        string prefix = regexForReplaceVarName.Replace(iesStr, "_");
                        id_name = $"{prefix}_{suffix}";
                        methodCallToParamName[iesStr] = id_name;
                    }

                    return SyntaxFactory.IdentifierName(id_name);
                });

                // 1.2 Replace all this.a[x] occurrences with this_a_ABDE
                root = root.ReplaceNodes(elementAccess, (ea, _) =>
                {
                    string eaStr = ea.ToString();
                    if (!elementAccessToParamName.TryGetValue(eaStr, out string id_name))
                    {
                        // Generate a random suffix
                        string suffix = Guid.NewGuid().ToString().Substring(0, 5);
                        string prefix = regexForReplaceVarName.Replace(eaStr, "_");
                        id_name = $"{prefix}_{suffix}";
                        elementAccessToParamName[eaStr] = id_name;
                    }

                    return SyntaxFactory.IdentifierName(id_name);
                });

                var localsSet = new HashSet<string>();

                // 2. For every unique member ref, add a corresponding method param
                if (ma_values != null)
                {
                    foreach ((MemberAccessExpressionSyntax maes, JObject value) in memberAccesses.Zip(ma_values))
                    {
                        string node_str = maes.ToString();
                        if (!memberAccessToParamName.TryGetValue(node_str, out string id_name))
                        {
                            throw new Exception($"BUG: Expected to find an id name for the member access string: {node_str}");
                        }
                        memberAccessValues[id_name] = value;
                        AddLocalVariableWithValue(id_name, value);
                    }
                    // do not replace memberAccesses that were already replaced
                    memberAccesses = new List<MemberAccessExpressionSyntax>();
                }

                if (id_values != null)
                {
                    foreach ((IdentifierNameSyntax idns, JObject value) in identifiers.Zip(id_values))
                    {
                        AddLocalVariableWithValue(idns.Identifier.Text, value);
                    }
                }

                if (method_values != null)
                {
                    foreach ((InvocationExpressionSyntax ies, JObject value) in methodCalls.Zip(method_values))
                    {
                        string node_str = ies.ToString();
                        if (!methodCallToParamName.TryGetValue(node_str, out string id_name))
                        {
                            throw new Exception($"BUG: Expected to find an id name for the member access string: {node_str}");
                        }
                        AddLocalVariableWithValue(id_name, value);
                    }
                }

                if (ea_values != null)
                {
                    foreach ((ElementAccessExpressionSyntax eas, JObject value) in elementAccess.Zip(ea_values))
                    {
                        string node_str = eas.ToString();
                        if (!elementAccessToParamName.TryGetValue(node_str, out string id_name))
                        {
                            throw new Exception($"BUG: Expected to find an id name for the element access string: {node_str}");
                        }
                        AddLocalVariableWithValue(id_name, value);
                    }
                }

                return syntaxTree.WithRootAndOptions(root, syntaxTree.Options);

                void AddLocalVariableWithValue(string idName, JObject value)
                {
                    if (localsSet.Contains(idName))
                        return;
                    localsSet.Add(idName);
                    variableDefinitions.Add(ConvertJSToCSharpLocalVariableAssignment(idName, value));
                }
            }
        }

        public static string ConvertJSToCSharpLocalVariableAssignment(string idName, JToken variable)
        {
            string typeRet;
            object valueRet;
            JToken value = variable["value"];
            string type = variable["type"].Value<string>();
            string subType = variable["subtype"]?.Value<string>();
            switch (type)
            {
                case "string":
                    {
                        var str = value?.Value<string>();
                        str = str.Replace("\"", "\\\"");
                        valueRet = $"\"{str}\"";
                        typeRet = "string";
                        break;
                    }
                case "symbol":
                    {
                        valueRet = $"'{value?.Value<char>()}'";
                        typeRet = "char";
                        break;
                    }
                case "number":
                    //casting to double and back to string would loose precision; so casting straight to string
                    valueRet = value?.Value<string>();
                    typeRet = "double";
                    break;
                case "boolean":
                    valueRet = value?.Value<string>().ToLowerInvariant();
                    typeRet = "bool";
                    break;
                case "object":
                    if (variable["subtype"]?.Value<string>() == "null")
                    {
                        (valueRet, typeRet) = GetNullObject(variable["className"]?.Value<string>());
                    }
                    else
                    {
                        if (!DotnetObjectId.TryParse(variable["objectId"], out DotnetObjectId objectId))
                            throw new Exception($"Internal error: Cannot parse objectId for var {idName}, with value: {variable}");

                        switch (objectId?.Scheme)
                        {
                            case "valuetype" when variable["isEnum"]?.Value<bool>() == true:
                                typeRet = variable["className"]?.Value<string>();
                                valueRet = $"({typeRet}) {value["value"].Value<double>()}";
                                break;
                            case "object":
                            default:
                                valueRet = "Newtonsoft.Json.Linq.JObject.FromObject(new {"
                                        + $"type = \"{type}\""
                                        + $", description = \"{variable["description"].Value<string>()}\""
                                        + $", className = \"{variable["className"].Value<string>()}\""
                                        + (subType != null ? $", subtype = \"{subType}\"" : "")
                                        + (objectId != null ? $", objectId = \"{objectId}\"" : "")
                                        + "})";
                                typeRet = "object";
                                break;
                        }
                    }
                    break;
                case "void":
                    (valueRet, typeRet) = GetNullObject("object");
                    break;
                default:
                    throw new Exception($"Evaluate of this datatype {type} not implemented yet");//, "Unsupported");
            }
            return $"{typeRet} {idName} = {valueRet};";

            static (string, string) GetNullObject(string className = "object")
                => ("Newtonsoft.Json.Linq.JObject.FromObject(new {"
                + $"type = \"object\","
                + $"description = \"object\","
                + $"className = \"{className}\","
                + $"subtype = \"null\""
                + "})",
                "object");
        }

        private static async Task<IList<JObject>> Resolve<T>(IList<T> collectionToResolve, MemberReferenceResolver resolver,
                                Func<T, MemberReferenceResolver, CancellationToken, Task<JObject>> resolutionFunc, CancellationToken token)
        {
            IList<JObject> values = new List<JObject>();
            foreach (T element in collectionToResolve)
                values.Add(await resolutionFunc(element, resolver, token));
            return values;
        }

        private static async Task<JObject> ResolveMemberAccessExpression(MemberAccessExpressionSyntax memberAccess,
                                MemberReferenceResolver resolver, CancellationToken token)
        {
            string memberAccessString = memberAccess.ToString();
            JObject value = await resolver.Resolve(memberAccessString, token);
            return value ?? throw new ReturnAsErrorException($"Failed to resolve member access for {memberAccessString}", "ReferenceError");
        }

        private static async Task<JObject> ResolveIdentifier(IdentifierNameSyntax identifier,
                                MemberReferenceResolver resolver, CancellationToken token)
        {
            JObject value = await resolver.Resolve(identifier.Identifier.Text, token);
            return value ?? throw new ReturnAsErrorException($"The name {identifier.Identifier.Text} does not exist in the current context", "ReferenceError");
        }

        private static async Task<(IList<JObject>, IList<JObject>, IList<JObject>)> ResolveMethodCalls(ExpressionSyntaxReplacer replacer, MemberReferenceResolver resolver, CancellationToken token)
        {
            var methodCallValues = new List<JObject>(capacity: replacer.methodCalls.Count);
            // used for replacing method call on primitive:
            var maesValues = new List<JObject>(capacity: replacer.methodCalls.Count);
            var identifierValues = new List<JObject>(capacity: replacer.methodCalls.Count);
            InvocationExpressionSyntax[] methodCallsCopy = replacer.methodCalls.ToArray();
            foreach (InvocationExpressionSyntax methodCall in methodCallsCopy)
            {
                JObject value = await resolver.Resolve(methodCall, replacer.memberAccessValues, token);
                if (value == null)
                {
                    await ReplaceMethodCall(methodCall);
                    continue;
                }
                methodCallValues.Add(value);
            }
            return (methodCallValues, maesValues, identifierValues);

            async Task ReplaceMethodCall(InvocationExpressionSyntax method)
            {
                /*
                    Instead of invoking the method on the primitive type in the runtime,
                    we emit a local for the primitive, and emit the method call itself
                    in the script. For example:
                    double test_propUlong_2c64c = 12;
                    return (test_propUlong_2c64c.ToString());
                */
                replacer.methodCalls.Remove(method);
                if (method.Expression is MemberAccessExpressionSyntax mses)
                {
                    // primitive is a member field:
                    if (mses.Expression is MemberAccessExpressionSyntax msesExpr)
                    {
                        replacer.memberAccesses.Add(msesExpr);
                        maesValues.Add(await ResolveMemberAccessExpression(msesExpr, resolver, token));
                    }
                    // primitive is a local value:
                    else if (mses.Expression is IdentifierNameSyntax identifierExpr)
                    {
                        replacer.identifiers.Add(identifierExpr);
                        identifierValues.Add(await ResolveIdentifier(identifierExpr, resolver, token));
                    }
                }
            }
        }

        private static async Task<IList<JObject>> ResolveElementAccess(ExpressionSyntaxReplacer replacer, MemberReferenceResolver resolver, CancellationToken token)
        {
            var values = new List<JObject>();
            JObject index = null;
            IEnumerable<ElementAccessExpressionSyntax> elementAccesses = replacer.elementAccess;
            foreach (ElementAccessExpressionSyntax elementAccess in elementAccesses.Reverse())
            {
                index = await resolver.Resolve(elementAccess, replacer.memberAccessValues, index, replacer.variableDefinitions, token);
                if (index == null)
                    throw new ReturnAsErrorException($"Failed to resolve element access for {elementAccess}", "ReferenceError");
            }
            values.Add(index);
            return values;
        }

        internal static async Task<JObject> CompileAndRunTheExpression(
            string expression, MemberReferenceResolver resolver, ILogger logger, CancellationToken token)
        {
            expression = expression.Trim();
            if (!expression.StartsWith('('))
            {
                expression = "(" + expression + "\n)";
            }
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(expression + @";", cancellationToken: token);

            SyntaxNode expressionTree = syntaxTree.GetCompilationUnitRoot(token);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");
            ExpressionSyntaxReplacer replacer = new ExpressionSyntaxReplacer();
            replacer.VisitInternal(expressionTree);
            // this fails with `"a)"`
            // because the code becomes: return (a));
            // and the returned expression from GetExpressionFromSyntaxTree is `a`!
            if (expressionTree.Kind() == SyntaxKind.IdentifierName || expressionTree.Kind() == SyntaxKind.ThisExpression)
            {
                string varName = expressionTree.ToString();
                JObject value = await resolver.Resolve(varName, token);
                if (value == null)
                    throw new ReturnAsErrorException($"Cannot find member named '{varName}'.", "ReferenceError");

                return value;
            }

            IList<JObject> memberAccessValues = await Resolve(replacer.memberAccesses, resolver, ResolveMemberAccessExpression, token);
            IList<JObject> identifierValues = await Resolve(replacer.identifiers, resolver, ResolveIdentifier, token);
            syntaxTree = replacer.ReplaceVars(syntaxTree, memberAccessValues, identifierValues, null, null);

            // eg. "this.dateTime", "  dateTime.TimeOfDay"
            if (expressionTree.Kind() == SyntaxKind.SimpleMemberAccessExpression && replacer.memberAccesses.Count == 1)
            {
                return memberAccessValues[0];
            }

            if (replacer.hasMethodCalls)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                replacer.VisitInternal(expressionTree);

                (IList<JObject> methodValues, IList<JObject> newMemberAccessValues, IList<JObject> newIdentifierValues) =
                    await ResolveMethodCalls(replacer, resolver, token);
                syntaxTree = replacer.ReplaceVars(syntaxTree, newMemberAccessValues, newIdentifierValues, methodValues, null);
            }

            // eg. "elements[0]"
            if (replacer.hasElementAccesses)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                replacer.VisitInternal(expressionTree);

                IList<JObject> elementAccessValues = await ResolveElementAccess(replacer, resolver, token);

                syntaxTree = replacer.ReplaceVars(syntaxTree, null, null, null, elementAccessValues);
            }

            expressionTree = syntaxTree.GetCompilationUnitRoot(token);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");

            return await EvaluateSimpleExpression(resolver, syntaxTree.ToString(), expression, replacer.variableDefinitions, logger, token);
        }

        internal static async Task<JObject> EvaluateSimpleExpression(
            MemberReferenceResolver resolver, string compiledExpression, string originalExpression, List<string> variableDefinitions, ILogger logger, CancellationToken token)
        {
            Script<object> newScript = script;
            try
            {
                newScript = script.ContinueWith(string.Join("\n", variableDefinitions) + "\nreturn " + compiledExpression + ";");
                var state = await newScript.RunAsync(cancellationToken: token);
                return JObject.FromObject(resolver.ConvertCSharpToJSType(state.ReturnValue, state.ReturnValue.GetType()));
            }
            catch (CompilationErrorException cee)
            {
                logger.LogDebug($"Cannot evaluate '{originalExpression}'. Script used to compile it: {newScript.Code}{Environment.NewLine}{cee.Message}");
                throw new ReturnAsErrorException($"Cannot evaluate '{originalExpression}': {cee.Message}", "CompilationError");
            }
            catch (Exception ex)
            {
                throw new Exception($"Internal Error: Unable to run {originalExpression}, error: {ex.Message}.", ex);
            }
        }
    }

    internal sealed class ReturnAsErrorException : Exception
    {
        private Result _error;
        public Result Error
        {
            get
            {
                _error.Value["exceptionDetails"]["stackTrace"] = StackTrace;
                return _error;
            }
            set { }
        }
        public ReturnAsErrorException(JObject error) : base(error.ToString())
            => Error = Result.Err(error);

        public ReturnAsErrorException(string message, string className)
            : base($"[{className}] {message}")
        {
            var result = new
            {
                type = "object",
                subtype = "error",
                description = message,
                className
            };
            _error = Result.UserVisibleErr(JObject.FromObject(
                new
                {
                    result = result,
                    exceptionDetails = new
                    {
                        exception = result,
                        stackTrace = StackTrace
                    }
                }));
        }

        public override string ToString() => $"Error object: {Error}. {base.ToString()}";
    }
}
