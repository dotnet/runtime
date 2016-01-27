// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** PURPOSE:  Helpers for XML input & output
**
===========================================================*/
namespace System.Security.Util  {
    
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.IO;
    using System.Text;
    using System.Runtime.CompilerServices;
    using PermissionState = System.Security.Permissions.PermissionState;
    using BindingFlags = System.Reflection.BindingFlags;
    using Assembly = System.Reflection.Assembly;
    using System.Threading;
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics.Contracts;

    internal static class XMLUtil
    {
        //
        // Warning: Element constructors have side-effects on their
        //          third argument.
        //
        
        private const String BuiltInPermission = "System.Security.Permissions.";
#if FEATURE_CAS_POLICY        
        private const String BuiltInMembershipCondition = "System.Security.Policy.";
        private const String BuiltInCodeGroup = "System.Security.Policy.";
        private const String BuiltInApplicationSecurityManager = "System.Security.Policy.";
        private static readonly char[] sepChar =  {',', ' '};
#endif        
        public static SecurityElement
        NewPermissionElement (IPermission ip)
        {
            return NewPermissionElement (ip.GetType ().FullName) ;
        }
        
        public static SecurityElement
        NewPermissionElement (String name)
        {
            SecurityElement ecr = new SecurityElement( "Permission" );
            ecr.AddAttribute( "class", name );
            return ecr;
        }
        
        public static void
        AddClassAttribute( SecurityElement element, Type type, String typename )
        {
            // Replace any quotes with apostrophes so that we can include quoted materials
            // within classnames.  Notably the assembly name member 'loc' uses a quoted string.
        
            // NOTE: this makes assumptions as to what reflection is expecting for a type string
            // it will need to be updated if reflection changes what it wants.
        
            if ( typename == null )
                typename = type.FullName;
            Contract.Assert( type.FullName.Equals( typename ), "Incorrect class name passed! Was : " + typename + " Shoule be: " + type.FullName);
            element.AddAttribute( "class", typename + ", " + type.Module.Assembly.FullName.Replace( '\"', '\'' ) );
        }
        
        internal static bool ParseElementForAssemblyIdentification(SecurityElement el,
                                                                   out String className,
                                                                   out String assemblyName, // for example "WindowsBase"
                                                                   out String assemblyVersion)
        {

            className = null;
            assemblyName = null;
            assemblyVersion = null;
            
            String fullClassName = el.Attribute( "class" );
            
            if (fullClassName == null)
            {
                return false;
            }
            if (fullClassName.IndexOf('\'') >= 0)
            {
                fullClassName = fullClassName.Replace( '\'', '\"' );
            }

            int commaIndex = fullClassName.IndexOf( ',' );
            int namespaceClassNameLength;
            
            // If the classname is tagged with assembly information, find where
            // the assembly information begins.
            
            if (commaIndex == -1)
            {
                return false;
            }

            namespaceClassNameLength = commaIndex;
            className = fullClassName.Substring(0, namespaceClassNameLength);
            String assemblyFullName = fullClassName.Substring(commaIndex + 1);
            AssemblyName an = new AssemblyName(assemblyFullName);
            assemblyName = an.Name;
            assemblyVersion = an.Version.ToString();
            return true;
        }
        [System.Security.SecurityCritical]  // auto-generated
        private static bool
        ParseElementForObjectCreation( SecurityElement el,
                                       String requiredNamespace,
                                       out String className,
                                       out int classNameStart,
                                       out int classNameLength )
        {
            className = null;
            classNameStart = 0;
            classNameLength = 0;
            
            int requiredNamespaceLength = requiredNamespace.Length;

            String fullClassName = el.Attribute( "class" );
            
            if (fullClassName == null)
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_NoClass" ) );
            }
            
            if (fullClassName.IndexOf('\'') >= 0)
            {
                fullClassName = fullClassName.Replace( '\'', '\"' );
            }
            
            if (!PermissionToken.IsMscorlibClassName( fullClassName ))
            {
                return false;
            }

            int commaIndex = fullClassName.IndexOf( ',' );
            int namespaceClassNameLength;
            
            // If the classname is tagged with assembly information, find where
            // the assembly information begins.
            
            if (commaIndex == -1)
            {
                namespaceClassNameLength = fullClassName.Length;
            }
            else
            {
                namespaceClassNameLength = commaIndex;
            }

            // Only if the length of the class name is greater than the namespace info
            // on our requiredNamespace do we continue
            // with our check.
            
            if (namespaceClassNameLength > requiredNamespaceLength)
            {
                // Make sure we are in the required namespace.
                if (fullClassName.StartsWith(requiredNamespace, StringComparison.Ordinal))
                {
                    className = fullClassName;
                    classNameLength = namespaceClassNameLength - requiredNamespaceLength;
                    classNameStart = requiredNamespaceLength;
                    return true;
                }
            }
            
            return false;
        }

#if FEATURE_CAS_POLICY
        public static String SecurityObjectToXmlString(Object ob)
        {
            if(ob == null)
                return "";
            PermissionSet pset = ob as PermissionSet;
            if(pset != null)
                return pset.ToXml().ToString();
            return ((IPermission)ob).ToXml().ToString();
        }

        [System.Security.SecurityCritical]  // auto-generated
        public static Object XmlStringToSecurityObject(String s)
        {
            if(s == null)
                return null;
            if(s.Length < 1)
                return null;
            return SecurityElement.FromString(s).ToSecurityObject();
        }
#endif // FEATURE_CAS_POLICY

        [SecuritySafeCritical]
        public static IPermission
        CreatePermission (SecurityElement el, PermissionState permState, bool ignoreTypeLoadFailures)
        {
            if (el == null || !(el.Tag.Equals("Permission") || el.Tag.Equals("IPermission")) )
                throw new ArgumentException( String.Format( null, Environment.GetResourceString( "Argument_WrongElementType" ), "<Permission>" ) ) ;
            Contract.EndContractBlock();
    
            String className;
            int classNameLength;
            int classNameStart;
            
            if (!ParseElementForObjectCreation( el,
                                                BuiltInPermission,
                                                out className,
                                                out classNameStart,
                                                out classNameLength ))
            {
                goto USEREFLECTION;
            }
                                              
            // We have a built in permission, figure out which it is.
                    
            // UIPermission
            // FileIOPermission
            // SecurityPermission
            // PrincipalPermission
            // ReflectionPermission
            // FileDialogPermission
            // EnvironmentPermission
            // GacIdentityPermission
            // UrlIdentityPermission
            // SiteIdentityPermission
            // ZoneIdentityPermission
            // KeyContainerPermission
            // UnsafeForHostPermission
            // HostProtectionPermission
            // StrongNameIdentityPermission
#if !FEATURE_CORECLR                              
            // IsolatedStorageFilePermission
#endif
            // RegistryPermission
            // PublisherIdentityPermission

            switch (classNameLength)
            {
                case 12:
                    // UIPermission
                    if (String.Compare(className, classNameStart, "UIPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new UIPermission( permState );
                    else
                        goto USEREFLECTION;
                                
                case 16:
                    // FileIOPermission
                    if (String.Compare(className, classNameStart, "FileIOPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new FileIOPermission( permState );
                    else
                        goto USEREFLECTION;
                            
                case 18:
                    // RegistryPermission
                    // SecurityPermission
                    if (className[classNameStart] == 'R')
                    {
                        if (String.Compare(className, classNameStart, "RegistryPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new RegistryPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "SecurityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new SecurityPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                  
#if !FEATURE_CORECLR              
                case 19:
                    // PrincipalPermission
                    if (String.Compare(className, classNameStart, "PrincipalPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new PrincipalPermission( permState );
                    else
                        goto USEREFLECTION;
#endif // !FEATURE_CORECLR
                case 20:
                    // ReflectionPermission
                    // FileDialogPermission
                    if (className[classNameStart] == 'R')
                    {
                        if (String.Compare(className, classNameStart, "ReflectionPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new ReflectionPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "FileDialogPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new FileDialogPermission( permState );
                        else
                            goto USEREFLECTION;
                    }

                case 21:
                    // EnvironmentPermission
                    // UrlIdentityPermission
                    // GacIdentityPermission
                    if (className[classNameStart] == 'E')
                    {
                        if (String.Compare(className, classNameStart, "EnvironmentPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new EnvironmentPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else if (className[classNameStart] == 'U')
                    {
                        if (String.Compare(className, classNameStart, "UrlIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new UrlIdentityPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "GacIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new GacIdentityPermission( permState );
                        else
                            goto USEREFLECTION;
                    }

                            
                case 22:
                    // SiteIdentityPermission
                    // ZoneIdentityPermission
                    // KeyContainerPermission
                    if (className[classNameStart] == 'S')
                    {
                        if (String.Compare(className, classNameStart, "SiteIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new SiteIdentityPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else if (className[classNameStart] == 'Z')
                    {
                        if (String.Compare(className, classNameStart, "ZoneIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new ZoneIdentityPermission( permState );
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "KeyContainerPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new KeyContainerPermission( permState );
                        else
                            goto USEREFLECTION;
                    }


                case 24:
                    // HostProtectionPermission
                    if (String.Compare(className, classNameStart, "HostProtectionPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new HostProtectionPermission( permState );
                    else
                        goto USEREFLECTION;

#if FEATURE_X509 && FEATURE_CAS_POLICY
                case 27:
                    // PublisherIdentityPermission
                    if (String.Compare(className, classNameStart, "PublisherIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new PublisherIdentityPermission( permState );
                    else
                        goto USEREFLECTION;
#endif // FEATURE_X509 && FEATURE_CAS_POLICY

                case 28:
                    // StrongNameIdentityPermission
                    if (String.Compare(className, classNameStart, "StrongNameIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new StrongNameIdentityPermission( permState );
                    else
                        goto USEREFLECTION;
#if !FEATURE_CORECLR                                                          
                case 29:
                    // IsolatedStorageFilePermission
                    if (String.Compare(className, classNameStart, "IsolatedStorageFilePermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new IsolatedStorageFilePermission( permState );
                    else
                        goto USEREFLECTION;
#endif                            
                default:
                    goto USEREFLECTION;
            }
    
USEREFLECTION:

            Object[] objs = new Object[1];
            objs[0] = permState;

            Type permClass = null;
            IPermission perm = null;

            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
            permClass = GetClassFromElement(el, ignoreTypeLoadFailures);
            if (permClass == null)
                return null;
            if (!(typeof(IPermission).IsAssignableFrom(permClass)))
                throw new ArgumentException( Environment.GetResourceString("Argument_NotAPermissionType") );

            perm = (IPermission) Activator.CreateInstance(permClass, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, objs, null );

            return perm;
        }

#if FEATURE_CAS_POLICY
#pragma warning disable 618 // CodeGroups are obsolete
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static CodeGroup
        CreateCodeGroup (SecurityElement el)
        {
            if (el == null || !el.Tag.Equals("CodeGroup"))
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Argument_WrongElementType" ), "<CodeGroup>" ) ) ;
            Contract.EndContractBlock();
    
            String className;
            int classNameLength;
            int classNameStart;

            if (!ParseElementForObjectCreation( el,
                                                BuiltInCodeGroup,
                                                out className,
                                                out classNameStart,
                                                out classNameLength ))
            {
                goto USEREFLECTION;
            }
    
            switch (classNameLength)
            {
                case 12:
                    // NetCodeGroup
                    if (String.Compare(className, classNameStart, "NetCodeGroup", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new NetCodeGroup();
                    else
                        goto USEREFLECTION;

                case 13:
                    // FileCodeGroup
                    if (String.Compare(className, classNameStart, "FileCodeGroup", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new FileCodeGroup();
                    else
                        goto USEREFLECTION;
                case 14:
                    // UnionCodeGroup
                    if (String.Compare(className, classNameStart, "UnionCodeGroup", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new UnionCodeGroup();
                    else
                        goto USEREFLECTION;
                                
                case 19:
                    // FirstMatchCodeGroup
                    if (String.Compare(className, classNameStart, "FirstMatchCodeGroup", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new FirstMatchCodeGroup();
                    else
                        goto USEREFLECTION;

                default:
                    goto USEREFLECTION;
            }

USEREFLECTION: 
            Type groupClass = null;
            CodeGroup group = null;

            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
            groupClass = GetClassFromElement(el, true);
            if (groupClass == null)
                return null;
            if (!(typeof(CodeGroup).IsAssignableFrom(groupClass)))
                throw new ArgumentException( Environment.GetResourceString("Argument_NotACodeGroupType") );

            group = (CodeGroup) Activator.CreateInstance(groupClass, true);

            Contract.Assert( groupClass.Module.Assembly != Assembly.GetExecutingAssembly(),
                "This path should not get called for mscorlib based classes" );

            return group;
        }
#pragma warning restore 618
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static IMembershipCondition
        CreateMembershipCondition( SecurityElement el )
        {
            if (el == null || !el.Tag.Equals("IMembershipCondition"))
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Argument_WrongElementType" ), "<IMembershipCondition>" ) ) ;
            Contract.EndContractBlock();
    
            String className;
            int classNameStart;
            int classNameLength;
            
            if (!ParseElementForObjectCreation( el,
                                                BuiltInMembershipCondition,
                                                out className,
                                                out classNameStart,
                                                out classNameLength ))
            {
                goto USEREFLECTION;
            }

            // We have a built in membership condition, figure out which it is.
                    
            // Here's the list of built in membership conditions as of 9/17/2002
            // System.Security.Policy.AllMembershipCondition
            // System.Security.Policy.URLMembershipCondition
            // System.Security.Policy.SHA1MembershipCondition
            // System.Security.Policy.SiteMembershipCondition
            // System.Security.Policy.ZoneMembershipCondition                                                                                                                                                              
            // System.Security.Policy.PublisherMembershipCondition
            // System.Security.Policy.StrongNameMembershipCondition
            // System.Security.Policy.ApplicationMembershipCondition
            // System.Security.Policy.DomainApplicationMembershipCondition
            // System.Security.Policy.ApplicationDirectoryMembershipCondition
                    
            switch (classNameLength)
            {
                case 22:
                    // AllMembershipCondition
                    // URLMembershipCondition
                    if (className[classNameStart] == 'A')
                    {
                        if (String.Compare(className, classNameStart, "AllMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new AllMembershipCondition();
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "UrlMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new UrlMembershipCondition();
                        else
                            goto USEREFLECTION;
                    }
                                
                case 23:
                    // HashMembershipCondition
                    // SiteMembershipCondition
                    // ZoneMembershipCondition                                                                                                                                                              
                    if (className[classNameStart] == 'H')
                    {
                        if (String.Compare(className, classNameStart, "HashMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new HashMembershipCondition();
                        else
                            goto USEREFLECTION;
                    }
                    else if (className[classNameStart] == 'S')
                    {
                        if (String.Compare(className, classNameStart, "SiteMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new SiteMembershipCondition();
                        else
                            goto USEREFLECTION;
                    }
                    else
                    {
                        if (String.Compare(className, classNameStart, "ZoneMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                            return new ZoneMembershipCondition();
                        else
                            goto USEREFLECTION;
                    }

                case 28:
                    // PublisherMembershipCondition
                    if (String.Compare(className, classNameStart, "PublisherMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new PublisherMembershipCondition();
                    else
                        goto USEREFLECTION;

                case 29:
                    // StrongNameMembershipCondition
                    if (String.Compare(className, classNameStart, "StrongNameMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new StrongNameMembershipCondition();
                    else
                        goto USEREFLECTION;

                case 39:
                    // ApplicationDirectoryMembershipCondition
                    if (String.Compare(className, classNameStart, "ApplicationDirectoryMembershipCondition", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new ApplicationDirectoryMembershipCondition();
                    else
                        goto USEREFLECTION;

                default:
                    goto USEREFLECTION;
            }

USEREFLECTION:
            Type condClass = null;
            IMembershipCondition cond = null;
    
            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
            condClass = GetClassFromElement(el, true);
            if (condClass == null)
                return null;
            if (!(typeof(IMembershipCondition).IsAssignableFrom(condClass)))
                throw new ArgumentException( Environment.GetResourceString("Argument_NotAMembershipCondition") );

            cond = (IMembershipCondition) Activator.CreateInstance(condClass, true);

            return cond;
        }
#endif //#if FEATURE_CAS_POLICY
        internal static Type
        GetClassFromElement (SecurityElement el, bool ignoreTypeLoadFailures)
        {
            String className = el.Attribute( "class" );

            if (className == null)
            {
                if (ignoreTypeLoadFailures)
                    return null;
                else
                    throw new ArgumentException( String.Format( null, Environment.GetResourceString("Argument_InvalidXMLMissingAttr"), "class") );
            }

            if (ignoreTypeLoadFailures)
            {
                try
                {
                    return Type.GetType(className, false, false);               
                }
                catch (SecurityException)
                {
                    return null;
                }
            }
            else
                return Type.GetType(className, true, false);               
        }

        public static bool
        IsPermissionElement (IPermission ip,
                             SecurityElement el)
        {
            if (!el.Tag.Equals ("Permission") && !el.Tag.Equals ("IPermission"))
                return false;
                
            return true;
        }
        
        public static bool
        IsUnrestricted (SecurityElement el)
        {
            String sUnrestricted = el.Attribute( "Unrestricted" );
            
            if (sUnrestricted == null)
                return false;

            return sUnrestricted.Equals( "true" ) || sUnrestricted.Equals( "TRUE" ) || sUnrestricted.Equals( "True" );
        }


        public static String BitFieldEnumToString( Type type, Object value )
        {
            int iValue = (int)value;

            if (iValue == 0)
                return Enum.GetName( type, 0 );

            StringBuilder result = StringBuilderCache.Acquire();
            bool first = true;
            int flag = 0x1;

            for (int i = 1; i < 32; ++i)
            {
                if ((flag & iValue) != 0)
                {
                    String sFlag = Enum.GetName( type, flag );

                    if (sFlag == null)
                        continue;

                    if (!first)
                    {
                        result.Append( ", " );
                    }

                    result.Append( sFlag );
                    first = false;
                }

                flag = flag << 1;
            }
            
            return StringBuilderCache.GetStringAndRelease(result);
        } 
    }
}
