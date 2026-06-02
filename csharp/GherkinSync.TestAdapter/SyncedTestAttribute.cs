using System;
using System.Collections.Generic;
using System.Text;

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
        public SyncedTestAttribute(string featureFileName)
        {
            FeatureFileName = featureFileName;
        }

        /// <summary>
        /// Gets the feature file name (without .feature extension).
        /// </summary>
        public string FeatureFileName { get; }
    }
}
