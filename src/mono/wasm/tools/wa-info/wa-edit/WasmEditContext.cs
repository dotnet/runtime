namespace WebAssemblyInfo
{
    public class WasmEditContext : WasmContext
    {
        public bool DataSectionAutoSplit = false;
        public string DataSectionFile = "";
        public DataMode DataSectionMode = DataMode.Active;
        public int DataOffset = 0;
    }
}
