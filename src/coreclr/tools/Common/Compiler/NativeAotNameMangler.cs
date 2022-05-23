// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using System.Diagnostics;

namespace ILCompiler
{
    public class NativeAotNameMangler : NameMangler
    {
        private SHA256 _sha256;

#if !READYTORUN
        private readonly bool _mangleForCplusPlus;

        public NativeAotNameMangler(NodeMangler nodeMangler, bool mangleForCplusPlus) : base(nodeMangler)
        {
            _mangleForCplusPlus = mangleForCplusPlus;
        }
#else
        private readonly bool _mangleForCplusPlus = false;
#endif

        private string _compilationUnitPrefix;

        public override string CompilationUnitPrefix
        {
            set { _compilationUnitPrefix = SanitizeNameWithHash(value); }
            get
            {
                Debug.Assert(_compilationUnitPrefix != null);
                return _compilationUnitPrefix;
            }
        }

        //
        // Turn a name into a valid C/C++ identifier
        //
        public override string SanitizeName(string s, bool typeName = false)
        {
            StringBuilder sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')))
                {
                    if (sb != null)
                        sb.Append(c);
                    continue;
                }

                if ((c >= '0') && (c <= '9'))
                {
                    // C identifiers cannot start with a digit. Prepend underscores.
                    if (i == 0)
                    {
                        if (sb == null)
                            sb = new StringBuilder(s.Length + 2);
                        sb.Append("_");
                    }
                    if (sb != null)
                        sb.Append(c);
                    continue;
                }

                if (sb == null)
                    sb = new StringBuilder(s, 0, i, s.Length);

                // For CppCodeGen, replace "." (C# namespace separator) with "::" (C++ namespace separator)
                if (typeName && c == '.' && _mangleForCplusPlus)
                {
                    sb.Append("::");
                    continue;
                }

                // Everything else is replaced by underscore.
                // TODO: We assume that there won't be collisions with our own or C++ built-in identifiers.
                sb.Append("_");
            }

            string sanitizedName = (sb != null) ? sb.ToString() : s;

            // The character sequences denoting generic instantiations, arrays, byrefs, or pointers must be
            // restricted to that use only. Replace them if they happened to be used in any identifiers in 
            // the compilation input.
            return _mangleForCplusPlus
                ? sanitizedName.Replace(EnterNameScopeSequence, "_AA_").Replace(ExitNameScopeSequence, "_VV_")
                : sanitizedName;
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
                    if (_sha256 == null)
                    {
                        // Use SHA256 hash here to provide a high degree of uniqueness to symbol names without requiring them to be long
                        // This hash function provides an exceedingly high likelihood that no two strings will be given equal symbol names
                        // This is not considered used for security purpose; however collisions would be highly unfortunate as they will cause compilation
                        // failure.
                        _sha256 = SHA256.Create();
                    }

                    hash = _sha256.ComputeHash(GetBytesFromString(literal));
                }
                                
                mangledName += "_" + BitConverter.ToString(hash).Replace("-", "");
            }

            return mangledName;
        }

        /// <summary>
        /// Dictionary given a mangled name for a given <see cref="TypeDesc"/>
        /// </summary>
        private Dictionary<TypeDesc, string> _mangledTypeNames = new Dictionary<TypeDesc, string>();

        /// <summary>
        /// Given a set of names <param name="set"/> check if <param name="origName"/>
        /// is unique, if not add a numbered suffix until it becomes unique.
        /// </summary>
        /// <param name="origName">Name to check for uniqueness.</param>
        /// <param name="set">Set of names already used.</param>
        /// <returns>A name based on <param name="origName"/> that is not part of <param name="set"/>.</returns>
        private string DisambiguateName(string origName, ISet<string> set)
        {
            int iter = 0;
            string result = origName;
            while (set.Contains(result))
            {
                result = string.Concat(origName, "_", (iter++).ToStringInvariant());
            }
            return result;
        }

        public override string GetMangledTypeName(TypeDesc type)
        {
            lock (this)
            {
                string mangledName;
                if (_mangledTypeNames.TryGetValue(type, out mangledName))
                    return mangledName;

                return ComputeMangledTypeName(type);
            }
        }

        private string EnterNameScopeSequence => _mangleForCplusPlus ? "_A_" : "<";
        private string ExitNameScopeSequence => _mangleForCplusPlus ? "_V_" : ">";
        private string DelimitNameScopeSequence => _mangleForCplusPlus? "_C_" : ",";

        protected string NestMangledName(string name)
        {
            return EnterNameScopeSequence + name + ExitNameScopeSequence;
        }

        /// <summary>
        /// If given <param name="type"/> is an <see cref="EcmaType"/> precompute its mangled type name
        /// along with all the other types from the same module as <param name="type"/>.
        /// Otherwise, it is a constructed type and to the EcmaType's mangled name we add a suffix to
        /// show what kind of constructed type it is (e.g. appending __Array for an array type).
        /// </summary>
        /// <param name="type">Type to mangled</param>
        /// <returns>Mangled name for <param name="type"/>.</returns>
        private string ComputeMangledTypeName(TypeDesc type)
        {
            if (type is EcmaType)
            {
                EcmaType ecmaType = (EcmaType)type;

                string assemblyName = ((EcmaAssembly)ecmaType.EcmaModule).GetName().Name;
                bool isSystemPrivate = assemblyName.StartsWith("System.Private.");
                
                // Abbreviate System.Private to S.P. This might conflict with user defined assembly names,
                // but we already have a problem due to running SanitizeName without disambiguating the result
                // This problem needs a better fix.
                if (isSystemPrivate && !_mangleForCplusPlus)
                    assemblyName = "S.P." + assemblyName.Substring(15);
                string prependAssemblyName = SanitizeName(assemblyName);

                var deduplicator = new HashSet<string>();

                // Add consistent names for all types in the module, independent on the order in which
                // they are compiled
                lock (this)
                {
                    bool isSystemModule = ecmaType.Module == ecmaType.Context.SystemModule;

                    if (!_mangledTypeNames.ContainsKey(type))
                    {
                        foreach (MetadataType t in ecmaType.EcmaModule.GetAllTypes())
                        {
                            string name = t.GetFullName();

                            // Include encapsulating type
                            DefType containingType = t.ContainingType;
                            while (containingType != null)
                            {
                                name = containingType.GetFullName() + "_" + name;
                                containingType = containingType.ContainingType;
                            }

                            name = SanitizeName(name, true);

                            if (_mangleForCplusPlus)
                            {
                                // Always generate a fully qualified name
                                name = "::" + prependAssemblyName + "::" + name;
                            }
                            else
                            {
                                name = prependAssemblyName + "_" + name;

                                // If this is one of the well known types, use a shorter name
                                // We know this won't conflict because all the other types are
                                // prefixed by the assembly name.
                                if (isSystemModule)
                                {
                                    switch (t.Category)
                                    {
                                        case TypeFlags.Boolean: name = "Bool"; break;
                                        case TypeFlags.Byte: name = "UInt8"; break;
                                        case TypeFlags.SByte: name = "Int8"; break;
                                        case TypeFlags.UInt16: name = "UInt16"; break;
                                        case TypeFlags.Int16: name = "Int16"; break;
                                        case TypeFlags.UInt32: name = "UInt32"; break;
                                        case TypeFlags.Int32: name = "Int32"; break;
                                        case TypeFlags.UInt64: name = "UInt64"; break;
                                        case TypeFlags.Int64: name = "Int64"; break;
                                        case TypeFlags.Char: name = "Char"; break;
                                        case TypeFlags.Double: name = "Double"; break;
                                        case TypeFlags.Single: name = "Single"; break;
                                        case TypeFlags.IntPtr: name = "IntPtr"; break;
                                        case TypeFlags.UIntPtr: name = "UIntPtr"; break;
                                        default:
                                            if (t.IsObject)
                                                name = "Object";
                                            else if (t.IsString)
                                                name = "String";
                                            break;
                                    }
                                }
                            }

                            // Ensure that name is unique and update our tables accordingly.
                            name = DisambiguateName(name, deduplicator);
                            deduplicator.Add(name);
                            _mangledTypeNames.Add(t, name);
                        }
                    }
                    return _mangledTypeNames[type];
                }
            }

            string mangledName;

            switch (type.Category)
            {
                case TypeFlags.Array:
                    mangledName = "__MDArray" + 
                                  EnterNameScopeSequence + 
                                  GetMangledTypeName(((ArrayType)type).ElementType) + 
                                  DelimitNameScopeSequence + 
                                  ((ArrayType)type).Rank.ToStringInvariant() + 
                                  ExitNameScopeSequence;
                    break;
                case TypeFlags.SzArray:
                    mangledName = "__Array" + NestMangledName(GetMangledTypeName(((ArrayType)type).ElementType));
                    break;
                case TypeFlags.ByRef:
                    mangledName = GetMangledTypeName(((ByRefType)type).ParameterType) + NestMangledName("ByRef");
                    break;
                case TypeFlags.Pointer:
                    mangledName = GetMangledTypeName(((PointerType)type).ParameterType) + NestMangledName("Pointer");
                    break;
                default:
                    // Case of a generic type. If `type' is a type definition we use the type name
                    // for mangling, otherwise we use the mangling of the type and its generic type
                    // parameters, e.g. A <B> becomes A_<___B_>_ in RyuJIT compilation, or A_A___B_V_
                    // in C++ compilation.
                    var typeDefinition = type.GetTypeDefinition();
                    if (typeDefinition != type)
                    {
                        mangledName = GetMangledTypeName(typeDefinition);

                        var inst = type.Instantiation;
                        string mangledInstantiation = "";
                        for (int i = 0; i < inst.Length; i++)
                        {
                            string instArgName = GetMangledTypeName(inst[i]);
                            if (_mangleForCplusPlus)
                                instArgName = instArgName.Replace("::", "_");
                            if (i > 0)
                                mangledInstantiation += "__";

                            mangledInstantiation += instArgName;
                        }
                        mangledName += NestMangledName(mangledInstantiation);
                    }
                    else if (type is IPrefixMangledMethod)
                    {
                        mangledName = GetPrefixMangledMethodName((IPrefixMangledMethod)type).ToString();
                    }
                    else if (type is IPrefixMangledType)
                    {
                        mangledName = GetPrefixMangledTypeName((IPrefixMangledType)type).ToString();
                    }
                    else
                    {
                        // This is a type definition. Since we didn't fall in the `is EcmaType` case above,
                        // it's likely a compiler-generated type.
                        mangledName = SanitizeName(((DefType)type).GetFullName(), true);

                        // Always generate a fully qualified name
                        if (_mangleForCplusPlus)
                            mangledName = "::" + mangledName;
                    }
                    break;
            }

            lock (this)
            {
                // Ensure that name is unique and update our tables accordingly.
                if (!_mangledTypeNames.ContainsKey(type))
                    _mangledTypeNames.Add(type, mangledName);
            }

            return mangledName;
        }

        private Dictionary<MethodDesc, Utf8String> _mangledMethodNames = new Dictionary<MethodDesc, Utf8String>();
        private Dictionary<MethodDesc, Utf8String> _unqualifiedMangledMethodNames = new Dictionary<MethodDesc, Utf8String>();

        public override Utf8String GetMangledMethodName(MethodDesc method)
        {
            if (_mangleForCplusPlus)
            {
                return GetUnqualifiedMangledMethodName(method);
            }
            else
            {
                Utf8String utf8MangledName;
                lock (this)
                {
                    if (_mangledMethodNames.TryGetValue(method, out utf8MangledName))
                        return utf8MangledName;
                }

                Utf8StringBuilder sb = new Utf8StringBuilder();
                sb.Append(GetMangledTypeName(method.OwningType));
                sb.Append("__");
                sb.Append(GetUnqualifiedMangledMethodName(method));
                utf8MangledName = sb.ToUtf8String();

                lock (this)
                {
                    if (!_mangledMethodNames.ContainsKey(method))
                        _mangledMethodNames.Add(method, utf8MangledName);
                }

                return utf8MangledName;
            }
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

            if (_mangleForCplusPlus)
            {
                string name = GetMangledTypeName(prefixMangledType.BaseType).ToString().Replace("::", "_");
                sb.Append(name);
            }
            else
            {
                sb.Append(GetMangledTypeName(prefixMangledType.BaseType));
            }

            return sb.ToUtf8String();
        }

        private Utf8String GetPrefixMangledSignatureName(IPrefixMangledSignature prefixMangledSignature)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(EnterNameScopeSequence).Append(prefixMangledSignature.Prefix).Append(ExitNameScopeSequence);

            var signature = prefixMangledSignature.BaseSignature;
            sb.Append(signature.Flags.ToStringInvariant());

            sb.Append(EnterNameScopeSequence);

            string sigRetTypeName = GetMangledTypeName(signature.ReturnType);
            if (_mangleForCplusPlus)
                sigRetTypeName = sigRetTypeName.Replace("::", "_");
            sb.Append(sigRetTypeName);

            for (int i = 0; i < signature.Length; i++)
            {
                sb.Append("__");
                string sigArgName = GetMangledTypeName(signature[i]);
                if (_mangleForCplusPlus)
                    sigArgName = sigArgName.Replace("::", "_");
                sb.Append(sigArgName);
            }

            sb.Append(ExitNameScopeSequence);

            return sb.ToUtf8String();
        }

        private Utf8String GetPrefixMangledMethodName(IPrefixMangledMethod prefixMangledMetod)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append(EnterNameScopeSequence).Append(prefixMangledMetod.Prefix).Append(ExitNameScopeSequence);

            if (_mangleForCplusPlus)
            {
                string name = GetMangledMethodName(prefixMangledMetod.BaseMethod).ToString().Replace("::", "_");
                sb.Append(name);
            }
            else
            {
                sb.Append(GetMangledMethodName(prefixMangledMetod.BaseMethod));
            }

            return sb.ToUtf8String();
        }

        private Utf8String ComputeUnqualifiedMangledMethodName(MethodDesc method)
        {
            if (method is EcmaMethod)
            {
                var deduplicator = new HashSet<string>();

                // Add consistent names for all methods of the type, independent on the order in which
                // they are compiled
                lock (this)
                {
                    if (!_unqualifiedMangledMethodNames.ContainsKey(method))
                    {
                        foreach (var m in method.OwningType.GetMethods())
                        {
                            string name = SanitizeName(m.Name);

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
                    string instArgName = GetMangledTypeName(inst[i]);
                    if (_mangleForCplusPlus)
                        instArgName = instArgName.Replace("::", "_");
                    if (i > 0)
                        sb.Append("__");
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
                    utf8MangledName = new Utf8String(SanitizeName(method.Name));
                }
            }

            // Unless we're doing CPP mangling, there's no point in caching the unqualified
            // method name. We only needed it to construct the fully qualified name. Nobody
            // is going to ask for the unqualified name again.
            if (_mangleForCplusPlus)
            {
                lock (this)
                {
                    if (!_unqualifiedMangledMethodNames.ContainsKey(method))
                        _unqualifiedMangledMethodNames.Add(method, utf8MangledName);
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
            string prependTypeName = null;
            if (!_mangleForCplusPlus)
                prependTypeName = GetMangledTypeName(field.OwningType);

            if (field is EcmaField)
            {
                var deduplicator = new HashSet<string>();

                // Add consistent names for all fields of the type, independent on the order in which
                // they are compiled
                lock (this)
                {
                    if (!_mangledFieldNames.ContainsKey(field))
                    {
                        foreach (var f in field.OwningType.GetFields())
                        {
                            string name = SanitizeName(f.Name);

                            name = DisambiguateName(name, deduplicator);
                            deduplicator.Add(name);

                            if (prependTypeName != null)
                                name = prependTypeName + "__" + name;

                            _mangledFieldNames.Add(f, name);
                        }
                    }
                    return _mangledFieldNames[field];
                }
            }


            string mangledName = SanitizeName(field.Name);

            if (prependTypeName != null)
                mangledName = prependTypeName + "__" + mangledName;

            Utf8String utf8MangledName = new Utf8String(mangledName);

            lock (this)
            {
                if (!_mangledFieldNames.ContainsKey(field))
                    _mangledFieldNames.Add(field, utf8MangledName);
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
                if (!_mangledStringLiterals.ContainsKey(literal))
                    _mangledStringLiterals.Add(literal, mangledName);
            }

            return mangledName;
        }
    }
}
