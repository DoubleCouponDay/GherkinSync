use proc_macro::TokenStream;
use std::path::{Path, PathBuf};

#[proc_macro_attribute]
pub fn synced_feature(attr: TokenStream, input: TokenStream) -> TokenStream {
    let spec: syn::LitStr =
        syn::parse(attr).expect("expected a string literal argument, e.g. #[feature(\"my_file\")]");
    let spec_value = spec.value();

    let input_ast: syn::ItemImpl =
        syn::parse(input.clone()).expect("feature attribute must be placed on an impl block");

    let manifest_dir =
        std::env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR not set");

    let feature_path = find_file(&PathBuf::from(&manifest_dir), &spec_value).unwrap_or_else(|| {
        panic!(
            "no file named '{}' found under '{}'",
            spec_value, manifest_dir
        )
    });

    let content = std::fs::read_to_string(&feature_path).unwrap_or_else(|e| {
        panic!(
            "failed to read '{}': {}",
            feature_path.display(),
            e
        )
    });

    let steps = parse_gherkin_steps(&content);

    let fn_names: Vec<String> = input_ast
        .items
        .iter()
        .filter_map(|item| {
            if let syn::ImplItem::Fn(method) = item {
                Some(method.sig.ident.to_string().to_lowercase())
            } else {
                None
            }
        })
        .collect();

    let self_ty = &input_ast.self_ty;
    let type_name = quote::quote!(#self_ty).to_string();

    for (line_text, step_fn) in &steps {
        if !fn_names.contains(step_fn) {
            panic!(
                "no matching function for gherkin step '{}' \
                 (expected fn `{}`) in impl block for `{}`",
                line_text, step_fn, type_name
            );
        }
    }

    input
}

/// Recursively search `root` for a file whose name matches `target`.
fn find_file(root: &Path, target: &str) -> Option<PathBuf> {
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

/// Extract (original_text, normalised_fn_name) for every Gherkin step line.
fn parse_gherkin_steps(content: &str) -> Vec<(String, String)> {
    let keywords = ["given", "when", "then", "and", "but"];

    content
        .lines()
        .filter_map(|line| {
            let trimmed = line.trim();
            let lower = trimmed.to_lowercase();
            if keywords.iter().any(|kw| lower.starts_with(kw)) {
                let normalised = lower
                    .chars()
                    .map(|c| if c.is_alphanumeric() { c } else { '_' })
                    .collect::<String>()
                    .split('_')
                    .filter(|s| !s.is_empty())
                    .collect::<Vec<_>>()
                    .join("_");
                Some((trimmed.to_string(), normalised))
            } else {
                None
            }
        })
        .collect()
}
