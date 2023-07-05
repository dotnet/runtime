// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This TypeNameBuilder is ported from CoreCLR's original.
// It replaces the C++ bits of the implementation with a faithful C# port.

using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace System.Reflection.Emit
{
    internal sealed class TypeNameBuilder
    {
        private readonly StringBuilder _str = new StringBuilder();
        private int _instNesting;
        private bool _firstInstArg;
        private bool _nestedName;
        private bool _hasAssemblySpec;
        private bool _useAngleBracketsForGenerics;
        private readonly List<int> _stack = new List<int>();
        private int _stackIdx;

        private TypeNameBuilder()
        {
        }

        private void OpenGenericArguments()
        {
            _instNesting++;
            _firstInstArg = true;

            if (_useAngleBracketsForGenerics)
                Append('<');
            else
                Append('[');
        }

        private void CloseGenericArguments()
        {
            Debug.Assert(_instNesting != 0);

            _instNesting--;

            if (_firstInstArg)
            {
                _str.Remove(_str.Length - 1, 1);
            }
            else
            {
                if (_useAngleBracketsForGenerics)
                    Append('>');
                else
                    Append(']');
            }
        }

        private void OpenGenericArgument()
        {
            Debug.Assert(_instNesting != 0);

            _nestedName = false;

            if (!_firstInstArg)
                Append(',');

            _firstInstArg = false;

            if (_useAngleBracketsForGenerics)
                Append('<');
            else
                Append('[');

            PushOpenGenericArgument();
        }

        private void CloseGenericArgument()
        {
            Debug.Assert(_instNesting != 0);

            if (_hasAssemblySpec)
            {
                if (_useAngleBracketsForGenerics)
                    Append('>');
                else
                    Append(']');
            }

            PopOpenGenericArgument();
        }

        private void AddName(string name)
        {
            Debug.Assert(name != null);

            if (_nestedName)
                Append('+');

            _nestedName = true;

            EscapeName(name);
        }

        private void AddArray(int rank)
        {
            Debug.Assert(rank > 0);

            if (rank == 1)
            {
                Append("[*]");
            }
            else if (rank > 64)
            {
                // Only taken in an error path, runtime will not load arrays of more than 32 dimensions
                _str.Append('[').Append(rank).Append(']');
            }
            else
            {
                Append('[');
                for (int i = 1; i < rank; i++)
                    Append(',');
                Append(']');
            }
        }

        private void AddAssemblySpec(string assemblySpec)
        {
            if (assemblySpec != null && !assemblySpec.Equals(""))
            {
                Append(", ");

                if (_instNesting > 0)
                {
                    EscapeEmbeddedAssemblyName(assemblySpec);
                }
                else
                {
                    EscapeAssemblyName(assemblySpec);
                }

                _hasAssemblySpec = true;
            }
        }

        public override string ToString()
        {
            Debug.Assert(_instNesting == 0);

            return _str.ToString();
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
                    if (IsTypeNameReservedChar(c))
                        _str.Append('\\');
                    _str.Append(c);
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
            if (name.Contains(']'))
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

        private void PushOpenGenericArgument()
        {
            _stack.Add(_str.Length);
            _stackIdx++;
        }

        private void PopOpenGenericArgument()
        {
            int index = _stack[--_stackIdx];
            _stack.RemoveAt(_stackIdx);

            if (!_hasAssemblySpec)
                _str.Remove(index - 1, 1);

            _hasAssemblySpec = false;
        }

        private void SetUseAngleBracketsForGenerics(bool value)
        {
            _useAngleBracketsForGenerics = value;
        }

        private void Append(string pStr)
        {
            int i = pStr.IndexOf('\0');
            if (i < 0)
            {
                _str.Append(pStr);
            }
            else if (i > 0)
            {
                _str.Append(pStr.AsSpan(0, i));
            }
        }

        private void Append(char c)
        {
            _str.Append(c);
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

            var tnb = new TypeNameBuilder();
            tnb.AddAssemblyQualifiedName(type, format);
            return tnb.ToString();
        }

        private void AddElementType(Type type)
        {
            if (!type.HasElementType)
                return;

            AddElementType(type.GetElementType()!);

            if (type.IsPointer)
                Append('*');
            else if (type.IsByRef)
                Append('&');
            else if (type.IsSZArray)
                Append("[]");
            else if (type.IsArray)
                AddArray(type.GetArrayRank());
        }

        private void AddAssemblyQualifiedName(Type type, Format format)
        {
            Type rootType = type;

            while (rootType.HasElementType)
                rootType = rootType.GetElementType()!;

            // Append namespace + nesting + name
            var nestings = new List<Type>();
            for (Type? t = rootType; t != null; t = t.IsGenericParameter ? null : t.DeclaringType)
                nestings.Add(t);

            for (int i = nestings.Count - 1; i >= 0; i--)
            {
                Type enclosingType = nestings[i];
                string name = enclosingType.Name;

                if (i == nestings.Count - 1 && !string.IsNullOrEmpty(enclosingType.Namespace))
                    name = enclosingType.Namespace + "." + name;

                AddName(name);
            }

            // Append generic arguments
            if (rootType.IsGenericType && (!rootType.IsGenericTypeDefinition || format == Format.ToString))
            {
                Type[] genericArguments = rootType.GetGenericArguments();

                OpenGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Format genericArgumentsFormat = format == Format.FullName ? Format.AssemblyQualifiedName : format;

                    OpenGenericArgument();
                    AddAssemblyQualifiedName(genericArguments[i], genericArgumentsFormat);
                    CloseGenericArgument();
                }
                CloseGenericArguments();
            }

            // Append pointer, byRef and array qualifiers
            AddElementType(type);

            if (format == Format.AssemblyQualifiedName)
                AddAssemblySpec(type.Module.Assembly.FullName!);
        }
    }
}
