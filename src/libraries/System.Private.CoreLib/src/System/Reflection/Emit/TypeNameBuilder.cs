// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//  This TypeNameBuilder is ported from CoreCLR's original.
//  It replaces the C++ bits of the implementation with a faithful C# port.

using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace System.Reflection.Emit
{
    internal class TypeNameBuilder
    {
        private enum ParseState
        {
            START = 0x0001,
            NAME = 0x0004,
            GENARGS = 0x0008,
            PTRARR = 0x0010,
            BYREF = 0x0020,
            ASSEMSPEC = 0x0080,
        }

        private ParseState parseState;
        private StringBuilder str = new StringBuilder();
        private int instNesting;
        private bool firstInstArg;
        private bool nestedName;
        private bool hasAssemblySpec;
        private bool useAngleBracketsForGenerics;
        private List<int> stack = new List<int>();
        private int stackIdx;

        public TypeNameBuilder()
        {
            parseState = ParseState.START;
        }

        public void OpenGenericArguments()
        {
            CheckParseState(ParseState.NAME);

            parseState = ParseState.START;
            instNesting++;
            firstInstArg = true;

            if (useAngleBracketsForGenerics)
                Append('<');
            else
                Append('[');
        }

        public void CloseGenericArguments()
        {
            CheckParseState(ParseState.START);

            Debug.Assert(instNesting != 0);

            parseState = ParseState.GENARGS;

            instNesting--;

            if (firstInstArg)
            {
                str.Remove(str.Length - 1, 1);
            }
            else
            {
                if (useAngleBracketsForGenerics)
                    Append('>');
                else
                    Append(']');
            }
        }

        public void OpenGenericArgument()
        {
            CheckParseState(ParseState.START);

            Debug.Assert(instNesting != 0);

            parseState = ParseState.START;
            nestedName = false;

            if (!firstInstArg)
                Append(',');

            firstInstArg = false;

            if (useAngleBracketsForGenerics)
                Append('<');
            else
                Append('[');

            PushOpenGenericArgument();
        }

        public void CloseGenericArgument()
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR | ParseState.BYREF | ParseState.ASSEMSPEC);

            Debug.Assert(instNesting != 0);

            parseState = ParseState.START;

            if (hasAssemblySpec)
            {
                if (useAngleBracketsForGenerics)
                    Append('>');
                else
                    Append(']');
            }

            PopOpenGenericArgument();
        }

        public void AddName(string name)
        {
            Debug.Assert(name != null);

            CheckParseState(ParseState.START | ParseState.NAME);

            parseState = ParseState.NAME;

            if (nestedName)
                Append('+');

            nestedName = true;

            EscapeName(name!);
        }

        public void AddPointer()
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR);

            parseState = ParseState.PTRARR;

            Append('*');
        }

        public void AddByRef()
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR);

            parseState = ParseState.BYREF;

            Append('&');
        }

        public void AddSzArray()
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR);

            parseState = ParseState.PTRARR;

            Append("[]");
        }

        public void AddArray(int rank)
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR);

            parseState = ParseState.PTRARR;

            if (rank <= 0)
                throw new ArgumentOutOfRangeException();

            if (rank == 1)
            {
                Append("[*]");
            }
            else if (rank > 64)
            {
                // Only taken in an error path, runtime will not load arrays of more than 32 dimensions
                Append($"[{rank}]");
            }
            else
            {
                Append('[');
                for (int i = 1; i < rank; i++)
                    Append(',');
                Append(']');
            }
        }

        public void AddAssemblySpec(string assemblySpec)
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR | ParseState.BYREF);

            parseState = ParseState.ASSEMSPEC;

            if (assemblySpec != null && !assemblySpec.Equals(""))
            {
                Append(", ");

                if (instNesting > 0)
                {
                    EscapeEmbeddedAssemblyName(assemblySpec);
                }
                else
                {
                    EscapeAssemblyName(assemblySpec);
                }

                hasAssemblySpec = true;
            }
        }

        public override string ToString()
        {
            CheckParseState(ParseState.NAME | ParseState.GENARGS | ParseState.PTRARR | ParseState.BYREF | ParseState.ASSEMSPEC);

            Debug.Assert(instNesting == 0);

            return str.ToString();
        }

        private static bool ContainsReservedChar(string name)
        {
            foreach (char c in name)
            {
                if (c == '\0')
                    break;
                if (IsTypeNameReservedChar(c))
                    return true;
            }
            return false;
        }

        private static bool IsTypeNameReservedChar(char ch)
        {
            switch (ch)
            {
                case ',':
                case '[':
                case ']':
                case '&':
                case '*':
                case '+':
                case '\\':
                    return true;

                default:
                    return false;
            }
        }

        private void EscapeName(string name)
        {
            if (ContainsReservedChar(name))
            {
                foreach (char c in name)
                {
                    if (c == '\0')
                        break;
                    if (TypeNameBuilder.IsTypeNameReservedChar(c))
                        str.Append('\\');
                    str.Append(c);
                }
            }
            else
                Append(name);
        }

        private void EscapeAssemblyName(string name)
        {
            Append(name);
        }

        private void EscapeEmbeddedAssemblyName(string name)
        {
            bool containsReservedChar = false;

            foreach (char c in name)
            {
                if (c == ']')
                {
                    containsReservedChar = true;
                    break;
                }
            }

            if (containsReservedChar)
            {
                foreach (char c in name)
                {
                    if (c == ']')
                        Append('\\');

                    Append(c);
                }
            }
            else
            {
                Append(name);
            }
        }

        private void CheckParseState(ParseState validState)
        {
            Debug.Assert((parseState & validState) != 0);
        }

        private void PushOpenGenericArgument()
        {
            stack.Add(str.Length);
            stackIdx++;
        }

        private void PopOpenGenericArgument()
        {
            int index = stack[--stackIdx];
            stack.RemoveAt(stackIdx);

            if (!hasAssemblySpec)
                str.Remove(index - 1, 1);

            hasAssemblySpec = false;
        }

        private void SetUseAngleBracketsForGenerics(bool value)
        {
            useAngleBracketsForGenerics = value;
        }

        private void Append(string pStr)
        {
            foreach (char c in pStr)
            {
                if (c == '\0')
                    break;
                str.Append(c);
            }
        }

        private void Append(char c)
        {
            str.Append(c);
        }

        internal enum Format
        {
            ToString,
            FullName,
            AssemblyQualifiedName,
        }

        internal static string? ToString(Type type, Format format)
        {
            if (format == Format.FullName || format == Format.AssemblyQualifiedName)
            {
                if (!type.IsGenericTypeDefinition && type.ContainsGenericParameters)
                    return null;
            }

            TypeNameBuilder tnb = new TypeNameBuilder();
            ConstructAssemblyQualifiedNameWorker(tnb, type, format);
            return tnb.ToString();
        }

        private static void AddElementType(TypeNameBuilder tnb, Type elementType)
        {
            if (elementType.HasElementType)
                AddElementType(tnb, elementType.GetElementType()!);

            if (elementType.IsPointer)
                tnb.AddPointer();
            else if (elementType.IsByRef)
                tnb.AddByRef();
            else if (elementType.IsSZArray)
                tnb.AddSzArray();
            else if (elementType.IsArray)
                tnb.AddArray(elementType.GetArrayRank());
        }

        private static void ConstructAssemblyQualifiedNameWorker(TypeNameBuilder tnb, Type type, Format format)
        {
            Type rootType = type;

            while (rootType.HasElementType)
                rootType = rootType.GetElementType()!;

            // Append namespace + nesting + name
            List<Type> nestings = new List<Type>();
            for (Type? t = rootType; t != null; t = t.IsGenericParameter ? null : t.DeclaringType)
                nestings.Add(t);

            for (int i = nestings.Count - 1; i >= 0; i--)
            {
                Type enclosingType = nestings[i];
                string name = enclosingType.Name;

                if (i == nestings.Count - 1 && enclosingType.Namespace != null && enclosingType.Namespace.Length != 0)
                    name = enclosingType.Namespace + "." + name;

                tnb.AddName(name);
            }

            // Append generic arguments
            if (rootType.IsGenericType && (!rootType.IsGenericTypeDefinition || format == Format.ToString))
            {
                Type[] genericArguments = rootType.GetGenericArguments();

                tnb.OpenGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Format genericArgumentsFormat = format == Format.FullName ? Format.AssemblyQualifiedName : format;

                    tnb.OpenGenericArgument();
                    ConstructAssemblyQualifiedNameWorker(tnb, genericArguments[i], genericArgumentsFormat);
                    tnb.CloseGenericArgument();
                }
                tnb.CloseGenericArguments();
            }

            // Append pointer, byRef and array qualifiers
            AddElementType(tnb, type);

            if (format == Format.AssemblyQualifiedName)
                tnb.AddAssemblySpec(type.Module.Assembly.FullName!);
        }
    }
}
