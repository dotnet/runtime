// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security {
    using System;
    using System.Security.Util;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Collections;
    using System.Threading;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    [Flags]
    internal enum PermissionTokenType
    {
        Normal = 0x1,
        IUnrestricted = 0x2,
        DontKnow = 0x4,
        BuiltIn = 0x8
    }

    [Serializable]
    internal sealed class PermissionTokenKeyComparer : IEqualityComparer
    {
        private Comparer _caseSensitiveComparer;
        private TextInfo _info;

        public PermissionTokenKeyComparer()
        {
            _caseSensitiveComparer = new Comparer(CultureInfo.InvariantCulture);
            _info = CultureInfo.InvariantCulture.TextInfo;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public int Compare(Object a, Object b)
        {
            String strA = a as String;
            String strB = b as String;

            // if it's not a string then we just call the object comparer
            if (strA == null || strB == null)
                return _caseSensitiveComparer.Compare(a, b);

            int i = _caseSensitiveComparer.Compare(a,b);
            if (i == 0)
                return 0;

            if (SecurityManager.IsSameType(strA, strB))
                return 0;
            
            return i;
        }

        public new bool Equals( Object a, Object b )
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            return Compare( a, b ) == 0;
        }

        // The data structure consuming this will be responsible for dealing with null objects as keys.
        public int GetHashCode(Object obj)
        {            
            if (obj == null) throw new ArgumentNullException("obj");
            Contract.EndContractBlock();
            
            String str = obj as String;

            if (str == null)
                return obj.GetHashCode();

            int iComma = str.IndexOf( ',' );
            if (iComma == -1)
                iComma = str.Length;

            int accumulator = 0;
            for (int i = 0; i < iComma; ++i)
            {
                accumulator = (accumulator << 7) ^ str[i] ^ (accumulator >> 25);
            }

            return accumulator;
        }
    }

    [Serializable]
    internal sealed class PermissionToken : ISecurityEncodable
    {
        private static readonly PermissionTokenFactory s_theTokenFactory;
#if FEATURE_CAS_POLICY
        private static volatile ReflectionPermission s_reflectPerm = null;
#endif // FEATURE_CAS_POLICY

        private const string c_mscorlibName = System.CoreLib.Name;
        internal int    m_index;
        internal volatile PermissionTokenType m_type;
#if FEATURE_CAS_POLICY
        internal String m_strTypeName;
#endif // FEATURE_CAS_POLICY
        static internal TokenBasedSet s_tokenSet = new TokenBasedSet();

        internal static bool IsMscorlibClassName (string className) {
            Contract.Assert( c_mscorlibName == ((RuntimeAssembly)Assembly.GetExecutingAssembly()).GetSimpleName(),
                System.CoreLib.Name+" name mismatch" );

            // If the class name does not look like a fully qualified name, we cannot simply determine if it's 
            // an mscorlib.dll type so we should return true so the type can be matched with the
            // right index in the TokenBasedSet.
            int index = className.IndexOf(',');
            if (index == -1)
                return true;

            index = className.LastIndexOf(']');
            if (index == -1)
                index = 0;

            // Search for the string 'mscorlib' in the classname. If we find it, we will conservatively assume it's an mscorlib.dll type and load it.
            for (int i = index; i < className.Length; i++) {
#if FEATURE_CORECLR
                if (className[i] == 's' || className[i] == 'S') 
#else
                if (className[i] == 'm' || className[i] == 'M') 
#endif                 
                {
                    if (String.Compare(className, i, c_mscorlibName, 0, c_mscorlibName.Length, StringComparison.OrdinalIgnoreCase) == 0)
                        return true;
                }
            }
            return false;
        }

        static PermissionToken()
        {
            s_theTokenFactory = new PermissionTokenFactory( 4 );
        }

        internal PermissionToken()
        {
        }

        internal PermissionToken(int index, PermissionTokenType type, String strTypeName)
        {
            m_index = index;
            m_type = type;
#if FEATURE_CAS_POLICY
            m_strTypeName = strTypeName;
#endif // FEATURE_CAS_POLICY
        }

        [System.Security.SecurityCritical]  // auto-generated
        public static PermissionToken GetToken(Type cls)
        {
            if (cls == null)
                return null;
            
#if FEATURE_CAS_POLICY
            if (cls.GetInterface( "System.Security.Permissions.IBuiltInPermission" ) != null)
            {
                if (s_reflectPerm == null)
                    s_reflectPerm = new ReflectionPermission(PermissionState.Unrestricted);
                s_reflectPerm.Assert();
                MethodInfo method = cls.GetMethod( "GetTokenIndex", BindingFlags.Static | BindingFlags.NonPublic );
                Contract.Assert( method != null, "IBuiltInPermission types should have a static method called 'GetTokenIndex'" );

                // GetTokenIndex needs to be invoked without any security checks, since doing a security check
                // will involve a ReflectionTargetDemand which creates a CompressedStack and attempts to get the
                // token.
                RuntimeMethodInfo getTokenIndex = method as RuntimeMethodInfo;
                Contract.Assert(getTokenIndex != null, "method is not a RuntimeMethodInfo");
                int token = (int)getTokenIndex.UnsafeInvoke(null, BindingFlags.Default, null, null, null);
                return s_theTokenFactory.BuiltInGetToken(token, null, cls);
            }
            else
#endif // FEATURE_CAS_POLICY
            {
                return s_theTokenFactory.GetToken(cls, null);
            }
        }

        public static PermissionToken GetToken(IPermission perm)
        {
            if (perm == null)
                return null;

            IBuiltInPermission ibPerm = perm as IBuiltInPermission;

            if (ibPerm != null)
                return s_theTokenFactory.BuiltInGetToken( ibPerm.GetTokenIndex(), perm, null );
            else
                return s_theTokenFactory.GetToken(perm.GetType(), perm);
        }

#if FEATURE_CAS_POLICY
        public static PermissionToken GetToken(String typeStr)
        {
            return GetToken( typeStr, false );
        }

#if _DEBUG
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private static void GetTokenHelper(String typeStr)
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Type type = RuntimeTypeHandle.GetTypeByName( typeStr.Trim().Replace( '\'', '\"' ), ref stackMark);
            Contract.Assert( (type == null) || (type.Module.Assembly != System.Reflection.Assembly.GetExecutingAssembly()) || (typeStr.IndexOf("mscorlib", StringComparison.Ordinal) < 0),
                "We should not go through this path for mscorlib based permissions" );
        }
#endif

        public static PermissionToken GetToken(String typeStr, bool bCreateMscorlib)
        {
            if (typeStr == null)
                return null;

            if (IsMscorlibClassName( typeStr ))
            {
                if (!bCreateMscorlib)
                {
                    return null;
                }
                else
                {
                    return FindToken( Type.GetType( typeStr ) );
                }
            }
            else
            {
                PermissionToken token = s_theTokenFactory.GetToken(typeStr);
#if _DEBUG
                GetTokenHelper(typeStr);
#endif
                return token;
            }
        }

        [SecuritySafeCritical]
        public static PermissionToken FindToken( Type cls )
        {
            if (cls == null)
                return null;
             
#if FEATURE_CAS_POLICY
            if (cls.GetInterface( "System.Security.Permissions.IBuiltInPermission" ) != null)
            {
                if (s_reflectPerm == null)
                    s_reflectPerm = new ReflectionPermission(PermissionState.Unrestricted);
                s_reflectPerm.Assert();
                MethodInfo method = cls.GetMethod( "GetTokenIndex", BindingFlags.Static | BindingFlags.NonPublic );
                Contract.Assert( method != null, "IBuiltInPermission types should have a static method called 'GetTokenIndex'" );

                // GetTokenIndex needs to be invoked without any security checks, since doing a security check
                // will involve a ReflectionTargetDemand which creates a CompressedStack and attempts to get the
                // token.
                RuntimeMethodInfo getTokenIndex = method as RuntimeMethodInfo;
                Contract.Assert(getTokenIndex != null, "method is not a RuntimeMethodInfo");
                int token = (int)getTokenIndex.UnsafeInvoke(null, BindingFlags.Default, null, null, null);
                return s_theTokenFactory.BuiltInGetToken(token, null, cls);
            }
            else
#endif // FEATURE_CAS_POLICY
            {
                return s_theTokenFactory.FindToken( cls );
            }
        }
#endif // FEATURE_CAS_POLICY

        public static PermissionToken FindTokenByIndex( int i )
        {
            return s_theTokenFactory.FindTokenByIndex( i );
        }

        public static bool IsTokenProperlyAssigned( IPermission perm, PermissionToken token )
        {
            PermissionToken heldToken = GetToken( perm );
            if (heldToken.m_index != token.m_index)
                return false;

            if (token.m_type != heldToken.m_type)
                return false;

            if (perm.GetType().Module.Assembly == Assembly.GetExecutingAssembly() &&
                heldToken.m_index >= BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED)
                return false;

            return true;
        }

#if FEATURE_CAS_POLICY
        public SecurityElement ToXml()
        {
            Contract.Assert( (m_type & PermissionTokenType.DontKnow) == 0, "Should have valid token type when ToXml is called" );
            SecurityElement elRoot = new SecurityElement( "PermissionToken" );
            if ((m_type & PermissionTokenType.BuiltIn) != 0)
                elRoot.AddAttribute( "Index", "" + this.m_index );
            else
                elRoot.AddAttribute( "Name", SecurityElement.Escape( m_strTypeName ) );
            elRoot.AddAttribute("Type", m_type.ToString("F"));
            return elRoot;
        }

        public void FromXml(SecurityElement elRoot)
        {
            // For the most part there is no parameter checking here since this is an
            // internal class and the serialization/deserialization path is controlled.

            if (!elRoot.Tag.Equals( "PermissionToken" ))
                Contract.Assert( false, "Tried to deserialize non-PermissionToken element here" );

            String strName = elRoot.Attribute( "Name" );
            PermissionToken realToken;
            if (strName != null)
                realToken = GetToken( strName, true );
            else
                realToken = FindTokenByIndex( Int32.Parse( elRoot.Attribute( "Index" ), CultureInfo.InvariantCulture ) );
            
            this.m_index = realToken.m_index;
            this.m_type = (PermissionTokenType) Enum.Parse(typeof(PermissionTokenType), elRoot.Attribute("Type"));
            Contract.Assert((this.m_type & PermissionTokenType.DontKnow) == 0, "Should have valid token type when FromXml is called.");
            this.m_strTypeName = realToken.m_strTypeName;
        }
#endif // FEATURE_CAS_POLICY
    }

    // Package access only
    internal class PermissionTokenFactory
    {
        private volatile int       m_size;
        private volatile int       m_index;
        private volatile Hashtable m_tokenTable;    // Cache of tokens by class string name
        private volatile Hashtable m_handleTable;   // Cache of tokens by type handle (IntPtr)
        private volatile Hashtable m_indexTable;    // Cache of tokens by index


        // We keep an array of tokens for our built-in permissions.
        // This is ordered in terms of unrestricted perms first, normals
        // second.  Of course, all the ordering is based on the individual
        // permissions sticking to the deal, so we do some simple boundary
        // checking but mainly leave it to faith.

        private volatile PermissionToken[] m_builtIn;

        private const String s_unrestrictedPermissionInferfaceName = "System.Security.Permissions.IUnrestrictedPermission";

        internal PermissionTokenFactory( int size )
        {
            m_builtIn = new PermissionToken[BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED];

            m_size = size;
            m_index = BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED;
            m_tokenTable = null;
            m_handleTable = new Hashtable(size);
            m_indexTable = new Hashtable(size);
        }

#if FEATURE_CAS_POLICY
        [SecuritySafeCritical]
        internal PermissionToken FindToken( Type cls )
        {
            IntPtr typePtr = cls.TypeHandle.Value;
            PermissionToken tok = (PermissionToken)m_handleTable[typePtr];

            if (tok != null)
                return tok;

            if (m_tokenTable == null)
                return null;

            tok = (PermissionToken)m_tokenTable[cls.AssemblyQualifiedName];

            if (tok != null)
            {
                lock (this)
                {
                    m_handleTable.Add(typePtr, tok);
                }
            }

            return tok;
        }
#endif // FEATURE_CAS_POLICY

        internal PermissionToken FindTokenByIndex( int i )
        {
            PermissionToken token;

            if (i < BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED)
            {
                token = BuiltInGetToken( i, null, null );
            }
            else
            {
                token = (PermissionToken)m_indexTable[i];
            }

            return token;
        }

        [SecuritySafeCritical]
        internal PermissionToken GetToken(Type cls, IPermission perm)
        {
            Contract.Assert( cls != null, "Must pass in valid type" );

            IntPtr typePtr = cls.TypeHandle.Value;
            object tok = m_handleTable[typePtr];
            if (tok == null)
            {
                String typeStr = cls.AssemblyQualifiedName;
                tok = m_tokenTable != null ? m_tokenTable[typeStr] : null; // Assumes asynchronous lookups are safe

                if (tok == null)
                {
                    lock (this)
                    {
                        if (m_tokenTable != null)
                        {
                            tok = m_tokenTable[typeStr]; // Make sure it wasn't just added
                        }
                        else
                            m_tokenTable = new Hashtable(m_size, 1.0f, new PermissionTokenKeyComparer());

                        if (tok == null)
                        {
                            if (perm != null)
                            {
                                tok = new PermissionToken( m_index++, PermissionTokenType.IUnrestricted, typeStr );
                            }
                            else
                            {
                                if (cls.GetInterface(s_unrestrictedPermissionInferfaceName) != null)
                                    tok = new PermissionToken( m_index++, PermissionTokenType.IUnrestricted, typeStr );
                                else
                                    tok = new PermissionToken( m_index++, PermissionTokenType.Normal, typeStr );
                            }
                            m_tokenTable.Add(typeStr, tok);
                            m_indexTable.Add(m_index - 1, tok);
                            PermissionToken.s_tokenSet.SetItem( ((PermissionToken)tok).m_index, tok );
                        }

                        if (!m_handleTable.Contains(typePtr))
                            m_handleTable.Add( typePtr, tok );
                    }
                }
                else
                {
                    lock (this)
                    {
                        if (!m_handleTable.Contains(typePtr))
                            m_handleTable.Add( typePtr, tok );
                    }
                }
            }

            if ((((PermissionToken)tok).m_type & PermissionTokenType.DontKnow) != 0)
            {
                if (perm != null)
                {
                    Contract.Assert( !(perm is IBuiltInPermission), "This should not be called for built-ins" );
                    ((PermissionToken)tok).m_type = PermissionTokenType.IUnrestricted;
#if FEATURE_CAS_POLICY
                    ((PermissionToken)tok).m_strTypeName = perm.GetType().AssemblyQualifiedName;
#endif // FEATURE_CAS_POLICY
                }
                else
                {
                    Contract.Assert( cls.GetInterface( "System.Security.Permissions.IBuiltInPermission" ) == null, "This shoudl not be called for built-ins" );
                    if (cls.GetInterface(s_unrestrictedPermissionInferfaceName) != null)
                        ((PermissionToken)tok).m_type = PermissionTokenType.IUnrestricted;
                    else
                        ((PermissionToken)tok).m_type = PermissionTokenType.Normal;
#if FEATURE_CAS_POLICY
                    ((PermissionToken)tok).m_strTypeName = cls.AssemblyQualifiedName;
#endif // FEATURE_CAS_POLICY
                }
            }

            return (PermissionToken)tok;
        }

        internal PermissionToken GetToken(String typeStr)
        {
            Object tok = null;
            tok = m_tokenTable != null ? m_tokenTable[typeStr] : null; // Assumes asynchronous lookups are safe
            if (tok == null)
            {
                lock (this)
                {
                    if (m_tokenTable != null)
                    {
                        tok = m_tokenTable[typeStr]; // Make sure it wasn't just added
                    }
                    else
                        m_tokenTable = new Hashtable(m_size, 1.0f, new PermissionTokenKeyComparer());
                        
                    if (tok == null)
                    {
                        tok = new PermissionToken( m_index++, PermissionTokenType.DontKnow, typeStr );
                        m_tokenTable.Add(typeStr, tok);
                        m_indexTable.Add(m_index - 1, tok);
                        PermissionToken.s_tokenSet.SetItem(((PermissionToken)tok).m_index, tok);
                    }
                }
            }

            return (PermissionToken)tok;
        }

        internal PermissionToken BuiltInGetToken( int index, IPermission perm, Type cls )
        {
            PermissionToken token = Volatile.Read(ref m_builtIn[index]);

            if (token == null)
            {
                lock (this)
                {
                    token = m_builtIn[index];

                    if (token == null)
                    {
                        PermissionTokenType permType = PermissionTokenType.DontKnow;

                        if (perm != null)
                        {
                            permType = PermissionTokenType.IUnrestricted;
                        }
                        else if (cls != null)
                        {
                            permType = PermissionTokenType.IUnrestricted;
                        }

                        token = new PermissionToken( index, permType | PermissionTokenType.BuiltIn, null );
                        Volatile.Write(ref m_builtIn[index], token);
                        PermissionToken.s_tokenSet.SetItem( token.m_index, token );
                    }
                }
            }

            if ((token.m_type & PermissionTokenType.DontKnow) != 0)
            {
                    token.m_type = PermissionTokenType.BuiltIn;

                    if (perm != null)
                    {
                        token.m_type |= PermissionTokenType.IUnrestricted;
                    }
                    else if (cls != null)
                    {
                        token.m_type |= PermissionTokenType.IUnrestricted;
                    }
                    else
                    {
                        token.m_type |= PermissionTokenType.DontKnow;
                    }
            }

            return token;
        }
    }
}
