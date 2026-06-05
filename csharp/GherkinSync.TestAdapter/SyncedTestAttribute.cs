using System.Runtime.CompilerServices;

namespace GherkinSync
{
    /// <summary>
    /// Provides the same functionality as a SyncedFeatureAttribute but will also mark the subject as a test class for unit test discovery and execution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SyncedTestAttribute : Attribute
    {
        /// <summary>
        /// Creates a SyncedFeatureAttribute with an explicit feature file name.
        /// </summary>
        /// <param name="featureFileName">The feature file name (without .feature extension)</param>
        /// <param name="sourceFilePath">Automatically populated by the compiler with the caller's source file path.</param>
        /// <param name="sourceLineNumber">Automatically populated by the compiler with the caller's line number.</param>
        public SyncedTestAttribute(
            string featureFileName,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            FeatureFileName = featureFileName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }

        /// <summary>
        /// Gets the feature file name (without .feature extension).
        /// </summary>
        public string FeatureFileName { get; }

        /// <summary>
        /// Gets the full path to the source file where the attributed class is defined.
        /// Populated automatically at compile time via <see cref="CallerFilePathAttribute"/>.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Gets the line number in the source file where the attribute is applied.
        /// Populated automatically at compile time via <see cref="CallerLineNumberAttribute"/>.
        /// </summary>
        public int SourceLineNumber { get; }
    }
}
