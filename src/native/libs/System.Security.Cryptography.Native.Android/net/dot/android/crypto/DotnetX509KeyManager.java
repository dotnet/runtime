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

import javax.net.ssl.X509KeyManager;

public final class DotnetX509KeyManager implements X509KeyManager {
    private static final String CLIENT_CERTIFICATE_ALIAS = "DOTNET_SSLStream_ClientCertificateContext";

    private final PrivateKey privateKey;
    private final X509Certificate[] certificateChain;

    public DotnetX509KeyManager(KeyStore.PrivateKeyEntry privateKeyEntry) {
        if (privateKeyEntry == null) {
            throw new IllegalArgumentException("PrivateKeyEntry must not be null");
        }

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

    @Override
    public String[] getClientAliases(String keyType, Principal[] issuers) {
        return new String[] { CLIENT_CERTIFICATE_ALIAS };
    }

    @Override
    public String chooseClientAlias(String[] keyType, Principal[] issuers, Socket socket) {
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
    public X509Certificate[] getCertificateChain(String alias) {
        return certificateChain;
    }

    @Override
    public PrivateKey getPrivateKey(String alias) {
        return privateKey;
    }
}
