use std::path::PathBuf;
use proc_macro::TokenStream;

use crate::helpers::{find_file, parse_gherkin_steps};

pub fn expand(attr: TokenStream, input: TokenStream) -> TokenStream {
    let spec: syn::LitStr =
        syn::parse(attr).expect("expected a string literal argument, e.g. #[feature(\"my_file\")]");
    let spec_value = spec.value();

    let input_ast: syn::ItemImpl =
        syn::parse(input.clone()).expect("feature attribute must be placed on an impl block");

    let manifest_dir = std::env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR not set");
    let features_folder = PathBuf::from(&manifest_dir)
        .parent()
        .unwrap_or_else(|| panic!("CARGO_MANIFEST_DIR '{}' has no parent directory", manifest_dir))
        .to_path_buf();

    let feature_path = find_file(&features_folder, &spec_value).unwrap_or_else(|| {
        panic!(
            "no file named '{}' found under '{}'",
            spec_value,
            features_folder.display()
        )
    });

    let content = std::fs::read_to_string(&feature_path).unwrap_or_else(|e| {
        panic!("failed to read '{}': {}", feature_path.display(), e)
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
