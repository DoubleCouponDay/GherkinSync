using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Reflection;

namespace GherkinSync.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://GherkinSyncTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private volatile bool _cancelled;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testCases = new List<TestCase>();
            foreach (var source in sources)
            {
                var discovered = TestDiscoverer.GetTestCases(source, frameworkHandle);
                testCases.AddRange(discovered);
            }
            RunTests(testCases, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _cancelled = false;

            var bySource = tests.GroupBy(t => t.Source);

            foreach (var group in bySource)
            {
                if (_cancelled) break;

                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(group.Key);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error,
                        $"[GherkinSync] Could not load '{group.Key}': {ex.Message}");
                    continue;
                }

                foreach (var testCase in group)
                {
                    if (_cancelled) break;
                    ExecuteTestCase(testCase, assembly, frameworkHandle);
                }
            }
        }

        public void Cancel() => _cancelled = true;

        private static void ExecuteTestCase(TestCase testCase, Assembly assembly, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle.RecordStart(testCase);

            var result = new TestResult(testCase)
            {
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                var typeFullName = testCase.GetPropertyValue(TestProperties.TypeFullName) as string;
                var scenarioName = testCase.GetPropertyValue(TestProperties.ScenarioName) as string;
                var featureFilePath = testCase.GetPropertyValue(TestProperties.FeatureFilePath) as string;

                if (string.IsNullOrEmpty(typeFullName) || string.IsNullOrEmpty(scenarioName) || string.IsNullOrEmpty(featureFilePath))
                    throw new InvalidOperationException("TestCase is missing required GherkinSync properties.");

                var type = assembly.GetType(typeFullName, throwOnError: true);
                var instance = Activator.CreateInstance(type);

                #pragma warning disable RS1035 // File IO is intentional in the test adapter (not Roslyn analyzer) code path
                                var featureContent = File.ReadAllText(featureFilePath);
                #pragma warning restore RS1035
                var scenarios = GherkinParser.ParseScenarios(featureContent);
                var scenario = scenarios.FirstOrDefault(s =>
                    string.Equals(s.Name, scenarioName, StringComparison.OrdinalIgnoreCase));

                if (scenario == null)
                    throw new InvalidOperationException($"Scenario '{scenarioName}' was not found in '{featureFilePath}'.");

                foreach (var step in scenario.Steps)
                {
                    var methodName = GherkinParser.StepToMethodName(step);
                    var method = type.GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                    if (method == null)
                        throw new MissingMethodException(typeFullName, methodName);

                    method.Invoke(instance, null);
                }

                result.Outcome = TestOutcome.Passed;
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                var inner = tie.InnerException;
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = inner.Message;
                result.ErrorStackTrace = inner.StackTrace;
            }
            catch (Exception ex)
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = ex.Message;
                result.ErrorStackTrace = ex.StackTrace;
            }
            finally
            {
                result.EndTime = DateTimeOffset.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            frameworkHandle.RecordResult(result);
            frameworkHandle.RecordEnd(testCase, result.Outcome);
        }
    }
}
