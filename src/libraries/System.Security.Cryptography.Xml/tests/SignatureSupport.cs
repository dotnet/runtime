namespace System.Security.Cryptography.Xml.Tests
{
    class SignatureSupport
    {
        private static int _supportsRsaSha1Signatures = 0;

        public static bool SupportsRsaSha1Signatures
        {
            get
            {
                if (_supportsRsaSha1Signatures == 0)
                {
                    bool supported = System.Security.Cryptography.Tests.SignatureSupport.CanProduceSha1Signature(new RSACryptoServiceProvider());
                    _supportsRsaSha1Signatures = supported ? 1 : -1;
                }
                return _supportsRsaSha1Signatures == 1;
            }
        }   
    }
}