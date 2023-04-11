using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

[TestFixture]
public class EmbeddingApiTests : BaseEmbeddingApiTests
{
    internal override ICoreCLRHostWrapper ClrHost { get; } = new CoreCLRHostWrappers();
}
