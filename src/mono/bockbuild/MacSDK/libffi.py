class LibFfiPackage (Package):

    def __init__(self):
        Package.__init__(self, 'libffi', '3.0.13', sources=[
            'ftp://sourceware.org/pub/%{name}/%{name}-%{version}.tar.gz'])

LibFfiPackage()
