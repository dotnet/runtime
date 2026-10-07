// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public partial class UnsafeContextCodeFixTests
    {
        [Fact]
        public Task NestedOperandInExpressionBody_GetsInlineMarker()
        {
            // The marker goes directly before `unsafe(`, not above the member.
            return VerifyAsync("""
                class C
                {
                    static unsafe int Read(int* p) => 0;

                    [System.Obsolete]
                    int M(int* p, int? cached, bool fast) => cached ?? (fast ? 0 : {|CS9362:Read(p)|} + 1);
                }
                """, """
                class C
                {
                    static unsafe int Read(int* p) => 0;

                    [System.Obsolete]
                    int M(int* p, int? cached, bool fast) => cached ?? (fast ? 0 : /* SAFETY: To be audited */ unsafe(Read(p)) + 1);
                }
                """);
        }

        [Fact]
        public Task PropertyExpressionBody_UnsafeGetter_ConvertsToGetAccessor()
        {
            // The getter of a wrapped property access is still checked outside `unsafe(...)`, so no wrapper is valid.
            return VerifyAsync("""
                class Base
                {
                    public virtual bool IsCollectible => false;
                }

                class Other
                {
                    public unsafe bool IsCollectible => true;
                }

                class C : Base
                {
                    Other ReflectedType => new Other();

                    public override bool IsCollectible => {|CS9362:ReflectedType.IsCollectible|};
                }
                """, """
                class Base
                {
                    public virtual bool IsCollectible => false;
                }

                class Other
                {
                    public unsafe bool IsCollectible => true;
                }

                class C : Base
                {
                    Other ReflectedType => new Other();

                    public override bool IsCollectible
                    {
                        get
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                return ReflectedType.IsCollectible;
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task SwitchCaseGuards_EachLabelIsAnAnchor()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe bool Check(object o) => true;

                    static void Use() { }

                    int M(object o)
                    {
                        switch (o)
                        {
                            case string s when {|CS9362:Check(s)|}:
                                Use();
                                return 1;
                            case { } when {|CS9362:Check(o)|}:
                                Use();
                                return 2;
                            default:
                                Use();
                                return 0;
                        }
                    }
                }
                """, """
                class C
                {
                    static unsafe bool Check(object o) => true;

                    static void Use() { }

                    int M(object o)
                    {
                        switch (o)
                        {
                            case string s when /* SAFETY: To be audited */ unsafe(Check(s)):
                                Use();
                                return 1;
                            case { } when /* SAFETY: To be audited */ unsafe(Check(o)):
                                Use();
                                return 2;
                            default:
                                Use();
                                return 0;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task SingleLineBody_IsExpanded()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe void Write(int id, string message) { }

                    public void Log(string message) { {|CS9362:Write(1, message)|}; }
                }
                """, """
                class C
                {
                    static unsafe void Write(int id, string message) { }

                    public void Log(string message)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            Write(1, message);
                        }
                    }
                }
                """);
        }
    }
}
#endif
