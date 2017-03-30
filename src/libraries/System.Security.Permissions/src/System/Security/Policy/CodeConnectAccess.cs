// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Serializable]
    public partial class CodeConnectAccess
    {
        public static readonly string AnyScheme;
        public static readonly int DefaultPort;
        public static readonly int OriginPort;
        public static readonly string OriginScheme;
        public CodeConnectAccess(string allowScheme, int allowPort) { }
        public int Port { get { return 0; } }
        public string Scheme { get { return null; } }
        public static CodeConnectAccess CreateAnySchemeAccess(int allowPort) { return default(CodeConnectAccess); }
        public static CodeConnectAccess CreateOriginSchemeAccess(int allowPort) { return default(CodeConnectAccess); }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
    }
}
