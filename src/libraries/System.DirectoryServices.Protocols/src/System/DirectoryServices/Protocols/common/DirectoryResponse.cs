// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    public abstract class DirectoryResponse : DirectoryOperation
    {
        private DirectoryControl[] _directoryControls;
        internal Uri[] _directoryReferral;

        internal DirectoryResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral)
        {
            MatchedDN = dn;
            _directoryControls = controls;
            ResultCode = result;
            ErrorMessage = message;
            _directoryReferral = referral;
        }

        public string RequestId { get; }

        public virtual string MatchedDN { get; }

        public virtual DirectoryControl[] Controls
        {
            get
            {
                if (_directoryControls == null)
                {
                    return Array.Empty<DirectoryControl>();
                }

                DirectoryControl[] tempControls = new DirectoryControl[_directoryControls.Length];
                for (int i = 0; i < _directoryControls.Length; i++)
                {
                    tempControls[i] = new DirectoryControl(_directoryControls[i].Type, _directoryControls[i].GetValue(), _directoryControls[i].IsCritical, _directoryControls[i].ServerSide);
                }
                DirectoryControl.TransformControls(tempControls);
                return tempControls;
            }
        }

        public virtual ResultCode ResultCode { get; }

        public virtual string ErrorMessage { get; }

        public virtual Uri[] Referral
        {
            get
            {
                if (_directoryReferral == null)
                {
                    return Array.Empty<Uri>();
                }

                Uri[] tempReferral = new Uri[_directoryReferral.Length];
                for (int i = 0; i < _directoryReferral.Length; i++)
                {
                    tempReferral[i] = new Uri(_directoryReferral[i].AbsoluteUri);
                }
                return tempReferral;
            }
        }
    }

    public class DeleteResponse : DirectoryResponse
    {
        internal DeleteResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    public class AddResponse : DirectoryResponse
    {
        internal AddResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    public class ModifyResponse : DirectoryResponse
    {
        internal ModifyResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    public class ModifyDNResponse : DirectoryResponse
    {
        internal ModifyDNResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    public class CompareResponse : DirectoryResponse
    {
        internal CompareResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    public class ExtendedResponse : DirectoryResponse
    {
        private byte[] _value;

        internal ExtendedResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        public string ResponseName { get; internal set; }

        public byte[] ResponseValue
        {
            get
            {
                if (_value == null)
                {
                    return Array.Empty<byte>();
                }

                byte[] tmpValue = new byte[_value.Length];
                for (int i = 0; i < _value.Length; i++)
                {
                    tmpValue[i] = _value[i];
                }
                return tmpValue;
            }
            internal set => _value = value;
        }
    }

    public class SearchResponse : DirectoryResponse
    {
        private SearchResultReferenceCollection _referenceCollection = new SearchResultReferenceCollection();
        private SearchResultEntryCollection _entryCollection = new SearchResultEntryCollection();
        internal bool searchDone = false;
        internal SearchResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        public SearchResultReferenceCollection References
        {
            get => _referenceCollection;
            internal set => _referenceCollection = value;
        }

        public SearchResultEntryCollection Entries
        {
            get => _entryCollection;
            internal set => _entryCollection = value;
        }
    }
}
