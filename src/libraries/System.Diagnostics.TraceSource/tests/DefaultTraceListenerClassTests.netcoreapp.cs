// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public partial class DefaultTraceListenerClassTests : RemoteExecutorTestBase
    {
        [Fact]
        public void EntryAssemblyName_Default_IncludedInTrace()
        {
            RemoteInvoke(() =>
            {
                var listener = new TestDefaultTraceListener();
                Trace.Listeners.Add(listener);
                Trace.TraceError("hello world");
                Assert.Equal(Assembly.GetEntryAssembly()?.GetName().Name + " Error: 0 : hello world", listener.Output.Trim());
            }).Dispose();
        }

        [Fact]
        public void EntryAssemblyName_Null_NotIncludedInTrace()
        {
            RemoteInvoke(() =>
            {
                MakeAssemblyGetEntryAssemblyReturnNull();

                var listener = new TestDefaultTraceListener();
                Trace.Listeners.Add(listener);
                Trace.TraceError("hello world");
                Assert.Equal("Error: 0 : hello world", listener.Output.Trim());
            }).Dispose();
        }

        /// <summary>
        /// Makes Assembly.GetEntryAssembly() return null by generating an AppDomainManager-derived type
        /// with private reflection and reflection emit, and then again using private reflection to
        /// store that instance as the current domain manager.  It overrides EntryAssembly to return
        /// null, simulating the situation of a custom host that doesn't have a managed .exe.
        /// </summary>
        private static void MakeAssemblyGetEntryAssemblyReturnNull()
        {
            Type appDomainType = typeof(object).Assembly.GetType("System.AppDomain", true, false);
            Assert.NotNull(appDomainType);

            Type appDomainManagerType = typeof(object).Assembly.GetType("System.AppDomainManager", true, false);
            Assert.NotNull(appDomainManagerType);

            var asmName = new AssemblyName("CustomAppDomainManager");
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(asmName.Name);
            asmBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                IgnoreAccessChecksToAttributeBuilder.AddToModule(modBuilder), // allow deriving from the internal AppDomainManager in coreclr
                new object[] { typeof(object).Assembly.GetName().Name }));

            TypeBuilder typeBuilder = modBuilder.DefineType("DerivedAppDomainManager", TypeAttributes.Class | TypeAttributes.Public, appDomainManagerType);
            PropertyBuilder propBuilder = typeBuilder.DefineProperty("EntryAssembly", PropertyAttributes.None, appDomainType, Type.EmptyTypes);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("get_EntryAssembly", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(Assembly), Type.EmptyTypes);
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            propBuilder.SetGetMethod(methodBuilder);

            appDomainType
                .GetField("_domainManager", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(appDomainType.GetProperty("CurrentDomain").GetValue(null),
                Activator.CreateInstance(typeBuilder.CreateType()));
        }
    }
}
