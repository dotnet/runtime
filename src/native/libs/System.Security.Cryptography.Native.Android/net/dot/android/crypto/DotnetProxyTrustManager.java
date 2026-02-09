// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot.android.crypto;

import android.net.http.X509TrustManagerExtensions;
import java.security.cert.CertificateException;
import java.security.cert.X509Certificate;
import javax.net.ssl.X509TrustManager;

/**
 * This class wraps the platform's default X509TrustManager to first consult
 * Android's trust infrastructure (which respects network-security-config.xml),
 * then delegates to the managed SslStream code for final validation.
 */
public final class DotnetProxyTrustManager implements X509TrustManager {
    private final long sslStreamProxyHandle;
    private final X509TrustManager platformTrustManager;
    private final String targetHost;

    public DotnetProxyTrustManager(long sslStreamProxyHandle, X509TrustManager platformTrustManager, String targetHost) {
        this.sslStreamProxyHandle = sslStreamProxyHandle;
        this.platformTrustManager = platformTrustManager;
        this.targetHost = targetHost;
    }

    public void checkClientTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        boolean platformTrusted = isClientTrustedByPlatformTrustManager(chain, authType);
        if (!verifyRemoteCertificate(sslStreamProxyHandle, platformTrusted)) {
            throw new CertificateException();
        }
    }

    public void checkServerTrusted(X509Certificate[] chain, String authType)
            throws CertificateException {
        boolean platformTrusted = isServerTrustedByPlatformTrustManager(chain, authType);
        if (!verifyRemoteCertificate(sslStreamProxyHandle, platformTrusted)) {
            throw new CertificateException();
        }
    }

    private boolean isServerTrustedByPlatformTrustManager(X509Certificate[] chain, String authType) {
        try {
            if (targetHost != null) {
                // X509TrustManagerExtensions.checkServerTrusted is available since API 17
                // and our minimum supported API level is 21
                X509TrustManagerExtensions extensions = new X509TrustManagerExtensions(platformTrustManager);
                extensions.checkServerTrusted(chain, authType, targetHost);
            } else {
                platformTrustManager.checkServerTrusted(chain, authType);
            }
            return true;
        } catch (CertificateException e) {
            return false;
        }
    }

    private boolean isClientTrustedByPlatformTrustManager(X509Certificate[] chain, String authType) {
        try {
            platformTrustManager.checkClientTrusted(chain, authType);
            return true;
        } catch (CertificateException e) {
            return false;
        }
    }

    public X509Certificate[] getAcceptedIssuers() {
        return platformTrustManager.getAcceptedIssuers();
    }

    static native boolean verifyRemoteCertificate(long sslStreamProxyHandle, boolean chainTrustedByPlatform);
}
