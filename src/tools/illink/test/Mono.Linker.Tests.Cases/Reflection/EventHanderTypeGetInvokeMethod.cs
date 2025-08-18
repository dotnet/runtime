using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
    [ExpectedNoWarnings]
    // Necessary to allow trimming unused EventInfo methods,
    // making the test behavior more consistent with ILC.
    [SetupLinkerTrimMode("link")]
    public class EventHanderTypeGetInvokeMethod
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
            public static event EventHandler MyEvent;

            [Kept]
            public static void Test()
            {
                var eventInfo = typeof(EventDelegate).GetEvent(nameof(MyEvent));
                var invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
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
