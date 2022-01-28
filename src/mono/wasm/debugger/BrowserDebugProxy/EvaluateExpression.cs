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

namespace Microsoft.WebAssembly.Diagnostics
{
    internal static class EvaluateExpression
    {
        internal static Script<object> script = CSharpScript.Create(
            "",
            ScriptOptions.Default.WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(JObject).Assembly
                ));
        private class FindVariableNMethodCall : CSharpSyntaxWalker
        {
            private static Regex regexForReplaceVarName = new Regex(@"[^A-Za-z0-9_]", RegexOptions.Singleline);
            public List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();
            public List<InvocationExpressionSyntax> methodCall = new List<InvocationExpressionSyntax>();
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
                        methodCall.Add(node as InvocationExpressionSyntax);
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
                root = root.ReplaceNodes(methodCall, (m, _) =>
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
                        string prefix = eaStr.Trim().Replace(".", "_").Replace("[", "_").Replace("]", "_");
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
                    foreach ((InvocationExpressionSyntax ies, JObject value) in methodCall.Zip(method_values))
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

            private static string ConvertJSToCSharpLocalVariableAssignment(string idName, JToken variable)
            {
                string typeRet;
                object valueRet;
                JToken value = variable["value"];
                string type = variable["type"].Value<string>();
                string subType = variable["subtype"]?.Value<string>();
                string objectId = variable["objectId"]?.Value<string>();
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
                        valueRet = value?.Value<string>().ToLower();
                        typeRet = "bool";
                        break;
                    case "object":
                        valueRet = "Newtonsoft.Json.Linq.JObject.FromObject(new {"
                            + $"type = \"{type}\""
                            + $", description = \"{variable["description"].Value<string>()}\""
                            + $", className = \"{variable["className"].Value<string>()}\""
                            + (subType != null ? $", subtype = \"{subType}\"" : "")
                            + (objectId != null ? $", objectId = \"{objectId}\"" : "")
                            + "})";
                        typeRet = "object";
                        break;
                    case "void":
                        valueRet = "Newtonsoft.Json.Linq.JObject.FromObject(new {"
                                + $"type = \"object\","
                                + $"description = \"object\","
                                + $"className = \"object\","
                                + $"subtype = \"null\""
                                + "})";
                        typeRet = "object";
                        break;
                    default:
                        throw new Exception($"Evaluate of this datatype {type} not implemented yet");//, "Unsupported");
                }
                return $"{typeRet} {idName} = {valueRet};";
            }
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

        private static async Task<IList<JObject>> ResolveMethodCalls(IEnumerable<InvocationExpressionSyntax> methodCalls, Dictionary<string, JObject> memberAccessValues, MemberReferenceResolver resolver, CancellationToken token)
        {
            var values = new List<JObject>();
            foreach (InvocationExpressionSyntax methodCall in methodCalls)
            {
                JObject value = await resolver.Resolve(methodCall, memberAccessValues, token);
                if (value == null)
                    throw new ReturnAsErrorException($"Failed to resolve member access for {methodCall}", "ReferenceError");

                values.Add(value);
            }
            return values;
        }

        private static async Task<IList<JObject>> ResolveElementAccess(IEnumerable<ElementAccessExpressionSyntax> elementAccesses, Dictionary<string, JObject> memberAccessValues, MemberReferenceResolver resolver, CancellationToken token)
        {
            var values = new List<JObject>();
            JObject index = null;
            foreach (ElementAccessExpressionSyntax elementAccess in elementAccesses.Reverse())
            {
                index = await resolver.Resolve(elementAccess, memberAccessValues, index, token);
                if (index == null)
                    throw new ReturnAsErrorException($"Failed to resolve element access for {elementAccess}", "ReferenceError");
            }
            values.Add(index);
            return values;
        }

        internal static async Task<JObject> CompileAndRunTheExpression(string expression, MemberReferenceResolver resolver, CancellationToken token)
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
            FindVariableNMethodCall findVarNMethodCall = new FindVariableNMethodCall();
            findVarNMethodCall.VisitInternal(expressionTree);
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

            IList<JObject> memberAccessValues = await ResolveMemberAccessExpressions(findVarNMethodCall.memberAccesses, resolver, token);

            // eg. "this.dateTime", "  dateTime.TimeOfDay"
            if (expressionTree.Kind() == SyntaxKind.SimpleMemberAccessExpression && findVarNMethodCall.memberAccesses.Count == 1)
            {
                return memberAccessValues[0];
            }

            IList<JObject> identifierValues = await ResolveIdentifiers(findVarNMethodCall.identifiers, resolver, token);

            syntaxTree = findVarNMethodCall.ReplaceVars(syntaxTree, memberAccessValues, identifierValues, null, null);

            if (findVarNMethodCall.hasMethodCalls)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                findVarNMethodCall.VisitInternal(expressionTree);

                IList<JObject> methodValues = await ResolveMethodCalls(findVarNMethodCall.methodCall, findVarNMethodCall.memberAccessValues, resolver, token);

                syntaxTree = findVarNMethodCall.ReplaceVars(syntaxTree, null, null, methodValues, null);
            }

            // eg. "elements[0]"
            if (findVarNMethodCall.hasElementAccesses)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                findVarNMethodCall.VisitInternal(expressionTree);

                IList<JObject> elementAccessValues = await ResolveElementAccess(findVarNMethodCall.elementAccess, findVarNMethodCall.memberAccessValues, resolver, token);

                syntaxTree = findVarNMethodCall.ReplaceVars(syntaxTree, null, null, null, elementAccessValues);
            }

            expressionTree = syntaxTree.GetCompilationUnitRoot(token);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");

            try
            {
                var newScript = script.ContinueWith(
                    string.Join("\n", findVarNMethodCall.variableDefinitions) + "\nreturn " + syntaxTree.ToString());

                var state = await newScript.RunAsync(cancellationToken: token);
                return JObject.FromObject(ConvertCSharpToJSType(state.ReturnValue, state.ReturnValue?.GetType()));
            }
            catch (CompilationErrorException cee)
            {
                throw new ReturnAsErrorException($"Cannot evaluate '{expression}': {cee.Message}", "CompilationError");
            }
            catch (Exception ex)
            {
                throw new Exception($"Internal Error: Unable to run {expression}, error: {ex.Message}.", ex);
            }
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(decimal), typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(float), typeof(double)
        };

        private static object ConvertCSharpToJSType(object v, Type type)
        {
            if (v == null)
                return new { type = "object", subtype = "null", className = type?.ToString(), description = type?.ToString() };
            if (v is string s)
                return new { type = "string", value = s, description = s };
            if (v is char c)
                return new { type = "symbol", value = c, description = $"{(int)c} '{c}'" };
            if (NumericTypes.Contains(v.GetType()))
                return new { type = "number", value = v, description = Convert.ToDouble(v).ToString(CultureInfo.InvariantCulture) };
            if (v is JObject)
                return v;
            return new { type = "object", value = v, description = v.ToString(), className = type.ToString() };
        }

    }

    internal class ReturnAsErrorException : Exception
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
