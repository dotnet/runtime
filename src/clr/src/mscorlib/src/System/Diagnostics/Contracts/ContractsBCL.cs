// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
**
** Implementation details of CLR Contracts.
**
===========================================================*/
#define DEBUG // The behavior of this contract library should be consistent regardless of build type.

#if SILVERLIGHT
#define FEATURE_UNTRUSTED_CALLERS
#elif REDHAWK_RUNTIME

#elif BARTOK_RUNTIME

#else // CLR
#define FEATURE_UNTRUSTED_CALLERS
#define FEATURE_RELIABILITY_CONTRACTS
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;

#if FEATURE_RELIABILITY_CONTRACTS
using System.Runtime.ConstrainedExecution;
#endif
#if FEATURE_UNTRUSTED_CALLERS
using System.Security;
#endif

namespace System.Diagnostics.Contracts
{
    public static partial class Contract
    {
        #region Private Methods

        [ThreadStatic]
        private static bool _assertingMustUseRewriter;

        /// <summary>
        /// This method is used internally to trigger a failure indicating to the "programmer" that he is using the interface incorrectly.
        /// It is NEVER used to indicate failure of actual contracts at runtime.
        /// </summary>
        static partial void AssertMustUseRewriter(ContractFailureKind kind, String contractKind)
        {
            if (_assertingMustUseRewriter)
                System.Diagnostics.Assert.Fail("Asserting that we must use the rewriter went reentrant.", "Didn't rewrite this mscorlib?");
            _assertingMustUseRewriter = true;

            // For better diagnostics, report which assembly is at fault.  Walk up stack and
            // find the first non-mscorlib assembly.
            Assembly thisAssembly = typeof(Contract).Assembly;  // In case we refactor mscorlib, use Contract class instead of Object.
            StackTrace stack = new StackTrace();
            Assembly probablyNotRewritten = null;
            for (int i = 0; i < stack.FrameCount; i++)
            {
                Assembly caller = stack.GetFrame(i).GetMethod().DeclaringType.Assembly;
                if (caller != thisAssembly)
                {
                    probablyNotRewritten = caller;
                    break;
                }
            }

            if (probablyNotRewritten == null)
                probablyNotRewritten = thisAssembly;
            String simpleName = probablyNotRewritten.GetName().Name;
            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(kind, SR.Format(SR.MustUseCCRewrite, contractKind, simpleName), null, null, null);

            _assertingMustUseRewriter = false;
        }

        #endregion Private Methods

        #region Failure Behavior

        /// <summary>
        /// Without contract rewriting, failing Assert/Assumes end up calling this method.
        /// Code going through the contract rewriter never calls this method. Instead, the rewriter produced failures call
        /// System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent, followed by 
        /// System.Runtime.CompilerServices.ContractHelper.TriggerFailure.
        /// </summary>
        [SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Security.SecuritySafeCriticalAttribute")]
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
#endif
        static partial void ReportFailure(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), nameof(failureKind));
            Contract.EndContractBlock();

            // displayMessage == null means: yes we handled it. Otherwise it is the localized failure message
            var displayMessage = System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);

            if (displayMessage == null) return;

            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(failureKind, displayMessage, userMessage, conditionText, innerException);
        }

        /// <summary>
        /// Allows a managed application environment such as an interactive interpreter (IronPython)
        /// to be notified of contract failures and 
        /// potentially "handle" them, either by throwing a particular exception type, etc.  If any of the
        /// event handlers sets the Cancel flag in the ContractFailedEventArgs, then the Contract class will
        /// not pop up an assert dialog box or trigger escalation policy.  Hooking this event requires 
        /// full trust, because it will inform you of bugs in the appdomain and because the event handler
        /// could allow you to continue execution.
        /// </summary>
        public static event EventHandler<ContractFailedEventArgs> ContractFailed
        {
#if FEATURE_UNTRUSTED_CALLERS
#endif
            add
            {
                System.Runtime.CompilerServices.ContractHelper.InternalContractFailed += value;
            }
#if FEATURE_UNTRUSTED_CALLERS
#endif
            remove
            {
                System.Runtime.CompilerServices.ContractHelper.InternalContractFailed -= value;
            }
        }
        #endregion FailureBehavior
    }

    public sealed class ContractFailedEventArgs : EventArgs
    {
        private ContractFailureKind _failureKind;
        private String _message;
        private String _condition;
        private Exception _originalException;
        private bool _handled;
        private bool _unwind;

        internal Exception thrownDuringHandler;

#if FEATURE_RELIABILITY_CONTRACTS
#endif
        public ContractFailedEventArgs(ContractFailureKind failureKind, String message, String condition, Exception originalException)
        {
            Contract.Requires(originalException == null || failureKind == ContractFailureKind.PostconditionOnException);
            _failureKind = failureKind;
            _message = message;
            _condition = condition;
            _originalException = originalException;
        }

        public String Message { get { return _message; } }
        public String Condition { get { return _condition; } }
        public ContractFailureKind FailureKind { get { return _failureKind; } }
        public Exception OriginalException { get { return _originalException; } }

        // Whether the event handler "handles" this contract failure, or to fail via escalation policy.
        public bool Handled
        {
            get { return _handled; }
        }

#if FEATURE_UNTRUSTED_CALLERS
#endif
        public void SetHandled()
        {
            _handled = true;
        }

        public bool Unwind
        {
            get { return _unwind; }
        }

#if FEATURE_UNTRUSTED_CALLERS
#endif
        public void SetUnwind()
        {
            _unwind = true;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    internal sealed class ContractException : Exception
    {
        private readonly ContractFailureKind _Kind;
        private readonly string _UserMessage;
        private readonly string _Condition;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ContractFailureKind Kind { get { return _Kind; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Failure { get { return this.Message; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string UserMessage { get { return _UserMessage; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Condition { get { return _Condition; } }

        // Called by COM Interop, if we see COR_E_CODECONTRACTFAILED as an HRESULT.
        private ContractException()
        {
            HResult = System.Runtime.CompilerServices.ContractHelper.COR_E_CODECONTRACTFAILED;
        }

        public ContractException(ContractFailureKind kind, string failure, string userMessage, string condition, Exception innerException)
            : base(failure, innerException)
        {
            HResult = System.Runtime.CompilerServices.ContractHelper.COR_E_CODECONTRACTFAILED;
            _Kind = kind;
            _UserMessage = userMessage;
            _Condition = condition;
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}


namespace System.Runtime.CompilerServices
{
    public static partial class ContractHelper
    {
        #region Private fields

        private static volatile EventHandler<ContractFailedEventArgs> contractFailedEvent;
        private static readonly Object lockObject = new Object();

        internal const int COR_E_CODECONTRACTFAILED = unchecked((int)0x80131542);

        #endregion

        /// <summary>
        /// Allows a managed application environment such as an interactive interpreter (IronPython) or a
        /// web browser host (Jolt hosting Silverlight in IE) to be notified of contract failures and 
        /// potentially "handle" them, either by throwing a particular exception type, etc.  If any of the
        /// event handlers sets the Cancel flag in the ContractFailedEventArgs, then the Contract class will
        /// not pop up an assert dialog box or trigger escalation policy.  Hooking this event requires 
        /// full trust.
        /// </summary>
        internal static event EventHandler<ContractFailedEventArgs> InternalContractFailed
        {
#if FEATURE_UNTRUSTED_CALLERS
#endif
            add
            {
                // Eagerly prepare each event handler _marked with a reliability contract_, to 
                // attempt to reduce out of memory exceptions while reporting contract violations.
                // This only works if the new handler obeys the constraints placed on 
                // constrained execution regions.  Eagerly preparing non-reliable event handlers
                // would be a perf hit and wouldn't significantly improve reliability.
                // UE: Please mention reliable event handlers should also be marked with the 
                // PrePrepareMethodAttribute to avoid CER eager preparation work when ngen'ed.
                System.Runtime.CompilerServices.RuntimeHelpers.PrepareContractedDelegate(value);
                lock (lockObject)
                {
                    contractFailedEvent += value;
                }
            }
#if FEATURE_UNTRUSTED_CALLERS
#endif
            remove
            {
                lock (lockObject)
                {
                    contractFailedEvent -= value;
                }
            }
        }

        /// <summary>
        /// Rewriter will call this method on a contract failure to allow listeners to be notified.
        /// The method should not perform any failure (assert/throw) itself.
        /// This method has 3 functions:
        /// 1. Call any contract hooks (such as listeners to Contract failed events)
        /// 2. Determine if the listeneres deem the failure as handled (then resultFailureMessage should be set to null)
        /// 3. Produce a localized resultFailureMessage used in advertising the failure subsequently.
        /// </summary>
        /// <param name="resultFailureMessage">Should really be out (or the return value), but partial methods are not flexible enough.
        /// On exit: null if the event was handled and should not trigger a failure.
        ///          Otherwise, returns the localized failure message</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.DebuggerNonUserCode]
#if FEATURE_RELIABILITY_CONTRACTS
#endif
        static partial void RaiseContractFailedEventImplementation(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException, ref string resultFailureMessage)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), nameof(failureKind));
            Contract.EndContractBlock();

            string returnValue;
            String displayMessage = "contract failed.";  // Incomplete, but in case of OOM during resource lookup...
            ContractFailedEventArgs eventArgs = null;  // In case of OOM.
#if FEATURE_RELIABILITY_CONTRACTS
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
                displayMessage = GetDisplayMessage(failureKind, userMessage, conditionText);
                EventHandler<ContractFailedEventArgs> contractFailedEventLocal = contractFailedEvent;
                if (contractFailedEventLocal != null)
                {
                    eventArgs = new ContractFailedEventArgs(failureKind, displayMessage, conditionText, innerException);
                    foreach (EventHandler<ContractFailedEventArgs> handler in contractFailedEventLocal.GetInvocationList())
                    {
                        try
                        {
                            handler(null, eventArgs);
                        }
                        catch (Exception e)
                        {
                            eventArgs.thrownDuringHandler = e;
                            eventArgs.SetUnwind();
                        }
                    }
                    if (eventArgs.Unwind)
                    {
                        // unwind
                        if (innerException == null) { innerException = eventArgs.thrownDuringHandler; }
                        throw new ContractException(failureKind, displayMessage, userMessage, conditionText, innerException);
                    }
                }
            }
            finally
            {
                if (eventArgs != null && eventArgs.Handled)
                {
                    returnValue = null; // handled
                }
                else
                {
                    returnValue = displayMessage;
                }
            }
            resultFailureMessage = returnValue;
        }

        /// <summary>
        /// Rewriter calls this method to get the default failure behavior.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "conditionText")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "userMessage")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "kind")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "innerException")]
        [System.Diagnostics.DebuggerNonUserCode]
        static partial void TriggerFailureImplementation(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            // If we're here, our intent is to pop up a dialog box (if we can).  For developers 
            // interacting live with a debugger, this is a good experience.  For Silverlight 
            // hosted in Internet Explorer, the assert window is great.  If we cannot
            // pop up a dialog box, throw an exception (consider a library compiled with 
            // "Assert On Failure" but used in a process that can't pop up asserts, like an 
            // NT Service).

            if (!Environment.UserInteractive)
            {
                throw new ContractException(kind, displayMessage, userMessage, conditionText, innerException);
            }

            // May need to rethink Assert.Fail w/ TaskDialogIndirect as a model.  Window title.  Main instruction.  Content.  Expanded info.
            // Optional info like string for collapsed text vs. expanded text.
            String windowTitle = SR.GetResourceString(GetResourceNameForFailure(kind));
            const int numStackFramesToSkip = 2;  // To make stack traces easier to read
            System.Diagnostics.Assert.Fail(conditionText, displayMessage, windowTitle, COR_E_CODECONTRACTFAILED, StackTrace.TraceFormat.Normal, numStackFramesToSkip);
            // If we got here, the user selected Ignore.  Continue.
        }

        private static String GetResourceNameForFailure(ContractFailureKind failureKind)
        {
            String resourceName = null;
            switch (failureKind)
            {
                case ContractFailureKind.Assert:
                    resourceName = "AssertionFailed";
                    break;

                case ContractFailureKind.Assume:
                    resourceName = "AssumptionFailed";
                    break;

                case ContractFailureKind.Precondition:
                    resourceName = "PreconditionFailed";
                    break;

                case ContractFailureKind.Postcondition:
                    resourceName = "PostconditionFailed";
                    break;

                case ContractFailureKind.Invariant:
                    resourceName = "InvariantFailed";
                    break;

                case ContractFailureKind.PostconditionOnException:
                    resourceName = "PostconditionOnExceptionFailed";
                    break;

                default:
                    Contract.Assume(false, "Unreachable code");
                    resourceName = "AssumptionFailed";
                    break;
            }
            return resourceName;
        }

#if FEATURE_RELIABILITY_CONTRACTS
#endif
        private static String GetDisplayMessage(ContractFailureKind failureKind, String userMessage, String conditionText)
        {
            String resourceName = GetResourceNameForFailure(failureKind);
            // Well-formatted English messages will take one of four forms.  A sentence ending in
            // either a period or a colon, the condition string, then the message tacked 
            // on to the end with two spaces in front.
            // Note that both the conditionText and userMessage may be null.  Also, 
            // on Silverlight we may not be able to look up a friendly string for the
            // error message.  Let's leverage Silverlight's default error message there.
            String failureMessage;
            if (!String.IsNullOrEmpty(conditionText))
            {
                resourceName += "_Cnd";
                failureMessage = SR.Format(SR.GetResourceString(resourceName), conditionText);
            }
            else
            {
                failureMessage = SR.GetResourceString(resourceName);
            }

            // Now add in the user message, if present.
            if (!String.IsNullOrEmpty(userMessage))
            {
                return failureMessage + "  " + userMessage;
            }
            else
            {
                return failureMessage;
            }
        }
    }
}  // namespace System.Runtime.CompilerServices

