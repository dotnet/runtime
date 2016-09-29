SourceForgePackage('gtk-osx', 'ige-mac-integration', '0.9.4', ['--without-compile-warnings'],
                   override_properties={'configure': './configure --prefix="%{staged_prefix}"',
                                        'makeinstall': 'make install'})
