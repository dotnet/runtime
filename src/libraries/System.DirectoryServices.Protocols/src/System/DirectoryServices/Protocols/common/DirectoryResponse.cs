// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    public abstract class DirectoryResponse : DirectoryOperation
    {
        internal string dn = null;
        internal DirectoryControl[] directoryControls = null;
        internal ResultCode result = (ResultCode)(-1);
        internal string directoryMessage = null;
        internal Uri[] directoryReferral = null;

        private string _requestID = null;

        internal DirectoryResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral)
        {
            this.dn = dn;
            this.directoryControls = controls;
            this.result = result;
            this.directoryMessage = message;
            this.directoryReferral = referral;
        }

        public string RequestId
        {
            get
            {
                return _requestID;
            }
        }

        public virtual string MatchedDN
        {
            get
            {
                return dn;
            }
        }

        public virtual DirectoryControl[] Controls
        {
            get
            {
                if (directoryControls == null)
                    return new DirectoryControl[0];
                else
                {
                    DirectoryControl[] tempControls = new DirectoryControl[directoryControls.Length];
                    for (int i = 0; i < directoryControls.Length; i++)
                        tempControls[i] = new DirectoryControl(directoryControls[i].Type, directoryControls[i].GetValue(), directoryControls[i].IsCritical, directoryControls[i].ServerSide);

                    DirectoryControl.TransformControls(tempControls);

                    return tempControls;
                }
            }
        }

        public virtual ResultCode ResultCode
        {
            get
            {
                return result;
            }
        }

        public virtual string ErrorMessage
        {
            get
            {
                return directoryMessage;
            }
        }

        public virtual Uri[] Referral
        {
            get
            {
                if (directoryReferral == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempReferral = new Uri[directoryReferral.Length];
                    for (int i = 0; i < directoryReferral.Length; i++)
                    {
                        tempReferral[i] = new Uri(directoryReferral[i].AbsoluteUri);
                    }
                    return tempReferral;
                }
            }
        }

        //
        // Private/protected
        //
    }

    public class DeleteResponse : DirectoryResponse
    {
        internal DeleteResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The AddResponse class for representing <addResponse>
    /// </summary>
    public class AddResponse : DirectoryResponse
    {
        internal AddResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ModifyResponse class for representing <modifyResponse>
    /// </summary>
    public class ModifyResponse : DirectoryResponse
    {
        internal ModifyResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ModifyDnResponse class for representing <modDNResponse>
    /// </summary>
    public class ModifyDNResponse : DirectoryResponse
    {
        internal ModifyDNResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The CompareResponse class for representing <compareResponse>
    /// </summary>
    public class CompareResponse : DirectoryResponse
    {
        internal CompareResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ExtendedResponse class for representing <extendedResponse>
    /// </summary>
    public class ExtendedResponse : DirectoryResponse
    {
        internal string name = null;
        internal byte[] value = null;

        internal ExtendedResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        //
        // Public
        //

        // Properties
        public string ResponseName
        {
            get
            {
                return name;
            }
        }

        public byte[] ResponseValue
        {
            get
            {
                if (value == null)
                    return new byte[0];
                else
                {
                    byte[] tmpValue = new byte[value.Length];
                    for (int i = 0; i < value.Length; i++)
                    {
                        tmpValue[i] = value[i];
                    }
                    return tmpValue;
                }
            }
        }
    }

    public class SearchResponse : DirectoryResponse
    {
        private SearchResultReferenceCollection _referenceCollection = new SearchResultReferenceCollection();
        private SearchResultEntryCollection _entryCollection = new SearchResultEntryCollection();
        internal bool searchDone = false;
        internal SearchResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        public override string MatchedDN
        {
            get
            {
                return dn;
            }
        }

        public override DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;

                if (directoryControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[directoryControls.Length];
                    for (int i = 0; i < directoryControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(directoryControls[i].Type, directoryControls[i].GetValue(), directoryControls[i].IsCritical, directoryControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }

        public override ResultCode ResultCode
        {
            get
            {
                return result;
            }
        }

        public override string ErrorMessage
        {
            get
            {
                return directoryMessage;
            }
        }

        public override Uri[] Referral
        {
            get
            {
                if (directoryReferral == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempReferral = new Uri[directoryReferral.Length];
                    for (int i = 0; i < directoryReferral.Length; i++)
                    {
                        tempReferral[i] = new Uri(directoryReferral[i].AbsoluteUri);
                    }
                    return tempReferral;
                }
            }
        }

        public SearchResultReferenceCollection References
        {
            get
            {
                return _referenceCollection;
            }
        }

        public SearchResultEntryCollection Entries
        {
            get
            {
                return _entryCollection;
            }
        }

        internal void SetReferences(SearchResultReferenceCollection col)
        {
            _referenceCollection = col;
        }

        internal void SetEntries(SearchResultEntryCollection col)
        {
            _entryCollection = col;
        }
    }
}
