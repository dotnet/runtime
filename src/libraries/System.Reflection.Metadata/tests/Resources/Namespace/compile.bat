@echo off
tf edit NamespaceForwardedCS.dll
csc /target:library NamespaceForwardedCS.cs
tf edit NamespaceTests.dll
csc /r:NamespaceForwardedCS.dll /target:library /out:NamespaceTests.dll NamespaceTestingCS.cs