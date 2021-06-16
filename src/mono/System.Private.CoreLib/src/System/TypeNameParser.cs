// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;

namespace System
{
    internal static class TypeNameParser
    {
        private static readonly char[] SPECIAL_CHARS = { ',', '[', ']', '&', '*', '+', '\\' };

        [RequiresUnreferencedCode("Types might be removed")]
        internal static Type? GetType(
            string typeName,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));

            ParsedName? pname = ParseName(typeName, false, 0, out int end_pos);
            if (pname == null)
            {
                if (throwOnError)
                    throw new ArgumentException();
                return null;
            }

            return ConstructType(pname, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private static Type? ConstructType(
            ParsedName pname,
            Func<AssemblyName, Assembly?>? assemblyResolver,
            Func<Assembly, string, bool, Type?>? typeResolver,
            bool throwOnError,
            bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            // Resolve assembly
            Assembly? assembly = null;
            if (pname.AssemblyName != null)
            {
                assembly = ResolveAssembly(pname.AssemblyName, assemblyResolver, throwOnError, ref stackMark);
                if (assembly == null)
                    // If throwOnError is true, an exception was already thrown
                    return null;
            }

            // Resolve base type
            Type? type = ResolveType(assembly!, pname.Names!, typeResolver, throwOnError, ignoreCase, ref stackMark);
            if (type == null)
                return null;

            // Resolve type arguments
            if (pname.TypeArguments != null)
            {
                var args = new Type?[pname.TypeArguments.Count];
                for (int i = 0; i < pname.TypeArguments.Count; ++i)
                {
                    args[i] = ConstructType(pname.TypeArguments[i], assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
                    if (args[i] == null)
                        return null;
                }
                type = type.MakeGenericType(args!);
            }

            // Resolve modifiers
            if (pname.Modifiers != null)
            {
                bool bounded = false;
                foreach (int mod in pname.Modifiers)
                {
                    switch (mod)
                    {
                        case 0:
                            type = type.MakeByRefType();
                            break;
                        case -1:
                            type = type.MakePointerType();
                            break;
                        case -2:
                            bounded = true;
                            break;
                        case 1:
                            if (bounded)
                                type = type.MakeArrayType(1);
                            else
                                type = type.MakeArrayType();
                            break;
                        default:
                            type = type.MakeArrayType(mod);
                            break;
                    }
                }
            }

            return type;
        }

        private static Assembly? ResolveAssembly(string name, Func<AssemblyName, Assembly?>? assemblyResolver, bool throwOnError,
                                         ref StackCrawlMark stackMark)
        {
            var aname = new AssemblyName(name);

            if (assemblyResolver == null)
            {
                if (throwOnError)
                {
                    return Assembly.Load(aname, ref stackMark, null);
                }
                else
                {
                    try
                    {
                        return Assembly.Load(aname, ref stackMark, null);
                    }
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }
            }
            else
            {
                Assembly? assembly = assemblyResolver(aname);
                if (assembly == null && throwOnError)
                    throw new FileNotFoundException(SR.FileNotFound_ResolveAssembly, name);
                return assembly;
            }
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private static Type? ResolveType(Assembly assembly, List<string> names, Func<Assembly, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
        {
            Type? type = null;

            string name = EscapeTypeName(names[0]);
            // Resolve the top level type.
            if (typeResolver != null)
            {
                type = typeResolver(assembly, name, ignoreCase);
                if (type == null && throwOnError)
                {
                    if (assembly == null)
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveType, name));
                    else
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, name, assembly.FullName));
                }
            }
            else
            {
                if (assembly == null)
                    type = RuntimeType.GetType(name, throwOnError, ignoreCase, ref stackMark);
                else
                    type = assembly.GetType(name, throwOnError, ignoreCase);
            }

            if (type == null)
                return null;

            // Resolve nested types.
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public;
            if (ignoreCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            for (int i = 1; i < names.Count; ++i)
            {
                type = type.GetNestedType(names[i], bindingFlags);
                if (type == null)
                {
                    if (throwOnError)
                        throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, names[i], names[i - 1]));
                    else
                        break;
                }
            }
            return type;
        }

        private static string EscapeTypeName(string name)
        {
            if (name.IndexOfAny(SPECIAL_CHARS) < 0)
                return name;

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (Array.IndexOf<char>(SPECIAL_CHARS, c) >= 0)
                    sb.Append('\\');
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string UnescapeTypeName(string name)
        {
            if (name.IndexOfAny(SPECIAL_CHARS) < 0)
                return name;

            var sb = new StringBuilder(name.Length - 1);
            for (int i = 0; i < name.Length; ++i)
            {
                if (name[i] == '\\' && i + 1 < name.Length)
                    i++;
                sb.Append(name[i]);
            }

            return sb.ToString();
        }

        private sealed class ParsedName
        {
            public List<string>? Names;
            public List<ParsedName>? TypeArguments;
            public List<int>? Modifiers;
            public string? AssemblyName;

            /* For debugging
            public override string ToString () {
                var sb = new StringBuilder ();
                sb.Append (Names [0]);
                if (TypeArguments != null) {
                    sb.Append ("[");
                    for (int i = 0; i < TypeArguments.Count; ++i) {
                        if (TypeArguments [i].AssemblyName != null)
                            sb.Append ('[');
                        sb.Append (TypeArguments [i].ToString ());
                        if (TypeArguments [i].AssemblyName != null)
                            sb.Append (']');
                        if (i < TypeArguments.Count - 1)
                            sb.Append (", ");
                    }
                    sb.Append ("]");
                }
                if (AssemblyName != null)
                    sb.Append ($", {AssemblyName}");
                return sb.ToString ();
            }
            */
        }

        // Ported from the C version in mono_reflection_parse_type_checked ()
        // Entries to the Names list are unescaped to internal form while AssemblyName is not, in an effort to maintain
        // consistency with our native parser. Since this function is just called recursively, that should also be true
        // for ParsedNames in TypeArguments.
        private static ParsedName? ParseName(string name, bool recursed, int pos, out int end_pos)
        {
            end_pos = 0;

            while (pos < name.Length && name[pos] == ' ')
                pos++;

            var res = new ParsedName() { Names = new List<string>() };

            int start = pos;
            int name_start = pos;
            bool in_modifiers = false;
            while (pos < name.Length)
            {
                switch (name[pos])
                {
                    case '+':
                        res.Names.Add(UnescapeTypeName(name.Substring(name_start, pos - name_start)));
                        name_start = pos + 1;
                        break;
                    case '\\':
                        pos++;
                        break;
                    case '&':
                    case '*':
                    case '[':
                    case ',':
                    case ']':
                        in_modifiers = true;
                        break;
                    default:
                        break;
                }
                if (in_modifiers)
                    break;
                pos++;
            }

            res.Names.Add(UnescapeTypeName(name.Substring(name_start, pos - name_start)));

            bool isbyref = false;
            bool isptr = false;
            int rank = -1;

            bool end = false;
            while (pos < name.Length && !end)
            {
                switch (name[pos])
                {
                    case '&':
                        if (isbyref)
                            return null;
                        pos++;
                        isbyref = true;
                        isptr = false;
                        if (res.Modifiers == null)
                            res.Modifiers = new List<int>();
                        res.Modifiers.Add(0);
                        break;
                    case '*':
                        if (isbyref)
                            return null;
                        pos++;
                        if (res.Modifiers == null)
                            res.Modifiers = new List<int>();
                        res.Modifiers.Add(-1);
                        isptr = true;
                        break;
                    case '[':
                        // An array or generic arguments
                        if (isbyref)
                            return null;
                        pos++;
                        if (pos == name.Length)
                            return null;

                        if (name[pos] == ',' || name[pos] == '*' || name[pos] == ']')
                        {
                            // Array
                            bool bounded = false;
                            isptr = false;
                            rank = 1;
                            while (pos < name.Length)
                            {
                                if (name[pos] == ']')
                                    break;
                                if (name[pos] == ',')
                                    rank++;
                                else if (name[pos] == '*') /* '*' means unknown lower bound */
                                    bounded = true;
                                else
                                    return null;
                                pos++;
                            }
                            if (pos == name.Length)
                                return null;
                            if (name[pos] != ']')
                                return null;
                            pos++;
                            /* bounded only allowed when rank == 1 */
                            if (bounded && rank > 1)
                                return null;
                            /* n.b. bounded needs both modifiers: -2 == bounded, 1 == rank 1 array */
                            if (res.Modifiers == null)
                                res.Modifiers = new List<int>();
                            if (bounded)
                                res.Modifiers.Add(-2);
                            res.Modifiers.Add(rank);
                        }
                        else
                        {
                            // Generic args
                            if (rank > 0 || isptr)
                                return null;
                            isptr = false;
                            res.TypeArguments = new List<ParsedName>();
                            while (pos < name.Length)
                            {
                                while (pos < name.Length && name[pos] == ' ')
                                    pos++;
                                bool fqname = false;
                                if (pos < name.Length && name[pos] == '[')
                                {
                                    pos++;
                                    fqname = true;
                                }

                                ParsedName? arg = ParseName(name, true, pos, out pos);
                                if (arg == null)
                                    return null;
                                res.TypeArguments.Add(arg);

                                /*MS is lenient on [] delimited parameters that aren't fqn - and F# uses them.*/
                                if (fqname && pos < name.Length && name[pos] != ']')
                                {
                                    if (name[pos] != ',')
                                        return null;
                                    pos++;
                                    int aname_start = pos;
                                    while (pos < name.Length && name[pos] != ']')
                                        pos++;
                                    if (pos == name.Length)
                                        return null;
                                    while (char.IsWhiteSpace(name[aname_start]))
                                        aname_start++;
                                    if (aname_start == pos)
                                        return null;
                                    arg.AssemblyName = name.Substring(aname_start, pos - aname_start);
                                    pos++;
                                }
                                else if (fqname && pos < name.Length && name[pos] == ']')
                                {
                                    pos++;
                                }
                                if (pos < name.Length && name[pos] == ']')
                                {
                                    pos++;
                                    break;
                                }
                                else if (pos == name.Length)
                                    return null;
                                pos++;
                            }
                        }
                        break;
                    case ']':
                        if (recursed)
                        {
                            end = true;
                            break;
                        }
                        return null;
                    case ',':
                        if (recursed)
                        {
                            end = true;
                            break;
                        }
                        pos++;
                        while (pos < name.Length && char.IsWhiteSpace(name[pos]))
                            pos++;
                        if (pos == name.Length)
                            return null;
                        res.AssemblyName = name.Substring(pos);
                        end = true;
                        break;
                    default:
                        return null;
                }
                if (end)
                    break;
            }

            end_pos = pos;
            return res;
        }
    }
}
