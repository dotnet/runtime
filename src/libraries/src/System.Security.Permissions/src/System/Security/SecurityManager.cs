// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using System.Collections;

namespace System.Security
{
    public static partial class SecurityManager
    {
        [Obsolete("Because execution permission checks can no longer be turned off, the CheckExecutionRights property no longer has any effect.")]
        public static bool CheckExecutionRights { get; set; }
        [Obsolete("Because security can no longer be turned off, the SecurityEnabled property no longer has any effect.")]
        public static bool SecurityEnabled { get; set; }
        public static void GetZoneAndOrigin(out System.Collections.ArrayList zone, out System.Collections.ArrayList origin) { zone = default(System.Collections.ArrayList); origin = default(System.Collections.ArrayList); }
        [Obsolete("IsGranted is obsolete and will be removed in a future release of the .NET Framework.  Please use the PermissionSet property of either AppDomain or Assembly instead.")]
        public static bool IsGranted(IPermission perm) { return false; }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PolicyLevel LoadPolicyLevelFromFile(string path, PolicyLevelType type) { return default(PolicyLevel); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PolicyLevel LoadPolicyLevelFromString(string str, PolicyLevelType type) { return default(PolicyLevel); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static IEnumerator PolicyHierarchy() { return default(IEnumerator); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PermissionSet ResolvePolicy(Evidence evidence) { return default(PermissionSet); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PermissionSet ResolvePolicy(System.Security.Policy.Evidence evidence, PermissionSet reqdPset, PermissionSet optPset, PermissionSet denyPset, out PermissionSet denied) { denied = default(PermissionSet); return default(PermissionSet); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PermissionSet ResolvePolicy(System.Security.Policy.Evidence[] evidences) { return default(PermissionSet); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static IEnumerator ResolvePolicyGroups(System.Security.Policy.Evidence evidence) { return default(IEnumerator); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PermissionSet ResolveSystemPolicy(System.Security.Policy.Evidence evidence) { return default(PermissionSet); }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static void SavePolicy() { }
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static void SavePolicyLevel(PolicyLevel level) { }
    }
}
