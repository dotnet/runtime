// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
// The SecurityManager class provides a general purpose API for interacting
// with the security system.
//

namespace System.Security {
    using System;
    using System.Security.Util;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
#if FEATURE_CLICKONCE
    using System.Runtime.Hosting;
#endif // FEATURE_CLICKONCE
    using System.Text;
    using System.Threading;
    using System.Reflection;
    using System.IO;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum PolicyLevelType
    {
        User = 0,
        Machine = 1,
        Enterprise = 2,
        AppDomain = 3
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    static public class SecurityManager {
#if FEATURE_CAS_POLICY
        private static volatile SecurityPermission executionSecurityPermission = null;

        private static PolicyManager polmgr = new PolicyManager();
        internal static PolicyManager PolicyManager {
            get {
                return polmgr;
            }
        }

        //
        // Public APIs
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("IsGranted is obsolete and will be removed in a future release of the .NET Framework.  Please use the PermissionSet property of either AppDomain or Assembly instead.")]
        public static bool IsGranted( IPermission perm )
        {
            if (perm == null)
                return true;

            PermissionSet granted = null, denied = null;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            GetGrantedPermissions( JitHelpers.GetObjectHandleOnStack(ref granted),
                                   JitHelpers.GetObjectHandleOnStack(ref denied),
                                   JitHelpers.GetStackCrawlMarkHandle(ref stackMark) );
            return granted.Contains( perm ) && (denied == null || !denied.Contains( perm ));
        }

        // Get a sandbox permission set that the CLR considers safe to grant an application with the given
        // evidence.  Note that this API is not a policy API, but rather a host helper API so that a host can
        // determine if an application's requested permission set is reasonable.  This is esentially just a
        // hard coded mapping of Zone -> Sandbox and is not configurable in any way.
        public static PermissionSet GetStandardSandbox(Evidence evidence)
        {
            if (evidence == null)
                throw new ArgumentNullException("evidence");
            Contract.EndContractBlock();

            //
            // The top-level switch for grant set is based upon Zone
            //   MyComputer -> FullTrust
            //   Intranet   -> LocalIntranet
            //   Trusted    -> Internet
            //   Internet   -> Internet
            //   All else   -> Nothing
            //   
            //   Both the Internet and LocalIntranet zones can have permission set extensions applied to them
            //   if there is Activation.
            //   

            Zone zone = evidence.GetHostEvidence<Zone>();
            if (zone == null)
            {
                return new PermissionSet(PermissionState.None);
            }
#if FEATURE_CAS_POLICY
            else if (zone.SecurityZone == SecurityZone.MyComputer)
            {
                return new PermissionSet(PermissionState.Unrestricted);
            }
            else if (zone.SecurityZone == SecurityZone.Intranet)
            {
                PermissionSet intranetGrantSet = BuiltInPermissionSets.LocalIntranet;

                // We also need to add in same site web and file IO permission
                PolicyStatement webPolicy =
                    new NetCodeGroup(new AllMembershipCondition()).Resolve(evidence);
                PolicyStatement filePolicy =
                    new FileCodeGroup(new AllMembershipCondition(), FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery).Resolve(evidence);

                if (webPolicy != null)
                {
                    intranetGrantSet.InplaceUnion(webPolicy.PermissionSet);
                }
                if (filePolicy != null)
                {
                    intranetGrantSet.InplaceUnion(filePolicy.PermissionSet);
                }

                return intranetGrantSet;
            }
            else if (zone.SecurityZone == SecurityZone.Internet ||
                     zone.SecurityZone == SecurityZone.Trusted)
            {
                PermissionSet internetGrantSet = BuiltInPermissionSets.Internet;

                // We also need to add in same site web permission
                PolicyStatement webPolicy =
                    new NetCodeGroup(new AllMembershipCondition()).Resolve(evidence);

                if (webPolicy != null)
                {
                    internetGrantSet.InplaceUnion(webPolicy.PermissionSet);
                }

                return internetGrantSet;
            }
#endif // FEATURE_CAS_POLICY
            else
            {
                return new PermissionSet(PermissionState.None);
            }
        }

        /// <internalonly/>
        [System.Security.SecurityCritical]  // auto-generated_required
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        static public void GetZoneAndOrigin( out ArrayList zone, out ArrayList origin )
        {
            StackCrawlMark mark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.GetZoneAndOrigin( ref mark, out zone, out origin );
        }
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy )]
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public PolicyLevel LoadPolicyLevelFromFile(string path, PolicyLevelType type)
        {
            if (path == null)
               throw new ArgumentNullException( "path" );
            Contract.EndContractBlock();

            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            // We need to retain V1.x compatibility by throwing the same exception type.
            if (!File.InternalExists(path))
                throw new ArgumentException( Environment.GetResourceString("Argument_PolicyFileDoesNotExist"));

            String fullPath = Path.GetFullPath( path );

            FileIOPermission perm = new FileIOPermission( PermissionState.None );
            perm.AddPathList( FileIOPermissionAccess.Read, fullPath );
            perm.AddPathList( FileIOPermissionAccess.Write, fullPath );
            perm.Demand();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    return LoadPolicyLevelFromStringHelper(reader.ReadToEnd(), path, type);
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy )]
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public PolicyLevel LoadPolicyLevelFromString(string str, PolicyLevelType type)
        {
            return LoadPolicyLevelFromStringHelper(str, null, type);
        }

        private static PolicyLevel LoadPolicyLevelFromStringHelper (string str, string path, PolicyLevelType type)
        {
            if (str == null)
                throw new ArgumentNullException( "str" );
            Contract.EndContractBlock();

            PolicyLevel level = new PolicyLevel(type, path);

            Parser parser = new Parser( str );
            SecurityElement elRoot = parser.GetTopElement();
            if (elRoot == null)
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Policy_BadXml" ), "configuration" ) );

            SecurityElement elMscorlib = elRoot.SearchForChildByTag( "mscorlib" );
            if (elMscorlib == null)
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Policy_BadXml" ), "mscorlib" ) );

            SecurityElement elSecurity = elMscorlib.SearchForChildByTag( "security" );
            if (elSecurity == null)
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Policy_BadXml" ), "security" ) );

            SecurityElement elPolicy = elSecurity.SearchForChildByTag( "policy" );
            if (elPolicy == null)
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Policy_BadXml" ), "policy" ) );

            SecurityElement elPolicyLevel = elPolicy.SearchForChildByTag( "PolicyLevel" );
            if (elPolicyLevel != null)
                level.FromXml( elPolicyLevel );
            else
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Policy_BadXml" ), "PolicyLevel" ) );

            return level;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy )]
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public void SavePolicyLevel( PolicyLevel level )
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            PolicyManager.EncodeLevel( level );
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public PermissionSet ResolvePolicy(Evidence evidence,
                                                  PermissionSet reqdPset,
                                                  PermissionSet optPset,
                                                  PermissionSet denyPset,
                                                  out PermissionSet denied)
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            return ResolvePolicy(evidence, reqdPset, optPset, denyPset, out denied, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public PermissionSet ResolvePolicy(Evidence evidence)
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            // If we aren't passed any evidence, just make an empty object
            if (evidence == null)
            {
                evidence = new Evidence();
            }

            return polmgr.Resolve(evidence);
        }

        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public PermissionSet ResolvePolicy( Evidence[] evidences )
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            if (evidences == null || evidences.Length == 0)
                evidences = new Evidence[] { null };

            PermissionSet retval = ResolvePolicy( evidences[0] );
            if (retval == null)
                return null;

            for (int i = 1; i < evidences.Length; ++i)
            {
                retval = retval.Intersect( ResolvePolicy( evidences[i] ) );
                if (retval == null || retval.IsEmpty())
                    return retval;
            }

            return retval;
        }

#if FEATURE_CAS_POLICY
        // Determine if the current thread would require a security context capture if the security state of
        // the thread needs to be re-created at a later point in time.  This can be used, for instance, if
        // sensitive data is being obtained after security demands succeed, and that data is to be cached. 
        // If there is an Assert up the stack, then we wouldn't want to cache the data without capturing the
        // corresponding security context to go along with it - otherwise we risk leaking data obtained
        // under an assert to code which may no longer be running with that assert in place.
        // 
        // A return value of false indicates that the CLR guarantees all of the following conditions are true:
        //   1. No partial trust AppDomains are on the stack
        //   2. No partial trust assemblies are on the stack
        //   3. There are no currently active PermitOnly or Deny modifiers on the stack
        //   
        // A return value of true means only that the CLR cannot guarantee that all of the conditions are
        // true, and not that one of the conditions really is false.
        // 
        // IMPORTANT: The above means is only reliable in the false return case.  If we say that the thread
        // does not require a context capture, then that answer is guaranteed to be correct.  However, we may
        // say that the thread does require a capture when it does not actually strictly need to capture the
        // state.  This is fine, as being overly conservative when capturing context will not lead to
        // security holes; being overly agresssive in avoding the capture could lead to holes however.
        // 
        // This API is SecurityCritical because its main use is to optimize away unnecessary security
        // context captures, which means that the code using it is security sensitive and needs to be audited.
        [SecurityCritical]
        public static bool CurrentThreadRequiresSecurityContextCapture()
        {
            // If we know that the thread is not made up of entirely full trust code, and that there are no
            // security stack modifiers on the thread, then there is no need to capture a security context.
            return !CodeAccessSecurityEngine.QuickCheckForAllDemands();
        }
#endif // FEATURE_CAS_POLICY

        //
        // This method resolves the policy for the specified evidence, but it
        // ignores the AppDomain level even when one is available in the current policy.
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PermissionSet ResolveSystemPolicy (Evidence evidence)
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            if (PolicyManager.IsGacAssembly(evidence))
            {
                return new PermissionSet(PermissionState.Unrestricted);
            }

            return polmgr.CodeGroupResolve(evidence, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public IEnumerator ResolvePolicyGroups(Evidence evidence)
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            return polmgr.ResolveCodeGroups(evidence);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static IEnumerator PolicyHierarchy()
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            return polmgr.PolicyHierarchy();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy )]
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static void SavePolicy()
        {
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            polmgr.Save();
        }


        [System.Security.SecurityCritical]  // auto-generated
        private static PermissionSet ResolveCasPolicy(Evidence evidence,
                                                      PermissionSet reqdPset,
                                                      PermissionSet optPset,
                                                      PermissionSet denyPset,
                                                      out PermissionSet denied,
                                                      out int securitySpecialFlags,
                                                      bool checkExecutionPermission)
        {
            Contract.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled);

            CodeAccessPermission.Assert(true);

            PermissionSet granted = ResolvePolicy(evidence,
                                                  reqdPset,
                                                  optPset,
                                                  denyPset,
                                                  out denied,
                                                  checkExecutionPermission);

            securitySpecialFlags = SecurityManager.GetSpecialFlags(granted, denied);
            return granted;
        }

        [System.Security.SecurityCritical]  // auto-generated
        static private PermissionSet ResolvePolicy(Evidence evidence,
                           PermissionSet reqdPset,
                           PermissionSet optPset,
                           PermissionSet denyPset,
                           out PermissionSet denied,
                           bool checkExecutionPermission)
        {
            Contract.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled);

            if (executionSecurityPermission == null)
                executionSecurityPermission = new SecurityPermission(SecurityPermissionFlag.Execution);

            PermissionSet requested = null;
            PermissionSet optional;
            PermissionSet allowed;

            Exception savedException = null;

            // We don't want to recurse back into here as a result of a
            // stackwalk during resolution. So simply assert full trust (this
            // implies that custom permissions cannot use any permissions that
            // don't implement IUnrestrictedPermission.
            // PermissionSet.s_fullTrust.Assert();

            // The requested set is the union of the minimal request and the
            // optional request. Minimal request defaults to empty, optional
            // is "AllPossible" (includes any permission that can be defined)
            // which is symbolized by null.
            optional = optPset;

            if (reqdPset == null)
                requested = optional;
            else
                // If optional is null, the requested set becomes null/"AllPossible".
                requested = optional == null ? null : reqdPset.Union(optional);

            // Make sure that the right to execute is requested (if this feature is
            // enabled).

            if (requested != null && !requested.IsUnrestricted())
                requested.AddPermission( executionSecurityPermission );

            // If we aren't passed any evidence, just make an empty object
            if (evidence == null)
            {
                evidence = new Evidence();
            }

            allowed = polmgr.Resolve(evidence);
            // Intersect the grant with the RequestOptional
            if (requested != null)
                allowed.InplaceIntersect(requested);

            // Check that we were granted the right to execute.
            if (checkExecutionPermission)
            {
                if (!allowed.Contains(executionSecurityPermission) ||
                    (denyPset != null && denyPset.Contains(executionSecurityPermission)))
                {
                    throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"),
                                              System.__HResults.CORSEC_E_NO_EXEC_PERM,
                                              savedException);
                }
            }

            // Check that we were granted at least the minimal set we asked for. Do
            // this before pruning away any overlap with the refused set so that
            // users have the flexability of defining minimal permissions that are
            // only expressable as set differences (e.g. allow access to "C:\" but
            // disallow "C:\Windows").
            if (reqdPset != null && !reqdPset.IsSubsetOf(allowed))
            {
                BCLDebug.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled, "Evaluating assembly level declarative security without legacy CAS policy enabled");
                throw new PolicyException(Environment.GetResourceString( "Policy_NoRequiredPermission" ),
                                          System.__HResults.CORSEC_E_MIN_GRANT_FAIL,
                                          savedException );
            }

            // Remove any granted permissions that are safe subsets of some denied
            // permission. The remaining denied permissions (if any) are returned
            // along with the modified grant set for use in checks.
            if (denyPset != null)
            {
                BCLDebug.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled, "Evaluating assembly level declarative security without legacy CAS policy enabled");
                denied = denyPset.Copy();
                allowed.MergeDeniedSet(denied);
                if (denied.IsEmpty())
                    denied = null;
            }
            else
                denied = null;

            allowed.IgnoreTypeLoadFailures = true;

            return allowed;
        }

        [Obsolete("Because execution permission checks can no longer be turned off, the CheckExecutionRights property no longer has any effect.")]
        static public bool CheckExecutionRights
        {
            get { return true; }

            set
            {
                // The setter for this property is a no-op since execution checking can no longer be turned off
            }
        }

        [Obsolete("Because security can no longer be turned off, the SecurityEnabled property no longer has any effect.")]
        public static bool SecurityEnabled
        {
            get { return true; }

            set
            {
                // The setter for this property is a no-op since security cannot be turned off
            }
        }
#endif // #if FEATURE_CAS_POLICY

        private static int[][] s_BuiltInPermissionIndexMap = {
            new int[] { BuiltInPermissionIndex.EnvironmentPermissionIndex, (int) PermissionType.EnvironmentPermission },
            new int[] { BuiltInPermissionIndex.FileDialogPermissionIndex, (int) PermissionType.FileDialogPermission },
            new int[] { BuiltInPermissionIndex.FileIOPermissionIndex, (int) PermissionType.FileIOPermission },
            new int[] { BuiltInPermissionIndex.ReflectionPermissionIndex, (int) PermissionType.ReflectionPermission },
            new int[] { BuiltInPermissionIndex.SecurityPermissionIndex, (int) PermissionType.SecurityPermission },
            new int[] { BuiltInPermissionIndex.UIPermissionIndex, (int) PermissionType.UIPermission }
        };

        private static CodeAccessPermission[] s_UnrestrictedSpecialPermissionMap = {
            new EnvironmentPermission(PermissionState.Unrestricted),
            new FileDialogPermission(PermissionState.Unrestricted),
            new FileIOPermission(PermissionState.Unrestricted),
            new ReflectionPermission(PermissionState.Unrestricted),
            new SecurityPermission(PermissionState.Unrestricted),
            new UIPermission(PermissionState.Unrestricted)
        };

        internal static int GetSpecialFlags (PermissionSet grantSet, PermissionSet deniedSet) {
            if ((grantSet != null && grantSet.IsUnrestricted()) && (deniedSet == null || deniedSet.IsEmpty())) {
                return -1;
            }
            else {
                SecurityPermission securityPermission = null;
#pragma warning disable 618
                SecurityPermissionFlag securityPermissionFlags = SecurityPermissionFlag.NoFlags;
#pragma warning restore 618
                ReflectionPermission reflectionPermission = null;
                ReflectionPermissionFlag reflectionPermissionFlags = ReflectionPermissionFlag.NoFlags;

                CodeAccessPermission[] specialPermissions = new CodeAccessPermission[6];
                if (grantSet != null) {
                    if (grantSet.IsUnrestricted()) {
#pragma warning disable 618
                        securityPermissionFlags = SecurityPermissionFlag.AllFlags;
#pragma warning restore 618
                        reflectionPermissionFlags = ReflectionPermission.AllFlagsAndMore;
                        for (int i = 0; i < specialPermissions.Length; i++) {
                            specialPermissions[i] = s_UnrestrictedSpecialPermissionMap[i];
                        }
                    }
                    else {
                        securityPermission = grantSet.GetPermission(BuiltInPermissionIndex.SecurityPermissionIndex) as SecurityPermission;
                        if (securityPermission != null)
                            securityPermissionFlags = securityPermission.Flags;
                        reflectionPermission = grantSet.GetPermission(BuiltInPermissionIndex.ReflectionPermissionIndex) as ReflectionPermission;
                        if (reflectionPermission != null)
                            reflectionPermissionFlags = reflectionPermission.Flags;
                        for (int i = 0; i < specialPermissions.Length; i++) {
                            specialPermissions[i] = grantSet.GetPermission(s_BuiltInPermissionIndexMap[i][0]) as CodeAccessPermission;
                        }
                    }
                }

                if (deniedSet != null) {
                    if (deniedSet.IsUnrestricted()) {
#pragma warning disable 618
                        securityPermissionFlags = SecurityPermissionFlag.NoFlags;
#pragma warning restore 618
                        reflectionPermissionFlags = ReflectionPermissionFlag.NoFlags;
                        for (int i = 0; i < s_BuiltInPermissionIndexMap.Length; i++) {
                            specialPermissions[i] = null;
                        }
                    }
                    else {
                        securityPermission = deniedSet.GetPermission(BuiltInPermissionIndex.SecurityPermissionIndex) as SecurityPermission;
                        if (securityPermission != null)
                            securityPermissionFlags &= ~securityPermission.Flags;
                        reflectionPermission = deniedSet.GetPermission(BuiltInPermissionIndex.ReflectionPermissionIndex) as ReflectionPermission;
                        if (reflectionPermission != null)
                            reflectionPermissionFlags &= ~reflectionPermission.Flags;
                        for (int i = 0; i < s_BuiltInPermissionIndexMap.Length; i++) {
                            CodeAccessPermission deniedSpecialPermission = deniedSet.GetPermission(s_BuiltInPermissionIndexMap[i][0]) as CodeAccessPermission;
                            if (deniedSpecialPermission != null && !deniedSpecialPermission.IsSubsetOf(null))
                                specialPermissions[i] = null; // we don't care about the exact value here.
                        }
                    }
                }
                int flags = MapToSpecialFlags(securityPermissionFlags, reflectionPermissionFlags);
                if (flags != -1) {
                    for (int i = 0; i < specialPermissions.Length; i++) {
                        if (specialPermissions[i] != null && ((IUnrestrictedPermission) specialPermissions[i]).IsUnrestricted())
                            flags |= (1 << (int) s_BuiltInPermissionIndexMap[i][1]);
                    }
                }
                return flags;
            }
        }

#pragma warning disable 618
        private static int MapToSpecialFlags (SecurityPermissionFlag securityPermissionFlags, ReflectionPermissionFlag reflectionPermissionFlags) {
            int flags = 0;
            if ((securityPermissionFlags & SecurityPermissionFlag.UnmanagedCode) == SecurityPermissionFlag.UnmanagedCode)
                flags |= (1 << (int) PermissionType.SecurityUnmngdCodeAccess);
            if ((securityPermissionFlags & SecurityPermissionFlag.SkipVerification) == SecurityPermissionFlag.SkipVerification)
                flags |= (1 << (int) PermissionType.SecuritySkipVerification);
            if ((securityPermissionFlags & SecurityPermissionFlag.Assertion) == SecurityPermissionFlag.Assertion)
                flags |= (1 << (int) PermissionType.SecurityAssert);
            if ((securityPermissionFlags & SecurityPermissionFlag.SerializationFormatter) == SecurityPermissionFlag.SerializationFormatter)
                flags |= (1 << (int) PermissionType.SecuritySerialization);
            if ((securityPermissionFlags & SecurityPermissionFlag.BindingRedirects) == SecurityPermissionFlag.BindingRedirects)
                flags |= (1 << (int) PermissionType.SecurityBindingRedirects);
            if ((securityPermissionFlags & SecurityPermissionFlag.ControlEvidence) == SecurityPermissionFlag.ControlEvidence)
                flags |= (1 << (int) PermissionType.SecurityControlEvidence);
            if ((securityPermissionFlags & SecurityPermissionFlag.ControlPrincipal) == SecurityPermissionFlag.ControlPrincipal)
                flags |= (1 << (int) PermissionType.SecurityControlPrincipal);

            if ((reflectionPermissionFlags & ReflectionPermissionFlag.RestrictedMemberAccess) == ReflectionPermissionFlag.RestrictedMemberAccess)
                flags |= (1 << (int)PermissionType.ReflectionRestrictedMemberAccess);
            if ((reflectionPermissionFlags & ReflectionPermissionFlag.MemberAccess) == ReflectionPermissionFlag.MemberAccess)
                flags |= (1 << (int) PermissionType.ReflectionMemberAccess);

            return flags;
        }
#pragma warning restore 618
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern bool IsSameType(String strLeft, String strRight);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool _SetThreadSecurity(bool bThreadSecurity);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void GetGrantedPermissions(ObjectHandleOnStack retGranted, ObjectHandleOnStack retDenied, StackCrawlMarkHandle stackMark);
    }
}
