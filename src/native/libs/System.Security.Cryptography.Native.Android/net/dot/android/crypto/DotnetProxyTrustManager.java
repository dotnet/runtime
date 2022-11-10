package net.dot.android.crypto;

import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

class DotnetProxyTrustManager implements X509TrustManager {
    private int sslStreamProxyHandle;

    public DotnetProxyTrustManager(int sslStreamProxyHandle) {
        this.sslStreamProxyHandle = sslStreamProxyHandle;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        if (!verifyRemoteCertificate(sslStreamProxyHandle)) {
            throw new CertificateException();
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType) throws CertificateException {
        if (!verifyRemoteCertificate(sslStreamProxyHandle)) {
            throw new CertificateException();
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return new X509Certificate[0];
    }

    static native boolean verifyRemoteCertificate(int sslStreamProxyHandle);
}
