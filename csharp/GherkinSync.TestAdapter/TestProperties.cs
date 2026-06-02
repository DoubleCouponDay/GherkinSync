using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GherkinSync
{
    /// <summary>
    /// Well-known <see cref="TestProperty"/> slots used to pass GherkinSync metadata
    /// from the discoverer to the executor without re-parsing.
    /// </summary>
    internal static class TestProperties
    {
        public static readonly TestProperty FeatureFilePath = TestProperty.Register(
            id: "GherkinSync.FeatureFilePath",
            label: "Feature File Path",
            valueType: typeof(string),
            owner: typeof(TestProperties));

        public static readonly TestProperty ScenarioName = TestProperty.Register(
            id: "GherkinSync.ScenarioName",
            label: "Scenario Name",
            valueType: typeof(string),
            owner: typeof(TestProperties));

        public static readonly TestProperty TypeFullName = TestProperty.Register(
            id: "GherkinSync.TypeFullName",
            label: "Type Full Name",
            valueType: typeof(string),
            owner: typeof(TestProperties));
    }
}
