
namespace TestSuite;

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public void TestBasicSucceeding()
    {
        DixValidator.AssertEqual(
            D("root",
                D("item1"),
                D("item2")
            ),
            D("root",
                D("item2"),
                D("item1")
            )            
        );
    }

    [TestMethod]
    public void TestBasicFailingValidation()
    {
        Assert.ThrowsException<DixValidationException>(() =>
        {
            DixValidator.AssertEqual(
                D("root",
                    D("item1"),
                    D("item2")
                ),
                D("root",
                    D("item1b"),
                    D("item2")
                )
            );
        });
    }

    [TestMethod]
    public void TestUnstructuredSucceeding()
    {
        DixValidator.AssertEqual(
            D("root",
                D("item1", "foo"),
                D("item2")
            ),
            D("root",
                D("item2"),
                D("item1", "foo")
            )
        );
    }

    [TestMethod]
    public void TestUnstructuredFailing()
    {
        Assert.ThrowsException<DixValidationException>(() =>
        {
            DixValidator.AssertEqual(
                D("root",
                    D("item1"),
                    D("item2")
                ),
                D("root",
                    D("item1"),
                    D("item2", "foo")
                )
            );
        });
    }

    [TestMethod]
    public void TestIgnoringUnstructuredFailing()
    {
        DixValidator.AssertEqual(
            D("root",
                D("item1"),
                D("item2")
            ),
            D("root", "foo",
                D("item1"),
                D("item2")
            ),
            DixValidatorFlags.IgnoreExtraUnstructuredIfNullInExpected
        );
    }

    [TestMethod]
    public void TestNotIgnoringUnstructuredForLeafs()
    {
        Assert.ThrowsException<DixValidationException>(() =>
        {
            DixValidator.AssertEqual(
                D("root",
                    D("item1"),
                    D("item2")
                ),
                D("root",
                    D("item1", "foo"),
                    D("item2")
                ),
                DixValidatorFlags.IgnoreExtraUnstructuredIfNullInExpected
            );
        });
    }

    [TestMethod]
    public void TestIgnoreExtraMetadataOnActual()
    {
        DixValidator.AssertEqual(
            D("root",
                D("item1"),
                D("item2")
            ),
            D("root",
                D("item1"),
                D("x:mystery"),
                D("item2")
            ),
            DixValidatorFlags.IgnoreExtraMetadataOnActual
        );
    }

}
