package net.dot.android.crypto;

import java.security.cert.Certificate;
import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.SSLSession;
import javax.net.ssl.X509TrustManager;

class RemoteCertificateValidationCallbackProxy implements X509TrustManager {
    private static class SslPolicyErrors {
        public static final int None = 0x0;
        public static final int RemoteCertificateNotAvailable = 0x1;
        public static final int RemoteCertificateNameMismatch = 0x2;
        public static final int RemoteCertificateChainErrors = 0x4;
    }

    private int dotnetRemoteCertificateValidatorHandle;
    private X509TrustManager internalTrustManager;
    private String targetHostName;

    public RemoteCertificateValidationCallbackProxy(
        int dotnetRemoteCertificateValidatorHandle,
        X509TrustManager internalTrustManager,
        String targetHostName)
    {
        this.dotnetRemoteCertificateValidatorHandle = dotnetRemoteCertificateValidatorHandle;
        this.internalTrustManager = internalTrustManager;
        this.targetHostName = targetHostName;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        internalTrustManager.checkClientTrusted(chain, authType);
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        int errors =
            chain.length == 0
                ? SslPolicyErrors.RemoteCertificateNotAvailable
                : SslPolicyErrors.None;

        try {
            internalTrustManager.checkClientTrusted(chain, authType);
        } catch (CertificateException ex) {
            errors |= SslPolicyErrors.RemoteCertificateChainErrors;
        }

        if (!verifyHostName(chain)) {
            errors |= SslPolicyErrors.RemoteCertificateNameMismatch;
        }

        if (!validateUsingDotnetCallback(chain, errors)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    private boolean verifyHostName(X509Certificate[] chain) {
        HostnameVerifier hostnameVerifier = HttpsURLConnection.getDefaultHostnameVerifier();
        SSLSession sslSession = new FakeSSLSession(chain);
        return hostnameVerifier.verify(targetHostName, sslSession);
    }

    public X509Certificate[] getAcceptedIssuers() {
        return internalTrustManager.getAcceptedIssuers();
    }

    private boolean validateUsingDotnetCallback(X509Certificate[] chain, int errors) {
        return validateRemoteCertificate(dotnetRemoteCertificateValidatorHandle, chain, errors);
    }

    static native boolean validateRemoteCertificate(int dotnetRemoteCertificateValidatorHandle, X509Certificate[] chain, int errors);

    private class FakeSSLSession implements SSLSession {
        private X509Certificate[] certificates;

        public FakeSSLSession(X509Certificate[] certificates) {
            this.certificates = certificates;
        }

        public Certificate[] getPeerCertificates() {
            return certificates;
        }

        public int getApplicationBufferSize() { return 0; }
        public String getCipherSuite() { return ""; }
        public long getCreationTime() { return 0; }
        public byte[] getId() { return null; }
        public long getLastAccessedTime() { return 0; }
        public Certificate[] getLocalCertificates() { return null; }
        public javax.security.cert.X509Certificate[] getPeerCertificateChain() { return null; }
        public int getPacketBufferSize() { return 0; }
        public String getPeerHost() { return ""; }
        public int getPeerPort() { return 0; }
        public java.security.Principal getLocalPrincipal() { return null; }
        public java.security.Principal getPeerPrincipal() { return null; }
        public String getProtocol() { return ""; }
        public javax.net.ssl.SSLSessionContext getSessionContext() { return null; }
        public Object getValue(String name) { return null; }
        public String[] getValueNames() { return null; }
        public void invalidate() { }
        public boolean isValid() { return false; }
        public void putValue(String name, Object value) { }
        public void removeValue(String name) { }
    }
}
