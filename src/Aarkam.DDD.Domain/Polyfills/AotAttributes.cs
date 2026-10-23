#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that certain members on a specified <see cref="Type"/> are accessed dynamically.
    /// Polyfill to support Native AOT and IL Trimming annotations on .NET Standard 2.1.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | 
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Parameter | 
        AttributeTargets.GenericParameter | AttributeTargets.ReturnValue, 
        Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    /// <summary>
    /// Suppresses reporting of a specific code analysis rule violation, unaffected by framework updates.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }
        public string CheckId { get; }
        public string? Justification { get; set; }
    }

    /// <summary>
    /// Specifies the types of members that are dynamically accessed.
    /// </summary>
    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicConstructors = 1,
        NonPublicConstructors = 2,
        PublicMethods = 4,
        NonPublicMethods = 8,
        PublicFields = 16,
        NonPublicFields = 32,
        PublicNestedTypes = 64,
        NonPublicNestedTypes = 128,
        PublicProperties = 256,
        NonPublicProperties = 512,
        PublicEvents = 1024,
        NonPublicEvents = 2048,
        Interfaces = 4096,
        All = -1
    }
}
#endif
