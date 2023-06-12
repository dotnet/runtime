// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpAnalyzerVerifier<
       Microsoft.Interop.Analyzers.RuntimeComApiUsageWithSourceGeneratedComAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class RuntimeComApiUsageWithSourceGeneratedComTests
    {
        [Fact]
        public async Task SetComObjectData()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public interface I
               {
               }
               
               [GeneratedComClass]
               public class C : I
               {
               }

               public static class Program
               {
                   public static void Foo(I i)
                   {
                       Marshal.SetComObjectData([|i|], new object(), new object());
                   }
                   public static void Foo(C c)
                   {
                       Marshal.SetComObjectData([|c|], new object(), new object());
                   }
                   public static void Foo(ComObject c)
                   {
                       Marshal.SetComObjectData([|c|], new object(), new object());
                   }
               }
               """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GetComObjectData()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public interface I
               {
               }
               
               [GeneratedComClass]
               public class C : I
               {
               }
               
               public static class Program
               {
                   public static void Foo(I i)
                   {
                       _ = Marshal.GetComObjectData([|i|], new object());
                   }
                   public static void Foo(C c)
                   {
                       _ = Marshal.GetComObjectData([|c|], new object());
                   }
                   public static void Foo(ComObject c)
                   {
                       _ = Marshal.GetComObjectData([|c|], new object());
                   }
               }
               """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ReleaseComObject()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;
               
               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public interface I
               {
               }
               
               [GeneratedComClass]
               public class C : I
               {
               }

               public static class Program
               {
                   public static void Foo(I i)
                   {
                       _ = Marshal.ReleaseComObject([|i|]);
                   }
                   public static void Foo(C c)
                   {
                       _ = Marshal.ReleaseComObject([|c|]);
                   }
                   public static void Foo(ComObject c)
                   {
                       _ = Marshal.ReleaseComObject([|c|]);
                   }
               }
               """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task FinalReleaseComObject()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;
               
               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public interface I
               {
               }
               
               [GeneratedComClass]
               public class C : I
               {
               }
               
               public static class Program
               {
                   public static void Foo(I i)
                   {
                       _ = Marshal.FinalReleaseComObject([|i|]);
                   }
                   public static void Foo(C c)
                   {
                       _ = Marshal.FinalReleaseComObject([|c|]);
                   }
                   public static void Foo(ComObject c)
                   {
                       _ = Marshal.FinalReleaseComObject([|c|]);
                   }
               }
               """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CreateAggregatedObject()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }
                
                [GeneratedComClass]
                public class C : I
                {
                }

                public static class Program
                {
                    public static void Foo(nint outer, I i)
                    {
                        _ = Marshal.CreateAggregatedObject(outer, (object)[|i|]);
                        _ = Marshal.CreateAggregatedObject(outer, [|i|]);
                        _ = Marshal.CreateAggregatedObject<[|I|]>(outer, [|i|]);
                    }
                    public static void Foo(nint outer, C c)
                    {
                        _ = Marshal.CreateAggregatedObject(outer, (object)[|c|]);
                        _ = Marshal.CreateAggregatedObject(outer, [|c|]);
                        _ = Marshal.CreateAggregatedObject<[|C|]>(outer, [|c|]);
                    }
                    public static void Foo(nint outer, ComObject c)
                    {
                        _ = Marshal.CreateAggregatedObject(outer, (object)[|c|]);
                        _ = Marshal.CreateAggregatedObject(outer, [|c|]);
                        _ = Marshal.CreateAggregatedObject<[|ComObject|]>(outer, [|c|]);
                    }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CreateWrapperOfType()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface J
                {
                }

                [GeneratedComClass]
                public class CI : I
                {
                }

                [GeneratedComClass]
                public class CJ : J
                {
                }

                public static class Program
                {
                    public static void Foo(I i)
                    {
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|J|]));
                        _ = Marshal.CreateWrapperOfType<[|I|], [|J|]>([|i|]);
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|CI|]));
                        _ = Marshal.CreateWrapperOfType<[|I|], [|CI|]>([|i|]);
                        _ = Marshal.CreateWrapperOfType<[|I|], [|ComObject|]>([|i|]);
                    }

                    public static void Foo(CI i)
                    {
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|J|]));
                        _ = Marshal.CreateWrapperOfType<[|CI|], [|J|]>([|i|]);
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|CJ|]));
                        _ = Marshal.CreateWrapperOfType<[|CI|], [|CJ|]>([|i|]);
                        _ = Marshal.CreateWrapperOfType<[|CI|], [|ComObject|]>([|i|]);
                    }

                    public static void Foo(ComObject i)
                    {
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|J|]));
                        _ = Marshal.CreateWrapperOfType<[|ComObject|], [|J|]>([|i|]);
                        _ = Marshal.CreateWrapperOfType([|i|], typeof([|CJ|]));
                        _ = Marshal.CreateWrapperOfType<[|ComObject|], [|CJ|]>([|i|]);
                    }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }        

        [Fact]
        public async Task GetTypedObjectForIUnknown()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }

                [GeneratedComClass]
                public class C : I
                {
                }

                public static class Program
                {
                    public static void Foo(nint unknown)
                    {
                        _ = Marshal.GetTypedObjectForIUnknown(unknown, typeof([|I|]));
                        _ = Marshal.GetTypedObjectForIUnknown(unknown, typeof([|C|]));
                        _ = Marshal.GetTypedObjectForIUnknown(unknown, typeof([|ComObject|]));
                    }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GetIUnknownForObject()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
      
                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }
      
                [GeneratedComClass]
                public class C : I
                {
                }
      
                public static class Program
                {
                    public static void Foo(I i)
                    {
                        _ = Marshal.GetIUnknownForObject([|i|]);
                    }
                    public static void Foo(C c)
                    {
                        _ = Marshal.GetIUnknownForObject([|c|]);
                    }
                    public static void Foo(ComObject c)
                    {
                        _ = Marshal.GetIUnknownForObject([|c|]);
                    }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GetIDispatchForObject()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
      
                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }
      
                [GeneratedComClass]
                public class C : I
                {
                }
      
                public static class Program
                {
                    public static void Foo(I i)
                    {
                        _ = Marshal.GetIDispatchForObject([|i|]);
                    }
                    public static void Foo(C c)
                    {
                        _ = Marshal.GetIDispatchForObject([|c|]);
                    }
                    public static void Foo(ComObject c)
                    {
                        _ = Marshal.GetIDispatchForObject([|c|]);
                    }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GetComInterfaceForObject()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;
      
               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public interface I
               {
               }
      
               [GeneratedComClass]
               public class C : I
               {
               }
      
               public static class Program
               {
                   public static void Foo(I i)
                   {
                       _ = Marshal.GetComInterfaceForObject([|i|], typeof([|I|]));
                       _ = Marshal.GetComInterfaceForObject([|i|], typeof([|I|]), CustomQueryInterfaceMode.Allow);
                       _ = Marshal.GetComInterfaceForObject<[|I|], [|I|]>([|i|]);
                   }

                   public static void Foo(C c)
                   {
                       _ = Marshal.GetComInterfaceForObject([|c|], typeof([|C|]));
                       _ = Marshal.GetComInterfaceForObject([|c|], typeof([|C|]), CustomQueryInterfaceMode.Allow);
                       _ = Marshal.GetComInterfaceForObject<[|C|], [|C|]>([|c|]);

                   }

                   public static void Foo(ComObject c)
                   {
                       _ = Marshal.GetComInterfaceForObject([|c|], typeof([|ComObject|]));
                       _ = Marshal.GetComInterfaceForObject([|c|], typeof([|ComObject|]), CustomQueryInterfaceMode.Allow);
                       _ = Marshal.GetComInterfaceForObject<[|ComObject|], [|ComObject|]>([|c|]);
                   }
               }
               """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
