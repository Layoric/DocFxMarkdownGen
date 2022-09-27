using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;
using ServiceStack;

namespace DocFxMarkdownGen;

public static class ItemExtensions
{
    public static Item[] GetProperties(this Dictionary<string, Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Property").ToArray();

    public static Item[] GetFields(this Dictionary<string, Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Field").ToArray();

    public static Item[] GetMethods(this Dictionary<string, Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Method").ToArray();

    public static Item[] GetEvents(this Dictionary<string, Item> items, string uid)
        => items.Values.Where(i => i.Parent == uid && i.Type == "Event").ToArray();

    public static Item[] GetInheritedMethods(this Dictionary<string, Item> items, string uid)
    {
        if (uid == "System.Object")
            return Array.Empty<Item>();
        var item = items.TryGet(uid);
        if (item == null || item?.InheritedMembers == null || item?.Inheritance == null || item.Inheritance.Length == 0)
            return Array.Empty<Item>();
        if (item.Inheritance.Last() == "System.Object")
            return Array.Empty<Item>();
        var baseClass = items.TryGet(item.Inheritance.Last());
        if (baseClass == null)
            return Array.Empty<Item>();
        var results = items.TryGetAll(baseClass.Children).Where(x => x.Type == "Method").ToArray();
        return results;
    }

    public static Item[] GetInheritedProperties(this Dictionary<string, Item> items, string uid)
    {
        if (uid == "System.Object")
            return Array.Empty<Item>();
        var item = items.TryGet(uid);
        if (item == null || item?.InheritedMembers == null || item?.Inheritance == null || item.Inheritance.Length == 0)
            return Array.Empty<Item>();
        if (item.Inheritance.Last() == "System.Object")
            return Array.Empty<Item>();
        var baseClass = items.TryGet(item.Inheritance.Last());
        if (baseClass == null)
            return Array.Empty<Item>();
        var results = items.TryGetAll(baseClass.Children).Where(x => x.Type == "Property").ToArray();
        return results;
    }

    public static Item? TryGet(this Dictionary<string, Item> items, string uid)
    {
        return items.ContainsKey(uid) ? items[uid] : null;
    }

    public static Item[] TryGetAll(this Dictionary<string, Item> items, string[] uids)
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

public static class MarkdownWritingExtensions
{
    static Regex xrefRegex = new("<xref href=\"(.+?)\" data-throw-if-not-resolved=\"false\"></xref>",
        RegexOptions.Compiled);

    static Regex langwordXrefRegex =
        new("<xref uid=\"langword_csharp_.+?\" name=\"(.+?)\" href=\"\"></xref>", RegexOptions.Compiled);

    static Regex codeRegex = new("<code>(.+?)</code>", RegexOptions.Compiled);
    static Regex linkRegex = new("<a href=\"(.+?)\">(.+?)</a>", RegexOptions.Compiled);

    private static List<string> csharpKeywords = new()
    {
        "class",
        "where"
    };
    
    public static string CSharpEscape(this string str)
    {
        if (!str.Contains("@") && str.ContainsAny(csharpKeywords.ToArray()))
            return $"@{str}";
        return str;
    }
    
    public static string Link(this Dictionary<string, Item> items, string uid, Config? config = null, bool nameOnly = false,
        bool indexLink = false)
    {
        var reference = items.TryGet(uid);
        if (uid.Contains('{') && reference == null)
        {
            // try to resolve single type argument references
            var replaced = uid.Replace(uid[uid.IndexOf('{')..(uid.LastIndexOf('}') + 1)], "`1");
            reference = items.TryGet(replaced);
        }

        if (reference == null)
            // todo: try to resolve to msdn links if System namespace maybe
            return $"``{uid.Replace('{', '<').Replace('}', '>')}``";
        var name = nameOnly ? reference.Name : reference.FullName;
        var dots = indexLink ? "./" : "../";
        var extension = indexLink ? ".md" : "";
        var refNameOutput = name.HtmlEscape();
        if (config?.UseIconify == true)
        {
            refNameOutput = reference.WithIconifyHeading("");
        }
        if (reference.Type is "Class" or "Interface" or "Enum" or "Struct" or "Delegate")
            return $"[{refNameOutput}]({FileEscape($"{dots}{reference.Namespace}/{reference.Name}{extension}")})";
        else if (reference.Type is "Namespace")
            return $"[{refNameOutput}]({FileEscape($"{dots}{reference.Name}/{reference.Name}{extension}")})";
        else
        {
            var parent = items.TryGet(reference.Parent);
            if (parent == null)
                return $"``{uid.Replace('{', '<').Replace('}', '>')}``";
            return
                $"[{HtmlEscape(name)}]({FileEscape($"{dots}{reference.Namespace}/{parent.Name}{extension}")}#{reference.Name.ToLower().Replace("(", "").Replace(")", "")})";
        }
    }
    

    public static string? HtmlEscape(this string? str)
        => str?.Replace("<", "&lt;")?.Replace(">", "&gt;");

    public static string? FileEscape(this string? str)
        => str?.Replace("<", "`")?.Replace(">", "`");

    public static string? WithIconifyHeading(this Item? item, string headingPrefix = "### ")
    {
        var result = headingPrefix;
        if (item == null)
            return result;
        switch (item.Type)
        {
            case "Class":
                result += $"<Icon icon=\"codicon:symbol-class\" className=\"symbol-class\" /> {item.Name.HtmlEscape()}";
                break;
            case "Property":
                result += $"<Icon icon=\"codicon:symbol-property\" className=\"symbol-property\" /> {item.Name.HtmlEscape()}";
                break;
            case "Field":
                result += $"<Icon icon=\"codicon:symbol-field\" className=\"symbol-field\" /> {item.Name.HtmlEscape()}";
                break;
            case "Method":
                result += $"<Icon icon=\"codicon:symbol-method\" className=\"symbol-method\" /> {item.Name.HtmlEscape()}";
                break;
            case "Enum":
                result += $"<Icon icon=\"codicon:symbol-enum\" className=\"symbol-enum\" /> {item.Name.HtmlEscape()}";
                break;
            case "Struct":
                result += $"<Icon icon=\"codicon:symbol-structure\" className=\"symbol-structure\" /> {item.Name.HtmlEscape()}";
                break;
            case "Interface":
                result += $"<Icon icon=\"codicon:symbol-interface\" className=\"symbol-interface\" /> {item.Name.HtmlEscape()}";
                break;
            case "Namespace":
                result += $"<Icon icon=\"codicon:symbol-namespace\" className=\"symbol-namespace\" /> {item.FullName.HtmlEscape()}";
                break;
            default:
                result += $"<Icon icon=\"codicon:symbol-property\" className=\"symbol-property\" /> {item.Name.HtmlEscape()}";
                break;
        }

        return result;
    }
    
    public static string? GetSummary(this Dictionary<string, Item> items, string? summary)
    {
        if (summary == null)
            return null;
        summary = xrefRegex.Replace(summary, match =>
        {
            var uid = match.Groups[1].Value;
            return items.Link(uid);
        });
        summary = langwordXrefRegex.Replace(summary, match => $"`{match.Groups[1].Value}`");
        summary = codeRegex.Replace(summary, match => $"`{match.Groups[1].Value}`");
        summary = linkRegex.Replace(summary, match => $"[{match.Groups[2].Value}]({match.Groups[1].Value})");
        
        return summary.HtmlEscape();
    }


    public static string SourceLink(this Item item)
        =>
            item.Source?.Remote?.Repo != null
                ? $"###### [ <Icon icon=\"codicon:github-inverted\" className=\"github-icon\" /> View Source]({item.Source.Remote.Repo}/blob/{item.Source.Remote.Branch}/{item.Source.Remote.Path}#L{item.Source.StartLine + 1})"
                : "";

    public static void Declaration(StringBuilder str, Item item)
    {
        str.AppendLine(SourceLink(item));
        str.AppendLine("```csharp title=\"Declaration\"");
        str.AppendLine(item.Syntax.Content);
        str.AppendLine("```");
    }

    public static void MethodSummary(this Dictionary<string, Item> items, StringBuilder str, Item method, Config? config = null)
    {
        if (config?.UseIconify == true)
            str.AppendLine(method.WithIconifyHeading());
        else
            str.AppendLine($"### {method.Name.HtmlEscape()}");
        str.AppendLine(items.GetSummary(method.Summary)?.Trim());
        Declaration(str, method);
        if (!string.IsNullOrWhiteSpace(method.Syntax.Return?.Type))
        {
            str.AppendLine();
            str.AppendLine("##### Returns");
            str.AppendLine();
            str.Append(items.Link(method.Syntax.Return.Type)?.Trim());
            if (string.IsNullOrWhiteSpace(method.Syntax.Return?.Description))
                str.AppendLine();
            else
                str.Append(": " + items.GetSummary(method.Syntax.Return.Description));
        }

        if ((method.Syntax?.Parameters?.Length ?? 0) != 0)
        {
            str.AppendLine();
            str.AppendLine("##### Parameters");
            str.AppendLine();
            if (method.Syntax.Parameters.Any(p => !string.IsNullOrWhiteSpace(p.Description)))
            {
                str.AppendLine("| Type | Name | Description |");
                str.AppendLine("|:--- |:--- |:--- |");
                foreach (var parameter in method.Syntax.Parameters)
                    str.AppendLine(
                        $"| {items.Link(parameter.Type)} | *{parameter.Id}* | {items.GetSummary(parameter.Description)} |");
            }
            else
            {
                str.AppendLine("| Type | Name |");
                str.AppendLine("|:--- |:--- |");
                foreach (var parameter in method.Syntax.Parameters)
                    str.AppendLine(
                        $"| {items.Link(parameter.Type)} | *{parameter.Id}* |");
            }

            str.AppendLine();
        }

        if ((method.Syntax.TypeParameters?.Length ?? 0) != 0)
        {
            str.AppendLine("##### Type Parameters");
            if (method.Syntax.TypeParameters.Any(tp => !string.IsNullOrWhiteSpace(tp.Description)))
            {
                str.AppendLine("| Name | Description |");
                str.AppendLine("|:--- |:--- |");
                foreach (var typeParameter in method.Syntax.TypeParameters)
                    str.AppendLine($"| {items.Link(typeParameter.Id)} | {typeParameter.Description} |");
            }
            else
                foreach (var typeParameter in method.Syntax.TypeParameters)
                    str.AppendLine($"* {items.Link(typeParameter.Id)}");
        }
    }
}