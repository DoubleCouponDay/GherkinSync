using System;
using System.Collections.Generic;
using System.Text;

namespace GherkinSync
{
    /// <summary>
    /// Marks a step definition class as synchronized with a Gherkin feature file.
    /// The feature file name is inferred from the class name by default.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SyncedFeatureAttribute : Attribute
    {
        /// <summary>
        /// Creates a SyncedFeatureAttribute with an explicit feature file name.
        /// </summary>
        /// <param name="featureFileName">The feature file name (without .feature extension)</param>
        public SyncedFeatureAttribute(string featureFileName)
        {
            FeatureFileName = featureFileName;
        }

        /// <summary>
        /// Gets the feature file name (without .feature extension).
        /// </summary>
        public string FeatureFileName { get; }
    }
}
