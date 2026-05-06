// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Tests
{
    public sealed class DynamicMethodILProvider : IILProvider
    {
        private static readonly Type s_runtimeILGenerator = Type.GetType("System.Reflection.Emit.RuntimeILGenerator");
        private static readonly FieldInfo s_fiLen = s_runtimeILGenerator.GetFieldAssert("m_length");
        private static readonly FieldInfo s_fiStream = s_runtimeILGenerator.GetFieldAssert("m_ILStream");
        private static readonly FieldInfo s_fiExceptions = s_runtimeILGenerator.GetFieldAssert("m_exceptions");
        private static readonly FieldInfo s_fiExceptionCount = s_runtimeILGenerator.GetFieldAssert("m_exceptionCount");
        private static readonly FieldInfo s_fiLocalSignature = s_runtimeILGenerator.GetFieldAssert("m_localSignature");
        private static readonly MethodInfo s_miBakeByteArray = s_runtimeILGenerator.GetMethodAssert("BakeByteArray");
        private static readonly MethodInfo s_miMaxStackSize = s_runtimeILGenerator.GetMethodAssert("GetMaxStackSize");

        private readonly DynamicMethod _method;
        private byte[] _byteArray;
        private ExceptionInfo[] _exceptionInfo;
        private byte[] _localSignature;
        private int? _maxStackSize;

        public DynamicMethodILProvider(DynamicMethod method)
        {
            _method = method;
        }

        public byte[] GetByteArray()
        {
            if (_byteArray == null)
            {
                ILGenerator ilgen = _method.GetILGenerator();
                try
                {
                    _byteArray = (byte[])s_miBakeByteArray.Invoke(ilgen, null);
                    if (_byteArray == null)
                    {
                        _byteArray = Array.Empty<byte>();
                    }
                }
                catch (TargetInvocationException)
                {
                    int length = (int)s_fiLen.GetValue(ilgen);
                    _byteArray = new byte[length];
                    Array.Copy((byte[])s_fiStream.GetValue(ilgen), _byteArray, length);
                }
            }

            return _byteArray;
        }

        public ExceptionInfo[] GetExceptionInfos()
        {
            if (_exceptionInfo == null)
            {
                ILGenerator ilgen = _method.GetILGenerator();

                var n = (int)s_fiExceptionCount.GetValue(ilgen);
                if (n > 0)
                {
                    var exceptions = (Array)s_fiExceptions.GetValue(ilgen);

                    _exceptionInfo = new ExceptionInfo[n];
                    for (var i = 0; i < n; i++)
                    {
                        _exceptionInfo[i] = new ExceptionInfo(exceptions.GetValue(i));
                    }
                }
                else
                {
                    _exceptionInfo = Array.Empty<ExceptionInfo>();
                }
            }

            return _exceptionInfo;
        }

        public byte[] GetLocalSignature()
        {
            if (_localSignature == null)
            {
                ILGenerator ilgen = _method.GetILGenerator();

                var sig = (SignatureHelper)s_fiLocalSignature.GetValue(ilgen);

                _localSignature = sig.GetSignature();
            }

            return _localSignature;
        }

        public int MaxStackSize
        {
            get
            {
                if (_maxStackSize == null)
                {
                    ILGenerator ilgen = _method.GetILGenerator();

                    _maxStackSize = (int)s_miMaxStackSize.Invoke(ilgen, null);
                }

                return _maxStackSize.Value;
            }
        }
    }
}
