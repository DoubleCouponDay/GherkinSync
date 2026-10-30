Feature: Valid Login

Scenario: Valid login
	Given I am on the login page
	When I enter valid credentials
	Then I should be logged in

Scenario: 2.1: User's login — retries; again
	Given I can't log in — yet
	When I retry; with the "right" password
	Then I'm logged in!
