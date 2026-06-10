use anyhow::anyhow;
use proc_macro2::{Span, TokenStream};
use std::{panic::{panic_any}, path::PathBuf};

use crate::helpers::{find_file, parse_gherkin_scenarios, to_indent};

/// Generates a `#[test]` function per Gherkin scenario found in the corresponding
/// feature file. Each test instantiates the struct via `Default::default()` and
/// calls the step methods in order at runtime.
///
/// Unlike `synced_feature`, this attribute does NOT produce compile-time errors
/// for missing steps — mismatches surface as normal test failures at runtime.
pub fn expand(attr: TokenStream, input: TokenStream) -> TokenStream {
    let input_ast: syn::ItemImpl =
        syn::parse2(input.clone()).expect("synced_test must be placed on an impl block");

    let self_ty = &input_ast.self_ty;

    let manifest_dir = std::env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR not set");
    let features_folder = PathBuf::from(&manifest_dir)
        .parent()
        .unwrap_or_else(|| panic_any(anyhow!("CARGO_MANIFEST_DIR '{}' has no parent directory", manifest_dir)))
        .to_path_buf();

    let feature_filename = if attr.is_empty() {
        let raw = quote::quote!(#self_ty).to_string().replace(' ', "");
        format!("{raw}.feature")
    } else {
        let spec: syn::LitStr = syn::parse2(attr).expect(
            "synced_test argument must be a string literal, e.g. #[synced_test(\"my_file.feature\")]",
        );
        spec.value()
    };

    let feature_path = find_file(&features_folder, &feature_filename).unwrap_or_else(|| {
        panic_any(anyhow!(
            "no file named '{}' found under '{}'",
            feature_filename,
            features_folder.display()
        ))
    });

    let content = std::fs::read_to_string(&feature_path)
        .unwrap_or_else(|e| panic_any(anyhow!("failed to read '{}': {}", feature_path.display(), e)));

    let scenarios = parse_gherkin_scenarios(&content);

    let test_fns: Vec<TokenStream> = scenarios.iter()
        .map(|scenario| {
            let test_fn_name = to_indent(&scenario.name);
            let step_calls: Vec<TokenStream> = scenario
                .steps
                .iter()
                .map(|(step_text, fn_name)| {
                    let fn_ident = syn::Ident::new(fn_name, Span::call_site());
                    quote::quote! {
                        println!("  {}", #step_text);
                        fixture.#fn_ident();
                    }
                })
                .collect();

            quote::quote! {
                #[test]
                fn #test_fn_name() {
                    let fixture: #self_ty = Default::default();
                    #(#step_calls)*
                }
            }
        })
        .collect();

    let mod_name = to_indent(
        &feature_filename
            .strip_suffix(".feature")
            .unwrap_or(&feature_filename),
    );

    quote::quote! {
        #input

        #[cfg(test)]
        mod #mod_name {
            use super::*;

            #(#test_fns)*
        }
    }
}
