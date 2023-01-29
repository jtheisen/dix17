// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

D("root",
    D("some-string", "foo"),
    D("some-list",
        D("item1"),
        D("item2")
    )
).Format();