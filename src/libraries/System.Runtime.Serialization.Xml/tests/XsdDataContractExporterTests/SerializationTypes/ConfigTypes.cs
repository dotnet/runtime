#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    using System;

    [Serializable]
    public class ConfigBase1
    {
        string foo = "bar";
    }

    [Serializable]
    public class ConfigDerived1 : ConfigBase1
    {
    }

    [Serializable]
    public class ConfigBase2
    {
        string foo = "bar";
    }

    [Serializable]
    public class ConfigDerived2 : ConfigBase2
    {
    }

    [Serializable]
    public class ConfigBase3
    {
        string foo = "bar";
    }

    [Serializable]
    public class ConfigDerived3 : ConfigBase3
    {
    }

    [Serializable]
    public class ConfigBase4
    {
        string foo = "bar";
    }

    [Serializable]
    public class ConfigDerived4 : ConfigBase4
    {
    }
}
