// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot.android.crypto;

import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

/**
 * This class is meant to replace the built-in X509TrustManager.
 * Its sole responsibility is to invoke the C# code in the SslStream
 * class during TLS handshakes to perform the validation of the remote
 * peer's certificate.
 */
public final class DotnetProxyTrustManager implements X509TrustManager {
    private final long sslStreamProxyHandle;

    public DotnetProxyTrustManager(long sslStreamProxyHandle) {
        this.sslStreamProxyHandle = sslStreamProxyHandle;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        if (!verifyRemoteCertificate(sslStreamProxyHandle)) {
            throw new CertificateException();
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        if (!verifyRemoteCertificate(sslStreamProxyHandle)) {
            throw new CertificateException();
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return new X509Certificate[0];
    }

    static native boolean verifyRemoteCertificate(long sslStreamProxyHandle);
}
