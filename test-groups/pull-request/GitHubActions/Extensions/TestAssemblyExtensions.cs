namespace InitialForce.GitHubActions.TestGroups.PullRequest.Extensions;

public static class TestAssemblyExtensions
{
    public static List<TestCategoryDto> GetTestCategories(this string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new Exception($"could not find assembly at {assemblyPath}");

        var assembly = Assembly.LoadFrom(assemblyPath);

        var categories = assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(TraitAttribute), false).Length > 0)
            .SelectMany(TraitHelper.GetTraits)
            .Where(x => x.Key == "Category")
            .Select(x => x.Value);

        return categories
            .GroupBy(category => category)
            .Select(grouped => new TestCategoryDto() { Category = grouped.Key, Count = grouped.Count() })
            .OrderByDescending(order => order.Count)
            .ToList();
    }

    public static List<TestCategoryDto> FilterByLabels(this List<TestCategoryDto> categories, List<string> issueLabels) => categories.Where(w => issueLabels.Contains(w.Category)).ToList();

    public static List<TestCategoryDto> PartitionByCount(this List<TestCategoryDto> groupCategories, int maximumItems)
    {
        var result = new List<TestCategoryDto>();

        foreach (var category in groupCategories)
        {
            if (category.Count >= maximumItems)
            {
                result.Add(category);
                continue;
            }

            var existentItem = result.FirstOrDefault(f => maximumItems - f.Count >= category.Count);

            if (existentItem == null)
            {
                result.Add(category);
                continue;
            }

            existentItem.Category = $"{existentItem.Category},{category.Category}";
            existentItem.Count += category.Count;
        }

        return result;
    }
}