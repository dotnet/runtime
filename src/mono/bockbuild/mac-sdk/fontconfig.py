class FontConfigPackage (Package):

    def __init__(self):
        Package.__init__(self, 'fontconfig', '2.10.2',
                         configure_flags=['--disable-docs'],
                         sources=[
                             'http://www.fontconfig.org/release/%{name}-%{version}.tar.gz'
                         ],
                         # note: a non-empty DESTDIR keeps fc-cache from running at
                         # install-time
                         )

    def build(self):
        if Package.profile.name == 'darwin':
            self.configure_flags.extend([
                '--with-cache-dir="~/Library/Caches/com.xamarin.fontconfig"',
                '--with-default-fonts=/System/Library/Fonts',
                '--with-add-fonts=/Library/Fonts,/Network/Library/Fonts,/System/Library/Fonts'
            ])
        Package.build(self)

FontConfigPackage()
