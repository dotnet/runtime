// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Diagnostics;

namespace System.Net
{
    internal class NTAuthentication
    {
        private static readonly Type s_type = typeof(NegotiateStream).Assembly.GetType("System.Net.NTAuthentication");
        private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly ConstructorInfo _ntAuthentication;
        private readonly MethodInfo _getOutgoingBlogString;
        private readonly MethodInfo _getOutgoingBlobBytes;
        private readonly MethodInfo _verifySignature;
        private readonly MethodInfo _makeSignature;
        private readonly MethodInfo _closeContext;
        private readonly MethodInfo _isCompleted;
        private object _instance;

        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            ConstructorInfo[] ctors = s_type.GetConstructors(NonPublicInstance & ~BindingFlags.Default);
            Debug.Assert(ctors.Length == 1);

            _ntAuthentication = ctors[0];
            _getOutgoingBlogString = s_type.GetMethod("GetOutgoingBlob", NonPublicInstance, null, new Type[] { typeof(string)}, null);
            _getOutgoingBlobBytes = s_type.GetMethod("GetOutgoingBlob", NonPublicInstance, null, new Type[] { typeof(byte[]), typeof(bool) }, null);
            _verifySignature = s_type.GetMethod("VerifySignature", NonPublicInstance);
            _makeSignature = s_type.GetMethod("MakeSignature", NonPublicInstance);
            _closeContext = s_type.GetMethod("CloseContext", NonPublicInstance);
            _isCompleted = s_type.GetProperty("IsCompleted", NonPublicInstance).GetMethod;

            _instance = _ntAuthentication.Invoke(new object[] { isServer, package, credential, spn, requestedContextFlags, channelBinding });
        }

        internal string GetOutgoingBlob(string challenge)
        {
            return (string)_getOutgoingBlogString.Invoke(_instance, new object[] { challenge });
        }

        internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError)
        {
            return (byte[])_getOutgoingBlobBytes.Invoke(_instance, new object[] { incomingBlob, throwOnError });
        }

        internal int VerifySignature(byte[] buffer, int offset, int count)
        {
            return (int)_verifySignature.Invoke(_instance, new object[] { buffer, offset, count });
        }

        internal int MakeSignature(byte[] buffer, int offset, int count, ref byte[] output)
        {
            return (int)_makeSignature.Invoke(_instance, new object[] { buffer, offset, count, output });
        }

        internal void CloseContext()
        {
            _closeContext.Invoke(_instance, null);
        }

        internal bool IsCompleted => (bool)_isCompleted.Invoke(_instance, null);
    }

    [Flags]
    internal enum ContextFlags
    {
        None = 0,
        Delegate = 0x00000001,
        MutualAuth = 0x00000002,
        ReplayDetect = 0x00000004,
        SequenceDetect = 0x00000008,
        Confidentiality = 0x00000010,
        UseSessionKey = 0x00000020,
        //AllocateMemory = 0x00000100,
        Connection = 0x00000800,
        InitExtendedError = 0x00004000,
        AcceptExtendedError = 0x00008000,
        InitStream = 0x00008000,
        AcceptStream = 0x00010000,
        InitIntegrity = 0x00010000,
        AcceptIntegrity = 0x00020000,
        InitManualCredValidation = 0x00080000,
        InitUseSuppliedCreds = 0x00000080,
        InitIdentify = 0x00020000,
        AcceptIdentify = 0x00080000,
        ProxyBindings = 0x04000000,
        AllowMissingBindings = 0x10000000,
        UnverifiedTargetName = 0x20000000,
    }
}
