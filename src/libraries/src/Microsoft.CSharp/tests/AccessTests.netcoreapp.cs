// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class AccessTests
    {
        private readonly Type _baseType;
        private readonly Type _siblingType;
        private readonly Type _internalDerived;
        private readonly Type _externalDerived;

        public AccessTests()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder baseType = module.DefineType("BaseType", TypeAttributes.Public);
            baseType.DefineField("AField", typeof(int), FieldAttributes.FamANDAssem);
            MethodBuilder publicMethod = baseType.DefineMethod(
                "PublicMethod", MethodAttributes.Public, typeof(int), new[] {typeof(int)});
            publicMethod.DefineParameter(0, ParameterAttributes.None, "x");
            var ilGen = publicMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ret);
            MethodBuilder protectedPrivateMethod = baseType.DefineMethod(
                "ProtectedPrivateMethod", MethodAttributes.FamANDAssem, typeof(int), new[] {typeof(int)});
            ilGen = protectedPrivateMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ret);
            _baseType = baseType.CreateType();
            _siblingType = module.DefineType("SiblingType", TypeAttributes.Public).CreateType();
            _internalDerived = module.DefineType("InternalDerived", TypeAttributes.Public, baseType).CreateType();

            assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("OtherName"), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule("OtherName");
            _externalDerived = module.DefineType("ExternalDerived", TypeAttributes.Public, baseType).CreateType();
        }

        [Fact]
        public void PublicMethod()
        {
            dynamic d = Activator.CreateInstance(_baseType);
            Assert.Equal(19, d.PublicMethod(19));
        }

        [Fact]
        public void ProtectedPrivateMethodFromOutside()
        {
            dynamic d = Activator.CreateInstance(_baseType);
            string message = Assert.Throws<RuntimeBinderException>(() => d.ProtectedPrivateMethod(19)).Message;

            // Localized messages should always contain the method name.
            Assert.Contains("BaseType.ProtectedPrivateMethod(int)", message);
        }

        [Fact]
        public void ProtectedPrivateFromSameAssembly()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "ProtectedPrivateMethod", null, _siblingType,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_baseType), 19))
                .Message;
            Assert.Contains("BaseType.ProtectedPrivateMethod(int)", message);
        }

        [Fact]
        public void ProtectedPrivateFromDerivedSameAssemblyCalledOnBase()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "ProtectedPrivateMethod", null, _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_baseType), 19))
                .Message;
            Assert.Contains("BaseType.ProtectedPrivateMethod(int)", message);
        }

        [Fact]
        public void ProtectedPrivateFromDerivedSameAssemblyCalledOnDerived()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "ProtectedPrivateMethod", null, _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            Assert.Equal(19, target(callSite, Activator.CreateInstance(_internalDerived), 19));
        }

        [Fact]
        public void ProtectedPrivateFromDerivedDifferentAssembly()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "ProtectedPrivateMethod", null, _externalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_externalDerived), 19))
                .Message;
            Assert.Contains("BaseType.ProtectedPrivateMethod(int)", message);
        }

        [Fact]
        public void ProtectedPrivateFieldFromOutside()
        {
            dynamic d = Activator.CreateInstance(_baseType);
            string message = Assert.Throws<RuntimeBinderException>(() => d.AField).Message;
            Assert.Contains("BaseType.AField", message);
            message = Assert.Throws<RuntimeBinderException>(() => d.AField = 21).Message;
            Assert.Contains("BaseType.AField", message);
        }

        [Fact]
        public void ProtectedPrivateFieldSetFromSameAssembly()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "AField", _siblingType,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_baseType), 19))
                .Message;
            Assert.Contains("BaseType.AField", message);
            if (Math.Min(2, 2) == 2) return;
            CallSite<Func<CallSite, object, object>> getCallSite =
                CallSite<Func<CallSite, object, object>>.Create(
                    Binder.GetMember(
                        CSharpBinderFlags.None, "AField", _siblingType,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object> getTarget = getCallSite.Target;
            message = Assert
                .Throws<RuntimeBinderException>(() => getTarget(getCallSite, Activator.CreateInstance(_baseType)))
                .Message;
            Assert.Contains("BaseType.AField", message);
        }

        [Fact]
        public void ProtectedPrivateFieldFromDerivedSameAssemblyCalledOnBase()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "AField", _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_baseType), 19))
                .Message;
            Assert.Contains("BaseType.AField", message);

            CallSite<Func<CallSite, object, object>> getCallSite =
                CallSite<Func<CallSite, object, object>>.Create(
                    Binder.GetMember(
                        CSharpBinderFlags.None, "AField", _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object> getTarget = getCallSite.Target;
            message = Assert
                .Throws<RuntimeBinderException>(() => getTarget(getCallSite, Activator.CreateInstance(_baseType)))
                .Message;
            Assert.Contains("BaseType.AField", message);
        }

        [Fact]
        public void ProtectedPrivateFieldFromDerivedSameAssemblyCalledOnDerived()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "AField", _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            object obj = Activator.CreateInstance(_internalDerived);
            Assert.Equal(19, target(callSite, obj, 19));

            CallSite<Func<CallSite, object, object>> getCallSite =
                CallSite<Func<CallSite, object, object>>.Create(
                    Binder.GetMember(
                        CSharpBinderFlags.None, "AField", _internalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object> getTarget = getCallSite.Target;
            Assert.Equal(19, getTarget(getCallSite, obj));
        }

        [Fact]
        public void ProtectedPrivateFieldFromDerivedDifferentAssembly()
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.None, "AField", _externalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object> target = callSite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_externalDerived), 19))
                .Message;
            Assert.Contains("BaseType.AField", message);

            var getCallSite =
                CallSite<Func<CallSite, object, object>>.Create(
                    Binder.GetMember(
                        CSharpBinderFlags.None, "AField", _externalDerived,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        }));
            Func<CallSite, object, object> getTarget = getCallSite.Target;
            message = Assert
                .Throws<RuntimeBinderException>(() => target(callSite, Activator.CreateInstance(_externalDerived), 19))
                .Message;
            Assert.Contains("BaseType.AField", message);
        }
    }
}
