#!/usr/bin/env bash

set -ex

CERT_COMMON_NAME="Apple Development: Self Sign"
TEMP_FOLDER=$(mktemp -d)

cd "$TEMP_FOLDER"

# Generate self-signed codesigning cert
cat <<EOF > selfsigncert.conf
[ req ]
distinguished_name = self_sign
prompt = no
[ self_sign ]
CN = $CERT_COMMON_NAME
[ extensions ]
basicConstraints = critical,CA:false
keyUsage = critical,digitalSignature
extendedKeyUsage = critical,1.3.6.1.5.5.7.3.3
EOF

openssl genrsa -out selfsigncert.key 2048
openssl req -x509 -new -config selfsigncert.conf -nodes -key selfsigncert.key -extensions extensions -sha256 -out selfsigncert.crt
openssl pkcs12 -export -inkey selfsigncert.key -in selfsigncert.crt -passout pass:PLACEHOLDERselfsignpass -out selfsigncert.p12

# create keychain and import cert into it
security delete-keychain selfsign.keychain || true
security create-keychain -p PLACEHOLDERselfsignpass selfsign.keychain
security import selfsigncert.p12 -P PLACEHOLDERselfsignpass -k selfsign.keychain -T /usr/bin/codesign -T /usr/bin/security
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k PLACEHOLDERselfsignpass selfsign.keychain
security unlock-keychain -p PLACEHOLDERselfsignpass selfsign.keychain
security set-keychain-settings -lut 21600 selfsign.keychain

# create template for provisioning profile .plist
cat <<EOF > SelfSign.plist
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>ApplicationIdentifierPrefix</key>
	<array>
	<string>SELFSIGN</string>
	</array>
	<key>CreationDate</key>
	<date>2000-01-01T00:00:00Z</date>
	<key>Platform</key>
	<array>
		<string>iOS</string>
	</array>
	<key>DeveloperCertificates</key>
	<array>
	</array>
	<key>Entitlements</key>
	<dict>
		<key>keychain-access-groups</key>
		<array>
			<string>SELFSIGN.*</string>
		</array>
		<key>get-task-allow</key>
		<false/>
		<key>application-identifier</key>
		<string>SELFSIGN.*</string>
		<key>com.apple.developer.team-identifier</key>
		<string>SELFSIGN</string>
	</dict>
	<key>ExpirationDate</key>
	<date>2100-01-01T00:00:00Z</date>
	<key>Name</key>
	<string>Self Sign Profile</string>
	<key>ProvisionedDevices</key>
	<array>
	</array>
	<key>TeamIdentifier</key>
	<array>
		<string>SELFSIGN</string>
	</array>
	<key>TeamName</key>
	<string>Selfsign Team</string>
	<key>TimeToLive</key>
	<integer>36500</integer>
	<key>UUID</key>
	<string></string>
	<key>Version</key>
	<integer>1</integer>
</dict>
</plist>
EOF

# fill out the template (UUID and the certificate content)
PROFILE_UUID=$(uuidgen)
CERT_CONTENT=$(sed -e '/BEGIN CERTIFICATE/d;/END CERTIFICATE/d' selfsigncert.crt)

plutil -replace UUID -string "$PROFILE_UUID" SelfSign.plist
plutil -replace DeveloperCertificates.0 -data "$CERT_CONTENT" SelfSign.plist

# append new keychain to keychain search so "security cms" finds the new certs
security list-keychains -d user -s $(security list-keychains -d user | tr -d '"') selfsign.keychain

# build .mobileprovision file out of the provisioning profile .plist
security cms -S -N "$CERT_COMMON_NAME" -k selfsign.keychain -p PLACEHOLDERselfsignpass -i SelfSign.plist -o SelfSign.mobileprovision

# copy provisioning profile to the right location
mkdir -p "$HOME/Library/MobileDevice/Provisioning Profiles/"
rm -f "$HOME/Library/MobileDevice/Provisioning Profiles/SelfSign.mobileprovision"
cp SelfSign.mobileprovision "$HOME/Library/MobileDevice/Provisioning Profiles/SelfSign.mobileprovision"
