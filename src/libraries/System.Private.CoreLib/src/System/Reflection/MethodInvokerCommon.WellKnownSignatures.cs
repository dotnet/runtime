// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        /// <summary>
        /// Tries to get the well-known invoke function for the specified method.
        /// </summary>
        // An alternative to hard-coding these types is to use a few shared function pointer signatures (at a minimum
        // for property setters and getters on reference types) plus add an intrinsic to invoke the function pointer
        // on an instance since currently function pointers only support calling statics.
        //
        // Another feature that would add extensibility is to add an API to specify the callback on MethodInfo\ConstructorInfo
        // when Invoke is called. This assumes the caller knows the signature. This would tie into the existing design here
        // without a perf impact to existing code since the callback would use the existing well-known signatures.
        private static bool TryGetWellKnownInvokeFunc(MethodBase method, out Delegate? invokeFunc, out InvokerStrategy strategy)
        {
            invokeFunc = null;

            if (!EventSource.IsSupported)
            {
                // Currently this method only checks for EventSource-related methods.
                strategy = InvokerStrategy.Uninitialized;
                return false;
            }

            Type declaringType = method.DeclaringType!;
            if (ReferenceEquals(declaringType, typeof(EventAttribute)))
            {
                switch (method.Name)
                {
                    case "set_Keywords":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeKeywordsSetter);
                        break;
                    case "set_Level":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeLevelSetter);
                        break;
                    case "set_Opcode":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeOpcodeSetter);
                        break;
                    case "set_Message":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeMessageSetter);
                        break;
                    case "set_Task":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeTaskSetter);
                        break;
                    case "set_Version":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventAttributeVersionSetter);
                        break;
                }
            }
            else if (ReferenceEquals(declaringType, typeof(EventSourceAttribute)))
            {
                switch (method.Name)
                {
                    case "set_Guid":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventSourceAttributeGuidSetter);
                        break;
                    case "set_Name":
                        invokeFunc = new InvokeFunc_Obj1Arg(EventSourceAttributeNameSetter);
                        break;
                }
            }

            // Todo: add other well-known methods here for scenarios other than minimal app.

            if (invokeFunc is not null)
            {
                // Currently we only have property setters.
                strategy = InvokerStrategy.Obj1;
                return true;
            }

            strategy = InvokerStrategy.Uninitialized;
            return false;
        }

        private static object? EventAttributeKeywordsSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Keywords = (EventKeywords)v!; return null; }
        private static object? EventAttributeLevelSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Level = (EventLevel)v!; return null; }
        private static object? EventAttributeOpcodeSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Opcode = (EventOpcode)v!; return null; }
        private static object? EventAttributeMessageSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Message = (string?)v; return null; }
        private static object? EventAttributeTaskSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Task = (EventTask)v!; return null; }
        private static object? EventAttributeVersionSetter(object? o, IntPtr _, object? v) { ((EventAttribute)o!).Version = (byte)v!; return null; }

        private static object? EventSourceAttributeGuidSetter(object? o, IntPtr _, object? v) { ((EventSourceAttribute)o!).Guid = (string?)v; return null; }
        private static object? EventSourceAttributeNameSetter(object? o, IntPtr _, object? v) { ((EventSourceAttribute)o!).Name = (string?)v; return null; }
    }
}
