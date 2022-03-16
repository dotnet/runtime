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
using BrowserDebugProxy;

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

            IList<JObject> memberAccessValues = await ResolveMemberAccessExpressions(replacer.memberAccesses, resolver, token);

            // eg. "this.dateTime", "  dateTime.TimeOfDay"
            if (expressionTree.Kind() == SyntaxKind.SimpleMemberAccessExpression && replacer.memberAccesses.Count == 1)
            {
                return memberAccessValues[0];
            }

            IList<JObject> identifierValues = await ResolveIdentifiers(replacer.identifiers, resolver, token);

            syntaxTree = replacer.ReplaceVars(syntaxTree, memberAccessValues, identifierValues, null, null);

            if (replacer.hasMethodCalls)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                replacer.VisitInternal(expressionTree);

                IList<JObject> methodValues = await ResolveMethodCalls(replacer.methodCall, replacer.memberAccessValues, resolver, token);

                syntaxTree = replacer.ReplaceVars(syntaxTree, null, null, methodValues, null);
            }

            // eg. "elements[0]"
            if (replacer.hasElementAccesses)
            {
                expressionTree = syntaxTree.GetCompilationUnitRoot(token);

                replacer.VisitInternal(expressionTree);

                IList<JObject> elementAccessValues = await ResolveElementAccess(replacer.elementAccess, replacer.memberAccessValues, resolver, token);

                syntaxTree = replacer.ReplaceVars(syntaxTree, null, null, null, elementAccessValues);
            }

            expressionTree = syntaxTree.GetCompilationUnitRoot(token);
            if (expressionTree == null)
                throw new Exception($"BUG: Unable to evaluate {expression}, could not get expression from the syntax tree");

            try
            {
                var newScript = script.ContinueWith(
                    string.Join("\n", replacer.variableDefinitions) + "\nreturn " + syntaxTree.ToString());

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
            if (v is bool)
                return new { type = "boolean", value = v, description = v.ToString().ToLower(), className = type.ToString() };
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
