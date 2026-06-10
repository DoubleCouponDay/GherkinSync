Feature: Valid Login

Scenario: 1.1: Valid login
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in
