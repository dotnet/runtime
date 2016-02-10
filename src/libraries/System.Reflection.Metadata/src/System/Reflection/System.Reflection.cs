// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    [Flags]
    public enum MethodSemanticsAttributes
    {
        /// <summary>
        /// Used to modify the value of the property.
        /// CLS-compliant setters are named with set_ prefix.
        /// </summary>
        Setter = 0x0001,

        /// <summary>
        /// Used to read the value of the property.
        /// CLS-compliant getters are named with get_ prefix.
        /// </summary>
        Getter = 0x0002,

        /// <summary>
        /// Other method for property (not getter or setter) or event (not adder, remover, or raiser).
        /// </summary>
        Other = 0x0004,

        /// <summary>
        /// Used to add a handler for an event.
        /// Corresponds to the AddOn flag in the Ecma 335 CLI specification.
        /// CLS-compliant adders are named with add_ prefix.
        /// </summary>
        Adder = 0x0008,

        /// <summary>
        /// Used to remove a handler for an event.
        /// Corresponds to the RemoveOn flag in the Ecma 335 CLI specification.
        /// CLS-compliant removers are named with remove_ prefix.
        /// </summary>
        Remover = 0x0010,

        /// <summary>
        /// Used to indicate that an event has occurred.
        /// Corresponds to the Fire flag in the Ecma 335 CLI specification.
        /// CLS-compliant raisers are named with raise_ prefix.
        /// </summary>
        Raiser = 0x0020,
    }

    /// <summary>
    /// Specifies the security actions that can be performed using declarative security.
    /// </summary>
    public enum DeclarativeSecurityAction : short
    {
        /// <summary>
        /// No declarative security action.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// Check that all callers in the call chain have been granted specified permission,
        /// </summary>
        Demand = 0x0002,

        /// <summary>
        /// The calling code can access the resource identified by the current permission object, even if callers higher in the stack have not been granted permission to access the resource.
        /// </summary>
        Assert = 0x0003,

        /// <summary>
        /// Without further checks refuse Demand for the specified permission.
        /// </summary>
        Deny = 0x0004,

        /// <summary>
        /// Without further checks, refuse Demand for all permissions other than those specified.
        /// </summary>
        PermitOnly = 0x0005,

        /// <summary>
        /// Check that the immediate caller has been granted the specified permission;
        /// </summary>
        LinkDemand = 0x0006,

        /// <summary>
        /// The derived class inheriting the class or overriding a method is required to have been granted the specified permission.
        /// </summary>
        InheritanceDemand = 0x0007,

        /// <summary>
        /// The request for the minimum permissions required for code to run. This action can only be used within the scope of the assembly.
        /// </summary>
        RequestMinimum = 0x0008,

        /// <summary>
        /// The request for additional permissions that are optional (not required to run). This request implicitly refuses all other permissions not specifically requested. This action can only be used within the scope of the assembly. 
        /// </summary>
        RequestOptional = 0x0009,

        /// <summary>
        /// The request that permissions that might be misused will not be granted to the calling code. This action can only be used within the scope of the assembly.
        /// </summary>
        RequestRefuse = 0x000A,
        // Wait for an actual need before exposing these. They all have ilasm keywords, but some are missing from the CLI spec and 
        // and none are defined in System.Security.Permissions.SecurityAction.
        //Request = 0x0001,
        //PrejitGrant = 0x000B,
        //PrejitDeny = 0x000C,
        //NonCasDemand = 0x000D,
        //NonCasLinkDemand = 0x000E,
        //NonCasInheritanceDemand = 0x000F,
    }

    [Flags]
    public enum MethodImportAttributes : short
    {
        None = 0x0,
        ExactSpelling = 0x0001,
        BestFitMappingDisable = 0x0020,
        BestFitMappingEnable = 0x0010,
        BestFitMappingMask = 0x0030,
        CharSetAnsi = 0x0002,
        CharSetUnicode = 0x0004,
        CharSetAuto = 0x0006,
        CharSetMask = 0x0006,
        ThrowOnUnmappableCharEnable = 0x1000,
        ThrowOnUnmappableCharDisable = 0x2000,
        ThrowOnUnmappableCharMask = 0x3000,
        SetLastError = 0x0040,
        CallingConventionWinApi = 0x0100,
        CallingConventionCDecl = 0x0200,
        CallingConventionStdCall = 0x0300,
        CallingConventionThisCall = 0x0400,
        CallingConventionFastCall = 0x0500,
        CallingConventionMask = 0x0700,
    }

    [Flags]
    public enum ManifestResourceAttributes
    {
        /// <summary>
        /// The Resource is exported from the Assembly
        /// </summary>
        Public = 0x00000001,

        /// <summary>
        /// The Resource is not exported from the Assembly
        /// </summary>
        Private = 0x00000002,

        /// <summary>
        /// Masks just the visibility-related attributes.
        /// </summary>
        VisibilityMask = 0x00000007, // Although it is odd that there is a 3rd bit with no corresponding flag, it is defined this way by the CLI spec.
    }

    /// <summary>
    /// Specifies all the hash algorithms used for hashing assembly files and for generating the strong name.
    /// </summary>
    public enum AssemblyHashAlgorithm
    {
        /// <summary>
        /// A mask indicating that there is no hash algorithm. If you specify None for a multi-module assembly, the common language runtime defaults to the SHA1 algorithm, since multi-module assemblies need to generate a hash.
        /// </summary>
        None = 0,

        /// <summary>
        /// Retrieves the MD5 message-digest algorithm. MD5 was developed by Rivest in 1991. It is basically MD4 with safety-belts and while it is slightly slower than MD4, it helps provide more security. The algorithm consists of four distinct rounds, which has a slightly different design from that of MD4. Message-digest size, as well as padding requirements, remain the same.
        /// </summary>
        MD5 = 0x8003,

        /// <summary>
        /// Retrieves a revision of the Secure Hash Algorithm that corrects an unpublished flaw in SHA.
        /// </summary>
        Sha1 = 0x8004,

        /// <summary>
        /// Retrieves a version of the Secure Hash Algorithm with a hash size of 256 bits.
        /// </summary>
        Sha256 = 0x800c,

        /// <summary>
        /// Retrieves a version of the Secure Hash Algorithm with a hash size of 384 bits.
        /// </summary>
        Sha384 = 0x800d,

        /// <summary>
        /// Retrieves a version of the Secure Hash Algorithm with a hash size of 512 bits.
        /// </summary>
        Sha512 = 0x800e
    }

    [Flags]
    public enum AssemblyFlags
    {
        /// <summary>
        /// The assembly reference holds the full (unhashed) public key.
        /// Not applicable on assembly definition.
        /// </summary>
        PublicKey = 0x00000001,

        /// <summary>
        /// The implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
        /// </summary>
        Retargetable = 0x00000100,

        /// <summary>
        /// The assembly contains Windows Runtime code.
        /// </summary>
        WindowsRuntime = 0x00000200,

        /// <summary>
        /// Content type mask. Masked bits correspond to values of <see cref="System.Reflection.AssemblyContentType"/>.
        /// </summary>
        ContentTypeMask = 0x00000e00,

        /// <summary>
        /// Specifies that just-in-time (JIT) compiler optimization is disabled for the assembly.
        /// </summary>
        DisableJitCompileOptimizer = 0x4000,

        /// <summary>
        /// Specifies that just-in-time (JIT) compiler tracking is enabled for the assembly.
        /// </summary>
        EnableJitCompileTracking = 0x8000,
    }

    internal static class TypeAttributesExtensions
    {
        // This flag will be added to the BCL (Bug #1041207), but we still 
        // need to define a copy here for downlevel portability.
        private const TypeAttributes Forwarder = (TypeAttributes)0x00200000;

        // This mask is the fastest way to check if a type is nested from its flags,
        // but it should not be added to the BCL enum as its semantics can be misleading.
        // Consider, for example, that (NestedFamANDAssem & NestedMask) == NestedFamORAssem.
        // Only comparison of the masked value to 0 is meaningful, which is different from
        // the other masks in the enum.
        private const TypeAttributes NestedMask = (TypeAttributes)0x00000006;

        public static bool IsForwarder(this TypeAttributes flags)
        {
            return (flags & Forwarder) != 0;
        }

        public static bool IsNested(this TypeAttributes flags)
        {
            return (flags & NestedMask) != 0;
        }
    }
}
