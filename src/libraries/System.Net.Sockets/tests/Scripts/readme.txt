These are Windows PowerShell scripts

Using SocketOptionName.ReuseUnicastPort socket option requires a Windows 10 machine. In addition, the network settings for 'AutoReusePortRangeStartPort' and 'AutoReusePortRangeNumberOfPorts' must be set to nonzero values.

The recommendation for configuration is to first choose a port range on which no applications will be listening(otherwise calls to explicitly bind inside the selected port range will subsequently fail). Using tools like 'netstat' on the command line such as "netstat -a -p tcp -n" can help you determine ports in use.

The scripts shows an example of setting the configuration properly. A machine reboot is required after changing the settings.
