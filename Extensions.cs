namespace DocFxMarkdownGen;

public static class Extensions
{
    public static Item[] GetProperties(this Dictionary<string,Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Property").ToArray();

    public static Item[] GetFields(this Dictionary<string,Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Field").ToArray();

    public static Item[] GetMethods(this Dictionary<string,Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Method").ToArray();

    public static Item[] GetEvents(this Dictionary<string,Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Event").ToArray();
    
    public static Item[] GetInheritedMethods(this Dictionary<string,Item> items,string uid)
    {
        if(uid == "System.Object")
            return Array.Empty<Item>();
        var item = items.TryGet(uid);
        if (item == null || item?.InheritedMembers == null || item?.Inheritance == null || item.Inheritance.Length == 0)
            return Array.Empty<Item>();
        if(item.Inheritance.Last() == "System.Object")
            return Array.Empty<Item>();
        var baseClass = items.TryGet(item.Inheritance.Last());
        if (baseClass == null)
            return Array.Empty<Item>();
        var results = items.TryGetAll(baseClass.Children).Where(x => x.Type == "Method").ToArray();
        return results;
    }

    public static Item[] GetInheritedProperties(this Dictionary<string,Item> items,string uid)
    {
        if(uid == "System.Object")
            return Array.Empty<Item>();
        var item = items.TryGet(uid);
        if (item == null || item?.InheritedMembers == null || item?.Inheritance == null || item.Inheritance.Length == 0)
            return Array.Empty<Item>();
        if(item.Inheritance.Last() == "System.Object")
            return Array.Empty<Item>();
        var baseClass = items.TryGet(item.Inheritance.Last());
        if (baseClass == null)
            return Array.Empty<Item>();
        var results = items.TryGetAll(baseClass.Children).Where(x => x.Type == "Property").ToArray();
        return results;
    }

    public static Item? TryGet(this Dictionary<string,Item> items,string uid)
    {
        return items.ContainsKey(uid) ? items[uid] : null;
    }

    public static Item[] TryGetAll(this Dictionary<string,Item> items,string[] uids)
    {
        var result = new List<Item>();
        foreach (var uid in uids)
        {
            var item = items.TryGet(uid);
            if (item != null)
                result.Add(item);
        }

        return result.ToArray();
    }

}