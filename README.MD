# GherkinSync

[![CI](https://github.com/DoubleCouponDay/gherkin_sync/actions/workflows/ci.yml/badge.svg)](https://github.com/DoubleCouponDay/gherkin_sync/actions/workflows/ci.yml)

Simple attributes for syncing Gherkin specifications with implementations.

The `GherkinSync.Analyzer` package prevents Documentation drift by creating a custom compilation error when the `SyncedFeature` attribute detects any mismatch between the Scenario Step lines and the attributed classes method names. Apply the attribute to your production code for an easy compile-time verification of [The Feature Pattern](https://www.linkedin.com/posts/samuel-jenkins-b645a2156_software-architecture-is-complicated-using-ugcPost-7384494542202269696-7DmV).

The `GherkinSync.TestAdapter` package is a simple Unit Test Runner that binds classes to Gherkin specification files using the `SyncedTest` attribute. Apply the attribute to your test class to execute the scenarios in the specification file as unit tests.

The attributes will search one directory up from the project root for the specification file.


## C# Example

- Install the `Microsoft.NET.Test.Sdk` package.

`Valid Login.feature` file:

```gherkin
Feature: Valid Login

Scenario: Valid login
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in

```

- You need to place these XML elements in your unit test project's .csproj file:

```xml
<PropertyGroup>
	<RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
	<RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
    <TestAdapterPath>$(OutputPath)</TestAdapterPath>
</PropertyGroup>

<ItemGroup>
  <AdditionalFiles Include="..\**\*.feature" />
</ItemGroup>
```

By convention, the implementation method names in Pascal Case format will match spaced, plain-text step names in the specification file.

```csharp
using GherkinSync;

[SyncedFeature("Valid Login.feature")]
[SyncedTest("Valid Login.feature")]
public class MyFeature
{
    public void GivenTheUserStartsTheApp()
    {
        Console.WriteLine("This occurs in the unit test first.");
    }

    public void WhenTheUserClicksTheButton()
    {
        Console.WriteLine("This occurs in the unit test second.");
        
    }

    public void ThenTheButtonShouldBeDisabled()
    {
        Console.WriteLine("Finally, this occurs in the unit test third.");
    }
}
```


## Rust Example

The `synced_feature` attribute prevents Documentation drift by creating a custom compilation error when any mismatch between the Scenario Step lines and the attributed classes method names, is detected.

`Valid Login.feature` file:

```gherkin
Feature: Valid Login

Scenario: Valid login
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in

```

By convention, the implementation method names in snake_case format will match spaced, plain-text step names in the specification file.

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
