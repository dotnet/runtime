// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class ComAwareEventInfoTests
    {
        [ComEventInterface(typeof(DispAttributeClass), typeof(int))]
        public interface DispAttributeInterface
        {
            event EventHandler Event;
        }

        public class DispAttributeClass
        {
            [DispId(10)]
            public void Event() { }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_ComObjectWithoutComEventInterfaceAttribute_ThrowsInvalidOperationException()
        {
            var attribute = new ComAwareEventInfo(typeof(NonComObject), nameof(NonComObject.Event));
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);

            Assert.Throws<InvalidOperationException>(() => attribute.AddEventHandler(target, handler));
            Assert.Throws<InvalidOperationException>(() => attribute.RemoveEventHandler(target, handler));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_ComObjectWithMultipleComEventInterfaceAttribute_ThrowsAmbiguousMatchException()
        {
            // C# doesn't let us apply multiple ComEventInterface values, so RefEmit is necessary.
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("name", TypeAttributes.Interface | TypeAttributes.Abstract);

            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ComEventInterfaceAttribute).GetConstructors()[0], new object[] { typeof(int), typeof(string) }));
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ComEventInterfaceAttribute).GetConstructors()[0], new object[] { typeof(string), typeof(string) }));

            MethodBuilder addMethod = typeBuilder.DefineMethod("add_Event", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract, typeof(void), new Type[] { typeof(EventHandler) });
            addMethod.GetILGenerator().Emit(OpCodes.Ret);

            EventBuilder eventBuilder = typeBuilder.DefineEvent("Event", EventAttributes.None, typeof(EventHandler));
            eventBuilder.SetAddOnMethod(addMethod);

            var attribute = new ComAwareEventInfo(typeBuilder.CreateType(), "Event");
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);
            Assert.Throws<AmbiguousMatchException>(() => attribute.AddEventHandler(target, handler));
            Assert.Throws<AmbiguousMatchException>(() => attribute.RemoveEventHandler(target, handler));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_NullSourceTypeEventInterface_ThrowsNullReferenceException()
        {
            var attribute = new ComAwareEventInfo(typeof(NullSourceType), nameof(NullSourceType.Event));
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);

            Assert.Throws<NullReferenceException>(() => attribute.AddEventHandler(target, handler));
            Assert.Throws<NullReferenceException>(() => attribute.RemoveEventHandler(target, handler));
        }

        [ComEventInterface(null, typeof(int))]
        public interface NullSourceType
        {
            event EventHandler Event;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_NoSuchSourceTypeEventInterface_ThrowsArgumentNullException()
        {
            var attribute = new ComAwareEventInfo(typeof(NoSuchSourceType), nameof(NoSuchSourceType.Event));
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);

            AssertExtensions.Throws<ArgumentNullException>("element", () => attribute.AddEventHandler(target, handler));
            AssertExtensions.Throws<ArgumentNullException>("element", () => attribute.RemoveEventHandler(target, handler));
        }

        [ComEventInterface(typeof(int), typeof(int))]
        public interface NoSuchSourceType
        {
            event EventHandler Event;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_NoDispIdAttribute_ThrowsInvalidOperationException()
        {
            var attribute = new ComAwareEventInfo(typeof(NoDispAttributeInterface), nameof(NoDispAttributeInterface.Event));
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);

            Assert.Throws<InvalidOperationException>(() => attribute.AddEventHandler(target, handler));
            Assert.Throws<InvalidOperationException>(() => attribute.RemoveEventHandler(target, handler));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void AddEventHandler_TargetNotIConnectionIConnectionPointContainer_ThrowsInvalidCastException()
        {
            var attribute = new ComAwareEventInfo(typeof(DispAttributeInterface), nameof(DispAttributeInterface.Event));
            var target = new ComImportObject();
            Delegate handler = new EventHandler(EventHandler);

            Assert.Throws<InvalidCastException>(() => attribute.AddEventHandler(target, handler));
            attribute.RemoveEventHandler(target, handler);
        }

        [ComEventInterface(typeof(NoDispAttributeClass), typeof(int))]
        public interface NoDispAttributeInterface
        {
            event EventHandler Event;
        }

        public class NoDispAttributeClass
        {
            public void Event() { }
        }
    }
}
