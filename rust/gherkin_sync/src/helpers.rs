use proc_macro2::Span;
use std::path::{Path, PathBuf};

/// Recursively search `root` for a file whose name matches `target`.
pub fn find_file(root: &Path, target: &str) -> Option<PathBuf> {
    if root.is_dir() {
        for entry in std::fs::read_dir(root).ok()? {
            let entry = entry.ok()?;
            let path = entry.path();
            if path.is_dir() {
                if let Some(found) = find_file(&path, target) {
                    return Some(found);
                }
            } else if path.file_name().and_then(|n| n.to_str()) == Some(target) {
                return Some(path);
            }
        }
    }
    None
}

/// Convert an arbitrary string into a lowercase snake_case `Ident`.
pub fn to_snake_ident(s: &str) -> syn::Ident {
    let snake = s
        .chars()
        .map(|c| if c.is_alphanumeric() { c.to_ascii_lowercase() } else { '_' })
        .collect::<String>()
        .split('_')
        .filter(|seg| !seg.is_empty())
        .collect::<Vec<_>>()
        .join("_");
    syn::Ident::new(&snake, Span::call_site())
}

/// Extract (original_text, normalised_fn_name) for every Gherkin step line.
pub fn parse_gherkin_steps(content: &str) -> Vec<(String, String)> {
    let keywords = ["given", "when", "then", "and", "but"];
    content
        .lines()
        .filter_map(|line| {
            let trimmed = line.trim();
            let lower = trimmed.to_lowercase();
            if keywords.iter().any(|kw| lower.starts_with(kw)) {
                Some((trimmed.to_string(), normalise_step(&lower)))
            } else {
                None
            }
        })
        .collect()
}

pub struct Scenario {
    pub name: String,
    pub steps: Vec<(String, String)>,
}

/// Parse a feature file into a list of scenarios, each with their ordered steps.
pub fn parse_gherkin_scenarios(content: &str) -> Vec<Scenario> {
    let step_keywords = ["given", "when", "then", "and", "but"];
    let mut scenarios: Vec<Scenario> = Vec::new();
    let mut current: Option<Scenario> = None;

    for line in content.lines() {
        let trimmed = line.trim();
        let lower = trimmed.to_lowercase();

        if lower.starts_with("scenario:") {
            if let Some(s) = current.take() {
                scenarios.push(s);
            }
            let name = trimmed["Scenario:".len()..].trim().to_string();
            current = Some(Scenario { name, steps: Vec::new() });
        } else if let Some(ref mut scenario) = current {
            if step_keywords.iter().any(|kw| lower.starts_with(kw)) {
                scenario.steps.push((trimmed.to_string(), normalise_step(&lower)));
            }
        }
    }

    if let Some(s) = current {
        scenarios.push(s);
    }

    scenarios
}

/// Normalise a step line into a valid snake_case function name.
pub fn normalise_step(lower: &str) -> String {
    lower
        .chars()
        .map(|c| if c.is_alphanumeric() { c } else { '_' })
        .collect::<String>()
        .split('_')
        .filter(|seg| !seg.is_empty())
        .collect::<Vec<_>>()
        .join("_")
}
