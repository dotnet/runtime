// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal class NTAuthentication
    {
        private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        internal static readonly Type s_type;
        internal static readonly ConstructorInfo s_ntAuthentication;
        internal static readonly MethodInfo s_getOutgoingBlogString;
        internal static readonly MethodInfo s_getOutgoingBlobBytes;
        internal static readonly MethodInfo s_verifySignature;
        internal static readonly MethodInfo s_makeSignature;
        internal static readonly MethodInfo s_closeContext;
        internal static readonly MethodInfo s_isCompleted;

        private delegate int MakeSignatureDelegate(byte[] buffer, int offset, int count, ref byte[] output);

        private object _instance;

        private Func<string, string> _getOutgoingBlobString;
        private Func<byte[], bool, byte[]> _getOutgoingBlobBytes;
        private Func<byte[], int, int, int> _verifySignature;
        private Func<bool> _isCompleted;
        private MakeSignatureDelegate _makeSignature;
        private Action _closeContext;

        static NTAuthentication()
        {
            s_type = typeof(NegotiateStream).Assembly.GetType("System.Net.NTAuthentication");
            ConstructorInfo[] ctors = s_type.GetConstructors(NonPublicInstance & ~BindingFlags.Default);
            Debug.Assert(ctors.Length == 1);

            s_ntAuthentication = ctors[0];
            s_getOutgoingBlogString = s_type.GetMethod("GetOutgoingBlob", NonPublicInstance, null, new Type[] { typeof(string) }, null);
            s_getOutgoingBlobBytes = s_type.GetMethod("GetOutgoingBlob", NonPublicInstance, null, new Type[] { typeof(byte[]), typeof(bool) }, null);
            s_verifySignature = s_type.GetMethod("VerifySignature", NonPublicInstance);
            s_makeSignature = s_type.GetMethod("MakeSignature", NonPublicInstance);
            s_closeContext = s_type.GetMethod("CloseContext", NonPublicInstance);
            s_isCompleted = s_type.GetProperty("IsCompleted", NonPublicInstance).GetMethod;
        }

        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            _instance = s_ntAuthentication.Invoke(new object[] { isServer, package, credential, spn, requestedContextFlags, channelBinding });
        }

        private void EnsureDelegateInitialized<TFunc>(ref TFunc delegateType, MethodInfo method)
        {
            if (delegateType == null)
            {
                delegateType = (TFunc)(object)method.CreateDelegate(typeof(TFunc), _instance);
            }
        }

        internal string GetOutgoingBlob(string challenge)
        {
            EnsureDelegateInitialized(ref _getOutgoingBlobString, s_getOutgoingBlogString);
            return _getOutgoingBlobString(challenge);
        }

        internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError)
        {
            EnsureDelegateInitialized(ref _getOutgoingBlobBytes, s_getOutgoingBlobBytes);
            return _getOutgoingBlobBytes(incomingBlob, throwOnError);
        }

        internal int VerifySignature(byte[] buffer, int offset, int count)
        {
            EnsureDelegateInitialized(ref _verifySignature, s_verifySignature);
            return _verifySignature(buffer, offset, count);
        }

        internal int MakeSignature(byte[] buffer, int offset, int count, ref byte[] output)
        {
            if (_makeSignature == null)
            {
                _makeSignature = (MakeSignatureDelegate)s_makeSignature.CreateDelegate(typeof(MakeSignatureDelegate), _instance);
            }

            return _makeSignature(buffer, offset, count, ref output);
        }

        internal void CloseContext()
        {
            EnsureDelegateInitialized(ref _closeContext, s_closeContext);
            _closeContext();
        }

        internal bool IsCompleted
        {
            get
            {
                EnsureDelegateInitialized(ref _isCompleted, s_isCompleted);
                return _isCompleted();
            }
        }
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
