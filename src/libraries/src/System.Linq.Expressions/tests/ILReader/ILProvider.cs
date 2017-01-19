// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public interface IILProvider
    {
        byte[] GetByteArray();
        ExceptionInfo[] GetExceptionInfos();
        byte[] GetLocalSignature();
        int MaxStackSize { get; }
    }

    public sealed class ExceptionInfo
    {
        private static readonly Type s_tyExceptionInfo = Type.GetType("System.Reflection.Emit.__ExceptionInfo", throwOnError: true);
        private static readonly MethodInfo s_miGetStartAddress = GetMethodInfo(nameof(GetStartAddress));
        private static readonly MethodInfo s_miGetEndAddress = GetMethodInfo(nameof(GetEndAddress));
        private static readonly MethodInfo s_miGetNumberOfCatches = GetMethodInfo(nameof(GetNumberOfCatches));
        private static readonly MethodInfo s_miGetCatchAddresses = GetMethodInfo(nameof(GetCatchAddresses));
        private static readonly MethodInfo s_miGetCatchEndAddresses = GetMethodInfo(nameof(GetCatchEndAddresses));
        private static readonly MethodInfo s_miGetCatchClass = GetMethodInfo(nameof(GetCatchClass));
        private static readonly MethodInfo s_miGetExceptionTypes = GetMethodInfo(nameof(GetExceptionTypes));

        public int GetStartAddress() => Invoke<int>(s_miGetStartAddress);
        public int GetEndAddress() => Invoke<int>(s_miGetEndAddress);
        public int GetNumberOfCatches() => Invoke<int>(s_miGetNumberOfCatches);
        public int[] GetCatchAddresses() => Invoke<int[]>(s_miGetCatchAddresses);
        public int[] GetCatchEndAddresses() => Invoke<int[]>(s_miGetCatchEndAddresses);
        public Type[] GetCatchClass() => Invoke<Type[]>(s_miGetCatchClass);
        public int[] GetExceptionTypes() => Invoke<int[]>(s_miGetExceptionTypes);

        private readonly object _exceptionInfo;

        public ExceptionInfo(object exceptionInfo)
        {
            _exceptionInfo = exceptionInfo;

            StartAddress = GetStartAddress();
            EndAddress = GetEndAddress();

            int n = GetNumberOfCatches();
            if (n > 0)
            {
                int[] handlerStart = GetCatchAddresses();
                int[] handlerEnd = GetCatchEndAddresses();
                Type[] catchType = GetCatchClass();
                int[] types = GetExceptionTypes();

                Handlers = new HandlerInfo[n];

                for (var i = 0; i < n; i++)
                {
                    Handlers[i] = new HandlerInfo(handlerStart[i], handlerEnd[i], catchType[i], types[i]);
                }
            }
            else
            {
                Handlers = Array.Empty<HandlerInfo>();
            }
        }

        public int StartAddress { get; }
        public int EndAddress { get; }
        public HandlerInfo[] Handlers { get; }

        private static MethodInfo GetMethodInfo(string name) => s_tyExceptionInfo.GetMethodAssert(name);
        private T Invoke<T>(MethodInfo method, params object[] args) => (T)method.Invoke(_exceptionInfo, args);
    }

    public sealed class HandlerInfo
    {
        public HandlerInfo(int startAddress, int endAddress, Type type, int kind)
        {
            StartAddress = startAddress;
            EndAddress = endAddress;
            Type = type;
            Kind = (HandlerKind)kind;
        }

        public int StartAddress { get; }
        public int EndAddress { get; }
        public Type Type { get; }
        public HandlerKind Kind { get; }
    }

    public enum HandlerKind
    {
        None,
        Filter,
        Finally,
        Fault,
        PreserveStack
    }
}
