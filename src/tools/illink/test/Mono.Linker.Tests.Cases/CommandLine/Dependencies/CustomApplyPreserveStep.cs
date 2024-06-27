namespace CustomStep
{
    public class CustomApplyPreserveStep : Mono.Linker.Steps.IStep
    {
        public void Process(Mono.Linker.LinkContext context)
        {
            var myType = context.GetType("Mono.Linker.Tests.Cases.CommandLine.CustomStepApplyPreserve/HasPreserveApplied");
            context.Annotations.SetPreserve(myType, Mono.Linker.TypePreserve.Methods);
            // Make sure we can set preserve multiple times
            context.Annotations.SetPreserve(myType, Mono.Linker.TypePreserve.All);
        }
    }
}
