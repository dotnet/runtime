package net.dot.android.crypto;

import java.security.cert.Certificate;
import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

class DotnetProxyTrustManager implements X509TrustManager {
    private int dotnetValidatorHandle;
    private X509TrustManager internalTrustManager;

    public DotnetProxyTrustManager(
        int dotnetValidatorHandle,
        X509TrustManager internalTrustManager)
    {
        this.dotnetValidatorHandle = dotnetValidatorHandle;
        this.internalTrustManager = internalTrustManager;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        internalTrustManager.checkClientTrusted(chain, authType);
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
        throws CertificateException
    {
        if (!validateRemoteCertificate(dotnetValidatorHandle, chain)) {
            throw new CertificateException("The remote certificate was rejected by the provided RemoteCertificateValidationCallback.");
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return internalTrustManager.getAcceptedIssuers();
    }

    static native boolean validateRemoteCertificate(int dotnetValidatorHandle, X509Certificate[] chain);
}
