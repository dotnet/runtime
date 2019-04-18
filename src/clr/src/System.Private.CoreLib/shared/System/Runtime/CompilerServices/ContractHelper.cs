// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define DEBUG // The behavior of this contract library should be consistent regardless of build type.

#nullable enable
using System.Diagnostics.Contracts;

namespace System.Runtime.CompilerServices
{
    public static class ContractHelper
    {
        /// <summary>
        /// Allows a managed application environment such as an interactive interpreter (IronPython) or a
        /// web browser host (Jolt hosting Silverlight in IE) to be notified of contract failures and 
        /// potentially "handle" them, either by throwing a particular exception type, etc.  If any of the
        /// event handlers sets the Cancel flag in the ContractFailedEventArgs, then the Contract class will
        /// not pop up an assert dialog box or trigger escalation policy.
        /// </summary>
        internal static event EventHandler<ContractFailedEventArgs> InternalContractFailed;

        /// <summary>
        /// Rewriter will call this method on a contract failure to allow listeners to be notified.
        /// The method should not perform any failure (assert/throw) itself.
        /// This method has 3 functions:
        /// 1. Call any contract hooks (such as listeners to Contract failed events)
        /// 2. Determine if the listeners deem the failure as handled (then resultFailureMessage should be set to null)
        /// 3. Produce a localized resultFailureMessage used in advertising the failure subsequently.
        /// On exit: null if the event was handled and should not trigger a failure.
        ///          Otherwise, returns the localized failure message.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        public static string? RaiseContractFailedEvent(ContractFailureKind failureKind, string? userMessage, string? conditionText, Exception? innerException)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), nameof(failureKind));

            string? returnValue;
            string displayMessage = "contract failed.";  // Incomplete, but in case of OOM during resource lookup...
            ContractFailedEventArgs? eventArgs = null;  // In case of OOM.

            try
            {
                displayMessage = GetDisplayMessage(failureKind, userMessage, conditionText);
                EventHandler<ContractFailedEventArgs> contractFailedEventLocal = InternalContractFailed;
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

            return returnValue;
        }

        /// <summary>
        /// Rewriter calls this method to get the default failure behavior.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        public static void TriggerFailure(ContractFailureKind kind, string? displayMessage, string? userMessage, string? conditionText, Exception? innerException)
        {
            if (string.IsNullOrEmpty(displayMessage))
            {
                displayMessage = GetDisplayMessage(kind, userMessage, conditionText);
            }

            System.Diagnostics.Debug.ContractFailure(displayMessage, string.Empty, GetFailureMessage(kind, null));
        }

        private static string GetFailureMessage(ContractFailureKind failureKind, string? conditionText)
        {
            bool hasConditionText = !string.IsNullOrEmpty(conditionText);
            switch (failureKind)
            {
                case ContractFailureKind.Assert:
                    return hasConditionText ? SR.Format(SR.AssertionFailed_Cnd, conditionText) : SR.AssertionFailed;

                case ContractFailureKind.Assume:
                    return hasConditionText ? SR.Format(SR.AssumptionFailed_Cnd, conditionText) : SR.AssumptionFailed;

                case ContractFailureKind.Precondition:
                    return hasConditionText ? SR.Format(SR.PreconditionFailed_Cnd, conditionText) : SR.PreconditionFailed;

                case ContractFailureKind.Postcondition:
                    return hasConditionText ? SR.Format(SR.PostconditionFailed_Cnd, conditionText) : SR.PostconditionFailed;

                case ContractFailureKind.Invariant:
                    return hasConditionText ? SR.Format(SR.InvariantFailed_Cnd, conditionText) : SR.InvariantFailed;

                case ContractFailureKind.PostconditionOnException:
                    return hasConditionText ? SR.Format(SR.PostconditionOnExceptionFailed_Cnd, conditionText) : SR.PostconditionOnExceptionFailed;

                default:
                    Contract.Assume(false, "Unreachable code");
                    return SR.AssumptionFailed;
            }
        }

        private static string GetDisplayMessage(ContractFailureKind failureKind, string? userMessage, string? conditionText)
        {
            string failureMessage;
            // Well-formatted English messages will take one of four forms.  A sentence ending in
            // either a period or a colon, the condition string, then the message tacked 
            // on to the end with two spaces in front.
            // Note that both the conditionText and userMessage may be null.  Also, 
            // on Silverlight we may not be able to look up a friendly string for the
            // error message.  Let's leverage Silverlight's default error message there. 
            if (!string.IsNullOrEmpty(conditionText))
            {
                failureMessage = GetFailureMessage(failureKind, conditionText);
            }
            else
            {
                failureMessage = "";
            }

            // Now add in the user message, if present.
            if (!string.IsNullOrEmpty(userMessage))
            {
                return failureMessage + "  " + userMessage;
            }
            else
            {
                return failureMessage;
            }
        }
    }
}
