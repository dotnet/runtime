// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Net
{
    internal static class NetRes
    {
        public static string GetWebStatusCodeString(FtpStatusCode statusCode, string statusDescription)
        {
            string webStatusCode = "(" + ((int)statusCode).ToString(NumberFormatInfo.InvariantInfo) + ")";
            string statusMessage = null;

            switch (statusCode)
            {
                case FtpStatusCode.ServiceNotAvailable:
                    statusMessage = SR.net_ftpstatuscode_ServiceNotAvailable;
                    break;
                case FtpStatusCode.CantOpenData:
                    statusMessage = SR.net_ftpstatuscode_CantOpenData;
                    break;
                case FtpStatusCode.ConnectionClosed:
                    statusMessage = SR.net_ftpstatuscode_ConnectionClosed;
                    break;
                case FtpStatusCode.ActionNotTakenFileUnavailableOrBusy:
                    statusMessage = SR.net_ftpstatuscode_ActionNotTakenFileUnavailableOrBusy;
                    break;
                case FtpStatusCode.ActionAbortedLocalProcessingError:
                    statusMessage = SR.net_ftpstatuscode_ActionAbortedLocalProcessingError;
                    break;
                case FtpStatusCode.ActionNotTakenInsufficientSpace:
                    statusMessage = SR.net_ftpstatuscode_ActionNotTakenInsufficientSpace;
                    break;
                case FtpStatusCode.CommandSyntaxError:
                    statusMessage = SR.net_ftpstatuscode_CommandSyntaxError;
                    break;
                case FtpStatusCode.ArgumentSyntaxError:
                    statusMessage = SR.net_ftpstatuscode_ArgumentSyntaxError;
                    break;
                case FtpStatusCode.CommandNotImplemented:
                    statusMessage = SR.net_ftpstatuscode_CommandNotImplemented;
                    break;
                case FtpStatusCode.BadCommandSequence:
                    statusMessage = SR.net_ftpstatuscode_BadCommandSequence;
                    break;
                case FtpStatusCode.NotLoggedIn:
                    statusMessage = SR.net_ftpstatuscode_NotLoggedIn;
                    break;
                case FtpStatusCode.AccountNeeded:
                    statusMessage = SR.net_ftpstatuscode_AccountNeeded;
                    break;
                case FtpStatusCode.ActionNotTakenFileUnavailable:
                    statusMessage = SR.net_ftpstatuscode_ActionNotTakenFileUnavailable;
                    break;
                case FtpStatusCode.ActionAbortedUnknownPageType:
                    statusMessage = SR.net_ftpstatuscode_ActionAbortedUnknownPageType;
                    break;
                case FtpStatusCode.FileActionAborted:
                    statusMessage = SR.net_ftpstatuscode_FileActionAborted;
                    break;
                case FtpStatusCode.ActionNotTakenFilenameNotAllowed:
                    statusMessage = SR.net_ftpstatuscode_ActionNotTakenFilenameNotAllowed;
                    break;
            }

            if (statusMessage != null && statusMessage.Length > 0)
            {
                webStatusCode += " " + statusMessage;
            }
            else
            {
                //
                // Otherwise try to map the base status.
                //
                if (statusDescription != null && statusDescription.Length > 0)
                {
                    webStatusCode += " " + statusDescription;
                }
            }

            return webStatusCode;
        }
    }
}

