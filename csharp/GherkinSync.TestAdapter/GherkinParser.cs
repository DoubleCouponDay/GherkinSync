using System.Text;

namespace GherkinSync.TestAdapter
{
    internal class ScenarioInfo
    {
        public string Name { get; set; }
        public List<string> Steps { get; set; } = new List<string>();
    }

    internal static class GherkinParser
    {
        private static readonly string[] StepKeywords = { "Given ", "When ", "Then ", "And ", "But " };

        /// <summary>
        /// Parses a .feature file and returns all scenarios with their ordered steps.
        /// </summary>
        public static List<ScenarioInfo> ParseScenarios(string featureContent)
        {
            var scenarios = new List<ScenarioInfo>();
            ScenarioInfo current = null;

            foreach (var rawLine in featureContent.Split('\n'))
            {
                var line = rawLine.Trim();

                if (line.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Scenario Outline:", StringComparison.OrdinalIgnoreCase))
                {
                    var colonIndex = line.IndexOf(':');
                    var name = colonIndex >= 0 ? line.Substring(colonIndex + 1).Trim() : line;
                    current = new ScenarioInfo { Name = name };
                    scenarios.Add(current);
                    continue;
                }

                if (current != null && IsStepLine(line))
                {
                    current.Steps.Add(line);
                }
            }

            return scenarios;
        }

        /// <summary>
        /// Converts a full step line (e.g. "Given I am on the login page") to a
        /// PascalCase method name (e.g. "GivenIAmOnTheLoginPage").
        /// </summary>
        public static string StepToMethodName(string stepLine)
        {
            var text = stepLine.Trim();
            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            var output = new string(
                stepLine.Trim()
                .Replace(" ", "")
                .ToLowerInvariant()
                .Where(a =>
                    char.IsLetterOrDigit(a) //strip every character from a step line except letters and digits
                ).ToArray()
            );
            foreach (var word in words)
            {
                if (word.Length == 0) continue;
                sb.Append(char.ToLowerInvariant(word[0]));
                if (word.Length > 1)
                    sb.Append(word.Substring(1));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a scenario name to a safe PascalCase identifier.
        /// E.g. "Valid login" -> "ValidLogin"
        /// </summary>
        public static string ScenarioNameToIdentifier(string scenarioName)
        {
            var words = scenarioName.Split(new[] { ' ', '\t', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var word in words)
            {
                if (word.Length == 0) continue;
                sb.Append(char.ToLowerInvariant(word[0]));
                if (word.Length > 1)
                    sb.Append(word.Substring(1));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Searches upward from <paramref name="startDirectory"/> for a file matching
        /// <paramref name="featureFileName"/>, consistent with the README convention of
        /// "one directory up from the project root".
        /// </summary>
        public static string FindFeatureFile(string startDirectory, string featureFileName)
        {
            var dir = new DirectoryInfo(startDirectory);

            while (dir != null)
            {
                foreach (var file in dir.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    if (string.Equals(file.Name, featureFileName, StringComparison.OrdinalIgnoreCase))
                        return file.FullName;
                }
                dir = dir.Parent;
            }

            return null;
        }

        private static bool IsStepLine(string line)
        {
            foreach (var kw in StepKeywords)
            {
                if (line.StartsWith(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
