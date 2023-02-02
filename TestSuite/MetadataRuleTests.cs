namespace TestSuite;

[TestClass]
public class MetadataRuleTests
{
    MetadataRule[] rules;
    MetadataRuleSet provider;

    public MetadataRuleTests()
    {
        rules = new[]
        {
            new MetadataRule(Dmf("x:flag"), Dmf("x:implied-flag")),
            new MetadataRule(Dm("x:type", "foo"), Dmf("x:type-foo"))
        };

        provider = new MetadataRuleSet(rules);
    }

    [TestMethod]
    public void TestFlagMetadata()
    {
        var dix = D("item", Dmf("x:flag")).WithContext(provider);

        Assert.IsTrue(dix.HasMetadataFlag("x:flag"));
        Assert.IsTrue(dix.HasMetadataFlag("x:implied-flag"));
        Assert.IsFalse(dix.HasMetadataFlag("x:type-foo"));
    }

    [TestMethod]
    public void TestValueMetadata()
    {
        var dix = D("item", Dm("x:type", "foo")).WithContext(provider);

        Assert.IsFalse(dix.HasMetadataFlag("x:flag"));
        Assert.IsFalse(dix.HasMetadataFlag("x:implied-flag"));
        Assert.IsTrue(dix.HasMetadataFlag("x:type-foo"));
    }
}
