namespace InitialForce.GitHubActions.TestGroups.Branch.Extensions;

public static class TestAssemblyExtensions
{
    public static List<TestClassDto> GetTestClasses(this string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new Exception($"could not find assembly at {assemblyPath}");

        var assembly = Assembly.LoadFrom(assemblyPath);

        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(TraitAttribute), false).Length > 0);

        return methods
            .GroupBy(method => method.DeclaringType!.FullName!)
            .Select(grouped => new TestClassDto { Class = grouped.Key, Count = grouped.Count() })
            .OrderByDescending(order => order.Count)
            .ToList();
    }

    public static List<TestClassDto> PartitionByCount(this List<TestClassDto> groupCategories, int maximumItems)
    {
        var result = new List<TestClassDto>();

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

            existentItem.Class = $"{existentItem.Class},{category.Class}";
            existentItem.Count += category.Count;
        }

        return result;
    }
}