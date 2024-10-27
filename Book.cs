class Book
{
    public static Book Build()
    {
        var guid = Guid.NewGuid().ToString();
        return new Book()
        {
            id = guid,
            pk = guid,
            foo0 = guid,
            foo1 = guid,
            foo2 = guid,
            foo3 = guid,
            foo4 = guid,
            foo5 = guid,
            foo6 = guid,
            foo7 = guid,
            foo8 = guid,
            foo9 = guid,
        };
    }

    public string id { get; set; } = string.Empty;
    public string pk { get; set; } = string.Empty;
    public string foo0 { get; set; } = string.Empty;
    public string foo1 { get; set; } = string.Empty;
    public string foo2 { get; set; } = string.Empty;
    public string foo3 { get; set; } = string.Empty;
    public string foo4 { get; set; } = string.Empty;
    public string foo5 { get; set; } = string.Empty;
    public string foo6 { get; set; } = string.Empty;
    public string foo7 { get; set; } = string.Empty;
    public string foo8 { get; set; } = string.Empty;
    public string foo9 { get; set; } = string.Empty;
}