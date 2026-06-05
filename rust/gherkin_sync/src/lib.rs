mod helpers;
mod synced_feature_impl;
mod synced_test_impl;

use proc_macro::TokenStream;

#[proc_macro_attribute]
pub fn synced_feature(attr: TokenStream, input: TokenStream) -> TokenStream {
    synced_feature_impl::expand(attr.into(), input.into()).into()
}

#[proc_macro_attribute]
pub fn synced_test(attr: TokenStream, input: TokenStream) -> TokenStream {
    synced_test_impl::expand(attr.into(), input.into()).into()
}