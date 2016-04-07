// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: An implementation of a CodeDomProvider.
**
**
=============================================================================*/

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;

namespace System.CodeDom.Compiler
{
    internal abstract class CodeDomProvider
    {
        public abstract string CreateEscapedIdentifier(string name);
    }
}

namespace Microsoft.CSharp
{
    // Ported from \ndp\fx\src\CompMod\Microsoft\CSharp\CSharpCodeProvider.cs
    internal class CSharpCodeProvider : CodeDomProvider
    {
        public override string CreateEscapedIdentifier(string name)
        {
            // Any identifier started with two consecutive underscores are 
            // reserved by CSharp.
            if (IsKeyword(name) || IsPrefixTwoUnderscore(name))
            {
                return "@" + name;
            }
            return name;
        }

        private static bool IsKeyword(string value)
        {
            switch (value)
            {
                case "as":
                case "do":
                case "if":
                case "in":
                case "is":
                case "for":
                case "int":
                case "new":
                case "out":
                case "ref":
                case "try":
                case "base":
                case "bool":
                case "byte":
                case "case":
                case "char":
                case "else":
                case "enum":
                case "goto":
                case "lock":
                case "long":
                case "null":
                case "this":
                case "true":
                case "uint":
                case "void":
                case "break":
                case "catch":
                case "class":
                case "const":
                case "event":
                case "false":
                case "fixed":
                case "float":
                case "sbyte":
                case "short":
                case "throw":
                case "ulong":
                case "using":
                case "where":
                case "while":
                case "yield":
                case "double":
                case "extern":
                case "object":
                case "params":
                case "public":
                case "return":
                case "sealed":
                case "sizeof":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "typeof":
                case "unsafe":
                case "ushort":
                case "checked":
                case "decimal":
                case "default":
                case "finally":
                case "foreach":
                case "partial":
                case "private":
                case "virtual":
                case "abstract":
                case "continue":
                case "delegate":
                case "explicit":
                case "implicit":
                case "internal":
                case "operator":
                case "override":
                case "readonly":
                case "volatile":
                case "__arglist":
                case "__makeref":
                case "__reftype":
                case "interface":
                case "namespace":
                case "protected":
                case "unchecked":
                case "__refvalue":
                case "stackalloc":
                    return true;
            }
            return false;
        }

        private static bool IsPrefixTwoUnderscore(string value)
        {
            if (value.Length < 3)
            {
                return false;
            }
            else
            {
                return ((value[0] == '_') && (value[1] == '_') && (value[2] != '_'));
            }
        }
    }
}
