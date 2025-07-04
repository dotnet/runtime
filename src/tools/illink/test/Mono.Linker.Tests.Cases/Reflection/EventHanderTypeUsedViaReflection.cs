using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
    [ExpectedNoWarnings]
    // Necessary to allow trimming unused EventInfo methods,
    // making the test behavior more consistent with ILC.
    [SetupLinkerTrimMode("link")]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(EventHandler<>), ".ctor(System.Object,System.IntPtr)")]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(EventHandler<>), "Invoke(System.Object,TEventArgs)")]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(EventHandler<>), "BeginInvoke(System.Object,TEventArgs,System.AsyncCallback,System.Object)",
        By = Tool.NativeAot)]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(EventHandler<>), "EndInvoke(System.IAsyncResult)",
        By = Tool.NativeAot)]
    public class EventHanderTypeUsedViaReflection
    {
        public static void Main()
        {
            EventDelegate.Test();
            NonDelegate.Test();
        }

        class EventDelegate
        {
            [Kept]
            [KeptBackingField]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            public static event DelegateType CustomDelegateEvent;

            [Kept]
            [KeptBaseType(typeof(MulticastDelegate))]
            [KeptMember(".ctor(System.Object,System.IntPtr)")]
            [KeptMember("Invoke(System.Int32,System.String)")]
            [KeptMember("BeginInvoke(System.Int32,System.String,System.AsyncCallback,System.Object)", By = Tool.NativeAot)]
            [KeptMember("EndInvoke(System.IAsyncResult)", By = Tool.NativeAot)]
            public delegate void DelegateType(int x, string y);

            [Kept]
            public static void TestDelegateTypeInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(CustomDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("Invoke");
            }

            [Kept]
            [ExpectedWarning("IL2075", nameof(Type.GetMethod))]
            public static void TestDelegateTypeBeginInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(CustomDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("BeginInvoke");
            }

            [Kept]
            [ExpectedWarning("IL2075", nameof(Type.GetMethod))]
            public static void TestDelegateTypeEndInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(CustomDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("EndInvoke");
            }

            [Kept]
            [KeptBackingField]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            public static event EventHandler<int> EventHandlerDelegateEvent;

            [Kept]
            public static void TestEventHandlerInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(EventHandlerDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("Invoke");
            }

            [Kept]
            [ExpectedWarning("IL2075", nameof(Type.GetMethod))]
            public static void TestEventHandlerBeginInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(EventHandlerDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("BeginInvoke");
            }

            [Kept]
            [ExpectedWarning("IL2075", nameof(Type.GetMethod))]
            public static void TestEventHandlerEndInvoke()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(EventHandlerDelegateEvent));
                eventInfo.EventHandlerType.GetMethod("EndInvoke");
            }

            [Kept]
            public static void Test()
            {
                TestDelegateTypeInvoke();
                TestDelegateTypeBeginInvoke();
                TestDelegateTypeEndInvoke();
                TestEventHandlerInvoke();
                TestEventHandlerBeginInvoke();
                TestEventHandlerEndInvoke();
            }
        }

        class NonDelegate
        {
            [Kept]
            [KeptBaseType(typeof(EventInfo))]
            [KeptMember(".ctor()")]
            class CustomEventInfo : EventInfo
            {
                [Kept]
                public override Type EventHandlerType
                {
                    [Kept]
                    get => typeof(NonDelegate);
                }

                // ILLink keeps more methods on MemberInfo due for stack trace support,
                // but differences in this class are not important for this testcase.

                [Kept(By = Tool.Trimmer)]
                public override bool IsDefined(Type type, bool inherit) => throw null;
                public override object[] GetCustomAttributes(bool inherit) => throw null;
                [Kept(By = Tool.Trimmer)]
                public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw null;
                [Kept(By = Tool.Trimmer)]
                public override string Name
                {
                    [Kept(By = Tool.Trimmer)]
                    get => throw null;
                }
                [Kept(By = Tool.Trimmer)]
                public override Type ReflectedType
                {
                    [Kept(By = Tool.Trimmer)]
                    get => throw null;
                }
                [Kept(By = Tool.Trimmer)]
                public override Type DeclaringType
                {
                    [Kept(By = Tool.Trimmer)]
                    get => throw null;
                }
                [Kept(By = Tool.Trimmer)]
                public override MethodInfo GetAddMethod(bool nonPublic) => throw null;
                public override MethodInfo GetRemoveMethod(bool nonPublic) => throw null;
                public override MethodInfo GetRaiseMethod(bool nonPublic) => throw null;
                public override EventAttributes Attributes => throw null;
            }

            // Strictly speaking this should be kept, but trimmer doesn't see through the custom event info.
            // See discussion at https://github.com/dotnet/runtime/issues/114113.
            [RequiresUnreferencedCode(nameof(Invoke))]
            public void Invoke()
            {
            }

            [Kept]
            [ExpectedWarning("IL2075", nameof(Type.GetMethod), Tool.Analyzer,
                "ILLink/ILC intrinsic handling assumes EventHandlerType is a delegate: https://github.com/dotnet/runtime/issues/114113")]
            public static void Test()
            {
                new CustomEventInfo().EventHandlerType.GetMethod("Invoke");
            }
        }
    }
}
