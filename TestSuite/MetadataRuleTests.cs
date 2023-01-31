namespace TestSuite;

[TestClass]
public class MetadataRuleTests
{
    MetadataRule[] rules;
    RuleMetadataProvider provider;

    public MetadataRuleTests()
    {
        rules = new[]
        {
            new MetadataRule(Dc(D("x:flag")), Dc(D("x:implied-flag"))),
            new MetadataRule(Dc(D("x:type", "foo")), Dc(D("x:type-foo")))
        };

        provider = new RuleMetadataProvider(rules);
    }

    [TestMethod]
    public void TestFlagMetadata()
    {
        var dix = D("item", D("x:flag")).WithContext(provider);

        Assert.IsTrue(dix.HasMetadataFlag("x:flag"));
        Assert.IsTrue(dix.HasMetadataFlag("x:implied-flag"));
        Assert.IsFalse(dix.HasMetadataFlag("x:type-foo"));
    }

    [TestMethod]
    public void TestValueMetadata()
    {
        var dix = D("item", D("x:type", "foo")).WithContext(provider);

        Assert.IsFalse(dix.HasMetadataFlag("x:flag"));
        Assert.IsFalse(dix.HasMetadataFlag("x:implied-flag"));
        Assert.IsTrue(dix.HasMetadataFlag("x:type-foo"));
    }
}
