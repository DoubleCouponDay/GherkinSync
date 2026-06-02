using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GherkinSync
{
    [FileExtension(".dll")]
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(
            IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            foreach (var source in sources)
            {
                try
                {
                    foreach (var tc in GetTestCases(source, logger))
                        discoverySink.SendTestCase(tc);
                }
                catch (Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Warning,
                        $"[GherkinSync] Failed to inspect '{source}': {ex.Message}");
                }
            }
        }

        internal static IEnumerable<TestCase> GetTestCases(string source, IMessageLogger logger)
        {
            var results = new List<TestCase>();

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(source);
            }
            catch (Exception ex)
            {
                logger?.SendMessage(TestMessageLevel.Warning,
                    $"[GherkinSync] Could not load assembly '{source}': {ex.Message}");
                return results;
            }

            var syncedTestAttributeType = typeof(SyncedTestAttribute);

            foreach (var type in assembly.GetTypes())
            {
                SyncedTestAttribute attr = null;
                foreach (var a in type.GetCustomAttributes(syncedTestAttributeType, false))
                {
                    attr = a as SyncedTestAttribute;
                    break;
                }

                if (attr == null)
                    continue;

                var featureFileName = attr.FeatureFileName;
                if (!featureFileName.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
                    featureFileName += ".feature";

                var assemblyDir = Path.GetDirectoryName(source);
                var featureFilePath = GherkinParser.FindFeatureFile(assemblyDir, featureFileName);

                if (featureFilePath == null)
                {
                    logger?.SendMessage(TestMessageLevel.Warning,
                        $"[GherkinSync] Could not find feature file '{featureFileName}' for type '{type.FullName}'.");
                    continue;
                }

                string featureContent;
                try
                {
#pragma warning disable RS1035 // File IO is intentional in the test adapter (not Roslyn analyzer) code path
                    featureContent = File.ReadAllText(featureFilePath);
#pragma warning restore RS1035
                }
                catch (Exception ex)
                {
                    logger?.SendMessage(TestMessageLevel.Warning,
                        $"[GherkinSync] Could not read '{featureFilePath}': {ex.Message}");
                    continue;
                }

                var scenarios = GherkinParser.ParseScenarios(featureContent);

                foreach (var scenario in scenarios)
                {
                    var fqn = $"{type.FullName}.{GherkinParser.ScenarioNameToIdentifier(scenario.Name)}";

                    var testCase = new TestCase(fqn, TestExecutor.ExecutorUri, source)
                    {
                        DisplayName = scenario.Name,
                        CodeFilePath = featureFilePath
                    };

                    testCase.SetPropertyValue(TestProperties.FeatureFilePath, featureFilePath);
                    testCase.SetPropertyValue(TestProperties.ScenarioName, scenario.Name);
                    testCase.SetPropertyValue(TestProperties.TypeFullName, type.FullName);

                    results.Add(testCase);
                }
            }

            return results;
        }
    }
}
