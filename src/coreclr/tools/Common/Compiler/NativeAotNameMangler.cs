// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class NativeAotNameMangler : NameMangler
    {
#if !READYTORUN
        public NativeAotNameMangler(NodeMangler nodeMangler) : base(nodeMangler)
        {
        }
#endif

        private Utf8String _compilationUnitPrefix;

        public override Utf8String CompilationUnitPrefix
        {
            get
            {
                Debug.Assert(!_compilationUnitPrefix.IsNull);
                return _compilationUnitPrefix;
            }
            set { _compilationUnitPrefix = SanitizeNameWithHash(value); }
        }

        //
        // Turn a name into a valid C/C++ identifier
        //
        private static string SanitizeName(string s)
        {
            StringBuilder sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (char.IsAsciiLetter(c))
                {
                    sb?.Append(c);
                    continue;
                }

                if (char.IsAsciiDigit(c))
                {
                    // C identifiers cannot start with a digit. Prepend underscores.
                    if (i == 0)
                    {
                        sb ??= new StringBuilder(s.Length + 2);
                        sb.Append('_');
                    }
                    sb?.Append(c);
                    continue;
                }

                sb ??= new StringBuilder(s, 0, i, s.Length);

                // Everything else is replaced by underscore.
                // TODO: We assume that there won't be collisions with our own or C++ built-in identifiers.
                sb.Append('_');
            }

            string sanitizedName = (sb != null) ? sb.ToString() : s;

            // The character sequences denoting generic instantiations, arrays, byrefs, or pointers must be
            // restricted to that use only. Replace them if they happened to be used in any identifiers in
            // the compilation input.
            return sanitizedName;
        }

        public override Utf8String SanitizeName(Utf8String s)
            => SanitizeName(s.AsSpan());

        private static Utf8String SanitizeName(ReadOnlySpan<byte> s)
        {
            Utf8StringBuilder sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                byte c = s[i];

                if (char.IsAsciiLetter((char)c) || c == '_')
                {
                    sb?.Append((char)c);
                    continue;
                }

                if (char.IsAsciiDigit((char)c))
                {
                    // C identifiers cannot start with a digit. Prepend underscores.
                    if (i == 0)
                    {
                        sb ??= new Utf8StringBuilder(s.Length + 2);
                        sb.Append('_');
                    }
                    sb?.Append((char)c);
                    continue;
                }

                if (sb == null)
                {
                    sb = new Utf8StringBuilder(s.Length);
                    if (i > 0)
                        sb.Append(s.Slice(0, i));
                }

                // Everything else is replaced by underscore.
                // TODO: We assume that there won't be collisions with our own or C++ built-in identifiers.
                sb.Append('_');

                // If this is a multibyte codepoint, seek to the next character
                if ((sbyte)c < 0)
                {
                    while ((i + 1 < s.Length) && ((s[i + 1] & 0b1100_0000) == 0b1000_0000))
                        i++;
                }
            }

            Utf8String sanitizedName = (sb != null) ? sb.ToUtf8String() : new Utf8String(s.ToArray());

            // The character sequences denoting generic instantiations, arrays, byrefs, or pointers must be
            // restricted to that use only. Replace them if they happened to be used in any identifiers in
            // the compilation input.
            return sanitizedName;
        }

        private static byte[] GetBytesFromString(string literal)
        {
            byte[] bytes = new byte[checked(literal.Length * 2)];
            for (int i = 0; i < literal.Length; i++)
            {
                int iByteBase = i * 2;
                char c = literal[i];
                bytes[iByteBase] = (byte)c;
                bytes[iByteBase + 1] = (byte)(c >> 8);
            }
            return bytes;
        }

        private string SanitizeNameWithHash(string literal)
        {
            string mangledName = SanitizeName(literal);

            if (mangledName.Length > 30)
                mangledName = mangledName.Substring(0, 30);

            if (mangledName != literal)
            {
                byte[] hash;
                lock (this)
                {
                    // Use SHA256 hash here to provide a high degree of uniqueness to symbol names without requiring them to be long
                    // This hash function provides an exceedingly high likelihood that no two strings will be given equal symbol names
                    // This is not considered used for security purpose; however collisions would be highly unfortunate as they will cause compilation
                    // failure.
                    hash = SHA256.HashData(GetBytesFromString(literal));
                }

                mangledName += "_" + Convert.ToHexString(hash);
            }

            return mangledName;
        }

        private Utf8String SanitizeNameWithHash(Utf8String literal)
        {
            Utf8String mangledName = SanitizeName(literal);

            if (mangledName.Length > 30)
                mangledName = new Utf8String(mangledName.AsSpan().Slice(0, 30).ToArray());

            if (!mangledName.AsSpan().SequenceEqual(literal.AsSpan()))
            {
                byte[] hash;
                lock (this)
                {
                    // Use SHA256 hash here to provide a high degree of uniqueness to symbol names without requiring them to be long
                    // This hash function provides an exceedingly high likelihood that no two strings will be given equal symbol names
                    // This is not considered used for security purpose; however collisions would be highly unfortunate as they will cause compilation
                    // failure.
                    hash = SHA256.HashData(literal.AsSpan());
                }

                mangledName = new Utf8StringBuilder()
                    .Append(mangledName)
                    .Append('_')
                    .AppendAscii(Convert.ToHexString(hash))
                    .ToUtf8String();
            }

            return mangledName;
        }

        /// <summary>
        /// Dictionary given a mangled name for a given <see cref="TypeDesc"/>
        /// </summary>
        private Dictionary<TypeDesc, Utf8String> _mangledTypeNames = new Dictionary<TypeDesc, Utf8String>();

        /// <summary>
        /// Given a set of names <param name="set"/> check if <param name="origName"/>
        /// is unique, if not add a numbered suffix until it becomes unique.
        /// </summary>
        /// <param name="origName">Name to check for uniqueness.</param>
        /// <param name="set">Set of names already used.</param>
        /// <returns>A name based on <param name="origName"/> that is not part of <param name="set"/>.</returns>
        private static Utf8String DisambiguateName(Utf8String origName, HashSet<Utf8String> set)
        {
            Utf8String result = origName;
            byte[] buffer = null;
            for (uint iter = 0; set.Contains(result); iter++)
            {
                int neededLength = origName.Length + 1 + CountDigits(iter);

                if (buffer == null || buffer.Length != neededLength)
                {
                    buffer = new byte[neededLength];
                    origName.AsSpan().CopyTo(buffer);
                    buffer[origName.Length] = (byte)'_';
                    result = new Utf8String(buffer);
                }

                bool b = iter.TryFormat(new Span<byte>(buffer).Slice(origName.Length + 1), out _);
                Debug.Assert(b);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountDigits(uint value)
        {
            // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
            ReadOnlySpan<long> table =
            [
                4294967296, 8589934582, 8589934582, 8589934582, 12884901788, 12884901788, 12884901788, 17179868184,
                17179868184, 17179868184, 21474826480, 21474826480, 21474826480, 21474826480, 25769703776, 25769703776,
                25769703776, 30063771072, 30063771072, 30063771072, 34349738368, 34349738368, 34349738368, 34349738368,
                38554705664, 38554705664, 38554705664, 41949672960, 41949672960, 41949672960, 42949672960, 42949672960,
            ];
            long tableValue = table[(int)uint.Log2(value)];
            return (int)((value + tableValue) >> 32);
        }


        public override Utf8String GetMangledTypeName(TypeDesc type)
        {
            lock (this)
            {
                if (_mangledTypeNames.TryGetValue(type, out Utf8String mangledName))
                    return mangledName;

                return ComputeMangledTypeName(type);
            }
        }

        private static Utf8String EnterNameScopeSequence = new Utf8String([ (byte)'<' ]);
        private static Utf8String ExitNameScopeSequence = new Utf8String([(byte)'>']);
        private static Utf8String DelimitNameScopeSequence = new Utf8String([(byte)',']);

        protected Utf8String NestMangledName(Utf8String name)
        {
            return Utf8String.Concat(EnterNameScopeSequence, name, ExitNameScopeSequence);
        }

        /// <summary>
        /// If given <param name="type"/> is an <see cref="EcmaType"/> precompute its mangled type name
        /// along with all the other types from the same module as <param name="type"/>.
        /// Otherwise, it is a constructed type and to the EcmaType's mangled name we add a suffix to
        /// show what kind of constructed type it is (e.g. appending __Array for an array type).
        /// </summary>
        /// <param name="type">Type to mangled</param>
        /// <returns>Mangled name for <param name="type"/>.</returns>
        private Utf8String ComputeMangledTypeName(TypeDesc type)
        {
            if (type is EcmaType ecmaType)
            {
                // Add consistent names for all types in the module, independent on the order in which
                // they are compiled
                lock (this)
                {
                    if (!_mangledTypeNames.TryGetValue(type, out Utf8String name))
                    {
                        bool isSystemModule = ecmaType.Module == ecmaType.Context.SystemModule;

                        string assemblyName = ((EcmaAssembly)ecmaType.Module).GetName().Name;
                        bool isSystemPrivate = assemblyName.StartsWith("System.Private.");

                        // Abbreviate System.Private to S.P. This might conflict with user defined assembly names,
                        // but we already have a problem due to running SanitizeName without disambiguating the result
                        // This problem needs a better fix.
                        if (isSystemPrivate)
                            assemblyName = string.Concat("S.P.", assemblyName.AsSpan(15));
                        Utf8String prependAssemblyName = new Utf8String(SanitizeName(assemblyName));

                        var deduplicator = new HashSet<Utf8String>();

                        var sb = new Utf8StringBuilder();
                        foreach (MetadataType t in ecmaType.Module.GetAllTypes())
                        {
                            sb.Clear().Append(prependAssemblyName).Append('_');

                            AppendTypeName(sb, t);

                            static void AppendTypeName(Utf8StringBuilder sb, MetadataType t)
                            {
                                MetadataType containingType = t.ContainingType;
                                if (containingType != null)
                                {
                                    AppendTypeName(sb, containingType);
                                    sb.Append('_');
                                }
                                else
                                {
                                    ReadOnlySpan<byte> ns = t.Namespace;
                                    if (ns.Length > 0)
                                        sb.Append(SanitizeName(ns)).Append('_');
                                }
                                sb.Append(SanitizeName(t.Name));
                            }

                            // If this is one of the well known types, use a shorter name
                            // We know this won't conflict because all the other types are
                            // prefixed by the assembly name.
                            if (isSystemModule)
                            {
                                switch (t.Category)
                                {
                                    case TypeFlags.Boolean: sb.Clear().Append("Bool"u8); break;
                                    case TypeFlags.Byte: sb.Clear().Append("UInt8"u8); break;
                                    case TypeFlags.SByte: sb.Clear().Append("Int8"u8); break;
                                    case TypeFlags.UInt16: sb.Clear().Append("UInt16"u8); break;
                                    case TypeFlags.Int16: sb.Clear().Append("Int16"u8); break;
                                    case TypeFlags.UInt32: sb.Clear().Append("UInt32"u8); break;
                                    case TypeFlags.Int32: sb.Clear().Append("Int32"u8); break;
                                    case TypeFlags.UInt64: sb.Clear().Append("UInt64"u8); break;
                                    case TypeFlags.Int64: sb.Clear().Append("Int64"u8); break;
                                    case TypeFlags.Char: sb.Clear().Append("Char"u8); break;
                                    case TypeFlags.Double: sb.Clear().Append("Double"u8); break;
                                    case TypeFlags.Single: sb.Clear().Append("Single"u8); break;
                                    case TypeFlags.IntPtr: sb.Clear().Append("IntPtr"u8); break;
                                    case TypeFlags.UIntPtr: sb.Clear().Append("UIntPtr"u8); break;
                                    default:
                                        if (t.IsObject)
                                            sb.Clear().Append("Object"u8);
                                        else if (t.IsString)
                                            sb.Clear().Append("String"u8);
                                        break;
                                }
                            }

                            name = sb.ToUtf8String();

                            // Ensure that name is unique and update our tables accordingly.
                            name = DisambiguateName(name, deduplicator);
                            deduplicator.Add(name);
                            _mangledTypeNames.Add(t, name);
                        }
                        name = _mangledTypeNames[type];
                    }
                    return name;
                }
            }

            Utf8String mangledName;

            switch (type.Category)
            {
                case TypeFlags.Array:
                    mangledName = new Utf8StringBuilder().Append("__MDArray"u8)
                                  .Append(EnterNameScopeSequence)
                                  .Append(GetMangledTypeName(((ArrayType)type).ElementType))
                                  .Append(DelimitNameScopeSequence)
                                  .Append(((ArrayType)type).Rank.ToStringInvariant())
                                  .Append(ExitNameScopeSequence).ToUtf8String();
                    break;
                case TypeFlags.SzArray:
                    mangledName = new Utf8StringBuilder().Append("__Array"u8)
                        .Append(EnterNameScopeSequence).Append(GetMangledTypeName(((ArrayType)type).ElementType)).Append(ExitNameScopeSequence).ToUtf8String();
                    break;
                case TypeFlags.ByRef:
                    mangledName = new Utf8StringBuilder()
                        .Append(GetMangledTypeName(((ByRefType)type).ParameterType))
                        .Append(EnterNameScopeSequence).Append("ByRef"u8).Append(ExitNameScopeSequence).ToUtf8String();
                    break;
                case TypeFlags.Pointer:
                    mangledName = new Utf8StringBuilder()
                        .Append(GetMangledTypeName(((PointerType)type).ParameterType))
                        .Append(EnterNameScopeSequence).Append("Pointer"u8).Append(ExitNameScopeSequence).ToUtf8String();
                    break;
                case TypeFlags.FunctionPointer:
                {
                    var fnPtrType = (FunctionPointerType)type;
                    var sb = new Utf8StringBuilder();
                    sb.Append("__FnPtr_"u8).Append(((int)fnPtrType.Signature.Flags).ToString("X2")).Append(EnterNameScopeSequence);
                    sb.Append(GetMangledTypeName(fnPtrType.Signature.ReturnType));

                    sb.Append(EnterNameScopeSequence);
                    for (int i = 0; i < fnPtrType.Signature.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(DelimitNameScopeSequence);
                        sb.Append(GetMangledTypeName(fnPtrType.Signature[i]));
                    }
                    sb.Append(ExitNameScopeSequence);

                    sb.Append(ExitNameScopeSequence);
                    mangledName = sb.ToUtf8String();
                    break;
                }
                default:
                    // Case of a generic type. If `type' is a type definition we use the type name
                    // for mangling, otherwise we use the mangling of the type and its generic type
                    // parameters, e.g. A <B> becomes A_<___B_>_.
                    var typeDefinition = type.GetTypeDefinition();
                    if (typeDefinition != type)
                    {
                        var sb = new Utf8StringBuilder();
                        sb.Append(GetMangledTypeName(typeDefinition));

                        var inst = type.Instantiation;
                        sb.Append(EnterNameScopeSequence);
                        for (int i = 0; i < inst.Length; i++)
                        {
                            if (i > 0)
                                sb.Append("__"u8);
                            sb.Append(GetMangledTypeName(inst[i]));
                        }
                        sb.Append(ExitNameScopeSequence);
                        mangledName = sb.ToUtf8String();
                    }
                    else if (type is IPrefixMangledMethod)
                    {
                        mangledName = GetPrefixMangledMethodName((IPrefixMangledMethod)type);
                    }
                    else if (type is IPrefixMangledType)
                    {
                        mangledName = GetPrefixMangledTypeName((IPrefixMangledType)type);
                    }
                    else
                    {
                        // This is a type definition. Since we didn't fall in the `is EcmaType` case above,
                        // it's likely a compiler-generated type.
                        var defType = (DefType)type;
                        var sb = new Utf8StringBuilder();
                        if (defType.Namespace.Length > 0)
                            sb.Append(SanitizeName(defType.Namespace)).Append('_');

                        sb.Append(SanitizeName(defType.Name));
                        mangledName = sb.ToUtf8String();
                    }
                    break;
            }

            lock (this)
            {
                // Ensure that name is unique and update our tables accordingly.
                _mangledTypeNames.TryAdd(type, mangledName);
            }

            return mangledName;
        }

        private Dictionary<MethodDesc, Utf8String> _mangledMethodNames = new Dictionary<MethodDesc, Utf8String>();
        private Dictionary<MethodDesc, Utf8String> _unqualifiedMangledMethodNames = new Dictionary<MethodDesc, Utf8String>();

        public override Utf8String GetMangledMethodName(MethodDesc method)
        {
            Utf8String utf8MangledName;
            lock (this)
            {
                if (_mangledMethodNames.TryGetValue(method, out utf8MangledName))
                    return utf8MangledName;
            }

            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(GetMangledTypeName(method.OwningType));
            sb.Append("__"u8);
            sb.Append(GetUnqualifiedMangledMethodName(method));
            utf8MangledName = sb.ToUtf8String();

            lock (this)
            {
                _mangledMethodNames.TryAdd(method, utf8MangledName);
            }

            return utf8MangledName;
        }

        private Utf8String GetUnqualifiedMangledMethodName(MethodDesc method)
        {
            lock (this)
            {
                Utf8String mangledName;
                if (_unqualifiedMangledMethodNames.TryGetValue(method, out mangledName))
                    return mangledName;

                return ComputeUnqualifiedMangledMethodName(method);
            }
        }

        private Utf8String GetPrefixMangledTypeName(IPrefixMangledType prefixMangledType)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(EnterNameScopeSequence).Append(prefixMangledType.Prefix).Append(ExitNameScopeSequence);
            sb.Append(GetMangledTypeName(prefixMangledType.BaseType));
            return sb.ToUtf8String();
        }

        private Utf8String GetPrefixMangledSignatureName(IPrefixMangledSignature prefixMangledSignature)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(EnterNameScopeSequence).Append(prefixMangledSignature.Prefix).Append(ExitNameScopeSequence);

            var signature = prefixMangledSignature.BaseSignature;
            sb.Append(signature.Flags.ToStringInvariant());

            sb.Append(EnterNameScopeSequence);

            Utf8String sigRetTypeName = GetMangledTypeName(signature.ReturnType);
            sb.Append(sigRetTypeName);

            for (int i = 0; i < signature.Length; i++)
            {
                sb.Append("__"u8);
                Utf8String sigArgName = GetMangledTypeName(signature[i]);
                sb.Append(sigArgName);
            }

            sb.Append(ExitNameScopeSequence);

            return sb.ToUtf8String();
        }

        private Utf8String GetPrefixMangledMethodName(IPrefixMangledMethod prefixMangledMethod)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(EnterNameScopeSequence).Append(prefixMangledMethod.Prefix).Append(ExitNameScopeSequence);
            sb.Append(GetMangledMethodName(prefixMangledMethod.BaseMethod));
            return sb.ToUtf8String();
        }

        private Utf8String ComputeUnqualifiedMangledMethodName(MethodDesc method)
        {
            if (method is EcmaMethod)
            {
                var deduplicator = new HashSet<Utf8String>();

                // Add consistent names for all methods of the type, independent on the order in which
                // they are compiled
                lock (this)
                {
                    if (!_unqualifiedMangledMethodNames.ContainsKey(method))
                    {
                        foreach (var m in method.OwningType.GetMethods())
                        {
                            Utf8String name = SanitizeName(m.Name);

                            name = DisambiguateName(name, deduplicator);
                            deduplicator.Add(name);

                            _unqualifiedMangledMethodNames.Add(m, name);
                        }
                    }
                    return _unqualifiedMangledMethodNames[method];
                }
            }

            Utf8String utf8MangledName;

            var methodDefinition = method.GetMethodDefinition();
            if (methodDefinition != method)
            {
                // Instantiated generic method
                Utf8StringBuilder sb = new Utf8StringBuilder();
                sb.Append(GetUnqualifiedMangledMethodName(methodDefinition.GetTypicalMethodDefinition()));

                sb.Append(EnterNameScopeSequence);

                var inst = method.Instantiation;
                for (int i = 0; i < inst.Length; i++)
                {
                    Utf8String instArgName = GetMangledTypeName(inst[i]);
                    if (i > 0)
                        sb.Append("__"u8);
                    sb.Append(instArgName);
                }

                sb.Append(ExitNameScopeSequence);

                utf8MangledName = sb.ToUtf8String();
            }
            else
            {
                var typicalMethodDefinition = method.GetTypicalMethodDefinition();
                if (typicalMethodDefinition != method)
                {
                    // Method on an instantiated type
                    utf8MangledName = GetUnqualifiedMangledMethodName(typicalMethodDefinition);
                }
                else if (method is IPrefixMangledMethod)
                {
                    utf8MangledName = GetPrefixMangledMethodName((IPrefixMangledMethod)method);
                }
                else if (method is IPrefixMangledType)
                {
                    utf8MangledName = GetPrefixMangledTypeName((IPrefixMangledType)method);
                }
                else if (method is IPrefixMangledSignature)
                {
                    utf8MangledName = GetPrefixMangledSignatureName((IPrefixMangledSignature)method);
                }
                else
                {
                    // Assume that Name is unique for all other methods
                    utf8MangledName = SanitizeName(method.Name);
                }
            }

            return utf8MangledName;
        }

        private Dictionary<FieldDesc, Utf8String> _mangledFieldNames = new Dictionary<FieldDesc, Utf8String>();

        public override Utf8String GetMangledFieldName(FieldDesc field)
        {
            lock (this)
            {
                Utf8String mangledName;
                if (_mangledFieldNames.TryGetValue(field, out mangledName))
                    return mangledName;

                return ComputeMangledFieldName(field);
            }
        }

        private Utf8String ComputeMangledFieldName(FieldDesc field)
        {
            Utf8String prependTypeName = GetMangledTypeName(field.OwningType);

            if (field is EcmaField)
            {
                var deduplicator = new HashSet<Utf8String>();

                // Add consistent names for all fields of the type, independent on the order in which
                // they are compiled
                lock (this)
                {
                    if (!_mangledFieldNames.ContainsKey(field))
                    {
                        Utf8StringBuilder sb = new Utf8StringBuilder();
                        foreach (var f in field.OwningType.GetFields())
                        {
                            sb.Clear().Append(prependTypeName).Append("__"u8);
                            Utf8String name = SanitizeName(f.Name);

                            name = DisambiguateName(name, deduplicator);
                            deduplicator.Add(name);

                            sb.Append(name);

                            _mangledFieldNames.Add(f, sb.ToUtf8String());
                        }
                    }
                    return _mangledFieldNames[field];
                }
            }


            Utf8String mangledName = SanitizeName(field.Name);
            Utf8String utf8MangledName = new Utf8StringBuilder().Append(prependTypeName).Append("__"u8).Append(mangledName).ToUtf8String();

            lock (this)
            {
                _mangledFieldNames.TryAdd(field, utf8MangledName);
            }

            return utf8MangledName;
        }

        private Dictionary<string, string> _mangledStringLiterals = new Dictionary<string, string>();

        public override string GetMangledStringName(string literal)
        {
            string mangledName;
            lock (this)
            {
                if (_mangledStringLiterals.TryGetValue(literal, out mangledName))
                    return mangledName;
            }

            mangledName = SanitizeNameWithHash(literal);

            lock (this)
            {
                _mangledStringLiterals.TryAdd(literal, mangledName);
            }

            return mangledName;
        }
    }
}
