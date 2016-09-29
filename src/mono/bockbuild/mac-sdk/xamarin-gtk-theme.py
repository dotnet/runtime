class XamarinGtkThemePackage (Package):

    def __init__(self):
        Package.__init__(self, 'xamarin-gtk-theme',
                         sources=[
                             'git://github.com/mono/xamarin-gtk-theme.git'],
                         revision='cc3fb66e56d494e968be3a529a0737a60e31c1f3')

    def build(self):
        try:
            self.sh('./autogen.sh --prefix=%{staged_prefix}')
        except:
            pass
        finally:
            #self.sh ('intltoolize --force --copy --debug')
            #self.sh ('./configure --prefix="%{package_prefix}"')
            Package.build(self)


XamarinGtkThemePackage()
