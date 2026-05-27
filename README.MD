# gherkin_sync

A attributes for syncing Gherkin specifications with implementations.

Documentation drift will result in a compilation error if the `synced_feature` macro detects any mismatch in scenario step names. By Convention, the implementation method names in snake_case format will match spaced, plain-text step names in the specification file. Apply the attribute to your production code for an easy compile-time verification.

The attributes will search one directory up from the project root for the specification file by default.

## Packages

[Rust](https://crates.io/crates/gherkin_sync)
[C#](https://crates.io/crates/gherkin_sync)

## Usage

`Valid_Login.feature` file:

```gherkin
Feature: Valid Login

Scenario: Valid login
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in

```

#### Rust Example

`my_feature.rs` file:

```rust
use gherkin_sync::synced_feature;

struct MyFeature;

#[synced_feature("Valid_Login.feature")]
impl MyFeature {
    fn given_i_am_on_the_login_page(&self) {

    }

    fn when_i_enter_valid_credentials(&self) {

    }

    fn then_i_should_be_logged_in(&self) {

    }
}

```

#### C# Example

You need to place these XML elements in your `.csproj` file:

```xml
<PropertyGroup>
	<RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
	<RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
</PropertyGroup>

<ItemGroup>
  <AdditionalFiles Include="..\**\*.feature" />
</ItemGroup>
```

```csharp
using gherkin_sync;

[SyncedFeature("Valid_Login.feature")]
public class MyFeature
{
    public void GivenTheUserStartsTheApp()
    {
        
    }

    public void WhenTheUserClicksTheButton()
    {
        
    }

    public void ThenTheButtonShouldBeDisabled()
    {
        
    }
}
```
