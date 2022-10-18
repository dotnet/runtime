# Project "Melanzana"

Implements framework for Apple Code Signing of bundles and Mach-O files in .NET.

## Status

The code is very much work in progress. It can sign simple application bundles and Mach-O executables.

There's a simple command line tool for testing that can sign with ad-hoc signatures, certificates from system keychain, or certificates from Azure Key Vault. For Azure Key Vault the only supported authentication is logging in with the Azure CLI tool.
