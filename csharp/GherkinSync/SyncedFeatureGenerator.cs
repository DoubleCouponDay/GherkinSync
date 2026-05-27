using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace GherkinSync
{
    [Generator]
    public class FeatureSyncValidationGenerator : IIncrementalGenerator
    {
        public const string DiagnosticId = "FEATURE_SYNC";

        private static readonly DiagnosticDescriptor Logging = new DiagnosticDescriptor(
            id: "FEATURE_SYNC_LOGGING",
            title: string.Empty,
            messageFormat: "{0}",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingFile = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Missing Method for Gherkin Step",
            messageFormat: "Could not find feature file '{0}'",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor UnreadableFile = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Missing Method for Gherkin Step",
            messageFormat: "Feature file '{0}' is unreadable",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingSpecLine = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Missing Method for Gherkin Step",
            messageFormat: "Feature File '{0}' has no corresponding scenario line '{1}'",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingMethod = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Missing Method for Gherkin Step",
            messageFormat: "Feature File Gherkin step '{0}' has no corresponding method in class '{1}'",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // NO AddSource call for the attribute - it's in a referenced assembly!

            var additionalFiles = context.AdditionalTextsProvider
                .Where(static file => {
                    var output = file.Path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase);
                    return output;
                })
                .Collect();

            var attributeName = typeof(SyncedFeatureAttribute).FullName;

            // Reference the actual SyncedFeatureAttribute from the compiled assembly
            var attributeProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: attributeName,  // ← From referenced assembly
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) =>
                    {
                        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
                        var attributeData = ctx.Attributes.First();

                        var featureFileName = string.Empty;

                        if (attributeData.ConstructorArguments.Length > 0)
                        {
                            var argValue = attributeData.ConstructorArguments[0].Value;
                            if (argValue is string fileName)
                            {
                                featureFileName = fileName;
                            }
                        }

                        return new ClassInfo
                        {
                            ClassSymbol = classSymbol,
                            ClassSyntax = (ClassDeclarationSyntax)ctx.TargetNode,
                            FeatureFileName = featureFileName ?? string.Empty
                        };
                    });

            IncrementalValuesProvider<((ClassInfo Left, ImmutableArray<AdditionalText> Right) Left, Compilation Right)> combined = attributeProvider
                .Combine(additionalFiles)
                .Combine(context.CompilationProvider);

            context.RegisterSourceOutput(combined, static (SourceProductionContext context, ((ClassInfo Left, ImmutableArray<AdditionalText> Right) Left, Compilation Right) pair) =>
            {
                var (classInfoAndFiles, compilation) = pair;
                var classInfo = classInfoAndFiles.Left;
                var additionalFiles = classInfoAndFiles.Right;

                if (string.IsNullOrEmpty(classInfo.FeatureFileName)) //required argument to attribute
                {
                    return;
                }

                var featureFileNameWithExtension = classInfo.FeatureFileName.EndsWith(".feature", StringComparison.OrdinalIgnoreCase)
                    ? classInfo.FeatureFileName
                    : $"{classInfo.FeatureFileName}.feature";

                var matchingAdditionalFile = additionalFiles.FirstOrDefault(file =>
                {
                    var fileName = Path.GetFileName(file.Path);
                    return fileName.Equals(featureFileNameWithExtension, StringComparison.OrdinalIgnoreCase);
                });

                if (matchingAdditionalFile == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingFile,
                        classInfo.ClassSyntax.GetLocation(),
                        featureFileNameWithExtension));
                    return;
                }

                var featureFileText = matchingAdditionalFile.GetText(context.CancellationToken);
                if (featureFileText == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnreadableFile,
                        classInfo.ClassSyntax.GetLocation(),
                        featureFileNameWithExtension));
                    return;
                }

                var gherkinSteps = ParseGherkinSteps(context, featureFileText.ToString());

                if (gherkinSteps.Count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingSpecLine,
                        classInfo.ClassSyntax.GetLocation(),
                        featureFileNameWithExtension,
                        classInfo.ClassSymbol.Name));
                    return;
                }

                var methodNames = GetMethodNamesFromClass(classInfo.ClassSymbol, compilation);
                var missingSteps = gherkinSteps.Where(step => !IsStepCovered(context, step, methodNames)).ToList();

                foreach (var missingStep in missingSteps)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingMethod,
                        classInfo.ClassSyntax.GetLocation(),
                        featureFileNameWithExtension,
                        missingStep));
                }
            });
        }

        private static ImmutableHashSet<string> GetMethodNamesFromClass(INamedTypeSymbol classSymbol, Compilation compilation)
        {
            var methodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    methodNames.Add(method.Name);
                    var stepPattern = ExtractStepPattern(method, compilation);
                    if (!string.IsNullOrEmpty(stepPattern))
                    {
                        var stepText = ExtractStepTextFromPattern(stepPattern);
                        if (!string.IsNullOrEmpty(stepText)) methodNames.Add(stepText);
                    }
                }
            }
            return methodNames.ToImmutableHashSet();
        }

        private static ImmutableHashSet<string> GetStepDefinitionMethodsFromParents(INamedTypeSymbol classSymbol, Compilation compilation)
        {
            var methodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var baseType = classSymbol.BaseType;
            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in baseType.GetMembers())
                {
                    if (member is IMethodSymbol method)
                    {
                        var stepPattern = ExtractStepPattern(method, compilation);
                        if (!string.IsNullOrEmpty(stepPattern))
                        {
                            var stepText = ExtractStepTextFromPattern(stepPattern);
                            if (!string.IsNullOrEmpty(stepText)) methodNames.Add(stepText);
                        }
                    }
                }
                baseType = baseType.BaseType;
            }
            return methodNames.ToImmutableHashSet();
        }

        private static string ExtractStepPattern(IMethodSymbol method, Compilation compilation)
        {
            var stepAttributes = new[] { "TechTalk.SpecFlow.StepAttribute", "TechTalk.SpecFlow.GivenAttribute", "TechTalk.SpecFlow.WhenAttribute", "TechTalk.SpecFlow.ThenAttribute" };
            foreach (var attr in method.GetAttributes())
            {
                var attrName = attr.AttributeClass?.ToDisplayString();
                if (attrName != null && stepAttributes.Any(a => attrName.Contains(a)) && attr.ConstructorArguments.Length > 0)
                    return attr.ConstructorArguments[0].Value?.ToString();
            }
            return null;
        }

        private static string ExtractStepTextFromPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return null;

            var cleaned = pattern.Replace("^", "").Replace("$", "").Replace("(", "").Replace(")", "").Replace("[^", "").Replace("]", "").Replace("*", "").Replace("+", "").Replace("?", "").Replace("{int}", "").Replace("{string}", "").Replace("{word}", "").Replace("<int>", "").Replace("<string>", "").Replace("\\", "");
            while (cleaned.Contains("  ")) cleaned = cleaned.Replace("  ", " ");
            return cleaned.Trim();
        }

        private static bool IsStepCovered(SourceProductionContext context, string step, ImmutableHashSet<string> methodNames)
        {
            var normalizedStep = step.Trim().ToLowerInvariant();

            //context.ReportDiagnostic(Diagnostic.Create(
            //    Logging,
            //    Location.None,
            //    $"Checking step '{step}' against method names: {string.Join(", ", methodNames)}"));

            var spaced = methodNames.Select(a => PascalCaseToSpaced(a).ToLowerInvariant());

            //context.ReportDiagnostic(Diagnostic.Create(
            //    Logging,
            //    Location.None,
            //    $"left: {normalizedStep}, right: {string.Join(", ", spaced)}"));

            var outcome1 = spaced.Any(a => String.Compare(a, normalizedStep, StringComparison.OrdinalIgnoreCase) == 0);

            if (outcome1) return true;

            var squished = methodNames.Select(a => a.Replace(" ", String.Empty).ToLowerInvariant());
            var outcome2 = squished.Any(a => String.Compare(a, normalizedStep, StringComparison.OrdinalIgnoreCase) == 0);

            //context.ReportDiagnostic(Diagnostic.Create(
            //    Logging,
            //    Location.None,
            //    $"outcome1: {outcome1}, outcome2: {outcome2}"));
            return outcome2;
        }

        private static string PascalCaseToSpaced(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && char.IsUpper(input[i]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }

            return result.ToString();
        }

        private static List<string> ParseGherkinSteps(SourceProductionContext context, string featureFileContent)
        {
            var steps = new List<string>();
            var inScenario = false;
            foreach (var line in featureFileContent.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Scenario Outline:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Background:", StringComparison.OrdinalIgnoreCase))
                {
                    inScenario = true; continue;
                }
                if (inScenario && IsStepKeyword(trimmed))
                {
                    var stepText = ExtractStepText(trimmed);
                    if (!string.IsNullOrEmpty(stepText)) steps.Add(stepText);
                }
            }
            return steps;
        }

        private static bool IsStepKeyword(string line) => new[] { "Given ", "When ", "Then ", "And ", "But " }.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        private static string? ExtractStepText(string line) {
            var prefixes = new[] { "Given ", "When ", "Then ", "And ", "But " };
            var match = prefixes.FirstOrDefault(p => line.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                return line.Trim();
            }
            return null;
        }

        private class ClassInfo
        {
            public INamedTypeSymbol ClassSymbol { get; init; }
            public ClassDeclarationSyntax ClassSyntax { get; init; }
            public string FeatureFileName { get; init; }
        }
    }
}
