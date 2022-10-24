package net.dot.android.crypto;

import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

class DotnetProxyTrustManager implements X509TrustManager {
    private int dotnetValidatorHandle;

    public DotnetProxyTrustManager(int dotnetValidatorHandle)
    {
        this.dotnetValidatorHandle = dotnetValidatorHandle;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        if (!validateRemoteCertificate(dotnetValidatorHandle, chain)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        if (!validateRemoteCertificate(dotnetValidatorHandle, chain)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return new X509Certificate[0];
    }

    static native boolean validateRemoteCertificate(int dotnetValidatorHandle, X509Certificate[] chain);
}
