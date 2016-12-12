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
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    internal static class XMLUtil
    {
        //
        // Warning: Element constructors have side-effects on their
        //          third argument.
        //
        
        private const String BuiltInPermission = "System.Security.Permissions.";

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
            Debug.Assert( type.FullName.Equals( typename ), "Incorrect class name passed! Was : " + typename + " Shoule be: " + type.FullName);
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
                case 28:
                    // StrongNameIdentityPermission
                    if (String.Compare(className, classNameStart, "StrongNameIdentityPermission", 0, classNameLength, StringComparison.Ordinal) == 0)
                        return new StrongNameIdentityPermission( permState );
                    else
                        goto USEREFLECTION;
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
