// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal class AppDomainGrammarProxy : MarshalByRefObject
    {
        internal SrgsRule[]? OnInit(string method, object[]? parameters, string? onInitParameters, out Exception? exceptionThrown)
        {
            exceptionThrown = null;
            try
            {
                // If the onInitParameters are provided as a string, get the values as an array of value.
                if (!string.IsNullOrEmpty(onInitParameters))
                {
                    parameters = MatchInitParameters(method, onInitParameters, _rule, _rule);
                }

                // Find the constructor to call - there could be several
                Type[] types = Array.Empty<Type>();

                if (parameters != null && parameters.Length > 0)
                {
                    types = new Type[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        types[i] = parameters[i].GetType();
                    }
                }

                MethodInfo? onInit = _grammarType.GetMethod(method, types);

                // If somehow we failed to find a constructor, let the system handle it
                if (onInit == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ArgumentMismatch));
                }

                return (SrgsRule[]?)onInit?.Invoke(_grammar, parameters);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
                return null;
            }
        }

        internal object? OnRecognition(string method, object[] parameters, out Exception? exceptionThrown)
        {
            exceptionThrown = null;
            try
            {
                MethodInfo? onRecognition = _grammarType.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                // Execute the parse routine
                return onRecognition!.Invoke(_grammar, parameters);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
            }
            return null;
        }

        internal object? OnParse(string rule, string method, object[] parameters, out Exception? exceptionThrown)
        {
            exceptionThrown = null;
            try
            {
                GetRuleInstance(rule, method, out MethodInfo? onParse, out System.Speech.Recognition.Grammar grammar);

                // Execute the parse routine
                return onParse!.Invoke(grammar, parameters);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
                return null;
            }
        }

        internal void OnError(string rule, string method, object?[]? parameters, out Exception? exceptionThrown)
        {
            exceptionThrown = null;
            try
            {
                GetRuleInstance(rule, method, out MethodInfo? onError, out System.Speech.Recognition.Grammar grammar);

                // Execute the parse routine
                onError!.Invoke(grammar, parameters);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
            }
        }

        [MemberNotNull(nameof(_grammarType))]
        [MemberNotNull(nameof(_assembly))]
        [MemberNotNull(nameof(_grammar))]
        internal void Init(string? rule, byte[] il, byte[]? pdb)
        {
            _assembly = Assembly.Load(il, pdb);

            // Get the grammar class carrying the .NET Semantics code
            Type? grammarType = GetTypeForRule(_assembly, rule);

            // Something is Wrong if the grammar class cannot be found
            if (grammarType == null)
            {
                throw new FormatException(SR.Get(SRID.RecognizerRuleNotFoundStream, rule));
            }

            _grammarType = grammarType;
            _rule = rule;
            try
            {
                _grammar = (System.Speech.Recognition.Grammar)_assembly.CreateInstance(_grammarType.FullName!)!;
            }
            catch (MissingMemberException)
            {
                throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, _grammarType.FullName, rule), nameof(rule));
            }
        }

        private void GetRuleInstance(string rule, string method, out MethodInfo? onParse, out System.Speech.Recognition.Grammar grammar)
        {
            Type? ruleClass = rule == _rule ? _grammarType : GetTypeForRule(_assembly, rule);
            if (ruleClass == null || !ruleClass.IsSubclassOf(typeof(System.Speech.Recognition.Grammar)))
            {
                throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar));
            }

            try
            {
                grammar = (ruleClass == _grammarType ? _grammar : (System.Speech.Recognition.Grammar?)_assembly.CreateInstance(ruleClass.FullName!))!;
            }
            catch (MissingMemberException)
            {
                throw new ArgumentException(SR.Get(SRID.RuleScriptInvalidParameters, ruleClass.FullName, rule), nameof(rule));
            }
            onParse = grammar!.MethodInfo(method);
        }

        private static Type? GetTypeForRule(Assembly assembly, string? rule)
        {
            Type[] types = assembly.GetTypes();
            for (int iType = 0; iType < types.Length; iType++)
            {
                Type type = types[iType];
                if (type.Name == rule && type.IsPublic && type.IsSubclassOf(typeof(System.Speech.Recognition.Grammar)))
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// Construct a list of parameters from a sapi:params string.
        /// </summary>
        private object[] MatchInitParameters(string method, string onInitParameters, string? grammar, string? rule)
        {
            MethodInfo[] mis = _grammarType.GetMethods();

            NameValuePair[] pairs = ParseInitParams(onInitParameters);
            object[] values = new object[pairs.Length];
            bool foundConstructor = false;
            for (int iCtor = 0; iCtor < mis.Length && !foundConstructor; iCtor++)
            {
                if (mis[iCtor].Name != method)
                {
                    continue;
                }
                ParameterInfo[] paramInfo = mis[iCtor].GetParameters();

                // Check if enough parameters are provided.
                if (paramInfo.Length > pairs.Length)
                {
                    continue;
                }
                foundConstructor = true;
                for (int i = 0; i < pairs.Length && foundConstructor; i++)
                {
                    NameValuePair pair = pairs[i];

                    // anonymous
                    if (pair._name == null)
                    {
                        values[i] = pair._value;
                    }
                    else
                    {
                        bool foundParameter = false;
                        for (int j = 0; j < paramInfo.Length; j++)
                        {
                            if (paramInfo[j].Name == pair._name)
                            {
                                values[j] = ParseValue(paramInfo[j].ParameterType, pair._value);
                                foundParameter = true;
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            foundConstructor = false;
                        }
                    }
                }
            }
            if (!foundConstructor)
            {
                throw new FormatException(SR.Get(SRID.CantFindAConstructor, grammar, rule, FormatConstructorParameters(mis, method)));
            }
            return values;
        }

        /// <summary>
        /// Parse the value for a type from a string to a strong type.
        /// If the type does not support the Parse method then the operation fails.
        /// </summary>
        private static object ParseValue(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            return type.InvokeMember("Parse", BindingFlags.InvokeMethod, null, null, new object[] { value }, CultureInfo.InvariantCulture)!;
        }

        /// <summary>
        /// Returns the list of the possible parameter names and type for a grammar
        /// </summary>
        private static string FormatConstructorParameters(MethodInfo[] cis, string method)
        {
            StringBuilder sb = new();
            for (int iCtor = 0; iCtor < cis.Length; iCtor++)
            {
                if (cis[iCtor].Name == method)
                {
                    sb.Append(sb.Length > 0 ? " or sapi:parms=\"" : "sapi:parms=\"");
                    ParameterInfo[] pis = cis[iCtor].GetParameters();
                    for (int i = 0; i < pis.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(';');
                        }
                        ParameterInfo pi = pis[i];
                        sb.Append(pi.Name);
                        sb.Append(':');
                        sb.Append(pi.ParameterType.Name);
                    }
                    sb.Append('"');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Split the init parameter strings into an array of name/values
        /// The format must be "name:value". If the ':' then parameter is anonymous.
        /// </summary>
        private static NameValuePair[] ParseInitParams(string initParameters)
        {
            string[] parameters = initParameters.Split(';', StringSplitOptions.None);
            NameValuePair[] pairs = new NameValuePair[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                string parameter = parameters[i];
                int posColon = parameter.IndexOf(':');
                if (posColon >= 0)
                {
                    pairs[i]._name = parameter.Substring(0, posColon);
                    pairs[i]._value = parameter.Substring(posColon + 1);
                }
                else
                {
                    pairs[i]._value = parameter;
                }
            }
            return pairs;
        }

#pragma warning disable 56524 // Arclist does not hold on any resources

        private System.Speech.Recognition.Grammar _grammar = null!;

#pragma warning restore 56524 // Arclist does not hold on any resources

        private Assembly _assembly = null!;
        private string? _rule;
        private Type _grammarType = null!;

        private struct NameValuePair
        {
            internal string _name;
            internal string _value;
        }
    }
}
