namespace TestSuite;

public static class Extensions
{
    public static void AssertSuccess(this Dix dix)
    {
        Boolean isSuccess = true;

        dix.Visit(d =>
        {
            if (d.Operation == DixOperation.Error) isSuccess = false;
        });

        if (!isSuccess)
        {
            Console.WriteLine($"Source returned an error:\n\n{dix.Format()}");

            Assert.Fail("Source returned an error, see output");
        }
    }
}
