## About

<!-- A description of the package and where one can find more documentation -->

Provides types that support using XML configuration files (`app.config`). This package exists only to support migrating existing .NET Framework code that already uses System.Configuration. When writing new code, use another configuration system instead, such as [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/).

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example shows how to read and modify the application configuration settings.

```cs
using System;
using System.Configuration;

class Program
{
    static void Main()
    {
        try
        {
            // Open current application configuration
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection section = config.AppSettings.Settings;

            // Print settings from configuration file
            foreach (string key in section.AllKeys)
            {
                Console.WriteLine($"{key}: {section[key].Value}");
            }

            // Add new setting
            section.Add("Database", "TestDatabase");

            // Change existing setting
            section["Username"].Value = "TestUser";

            // Save changes to file
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }
        catch (ConfigurationErrorsException ex)
        {
            Console.WriteLine("Error reading configuration: ");
            Console.WriteLine(ex.Message);
        }
    }
}
```

To run this example, include an `app.config` file with the following content in your project:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="Server" value="example.com"/>
    <add key="Username" value="Admin"/>
  </appSettings>
</configuration>
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Configuration.Configuration`
* `System.Configuration.ConfigurationManager`

## Additional Documentation

<!-- Links to further documentation -->

* [Configure apps by using configuration files](https://learn.microsoft.com/dotnet/framework/configure-apps/)
* [System.Configuration namespace](https://learn.microsoft.com/dotnet/api/system.configuration)
* [System.Configuration.Configuration](https://learn.microsoft.com/dotnet/api/system.configuration.configuration)
* [System.Configuration.ConfigurationManager](https://learn.microsoft.com/dotnet/api/system.configuration.configurationmanager)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Configuration.ConfigurationManager is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).