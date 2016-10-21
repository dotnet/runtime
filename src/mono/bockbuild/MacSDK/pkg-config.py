package = FreeDesktopPackage('%{name}', 'pkg-config', '0.27',
                             configure_flags=["--with-internal-glib"])

package.needs_lipo = True
