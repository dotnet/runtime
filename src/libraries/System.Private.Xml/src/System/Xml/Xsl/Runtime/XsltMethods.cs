// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    // List of all XPath/XSLT runtime methods
    internal static class XsltMethods
    {
        // Formatting error messages
        public static readonly MethodInfo FormatMessage = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.FormatMessage))!;

        // Runtime type checks and casts
        public static readonly MethodInfo EnsureNodeSet = typeof(XsltConvert)
            .GetMethod(nameof(XsltConvert.EnsureNodeSet), new[] { typeof(IList<XPathItem>) })!;

        // Comparisons
        public static readonly MethodInfo EqualityOperator = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.EqualityOperator))!;
        public static readonly MethodInfo RelationalOperator = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.RelationalOperator))!;

        // XPath functions
        public static readonly MethodInfo StartsWith = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.StartsWith))!;
        public static readonly MethodInfo Contains = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Contains))!;
        public static readonly MethodInfo SubstringBefore = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.SubstringBefore))!;
        public static readonly MethodInfo SubstringAfter = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.SubstringAfter))!;
        public static readonly MethodInfo Substring2 = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Substring), new[] { typeof(string), typeof(double) })!;
        public static readonly MethodInfo Substring3 = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Substring), new[] { typeof(string), typeof(double), typeof(double) })!;
        public static readonly MethodInfo NormalizeSpace = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.NormalizeSpace))!;
        public static readonly MethodInfo Translate = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Translate))!;
        public static readonly MethodInfo Lang = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Lang))!;
        public static readonly MethodInfo Floor = typeof(Math)
            .GetMethod(nameof(Math.Floor), new[] { typeof(double) })!;
        public static readonly MethodInfo Ceiling = typeof(Math)
            .GetMethod(nameof(Math.Ceiling), new[] { typeof(double) })!;
        public static readonly MethodInfo Round = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.Round))!;

        // XSLT functions and helper methods (static)
        public static readonly MethodInfo SystemProperty = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.SystemProperty))!;
        public static readonly MethodInfo BaseUri = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.BaseUri))!;
        public static readonly MethodInfo OuterXml = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.OuterXml))!;
        public static readonly MethodInfo OnCurrentNodeChanged = typeof(XmlQueryRuntime)
            .GetMethod(nameof(XmlQueryRuntime.OnCurrentNodeChanged))!;

        // MSXML extension functions
        public static readonly MethodInfo MSFormatDateTime = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSFormatDateTime))!;
        public static readonly MethodInfo MSStringCompare = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSStringCompare))!;
        public static readonly MethodInfo MSUtc = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSUtc))!;
        public static readonly MethodInfo MSNumber = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSNumber))!;
        public static readonly MethodInfo MSLocalName = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSLocalName))!;
        public static readonly MethodInfo MSNamespaceUri = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.MSNamespaceUri))!;

        // EXSLT functions
        public static readonly MethodInfo EXslObjectType = typeof(XsltFunctions)
            .GetMethod(nameof(XsltFunctions.EXslObjectType))!;

        // XSLT functions and helper methods (non-static)
        public static readonly MethodInfo CheckScriptNamespace = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.CheckScriptNamespace))!;
        public static readonly MethodInfo FunctionAvailable = GetFunctionAvailableMethod();
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Suppressing warning about not having the RequiresUnreferencedCode attribute since this code path " +
            "will only be emitting IL that will later be called by Transform() method which is already annotated as RequiresUnreferencedCode")]
        private static MethodInfo GetFunctionAvailableMethod() => typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.FunctionAvailable))!;
        public static readonly MethodInfo ElementAvailable = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.ElementAvailable))!;
        public static readonly MethodInfo RegisterDecimalFormat = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.RegisterDecimalFormat))!;
        public static readonly MethodInfo RegisterDecimalFormatter = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.RegisterDecimalFormatter))!;
        public static readonly MethodInfo FormatNumberStatic = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.FormatNumberStatic))!;
        public static readonly MethodInfo FormatNumberDynamic = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.FormatNumberDynamic))!;
        public static readonly MethodInfo IsSameNodeSort = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.IsSameNodeSort))!;
        public static readonly MethodInfo LangToLcid = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.LangToLcid))!;
        public static readonly MethodInfo NumberFormat = typeof(XsltLibrary)
            .GetMethod(nameof(XsltLibrary.NumberFormat))!;
    }
}
