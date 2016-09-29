class IntltoolPackage (Package):

    def __init__(self):
        Package.__init__(self, 'intltool', '0.50.2',
                         sources=[
                             'https://launchpad.net/%{name}/trunk/%{version}/+download/%{name}-%{version}.tar.gz'
                         ]
                         )

IntltoolPackage()
