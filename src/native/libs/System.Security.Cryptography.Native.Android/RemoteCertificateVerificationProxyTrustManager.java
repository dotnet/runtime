package net.dot.android.crypto;

import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

class RemoteCertificateVerificationProxyTrustManager implements X509TrustManager {
    private int dotnetHandle;

    public RemoteCertificateVerificationProxyTrustManager(int dotnetHandle)
    {
        this.dotnetHandle = dotnetHandle;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        if (!verifyRemoteCertificate(dotnetHandle, chain)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        if (!verifyRemoteCertificate(dotnetHandle, chain)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return new X509Certificate[0];
    }

    static native boolean verifyRemoteCertificate(int dotnetHandle, X509Certificate[] chain);
}
