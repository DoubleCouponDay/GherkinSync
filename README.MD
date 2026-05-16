# gherkin_sync

A simple macro for syncing Gherkin specifications with implementations.

Documentation drift will result in a compilation error if the `synced_feature` macro detects any mismatch in scenario step names. By Convention, the implementation method names in snake_case format will match spaced, plain-text step names in the specification file. Apply the attribute to your production code for an easy compile-time verification.

## Usage

`my_feature.feature` file:

```gherkin
Feature: My Feature
  Scenario: My Scenario
    Given the user starts the app
    When the user clicks the button
    Then the button should be disabled
```

`my_feature.rs` file:

```rust
use gherkin_sync::sync_gherkin;

struct MyFeature;

#[synced_feature("my_feature.feature")]
impl MyFeature {
    fn given_the_user_starts_the_app(&self) {
        
    }

    fn when_the_user_clicks_the_button(&self) {
        
    }

    fn then_the_button_should_be_disabled(&self) {
        
    }
}
```
