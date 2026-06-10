# gherkin_sync

Simple attributes for syncing Gherkin specifications with implementations.

The `synced_feature` attribute prevents Documentation drift by creating a custom compilation error when any mismatch between the Scenario Step lines and the attributed classes method names, is detected. Apply the attribute to your production code for an easy compile-time verification of [The Feature Pattern](https://www.linkedin.com/posts/samuel-jenkins-b645a2156_software-architecture-is-complicated-using-ugcPost-7384494542202269696-7DmV).

The `synced_test` attribute is a simple Unit Test Runner that binds classes to Gherkin specification files. Apply the attribute to your test class to execute the scenarios in the specification file as unit tests.

The attributes will search one directory up from the project root for the specification file.


### Rust Example

`Valid_Login.feature` file:

```gherkin
Feature: Valid Login

Scenario: User Logs In
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in

```

By convention, the implementation method names in snake_case format will match spaced, plain-text step names in the specification file.

`my_feature.rs` file:

```rust

use gherkin_sync::{synced_feature, synced_test};

#[derive(Default)]
struct ValidLogin;

#[allow(dead_code)]
#[synced_feature("Valid_Login.feature")]
#[synced_test("Valid_Login.feature")]
impl ValidLogin {
    fn given_i_am_on_the_login_page(&self) {}

    fn when_i_enter_valid_credentials(&self) {}

    fn then_i_should_be_logged_in(&self) {}
}
```
