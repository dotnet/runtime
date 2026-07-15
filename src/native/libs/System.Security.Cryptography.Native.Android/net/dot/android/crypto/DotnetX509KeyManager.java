// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package net.dot.android.crypto;

import java.net.Socket;
import java.security.KeyStore;
import java.security.Principal;
import java.security.PrivateKey;
import java.security.cert.Certificate;
import java.security.cert.X509Certificate;
import java.util.ArrayList;

import javax.net.ssl.KeyManager;
import javax.net.ssl.SSLEngine;
import javax.net.ssl.X509ExtendedKeyManager;
import javax.net.ssl.X509KeyManager;

public final class DotnetX509KeyManager extends X509ExtendedKeyManager {
    private static final String CLIENT_CERTIFICATE_ALIAS = "DOTNET_SSLStream_ClientCertificateContext";

    private final long sslStreamProxyHandle;
    private final PrivateKey privateKey;
    private final X509Certificate[] certificateChain;
    private volatile X509KeyManager selectedKeyManager;

    public DotnetX509KeyManager(KeyStore.PrivateKeyEntry privateKeyEntry) {
        if (privateKeyEntry == null) {
            throw new IllegalArgumentException("PrivateKeyEntry must not be null");
        }

        this.sslStreamProxyHandle = 0;
        this.privateKey = privateKeyEntry.getPrivateKey();

        Certificate[] certificates = privateKeyEntry.getCertificateChain();
        ArrayList<X509Certificate> x509Certificates = new ArrayList<>();
        for (Certificate certificate : certificates) {
            if (certificate instanceof X509Certificate) {
                x509Certificates.add((X509Certificate) certificate);
            }
        }

        if (x509Certificates.size() == 0) {
            throw new IllegalArgumentException("No valid X509 certificates found in the chain");
        }

        this.certificateChain = x509Certificates.toArray(new X509Certificate[0]);
    }

    public DotnetX509KeyManager(long sslStreamProxyHandle) {
        if (sslStreamProxyHandle == 0) {
            throw new IllegalArgumentException("SslStream proxy handle must not be zero");
        }

        this.sslStreamProxyHandle = sslStreamProxyHandle;
        this.privateKey = null;
        this.certificateChain = null;
    }

    @Override
    public String[] getClientAliases(String keyType, Principal[] issuers) {
        if (sslStreamProxyHandle != 0) {
            // Delayed selection is triggered only when the TLS provider processes CertificateRequest.
            return selectedKeyManager == null ? null : selectedKeyManager.getClientAliases(keyType, issuers);
        }

        return new String[] { CLIENT_CERTIFICATE_ALIAS };
    }

    @Override
    public String chooseClientAlias(String[] keyTypes, Principal[] issuers, Socket socket) {
        if (sslStreamProxyHandle != 0) {
            X509KeyManager keyManager = selectClientKeyManager(issuers);
            return keyManager == null ? null : keyManager.chooseClientAlias(keyTypes, issuers, socket);
        }

        return CLIENT_CERTIFICATE_ALIAS;
    }

    @Override
    public String chooseEngineClientAlias(String[] keyTypes, Principal[] issuers, SSLEngine engine) {
        if (sslStreamProxyHandle != 0) {
            X509KeyManager keyManager = selectClientKeyManager(issuers);
            if (keyManager == null) {
                return null;
            }

            if (keyManager instanceof X509ExtendedKeyManager) {
                return ((X509ExtendedKeyManager) keyManager).chooseEngineClientAlias(keyTypes, issuers, engine);
            }

            return keyManager.chooseClientAlias(keyTypes, issuers, null);
        }

        return CLIENT_CERTIFICATE_ALIAS;
    }

    @Override
    public String[] getServerAliases(String keyType, Principal[] issuers) {
        return new String[0];
    }

    @Override
    public String chooseServerAlias(String keyType, Principal[] issuers, Socket socket) {
        return null;
    }

    @Override
    public String chooseEngineServerAlias(String keyType, Principal[] issuers, SSLEngine engine) {
        return null;
    }

    @Override
    public X509Certificate[] getCertificateChain(String alias) {
        if (sslStreamProxyHandle != 0) {
            return selectedKeyManager == null ? null : selectedKeyManager.getCertificateChain(alias);
        }

        return certificateChain;
    }

    @Override
    public PrivateKey getPrivateKey(String alias) {
        if (sslStreamProxyHandle != 0) {
            return selectedKeyManager == null ? null : selectedKeyManager.getPrivateKey(alias);
        }

        return privateKey;
    }

    private synchronized X509KeyManager selectClientKeyManager(Principal[] issuers) {
        selectedKeyManager = null;

        KeyManager[] keyManagers = selectClientCertificate(sslStreamProxyHandle, getIssuerNames(issuers));
        if (keyManagers != null) {
            for (KeyManager keyManager : keyManagers) {
                if (keyManager instanceof X509KeyManager) {
                    selectedKeyManager = (X509KeyManager) keyManager;
                    break;
                }
            }
        }

        return selectedKeyManager;
    }

    private static String[] getIssuerNames(Principal[] issuers) {
        if (issuers == null || issuers.length == 0) {
            return new String[0];
        }

        String[] issuerNames = new String[issuers.length];
        for (int i = 0; i < issuers.length; i++) {
            // Principal.getName() uses the Android provider's RFC 2253 distinguished-name format.
            issuerNames[i] = issuers[i].getName();
        }

        return issuerNames;
    }

    private static native KeyManager[] selectClientCertificate(
            long sslStreamProxyHandle,
            String[] acceptableIssuers);
}
