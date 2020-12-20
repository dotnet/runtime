// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;

#endregion

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.
#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

// This class is used to validate the content of a strongly typed grammar. It is loaded in an app domain.

namespace System.Speech.Internal.SrgsCompiler
{
    /// <summary>
    /// TODOC
    /// </summary>
    internal class AppDomainCompilerProxy : MarshalByRefObject
    {
        // This method is used. It is referenced through reflection
        internal Exception CheckAssembly(byte[] il, int iCfg, string language, string nameSpace, string[] ruleNames, string[] methodNames, int[] methodScripts)
        {
            try
            {
                Assembly assembly = Assembly.Load(il);

                // Allocate the array of string for all the constructors
                _constructors = new string[ruleNames.Length];

                // Validate the rule scripts definition
                for (int i = 0, count = ruleNames.Length; i < count; i++)
                {
                    string sRule = ruleNames[i];
                    string sMethod = methodNames[i];
                    _constructors[i] = string.Empty;

                    // Get the class defition
                    string classname = (!string.IsNullOrEmpty(nameSpace) ? nameSpace + "." : string.Empty) + sRule;
                    Type typeClass = assembly.GetType(classname);

                    if (typeClass == null)
                    {
                        XmlParser.ThrowSrgsException(SRID.CannotFindClass, sRule, nameSpace);
                    }

                    // Make sure that it derives from Grammar
                    if (!(typeClass.IsSubclassOf(typeof(System.Speech.Recognition.Grammar))))
                    {
                        XmlParser.ThrowSrgsException(SRID.StrongTypedGrammarNotAGrammar, classname, nameSpace);
                    }

                    // Get all the method names to check the parameters
                    MethodInfo[] methods = typeClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    ScriptRefStruct ruleScript = new ScriptRefStruct(sRule, (RuleMethodScript)methodScripts[i]);
                    bool found = false;

                    for (int iMethod = 0; iMethod < methods.Length; iMethod++)
                    {
                        MethodInfo method = methods[iMethod];
                        if (method.Name == sMethod)
                        {
                            // Check for the parameters
                            ParameterInfo[] args = method.GetParameters();
                            Type returnType = null;
                            switch (ruleScript._method)
                            {
                                case RuleMethodScript.onInit:
                                    // Add the parameters for the new overload
                                    _constructors[i] += GenerateConstructor(iCfg, args, language, sRule);
                                    // build the returned type 
                                    returnType = typeof(SrgsRule[]);
                                    break;

                                case RuleMethodScript.onParse:
                                    ThrowIfMultipleOverloads(found, sMethod);
                                    if (args.Length != 2 || args[0].ParameterType != typeof(SemanticValue) || args[1].ParameterType != typeof(RecognizedWordUnit[]))
                                    {
                                        XmlParser.ThrowSrgsException(SRID.RuleScriptInvalidParameters, sMethod, ruleScript._rule);
                                    }
                                    returnType = typeof(object);
                                    break;

                                case RuleMethodScript.onRecognition:
                                    ThrowIfMultipleOverloads(found, sMethod);
                                    if (args.Length != 1 || args[0].ParameterType != typeof(RecognitionResult))
                                    {
                                        XmlParser.ThrowSrgsException(SRID.RuleScriptInvalidParameters, sMethod, ruleScript._rule);
                                    }
                                    returnType = typeof(object);
                                    break;

                                case RuleMethodScript.onError:
                                    ThrowIfMultipleOverloads(found, sMethod);
                                    if (args.Length != 1 || args[0].ParameterType != typeof(Exception))
                                    {
                                        XmlParser.ThrowSrgsException(SRID.RuleScriptInvalidParameters, sMethod, ruleScript._rule);
                                    }
                                    returnType = typeof(void);
                                    break;
                            }

                            // Check for the return type
                            if (method.ReturnType != returnType)
                            {
                                XmlParser.ThrowSrgsException(SRID.RuleScriptInvalidReturnType, sMethod, ruleScript._rule);
                            }

                            found = true;
                        }
                    }
                    if (!found)
                    {
                        XmlParser.ThrowSrgsException(SRID.RuleScriptNotFound, sMethod, ruleScript._rule, ruleScript._method.ToString());
                    }

                    // The class needs to be public
                    if (!typeClass.IsPublic)
                    {
                        XmlParser.ThrowSrgsException(SRID.ClassNotPublic, sRule);
                    }
                }
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        internal string[] Constructors()
        {
            return _constructors;
        }
        internal string GenerateConstructor(int iCfg, ParameterInfo[] parameters, string language, string classname)
        {
            string script = string.Empty;
            // Select an instance of a compiler and wrap the script code with the 
            // class definition
            switch (language)
            {
                case "C#":
                    script = WrapConstructorCSharp(iCfg, parameters, classname);
                    break;

                case "VB.Net":
                    script = WrapConstructorVB(iCfg, parameters, classname);
                    break;

                default:
                    XmlParser.ThrowSrgsException(SRID.UnsupportedLanguage, language);
                    break;
            }

            return script;
        }

        static private void ThrowIfMultipleOverloads(bool found, string method)
        {
            if (found)
            {
                XmlParser.ThrowSrgsException(SRID.OverloadNotAllowed, method);
            }
        }

        static private string WrapConstructorCSharp(int iCfg, ParameterInfo[] parameters, string classname)
        {
            StringBuilder sb = new StringBuilder(200);
            sb.Append(" public ");
            sb.Append(classname);
            sb.Append(" (");
            if (parameters != null)
            {
                int i = 0;
                foreach (ParameterInfo arg in parameters)
                {
                    if (i++ > 0)
                    {
                        sb.Append(", ");
                    }

                    if (i == parameters.Length && arg.ParameterType.IsArray)
                    {
                        object[] customAttributes = arg.GetCustomAttributes(false);
                        foreach (object attribute in customAttributes)
                        {
                            if (attribute is ParamArrayAttribute)
                            {
                                sb.Append("params ");
                                break;
                            }
                        }
                    }
                    sb.Append(arg.ParameterType.FullName);
                    sb.Append(" ");
                    sb.Append(arg.Name);
                }
            }
            sb.Append(" )\n  {\n object [] onInitParams = new object [");
            sb.Append(parameters == null ? 0 : parameters.Length);
            sb.Append("];\n");

            for (int iArg = 0; parameters != null && iArg < parameters.Length; iArg++)
            {
                sb.Append("onInitParams [");
                sb.Append(iArg);
                sb.Append("] = ");
                sb.Append(parameters[iArg].Name);
                sb.Append(";\n");
            }
            sb.Append("ResourceName = \"");
            sb.Append(iCfg.ToString(CultureInfo.InvariantCulture));
            sb.Append(".CFG\";\nStgInit (onInitParams);");
            sb.Append("\n  } \n");
            return sb.ToString();
        }

        static private string WrapConstructorVB(int iCfg, ParameterInfo[] parameters, string classname)
        {
            StringBuilder sb = new StringBuilder(200);
            sb.Append("Public Sub New");
            sb.Append(" (");
            if (parameters != null)
            {
                int i = 0;
                foreach (ParameterInfo arg in parameters)
                {
                    if (i++ > 0)
                    {
                        sb.Append(", ");
                    }

                    if (!arg.ParameterType.IsByRef)
                    {
                        sb.Append("ByVal ");
                    }
                    if (i == parameters.Length && arg.ParameterType.IsArray)
                    {
                        object[] customAttributes = arg.GetCustomAttributes(false);
                        foreach (object attribute in customAttributes)
                        {
                            if (attribute is ParamArrayAttribute)
                            {
                                sb.Append("ParamArray ");
                                break;
                            }
                        }
                    }
                    sb.Append(arg.Name);
                    if (arg.ParameterType.IsArray)
                    {
                        sb.Append("()");
                    }
                    sb.Append(" as ");
                    sb.Append(arg.ParameterType.Name);
                }
            }
            sb.Append(" )\n  Dim onInitParams () as Object = {");

            for (int iArg = 0; parameters != null && iArg < parameters.Length; iArg++)
            {
                if (iArg > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(parameters[iArg].Name);
            }
            sb.Append("}\n");

            sb.Append("ResourceName = \"");
            sb.Append(iCfg.ToString(CultureInfo.InvariantCulture));
            sb.Append(".CFG\"\nStgInit (onInitParams)\n");
            sb.Append("\nEnd Sub \n");
            return sb.ToString();
        }

        internal string[] _constructors;

        //*******************************************************************
        //
        // Private Types
        //
        //*******************************************************************

        #region Private Types

        /// <summary>
        /// Summary description for ScriptRef.
        /// </summary>
        // list of rules with scripts
        private class ScriptRefStruct
        {
            //*******************************************************************
            //
            // Constructors
            //
            //*******************************************************************

            #region Constructors

            internal ScriptRefStruct(string rule, RuleMethodScript method)
            {
                _rule = rule;
                _method = method;
            }

            #endregion

            //*******************************************************************
            //
            // Internal Fields
            //
            //*******************************************************************

            #region Internal Fields

            internal string _rule;

            internal RuleMethodScript _method;

            #endregion
        }

        #endregion
    }
}
